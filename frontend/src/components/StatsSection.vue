<script setup>
defineProps({
  totalFines: { type: String, required: true },
  averageFine: { type: String, required: true },
  finesCount: { type: String, required: true },
  maxFine: { type: String, required: true },
  finesByYear: { type: Array, default: () => [] },
  topCategoriesByFines: { type: Array, default: () => [] },
  finesByCity: { type: Array, default: () => [] },
  yearNote: { type: String, default: '' }
});
</script>

<template>
  <section class="stats-section" aria-label="Statistiques des amendes">
    <h2>Amendes imposées (condamnations)</h2>
    <div class="stats-kpi-grid">
      <div class="kpi-card">
        <span class="label">Montant total</span>
        <span class="value">{{ totalFines }}</span>
        <span class="hint">Somme de toutes les amendes enregistrées</span>
      </div>
      <div class="kpi-card">
        <span class="label">Amende moyenne</span>
        <span class="value">{{ averageFine }}</span>
        <span class="hint">Par dossier condamné</span>
      </div>
      <div class="kpi-card">
        <span class="label">Dossiers avec amende</span>
        <span class="value">{{ finesCount }}</span>
        <span class="hint">Condamnations avec montant</span>
      </div>
      <div class="kpi-card">
        <span class="label">Amende maximale</span>
        <span class="value">{{ maxFine }}</span>
        <span class="hint">Montant le plus élevé</span>
      </div>
    </div>
    <div class="stats-panels">
      <div class="stats-panel">
        <h3>Amendes par année (date du jugement)</h3>
        <p v-if="yearNote" class="stats-note">{{ yearNote }}</p>
        <ul class="stats-list">
          <li v-if="!finesByYear.length">Aucune donnée disponible.</li>
          <li v-for="item in finesByYear" :key="item.label">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </li>
        </ul>
      </div>
      <div class="stats-panel">
        <h3>Top catégories par montant total</h3>
        <ul class="stats-list">
          <li v-if="!topCategoriesByFines.length">Aucune catégorie disponible.</li>
          <li v-for="item in topCategoriesByFines" :key="item.label">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </li>
        </ul>
      </div>
      <div class="stats-panel">
        <h3>Amendes par territoire</h3>
        <ul class="stats-list">
          <li v-if="!finesByCity.length">Aucun territoire disponible.</li>
          <li v-for="item in finesByCity" :key="item.label">
            <span>{{ item.label }}</span>
            <strong>{{ item.value }}</strong>
          </li>
        </ul>
      </div>
    </div>
  </section>
</template>
