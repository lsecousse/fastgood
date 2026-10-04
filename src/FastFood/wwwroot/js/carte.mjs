import { libelleEpingle } from "./etoiles.mjs";

const L = window.L;
const TUILES = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
const ATTRIBUTION = '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';
const TAILLE_EPINGLE = 34;
const CLASSES_CATEGORIE = {
  "Fast-food": "categorie-fast-food",
  "Restaurant rapide": "categorie-restaurant-rapide",
  "Boulangerie": "categorie-boulangerie",
};

export const cleLieu = (lieu) => `${lieu.type}/${lieu.id}`;

export function creerCarte(element, surClicLieu) {
  const carte = L.map(element, { zoomControl: false });
  L.tileLayer(TUILES, { maxZoom: 19, attribution: ATTRIBUTION }).addTo(carte);
  carte.attributionControl.setPrefix(false);
  const epingles = L.layerGroup().addTo(carte);
  const marqueurs = new Map();

  const ajouter = (lieu) => {
    const marqueur = L.marker([lieu.lat, lieu.lon], { icon: icone(lieu), title: lieu.nom, keyboard: true });
    marqueur.on("click", () => surClicLieu(lieu));
    marqueurs.set(cleLieu(lieu), marqueur.addTo(epingles));
  };

  return {
    // Sans animation, moveend part avant le retour : l'appelant peut lancer une recherche juste après.
    centrer: (lat, lon, zoom) => carte.setView([lat, lon], zoom, { animate: false }),
    surDeplacement: (rappel) => carte.on("moveend", rappel),
    zoneVisible: () => zoneDe(carte.getBounds()),
    afficherLieux(lieux) {
      epingles.clearLayers();
      marqueurs.clear();
      lieux.forEach(ajouter);
    },
    mettreAJour: (lieu) => marqueurs.get(cleLieu(lieu))?.setIcon(icone(lieu)),
  };
}

function icone(lieu) {
  const pastille = document.createElement("div");
  pastille.className = `epingle ${CLASSES_CATEGORIE[lieu.categorie] ?? ""}`;
  pastille.textContent = libelleEpingle(lieu.moyenne);
  return L.divIcon({ html: pastille, className: "", iconSize: [TAILLE_EPINGLE, TAILLE_EPINGLE] });
}

function zoneDe(limites) {
  return { sud: limites.getSouth(), ouest: limites.getWest(), nord: limites.getNorth(), est: limites.getEast() };
}
