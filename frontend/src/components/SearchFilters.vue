<script setup>
const filters = defineModel('filters', { type: Object, required: true });
const pageSize = defineModel('pageSize', { type: Number, required: true });

const emit = defineEmits(['search', 'export', 'reset']);

function onPageSizeChange(event) {
  pageSize.value = Number(event.target.value);
  emit('search');
}
</script>

<template>
  <div class="filters">
    <input v-model.trim="filters.search" type="text" placeholder="Recherche globale" @keydown.enter="emit('search')" />
    <input v-model.trim="filters.etablissement" type="text" placeholder="Établissement" @keydown.enter="emit('search')" />
    <input v-model.trim="filters.adresse" type="text" placeholder="Adresse" @keydown.enter="emit('search')" />
    <input v-model.trim="filters.categorie" type="text" placeholder="Catégorie" @keydown.enter="emit('search')" />
    <input v-model.trim="filters.statut" type="text" placeholder="Statut établissement" @keydown.enter="emit('search')" />
  </div>

  <div class="filters compact">
    <input v-model.trim="filters.proprietaire" type="text" placeholder="Propriétaire" @keydown.enter="emit('search')" />
    <input v-model.trim="filters.description" type="text" placeholder="Description" @keydown.enter="emit('search')" />
    <select :value="pageSize" aria-label="Nombre de résultats" @change="onPageSizeChange">
      <option :value="10">10 résultats</option>
      <option :value="25">25 résultats</option>
      <option :value="50">50 résultats</option>
      <option :value="100">100 résultats</option>
    </select>
    <button class="btn-primary" type="button" @click="emit('search')">Rechercher</button>
    <button class="btn-secondary" type="button" @click="emit('export')">Exporter CSV</button>
    <button class="btn-secondary" type="button" @click="emit('reset')">Réinitialiser</button>
  </div>
</template>
