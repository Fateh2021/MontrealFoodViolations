<script setup>
import { computed } from 'vue';
import { formatCategory } from '../categories';
import { formatCalendarDate, formatCurrency, formatNumber, highestFine, statusClass, summarizeBusiness } from '../formatters';

const props = defineProps({
  open: { type: Boolean, default: false },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  data: { type: Object, default: null }
});

const summary = computed(() => summarizeBusiness(props.data));
const peakFine = computed(() => highestFine(props.data?.violations));

function isPeakFine(violation) {
  return peakFine.value != null && Number(violation.montant) === peakFine.value;
}

const emit = defineEmits(['close']);

function onBackdropClick(event) {
  if (event.target === event.currentTarget) {
    emit('close');
  }
}
</script>

<template>
  <div
    class="modal-backdrop"
    :class="{ open }"
    :aria-hidden="open ? 'false' : 'true'"
    @click="onBackdropClick"
  >
    <div class="modal" role="dialog" aria-modal="true" aria-labelledby="businessModalTitle">
      <div class="modal-header">
        <div>
          <h2 id="businessModalTitle">
            {{ loading ? 'Chargement...' : (error ? 'Erreur' : (data?.etablissement || 'Établissement')) }}
          </h2>
          <p v-if="!loading && !error && data" class="subtitle">
            {{ [data.adresse, data.ville].filter(Boolean).join(', ') }}
          </p>
          <p v-if="!loading && !error && data" class="business-summary">{{ summary }}</p>
        </div>
        <button class="modal-close" type="button" @click="emit('close')">Fermer</button>
      </div>

      <div v-if="!loading && !error && data" class="modal-meta">
        <div class="meta-item"><span>Infractions :</span><strong>{{ formatNumber(data.violationCount) }}</strong></div>
        <div class="meta-item"><span>Total amendes :</span><strong>{{ formatCurrency(data.totalFines, true) }}</strong></div>
        <div class="meta-item"><span>Propriétaire :</span><strong>{{ data.proprietaire || '—' }}</strong></div>
        <div class="meta-item"><span>Statut :</span><strong><span :class="statusClass(data.statut)">{{ data.statut || '—' }}</span></strong></div>
      </div>

      <div class="modal-table-wrap">
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Catégorie</th>
              <th>Montant</th>
              <th>Statut</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td colspan="5" class="empty">Chargement...</td>
            </tr>
            <tr v-else-if="error">
              <td colspan="5" class="empty">{{ error }}</td>
            </tr>
            <tr v-else-if="!data?.violations?.length">
              <td colspan="5" class="empty">Aucune infraction.</td>
            </tr>
            <tr v-else v-for="violation in data.violations" :key="violation.idPoursuite ?? violation.description">
              <td>{{ formatCalendarDate(violation.date) }}</td>
              <td>{{ formatCategory(violation.categorie) || '—' }}</td>
              <td>
                <span
                  class="amount-cell"
                  :class="{ 'amount-high': isPeakFine(violation) }"
                  :title="isPeakFine(violation) ? 'Amende la plus élevée de cet établissement' : undefined"
                >{{ violation.montant != null ? formatCurrency(violation.montant) : '—' }}</span>
              </td>
              <td><span :class="statusClass(violation.statut)">{{ violation.statut || '—' }}</span></td>
              <td>{{ violation.description ?? '' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
