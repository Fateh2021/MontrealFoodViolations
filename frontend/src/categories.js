const LABELS = {
  'rest service rapide': 'Restaurant service rapide',
  'restaurant service rapide': 'Restaurant service rapide',
  'ferme laitiere': 'Ferme laitière',
  'marche proxim permis prg': 'Marché de proximité',
  'marche proxim permis mcf': 'Marché de proximité',
  'rest mets emporter': 'Restaurant mets pour emporter',
  'hypermarche': 'Hypermarché',
  'centre d accueil': "Centre d'accueil",
  'epicerie boucherie': 'Épicerie-boucherie',
  'casse croute': 'Casse-croûte',
  'charcuterie prod transf': 'Charcuterie et produits transformés',
  'patisserie': 'Pâtisserie',
  'boul patis de depot': 'Boulangerie ou pâtisserie de dépôt',
  'bar laitier': 'Bar laitier',
  'eleveur bovins boucherie': 'Éleveur de bovins de boucherie',
  'cafeteria': 'Cafétéria',
  'decoupe a forfait': 'Découpe à forfait',
  'abattoir quebec': 'Abattoir du Québec',
  'charcuterie fromag': 'Charcuterie et fromagerie',
  'cafeteria inst ens': "Cafétéria d'établissement d'enseignement",
  'camion cuisine': 'Camion-cuisine',
  'magasin rayon sans permis': 'Magasin à rayons',
  'stand alim eve speciaux': "Stand d'alimentation, événements spéciaux",
  'biscuit boulang patiss': 'Biscuiterie, boulangerie ou pâtisserie',
  'locaux prep aliment': 'Local de préparation alimentaire',
  'mag rayon permis mcf prg': 'Magasin à rayons',
  'abattoir de proximite': 'Abattoir de proximité',
  'at prep prox prod transf': 'Atelier de préparation, produits transformés',
  'autre elevage anim consom': "Autre élevage d'animaux de consommation",
  'charcuterie gros v crues': 'Charcuterie en gros, viandes crues',
  'usine prep dist pro peche': 'Usine de produits de la pêche',
  'attenant usine transfo': 'Attenant à une usine de transformation',
  'autre permis prg ou sbl': 'Autre permis alimentaire',
  'autre sans permis ou mcf': 'Autre établissement sans permis',
  'autres animaux bea': 'Autres animaux',
  'boulangerie patisserie de depot': 'Boulangerie ou pâtisserie de dépôt',
  'camion dist prod carnes': 'Camion de distribution de produits carnés',
  'camp de vacances': 'Camp de vacances',
  'camp et ou pourvoirie': 'Camp ou pourvoirie',
  'eleveur ovin sauf laitier': 'Éleveur ovin',
  'equar viande crue': 'Équarrissage, viande crue',
  'poste classif d oeufs': "Poste de classification d'œufs",
  'usine prod lait transf': 'Usine de produits laitiers',
  'vente viande a la ferme': 'Vente de viande à la ferme'
};

function normalizeCategoryKey(value) {
  return String(value)
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim();
}

export function formatCategory(value) {
  const text = String(value ?? '').trim();
  if (!text) return '';
  return LABELS[normalizeCategoryKey(text)] || text;
}
