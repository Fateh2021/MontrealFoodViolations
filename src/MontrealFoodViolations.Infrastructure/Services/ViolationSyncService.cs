using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MontrealFoodViolations.Application.Interfaces;
using MontrealFoodViolations.Application.Models;
using MontrealFoodViolations.Application.Options;
using MontrealFoodViolations.Domain.Entities;
using MontrealFoodViolations.Infrastructure.Data;
using MontrealFoodViolations.Infrastructure.Models;

namespace MontrealFoodViolations.Infrastructure.Services;

public sealed class ViolationSyncService : IViolationSyncService
{
    private readonly MontrealFoodViolationsDbContext _dbContext;
    private readonly IMontrealDatasetClient _datasetClient;
    private readonly ILogger<ViolationSyncService> _logger;
    private readonly IDatasetSyncCoordinator _syncCoordinator;
    private readonly ViolationSyncOptions _options;

    public ViolationSyncService(
        MontrealFoodViolationsDbContext dbContext,
        IMontrealDatasetClient datasetClient,
        IDatasetSyncCoordinator syncCoordinator,
        IOptions<ViolationSyncOptions> options,
        ILogger<ViolationSyncService> logger)
    {
        _dbContext = dbContext;
        _datasetClient = datasetClient;
        _logger = logger;
        _syncCoordinator = syncCoordinator;
        _options = options.Value;
    }

    public async Task<ViolationSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        await _syncCoordinator.SyncLock.WaitAsync(cancellationToken);

        var result = new ViolationSyncResult
        {
            StartedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var downloaded = await _datasetClient.DownloadAsync(cancellationToken);
            result.DownloadSucceeded = true;
            result.DatasetHash = downloaded.Hash;

            var violations = ViolationCsvParser.Parse(downloaded.Content);
            result.TotalRows = violations.Count;

            var existing = await _dbContext.Violations
                .AsNoTracking()
                .Select(v => new ExistingViolationSnapshot
                {
                    IdPoursuite = v.IdPoursuite,
                    Description = v.Description,
                    BusinessId = v.BusinessId,
                    Date = v.Date,
                    Adresse = v.Adresse,
                    DateJugement = v.DateJugement,
                    Etablissement = v.Etablissement,
                    Montant = v.Montant,
                    Proprietaire = v.Proprietaire,
                    Ville = v.Ville,
                    Statut = v.Statut,
                    DateStatut = v.DateStatut,
                    Categorie = v.Categorie
                })
                .ToListAsync(cancellationToken);

            var existingByKey = existing.ToDictionary(v => v.IdPoursuite);
            var inserts = new List<Violation>();
            var updates = new List<Violation>();
            var unchanged = 0;
            var skipped = 0;

            foreach (var violation in violations)
            {
                if (!existingByKey.TryGetValue(violation.IdPoursuite, out var existingRow))
                {
                    inserts.Add(violation);
                    continue;
                }

                var isSame = ViolationSnapshotComparer.AreEquivalent(existingRow, violation);
                if (isSame)
                {
                    unchanged++;
                    continue;
                }

                var update = await _dbContext.Violations.FindAsync(new object[] { violation.IdPoursuite }, cancellationToken);
                if (update is null)
                {
                    skipped++;
                    continue;
                }

                update.BusinessId = violation.BusinessId;
                update.Date = violation.Date;
                update.Description = violation.Description;
                update.Adresse = violation.Adresse;
                update.DateJugement = violation.DateJugement;
                update.Etablissement = violation.Etablissement;
                update.Montant = violation.Montant;
                update.Proprietaire = violation.Proprietaire;
                update.Ville = violation.Ville;
                update.Statut = violation.Statut;
                update.DateStatut = violation.DateStatut;
                update.Categorie = violation.Categorie;
                update.UpdatedAt = DateTimeOffset.UtcNow;
                updates.Add(update);
            }

            if (inserts.Count > 0)
            {
                await _dbContext.Violations.AddRangeAsync(inserts, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.InsertedRows = inserts.Count;
            result.UpdatedRows = updates.Count;
            result.UnchangedRows = unchanged;
            result.SkippedRows = skipped;
            result.CompletedAt = DateTimeOffset.UtcNow;

            var syncState = await _dbContext.DatasetSyncStates.FirstOrDefaultAsync(x => x.DatasetName == "MontrealViolations", cancellationToken)
                ?? new DatasetSyncState { DatasetName = "MontrealViolations" };

            syncState.LastSyncStartedAt = result.StartedAt;
            syncState.LastSyncCompletedAt = result.CompletedAt;
            syncState.LastHash = downloaded.Hash;
            syncState.LastETag = downloaded.ETag;
            syncState.LastModified = downloaded.LastModified;
            syncState.LastRowCount = result.TotalRows;
            syncState.LastInsertedRows = result.InsertedRows;
            syncState.LastUpdatedRows = result.UpdatedRows;
            syncState.LastUnchangedRows = result.UnchangedRows;
            syncState.LastSkippedRows = result.SkippedRows;
            syncState.Status = "Success";
            syncState.LastSuccessAt = result.CompletedAt;
            syncState.LastError = null;

            if (syncState.Id == 0)
            {
                _dbContext.DatasetSyncStates.Add(syncState);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Synchronization completed. Inserted {Inserted}, Updated {Updated}, Unchanged {Unchanged}, Skipped {Skipped}, TotalRows {TotalRows} in {Duration}",
                result.InsertedRows, result.UpdatedRows, result.UnchangedRows, result.SkippedRows, result.TotalRows, result.CompletedAt - result.StartedAt);

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorCount = 1;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTimeOffset.UtcNow;
            var syncState = await _dbContext.DatasetSyncStates.FirstOrDefaultAsync(x => x.DatasetName == "MontrealViolations", cancellationToken)
                ?? new DatasetSyncState { DatasetName = "MontrealViolations" };
            syncState.Status = "Failed";
            syncState.LastError = ex.Message;
            syncState.LastSyncCompletedAt = result.CompletedAt;
            if (syncState.Id == 0)
            {
                _dbContext.DatasetSyncStates.Add(syncState);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Synchronization failed.");
            return result;
        }
        finally
        {
            _syncCoordinator.SyncLock.Release();
        }
    }

    public async Task<SyncStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var state = await _dbContext.DatasetSyncStates.FirstOrDefaultAsync(x => x.DatasetName == "MontrealViolations", cancellationToken);
        return new SyncStatusSnapshot
        {
            LastSyncCompletedAt = state?.LastSyncCompletedAt,
            Status = state?.Status ?? "Idle",
            ProcessedRows = state?.LastRowCount ?? 0,
            InsertedRows = state?.LastInsertedRows ?? 0,
            UpdatedRows = state?.LastUpdatedRows ?? 0,
            LastError = state?.LastError,
            NextScheduledRun = DateTimeOffset.UtcNow.AddHours(_options.IntervalHours)
        };
    }

}
