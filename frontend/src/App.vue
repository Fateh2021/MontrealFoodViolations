<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import BusinessModal from './components/BusinessModal.vue';
import SearchFilters from './components/SearchFilters.vue';
import StatsSection from './components/StatsSection.vue';
import ViolationsTable from './components/ViolationsTable.vue';
import { formatCurrency, formatDate, formatNumber } from './formatters';

const SOURCE_URL = 'https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0';
const MAPAQ_URL = 'https://www.donneesquebec.ca/recherche/dataset/condamnations-des-etablissements-alimentaires-et-condamnations-concernant-le-bien-etre-des-anim';

const filters = reactive({
  search: '',
  etablissement: '',
  adresse: '',
  categorie: '',
  statut: '',
  proprietaire: '',
  description: '',
  ville: 'Montréal'
});

const cities = ref([]);
const dark = ref(document.documentElement.dataset.theme === 'dark');

function toggleTheme() {
  dark.value = !dark.value;
  const theme = dark.value ? 'dark' : 'light';
  document.documentElement.dataset.theme = theme;
  localStorage.setItem('theme', theme);
}

const page = ref(1);
const pageSize = ref(25);
const sortBy = ref('Date');
const descending = ref(true);

const items = ref([]);
const totalCount = ref(0);
const totalPages = ref(1);
const summary = ref('Chargement...');
const loading = ref(true);
const emptyMessage = ref('Chargement...');

const totalViolations = ref('—');
const totalFines = ref('—');
const lastSync = ref('Chargement...');
const syncMetaClass = ref('');
const kpiTotalFines = ref('—');
const kpiAverageFine = ref('—');
const kpiFinesCount = ref('—');
const kpiMaxFine = ref('—');
const finesByYear = ref([{ label: 'Chargement...', value: '' }]);
const topCategoriesByFines = ref([{ label: 'Chargement...', value: '' }]);
const finesByCity = ref([{ label: 'Chargement...', value: '' }]);

const modalOpen = ref(false);
const modalLoading = ref(false);
const modalError = ref('');
const businessData = ref(null);

const hasFilters = computed(() =>
  Object.entries(filters).some(([key, value]) => key !== 'ville' && String(value).trim() !== '')
);

const activeSource = computed(() => {
  if (!filters.ville || filters.ville === 'Montréal') {
    return { label: 'data.montreal.ca', url: SOURCE_URL };
  }

  return { label: 'MAPAQ', url: MAPAQ_URL };
});

const pageButtons = computed(() => {
  const maxButtons = 5;
  const pages = Math.max(1, totalPages.value);
  let startPage = Math.max(1, page.value - 2);
  let endPage = Math.min(pages, startPage + maxButtons - 1);
  if (endPage - startPage + 1 < maxButtons && startPage > 1) {
    startPage = Math.max(1, endPage - maxButtons + 1);
  }

  const buttons = [];
  for (let current = startPage; current <= endPage; current += 1) {
    buttons.push(current);
  }
  return buttons;
});

function buildQueryParams({ includePaging } = { includePaging: true }) {
  const params = new URLSearchParams({
    sortBy: sortBy.value,
    descending: String(descending.value)
  });

  if (includePaging) {
    params.set('page', String(page.value));
    params.set('pageSize', String(pageSize.value));
  }

  Object.entries(filters).forEach(([key, value]) => {
    const trimmed = String(value).trim();
    if (trimmed) params.set(key, trimmed);
  });

  return params;
}

function applyFinesStats(stats) {
  const fines = stats.fines || {};
  const totalAmount = fines.totalAmount ?? 0;

  totalFines.value = formatCurrency(totalAmount, true);
  kpiTotalFines.value = formatCurrency(totalAmount, true);
  kpiAverageFine.value = formatCurrency(fines.averageAmount ?? 0);
  kpiFinesCount.value = formatNumber(fines.count ?? 0);
  kpiMaxFine.value = formatCurrency(fines.maxAmount ?? 0);

  finesByYear.value = (stats.finesByYear || []).map(entry => ({
    label: `${entry.year} (${formatNumber(entry.count)} dossiers)`,
    value: formatCurrency(entry.totalAmount, true)
  }));
  topCategoriesByFines.value = (stats.topCategoriesByFines || []).map(entry => ({
    label: `${entry.category || 'Sans catégorie'} (${formatNumber(entry.count)})`,
    value: formatCurrency(entry.totalAmount, true)
  }));
  finesByCity.value = (stats.finesByCity || []).map(entry => ({
    label: `${entry.city} (${formatNumber(entry.count)} dossiers)`,
    value: formatCurrency(entry.totalAmount, true)
  }));
}

async function loadMeta() {
  try {
    const ville = encodeURIComponent(filters.ville || 'Montréal');
    const [statsResponse, syncResponse] = await Promise.all([
      fetch(`/api/violations/stats?ville=${ville}`),
      fetch('/api/sync/status')
    ]);

    if (statsResponse.ok) {
      const stats = await statsResponse.json();
      totalViolations.value = formatNumber(stats.totalViolations);
      applyFinesStats(stats);
    } else {
      totalViolations.value = '—';
      totalFines.value = '—';
    }

    if (syncResponse.ok) {
      const sync = await syncResponse.json();
      const status = (sync.status || 'Idle').toLowerCase();

      if (status === 'success' && sync.lastSyncCompletedAt) {
        lastSync.value = formatDate(sync.lastSyncCompletedAt);
        syncMetaClass.value = 'success';
      } else if (status === 'failed') {
        lastSync.value = 'Échec — ' + formatDate(sync.lastSyncCompletedAt);
        syncMetaClass.value = 'error';
      } else if (status === 'idle') {
        lastSync.value = 'En attente de la première sync';
        syncMetaClass.value = '';
      } else {
        lastSync.value = sync.status || 'Inconnu';
        syncMetaClass.value = '';
      }
    } else {
      lastSync.value = 'Indisponible';
    }
  } catch {
    totalViolations.value = '—';
    totalFines.value = '—';
    lastSync.value = 'Indisponible';
    finesByYear.value = [{ label: 'Indisponible', value: '' }];
    topCategoriesByFines.value = [{ label: 'Indisponible', value: '' }];
    finesByCity.value = [{ label: 'Indisponible', value: '' }];
  }
}

async function search() {
  loading.value = true;
  items.value = [];
  summary.value = 'Chargement...';
  emptyMessage.value = 'Chargement...';

  try {
    const response = await fetch(`/api/violations/search?${buildQueryParams({ includePaging: true })}`);
    if (!response.ok) {
      throw new Error('Erreur de requête: ' + response.status);
    }

    const data = await response.json();
    items.value = data.items || [];
    totalPages.value = Math.max(1, Number(data.totalPages || 1));
    totalCount.value = data.totalCount || 0;
    summary.value = `Page ${page.value} / ${totalPages.value} • ${formatNumber(totalCount.value)} résultat(s)`;

    if (!items.value.length) {
      emptyMessage.value =
        totalCount.value === 0 && !hasFilters.value
          ? 'Aucune donnée pour le moment. La synchronisation est peut-être en cours — réessayez dans quelques minutes.'
          : 'Aucun résultat trouvé.';
    }
  } catch (error) {
    items.value = [];
    summary.value = 'Erreur';
    emptyMessage.value = error.message;
    totalPages.value = 1;
  } finally {
    loading.value = false;
  }
}

async function loadCities() {
  try {
    const response = await fetch('/api/violations/cities');
    if (!response.ok) {
      return;
    }

    const data = await response.json();
    cities.value = (data.cities || []).filter(city => city && city !== 'Montréal');
  } catch {
    cities.value = [];
  }
}

function onSearch() {
  page.value = 1;
  search();
  loadMeta();
}

function exportCsv() {
  window.location.href = `/api/violations/export?${buildQueryParams({ includePaging: false })}`;
}

function resetFilters() {
  filters.search = '';
  filters.etablissement = '';
  filters.adresse = '';
  filters.categorie = '';
  filters.statut = '';
  filters.proprietaire = '';
  filters.description = '';
  filters.ville = 'Montréal';
  pageSize.value = 25;
  page.value = 1;
  sortBy.value = 'Date';
  descending.value = true;
  search();
  loadMeta();
}

function onSort(sortKey) {
  const normalized = sortKey.charAt(0).toUpperCase() + sortKey.slice(1);
  if (sortBy.value === normalized) {
    descending.value = !descending.value;
  } else {
    sortBy.value = normalized;
    descending.value = false;
  }
  page.value = 1;
  search();
}

function goToPage(nextPage) {
  page.value = nextPage;
  search();
}

function closeBusinessModal() {
  modalOpen.value = false;
  modalError.value = '';
  businessData.value = null;
}

async function openBusinessModal(businessId) {
  if (!businessId) return;

  modalOpen.value = true;
  modalLoading.value = true;
  modalError.value = '';
  businessData.value = null;

  try {
    const response = await fetch(`/api/violations/business/${businessId}`);
    if (!response.ok) {
      throw new Error('Impossible de charger la fiche établissement.');
    }
    businessData.value = await response.json();
  } catch (error) {
    modalError.value = error.message;
  } finally {
    modalLoading.value = false;
  }
}

function onKeydown(event) {
  if (event.key === 'Escape') {
    closeBusinessModal();
  }
}

onMounted(() => {
  document.addEventListener('keydown', onKeydown);
  loadMeta();
  loadCities();
  search();
});

onUnmounted(() => {
  document.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <div class="container">
    <div class="card">
      <div class="topbar">
        <div>
          <h1>Montreal Food Violations</h1>
          <p class="subtitle">
            Consultez les condamnations alimentaires de Montréal, puis choisissez une autre ville
            pour voir celles publiées par le MAPAQ. Recherchez un restaurant ou une adresse.
          </p>
        </div>
        <div class="topbar-actions">
          <button
            class="theme-toggle"
            type="button"
            :aria-label="dark ? 'Mode clair' : 'Mode sombre'"
            @click="toggleTheme"
          >
            <svg v-if="dark" viewBox="0 0 24 24" aria-hidden="true">
              <circle cx="12" cy="12" r="4" fill="none" stroke="currentColor" stroke-width="2" />
              <path stroke="currentColor" stroke-width="2" stroke-linecap="round" d="M12 2v2.2M12 19.8V22M4.9 4.9l1.6 1.6M17.5 17.5l1.6 1.6M2 12h2.2M19.8 12H22M4.9 19.1l1.6-1.6M17.5 6.5l1.6-1.6" />
            </svg>
            <svg v-else viewBox="0 0 24 24" aria-hidden="true">
              <path fill="currentColor" d="M21 14.5A8.5 8.5 0 0 1 9.5 3a7 7 0 1 0 11.5 11.5z" />
            </svg>
          </button>
          <span class="badge-pill">Données ouvertes</span>
        </div>
      </div>

      <div class="meta-bar">
        <div class="meta-item">
          <span>Infractions en base :</span>
          <strong>{{ totalViolations }}</strong>
        </div>
        <div class="meta-item">
          <span>Total des amendes :</span>
          <strong>{{ totalFines }}</strong>
        </div>
        <div class="meta-item" :class="syncMetaClass">
          <span>Dernière sync :</span>
          <strong>{{ lastSync }}</strong>
        </div>
        <div class="meta-item">
          <span>Source :</span>
          <strong><a :href="activeSource.url" target="_blank" rel="noopener">{{ activeSource.label }}</a></strong>
        </div>
      </div>

      <StatsSection
        :total-fines="kpiTotalFines"
        :average-fine="kpiAverageFine"
        :fines-count="kpiFinesCount"
        :max-fine="kpiMaxFine"
        :fines-by-year="finesByYear"
        :top-categories-by-fines="topCategoriesByFines"
        :fines-by-city="finesByCity"
      />

      <div class="info-grid">
        <div class="info-block">
          <h2>Comment chercher ?</h2>
          <ul>
            <li>Choisissez une <strong>ville</strong>. Montréal inclut toute l'agglomération.</li>
            <li>Écrivez un <strong>nom ou une adresse</strong>, puis cliquez sur Rechercher.</li>
            <li>Ouvrez <strong>Recherche avancée</strong> pour filtrer le statut, la catégorie ou le propriétaire.</li>
            <li>Cliquez sur les en-têtes de colonnes pour <strong>trier</strong> les résultats.</li>
            <li>Cliquez sur un <strong>établissement</strong> pour voir sa fiche complète.</li>
            <li>Utilisez <strong>Exporter CSV</strong> pour télécharger les résultats filtrés.</li>
          </ul>
        </div>
        <div class="info-block">
          <h2>Pourquoi ce site ?</h2>
          <ul>
            <li><strong>Fiche établissement</strong> — historique complet par commerce.</li>
            <li><strong>Export CSV</strong> — téléchargez les résultats filtrés.</li>
            <li><strong>Statistiques d'amendes</strong> — totaux, moyennes et tendances par année.</li>
            <li><strong>Recherche avancée</strong> — filtres combinables sur tous les champs.</li>
            <li><strong>Données à jour</strong> — synchronisation automatique avec la Ville.</li>
            <li><strong>API ouverte</strong> — endpoints REST pour réutiliser les données.</li>
          </ul>
        </div>
        <div class="info-block">
          <h2>Mise à jour des données</h2>
          <p>
            Les données sont synchronisées automatiquement environ <strong>toutes les 24 heures</strong>
            depuis la Ville de Montréal et la liste des condamnations du MAPAQ.
            Si aucun résultat n'apparaît au premier chargement, la synchronisation est peut-être encore en cours.
          </p>
        </div>
      </div>

      <details class="legend">
        <summary>Légende des colonnes</summary>
        <div class="legend-table-wrap">
          <table class="legend-table">
            <thead>
              <tr>
                <th>Colonne</th>
                <th>Signification</th>
              </tr>
            </thead>
            <tbody>
              <tr><th>Établissement</th><td>Nom du commerce visé par l'infraction.</td></tr>
              <tr><th>Adresse / Ville</th><td>Lieu associé au dossier.</td></tr>
              <tr><th>Catégorie</th><td>Type d'infraction (hygiène, température, etc.).</td></tr>
              <tr><th>Description</th><td>Détail de la violation constatée.</td></tr>
              <tr><th>Statut</th><td>Statut de l'établissement (Ouvert, Fermé, changement d'exploitant, etc.).</td></tr>
              <tr><th>Date</th><td>Date de l'infraction.</td></tr>
              <tr><th>Date jugement</th><td>Date du jugement, liée au montant de l'amende.</td></tr>
              <tr><th>Montant</th><td>Amende imposée pour la condamnation (en dollars canadiens).</td></tr>
              <tr><th>Propriétaire</th><td>Nom du propriétaire déclaré.</td></tr>
            </tbody>
          </table>
        </div>
      </details>

      <div class="disclaimer">
        <strong>Avertissement</strong>
        Ces informations proviennent de sources publiques officielles et sont présentées à titre indicatif uniquement.
        La présence d'une infraction ne signifie pas nécessairement que l'établissement est dangereux aujourd'hui —
        vérifiez toujours le <strong>statut</strong> et la <strong>date</strong> du dossier.
        Ce site n'est pas affilié à la Ville de Montréal ni au MAPAQ. Les données du MAPAQ sont sous licence CC-BY 4.0.
      </div>

      <SearchFilters
        v-model:filters="filters"
        v-model:page-size="pageSize"
        :cities="cities"
        @search="onSearch"
        @export="exportCsv"
        @reset="resetFilters"
      />

      <div class="summary-row">
        <div>{{ summary }}</div>
        <div class="pagination">
          <button class="page-btn" type="button" :disabled="page <= 1" @click="goToPage(page - 1)">Précédent</button>
          <button
            v-for="pageNumber in pageButtons"
            :key="pageNumber"
            class="page-btn"
            :class="{ active: pageNumber === page }"
            type="button"
            @click="goToPage(pageNumber)"
          >
            {{ pageNumber }}
          </button>
          <button class="page-btn" type="button" :disabled="page >= totalPages" @click="goToPage(page + 1)">Suivant</button>
        </div>
      </div>

      <ViolationsTable
        :items="items"
        :loading="loading"
        :empty-message="emptyMessage"
        @sort="onSort"
        @open-business="openBusinessModal"
      />
    </div>

    <footer class="site-footer">
      Données publiques — source officielle :
      <a :href="SOURCE_URL" target="_blank" rel="noopener">Ville de Montréal</a>
      et
      <a :href="MAPAQ_URL" target="_blank" rel="noopener">MAPAQ</a>
    </footer>
  </div>

  <BusinessModal
    :open="modalOpen"
    :loading="modalLoading"
    :error="modalError"
    :data="businessData"
    @close="closeBusinessModal"
  />
</template>
