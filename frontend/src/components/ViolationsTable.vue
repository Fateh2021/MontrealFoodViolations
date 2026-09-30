<script setup>
import { formatCalendarDate, formatCurrency, statusClass } from '../formatters';

defineProps({
  items: { type: Array, default: () => [] },
  emptyMessage: { type: String, default: '' },
  loading: { type: Boolean, default: false }
});

const emit = defineEmits(['sort', 'open-business']);
</script>

<template>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th><button type="button" @click="emit('sort', 'etablissement')">Établissement</button></th>
          <th><button type="button" @click="emit('sort', 'adresse')">Adresse</button></th>
          <th><button type="button" @click="emit('sort', 'date')">Date</button></th>
          <th><button type="button" @click="emit('sort', 'dateJugement')">Date jugement</button></th>
          <th><button type="button" @click="emit('sort', 'montant')">Montant</button></th>
          <th><button type="button" @click="emit('sort', 'statut')">Statut</button></th>
          <th><button type="button" @click="emit('sort', 'ville')">Ville</button></th>
          <th><button type="button" @click="emit('sort', 'categorie')">Catégorie</button></th>
          <th><button type="button" @click="emit('sort', 'proprietaire')">Propriétaire</button></th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading || !items.length">
          <td colspan="10" class="empty">{{ emptyMessage }}</td>
        </tr>
        <tr v-for="item in items" :key="item.idPoursuite">
          <td>
            <button
              v-if="item.businessId"
              type="button"
              class="link-btn"
              @click="emit('open-business', item.businessId)"
            >
              {{ item.etablissement ?? '' }}
            </button>
            <template v-else>{{ item.etablissement ?? '' }}</template>
          </td>
          <td>{{ item.adresse ?? '' }}</td>
          <td>{{ formatCalendarDate(item.date) }}</td>
          <td>{{ formatCalendarDate(item.dateJugement) }}</td>
          <td class="amount-cell">{{ item.montant != null ? formatCurrency(item.montant) : '—' }}</td>
          <td><span :class="statusClass(item.statut)">{{ item.statut || '—' }}</span></td>
          <td>{{ item.ville ?? '' }}</td>
          <td>{{ item.categorie ?? '' }}</td>
          <td>{{ item.proprietaire ?? '' }}</td>
          <td>{{ item.description ?? '' }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
