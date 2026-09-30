using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RegistreAlimentaire.Application.Interfaces;
using RegistreAlimentaire.Application.Models;
using RegistreAlimentaire.Application.Options;
using RegistreAlimentaire.Domain.Entities;
using RegistreAlimentaire.Infrastructure.Data;
using RegistreAlimentaire.Infrastructure.Models;

namespace RegistreAlimentaire.Infrastructure.Services;

public sealed class ViolationSyncService : IViolationSyncService
{
    private readonly RegistreAlimentaireDbContext _dbContext;
    private readonly IMontrealDatasetClient _datasetClient;
    private readonly ILogger<ViolationSyncService> _logger;
    private readonly IDatasetSyncCoordinator _syncCoordinator;
    private readonly ViolationSyncOptions _options;

    public ViolationSyncService(
        RegistreAlimentaireDbContext dbContext,
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
            var counts = await UpsertAsync(violations, cancellationToken);
            result.TotalRows = violations.Count;
            result.InsertedRows = counts.Inserted;
            result.UpdatedRows = counts.Updated;
            result.UnchangedRows = counts.Unchanged;
            result.SkippedRows = counts.Skipped;

            try
            {
                var mapaq = await SyncMapaqAsync(cancellationToken);
                result.TotalRows += mapaq.Total;
                result.InsertedRows += mapaq.Inserted;
                result.UpdatedRows += mapaq.Updated;
                result.UnchangedRows += mapaq.Unchanged;
                result.SkippedRows += mapaq.Skipped;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "MAPAQ convictions sync failed. Montreal data remains available.");
            }

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

    private async Task<(int Total, int Inserted, int Updated, int Unchanged, int Skipped)> SyncMapaqAsync(CancellationToken cancellationToken)
    {
        var downloaded = await _datasetClient.DownloadConvictionsAsync(cancellationToken);
        var violations = MapaqConvictionParser.Parse(downloaded.Content);
        var counts = await UpsertAsync(violations, cancellationToken);

        var incoming = violations.Select(violation => violation.IdPoursuite).ToHashSet();
        var existingIds = await _dbContext.Violations
            .AsNoTracking()
            .Where(violation => violation.Source == MapaqConvictionParser.SourceName)
            .Select(violation => violation.IdPoursuite)
            .ToListAsync(cancellationToken);
        var staleIds = existingIds.Where(id => !incoming.Contains(id)).ToList();
        foreach (var chunk in staleIds.Chunk(500))
        {
            var chunkIds = chunk.ToArray();
            await _dbContext.Violations
                .Where(violation => chunkIds.Contains(violation.IdPoursuite))
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation(
            "MAPAQ sync completed. Inserted {Inserted}, Updated {Updated}, Unchanged {Unchanged}, Removed {Removed}, TotalRows {TotalRows}",
            counts.Inserted, counts.Updated, counts.Unchanged, staleIds.Count, violations.Count);

        return (violations.Count, counts.Inserted, counts.Updated, counts.Unchanged, counts.Skipped);
    }

    private async Task<(int Inserted, int Updated, int Unchanged, int Skipped)> UpsertAsync(
        IReadOnlyList<Violation> violations,
        CancellationToken cancellationToken)
    {
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
        var updates = 0;
        var unchanged = 0;
        var skipped = 0;

        foreach (var violation in violations)
        {
            if (!existingByKey.TryGetValue(violation.IdPoursuite, out var existingRow))
            {
                inserts.Add(violation);
                continue;
            }

            if (ViolationSnapshotComparer.AreEquivalent(existingRow, violation))
            {
                unchanged++;
                continue;
            }

            var update = await _dbContext.Violations.FindAsync([violation.IdPoursuite], cancellationToken);
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
            update.Source = violation.Source;
            update.UpdatedAt = DateTimeOffset.UtcNow;
            updates++;
        }

        if (inserts.Count > 0)
        {
            await _dbContext.Violations.AddRangeAsync(inserts, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (inserts.Count, updates, unchanged, skipped);
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
