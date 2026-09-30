<script setup>
import { computed } from 'vue';

const FEATURED_CITIES = ['Laval', 'Longueuil', 'Québec', 'Gatineau'];

const filters = defineModel('filters', { type: Object, required: true });
const pageSize = defineModel('pageSize', { type: Number, required: true });

const props = defineProps({
  cities: { type: Array, default: () => [] }
});

const featuredCities = computed(() =>
  FEATURED_CITIES.filter(city => props.cities.includes(city))
);

const otherCities = computed(() =>
  props.cities.filter(city => !FEATURED_CITIES.includes(city))
);

const emit = defineEmits(['search', 'export', 'reset']);

function onPageSizeChange(event) {
  pageSize.value = Number(event.target.value);
  emit('search');
}
</script>

<template>
  <div class="search-bar">
    <select v-model="filters.ville" aria-label="Ville" @change="emit('search')">
      <option value="Montréal">Montréal</option>
      <option value="Toutes">Toutes les villes</option>
      <optgroup v-if="featuredCities.length" label="Villes fréquentes">
        <option v-for="city in featuredCities" :key="city" :value="city">{{ city }}</option>
      </optgroup>
      <optgroup v-if="otherCities.length" label="Autres villes">
        <option v-for="city in otherCities" :key="city" :value="city">{{ city }}</option>
      </optgroup>
    </select>
    <input
      v-model.trim="filters.search"
      type="search"
      placeholder="Nom ou adresse"
      aria-label="Nom ou adresse"
      @keydown.enter="emit('search')"
    />
    <button class="btn-primary" type="button" @click="emit('search')">Rechercher</button>
    <button class="btn-secondary" type="button" @click="emit('export')">Exporter CSV</button>
    <button class="btn-secondary" type="button" @click="emit('reset')">Réinitialiser</button>
  </div>

  <details class="legend advanced-search">
    <summary>Recherche avancée</summary>
    <div class="filters">
      <input v-model.trim="filters.etablissement" type="text" placeholder="Établissement" @keydown.enter="emit('search')" />
      <input v-model.trim="filters.adresse" type="text" placeholder="Adresse" @keydown.enter="emit('search')" />
      <input v-model.trim="filters.categorie" type="text" placeholder="Catégorie" @keydown.enter="emit('search')" />
      <input v-model.trim="filters.statut" type="text" placeholder="Statut (Ouvert, Fermé...)" @keydown.enter="emit('search')" />
      <input v-model.trim="filters.proprietaire" type="text" placeholder="Propriétaire" @keydown.enter="emit('search')" />
      <input v-model.trim="filters.description" type="text" placeholder="Description" @keydown.enter="emit('search')" />
      <select :value="pageSize" aria-label="Nombre de résultats" @change="onPageSizeChange">
        <option :value="10">10 résultats</option>
        <option :value="25">25 résultats</option>
        <option :value="50">50 résultats</option>
        <option :value="100">100 résultats</option>
      </select>
    </div>
  </details>
</template>
