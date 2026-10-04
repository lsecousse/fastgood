import { creerApi } from "./api.mjs";
import { creerCarte } from "./carte.mjs";
import { creerFiche } from "./fiche.mjs";
import { creerRechercheUnique, estAnnulation } from "./recherche.mjs";
import { creerToast } from "./toast.mjs";
import { estTropGrande } from "./zone.mjs";

const PARIS = { latitude: 48.8534, longitude: 2.3488 };
const ZOOM = 16;
const DELAI_POSITION_MS = 10_000;
const POSITION_INDISPONIBLE = "Position indisponible : déplace la carte";
const CHERCHER_ICI = "Chercher ici";
const RECHERCHE_EN_COURS = "Recherche…";

const $ = (id) => document.getElementById(id);
const afficherMessage = creerToast($("toast"));
const api = creerApi((url, options) => fetch(url, options));
const fiche = creerFiche({ api, stockage: window.localStorage, afficherMessage, surNoteEnregistree: (lieu) => carte.mettreAJour(lieu) });
const carte = creerCarte($("carte"), fiche.ouvrir);
const rechercher = creerRechercheUnique(api.chercherLieux);

function demanderPosition(siOk, siErreur) {
  if (!navigator.geolocation) return siErreur();
  navigator.geolocation.getCurrentPosition((p) => siOk(p.coords), siErreur, { timeout: DELAI_POSITION_MS });
}

function centrer(coords) {
  carte.centrer(coords.latitude, coords.longitude, ZOOM);
}

function afficherBouton(libelle, actif) {
  const bouton = $("chercher");
  bouton.textContent = libelle;
  bouton.disabled = !actif;
}

function proposerRecherche() {
  const tropGrande = estTropGrande(carte.zoneVisible());
  afficherBouton(CHERCHER_ICI, true);
  $("chercher").hidden = tropGrande;
  $("zoome").hidden = !tropGrande;
}

function signalerRechercheEnCours() {
  afficherBouton(RECHERCHE_EN_COURS, false);
  $("chercher").hidden = false;
  $("zoome").hidden = true;
}

// Si la carte a bougé pendant la recherche, « Chercher ici » reste proposé pour la nouvelle zone.
function terminerRecherche() {
  if ($("chercher").disabled) $("chercher").hidden = true;
}

async function chercher() {
  signalerRechercheEnCours();
  try {
    carte.afficherLieux(await rechercher(carte.zoneVisible()));
    terminerRecherche();
  } catch (erreur) {
    if (estAnnulation(erreur)) return;
    afficherMessage(erreur.message);
    proposerRecherche();
  }
}

// centrer est synchrone : son moveend propose « Chercher ici » avant que chercher n'affiche « Recherche… ».
function centrerPuisChercher(coords) {
  centrer(coords);
  chercher();
}

function demarrer() {
  centrer(PARIS);
  carte.surDeplacement(proposerRecherche);
  $("chercher").addEventListener("click", chercher);
  $("ma-position").addEventListener("click", () =>
    demanderPosition(centrer, () => afficherMessage(POSITION_INDISPONIBLE)));
  demanderPosition(
    centrerPuisChercher,
    () => { afficherMessage(POSITION_INDISPONIBLE); centrerPuisChercher(PARIS); });
}

demarrer();
