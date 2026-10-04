import { creerApi } from "./api.mjs";
import { creerCarte } from "./carte.mjs";
import { creerFiche } from "./fiche.mjs";
import { creerToast } from "./toast.mjs";
import { estTropGrande } from "./zone.mjs";

const PARIS = { latitude: 48.8534, longitude: 2.3488 };
const ZOOM = 16;
const DELAI_POSITION_MS = 10_000;
const POSITION_INDISPONIBLE = "Position indisponible : déplace la carte";

const $ = (id) => document.getElementById(id);
const afficherMessage = creerToast($("toast"));
const api = creerApi((url, options) => fetch(url, options));
const fiche = creerFiche({ api, stockage: window.localStorage, afficherMessage, surNoteEnregistree: (lieu) => carte.mettreAJour(lieu) });
const carte = creerCarte($("carte"), fiche.ouvrir);

function demanderPosition(siOk, siErreur) {
  if (!navigator.geolocation) return siErreur();
  navigator.geolocation.getCurrentPosition((p) => siOk(p.coords), siErreur, { timeout: DELAI_POSITION_MS });
}

function centrer(coords) {
  carte.centrer(coords.latitude, coords.longitude, ZOOM);
}

function proposerRecherche() {
  const tropGrande = estTropGrande(carte.zoneVisible());
  $("chercher").hidden = tropGrande;
  $("zoome").hidden = !tropGrande;
}

let derniereRecherche = 0;

async function chercher() {
  const numero = ++derniereRecherche;
  $("chercher").hidden = true;
  $("zoome").hidden = true;
  try {
    const lieux = await api.chercherLieux(carte.zoneVisible());
    if (numero === derniereRecherche) carte.afficherLieux(lieux);
  } catch (erreur) {
    if (numero !== derniereRecherche) return;
    afficherMessage(erreur.message);
    proposerRecherche();
  }
}

function demarrer() {
  centrer(PARIS);
  carte.surDeplacement(proposerRecherche);
  $("chercher").addEventListener("click", chercher);
  $("ma-position").addEventListener("click", () =>
    demanderPosition(centrer, () => afficherMessage(POSITION_INDISPONIBLE)));
  demanderPosition(
    (coords) => { centrer(coords); chercher(); },
    () => { centrer(PARIS); afficherMessage(POSITION_INDISPONIBLE); chercher(); });
}

demarrer();
