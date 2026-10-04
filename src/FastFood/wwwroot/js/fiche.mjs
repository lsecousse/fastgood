import { resumeMoyenne } from "./etoiles.mjs";
import { lirePrenom, ecrirePrenom } from "./prenom.mjs";
import {
  compteur, dateEnFrancais, etoilesEnTexte, libelleCategorie, moyenneDesNotes, trouverMaNote,
} from "./notes.mjs";

const $ = (id) => document.getElementById(id);
const NOTE_ENREGISTREE = "Note enregistrée";

export function creerFiche({ api, stockage, afficherMessage, surNoteEnregistree }) {
  const etat = { lieu: null, etoiles: 0, prenom: null, notes: [] };
  const deps = { api, stockage, afficherMessage, surNoteEnregistree };
  creerEtoiles(etat);
  $("fiche-fermer").addEventListener("click", () => fermer(etat));
  $("changer-prenom").addEventListener("click", () => changerPrenom(etat));
  $("prenom").addEventListener("input", () => rafraichirFormulaire(etat));
  $("prenom").addEventListener("change", () => preremplir(etat, trouverMaNote(etat.notes, $("prenom").value)));
  $("commentaire").addEventListener("input", () => rafraichirFormulaire(etat));
  $("ma-note").addEventListener("submit", (e) => { e.preventDefault(); enregistrer(etat, deps); });
  return { ouvrir: (lieu) => ouvrir(etat, deps, lieu) };
}

function ouvrir(etat, deps, lieu) {
  Object.assign(etat, { lieu, prenom: lirePrenom(deps.stockage), notes: [] });
  $("prenom").value = "";
  afficherEntete(lieu);
  afficherNotes(etat);
  preremplir(etat, null);
  $("fiche").classList.add("ouverte");
  $("fiche").setAttribute("aria-hidden", "false");
  chargerNotes(etat, deps, lieu);
}

function fermer(etat) {
  etat.lieu = null;
  $("fiche").classList.remove("ouverte");
  $("fiche").setAttribute("aria-hidden", "true");
  document.activeElement?.blur();
}

async function chargerNotes(etat, deps, lieu) {
  try {
    const notes = await deps.api.listerNotes(lieu.type, lieu.id);
    if (etat.lieu !== lieu) return;
    recevoirNotes(etat, notes);
    preremplir(etat, trouverMaNote(notes, etat.prenom));
  } catch (erreur) {
    deps.afficherMessage(erreur.message);
  }
}

function recevoirNotes(etat, notes) {
  etat.notes = notes;
  Object.assign(etat.lieu, moyenneDesNotes(notes));
  afficherEntete(etat.lieu);
  afficherNotes(etat);
}

function afficherEntete(lieu) {
  $("fiche-nom").textContent = lieu.nom;
  $("fiche-categorie").textContent = libelleCategorie(lieu.categorie, lieu.cuisine);
  $("fiche-moyenne").textContent = resumeMoyenne(lieu.moyenne, lieu.nombreNotes);
}

function afficherNotes(etat) {
  $("fiche-notes").replaceChildren(...etat.notes.map(elementNote));
  $("fiche-avis").hidden = etat.notes.length === 0;
}

function elementNote(note) {
  const entete = element("div", "note-entete",
    element("span", "note-prenom", note.prenom),
    element("span", "note-etoiles", etoilesEnTexte(note.etoiles)),
    element("span", "note-date", dateEnFrancais(note.modifieeLe)));
  const item = element("li", "", entete);
  if (note.commentaire) item.append(element("p", "note-commentaire", note.commentaire));
  return item;
}

function element(balise, classe, ...enfants) {
  const noeud = document.createElement(balise);
  noeud.className = classe;
  noeud.append(...enfants);
  return noeud;
}

function creerEtoiles(etat) {
  const boutons = [1, 2, 3, 4, 5].map((n) => {
    const bouton = element("button", "etoile", "★");
    Object.assign(bouton, { type: "button" });
    bouton.setAttribute("aria-label", `${n} ${n > 1 ? "étoiles" : "étoile"}`);
    bouton.addEventListener("click", () => { etat.etoiles = n; rafraichirFormulaire(etat); });
    return bouton;
  });
  $("etoiles").replaceChildren(...boutons);
}

function preremplir(etat, note) {
  etat.etoiles = note?.etoiles ?? 0;
  $("commentaire").value = note?.commentaire ?? "";
  rafraichirFormulaire(etat);
}

function changerPrenom(etat) {
  etat.prenom = null;
  preremplir(etat, null);
  $("prenom").focus();
}

function prenomSaisi(etat) {
  return etat.prenom ?? $("prenom").value.trim();
}

function rafraichirFormulaire(etat) {
  [...$("etoiles").children].forEach((bouton, i) =>
    bouton.setAttribute("aria-pressed", String(i < etat.etoiles)));
  $("compteur").textContent = compteur($("commentaire").value);
  $("bloc-prenom").hidden = etat.prenom !== null;
  $("changer-prenom").hidden = etat.prenom === null;
  $("changer-prenom").textContent = `Pas ${etat.prenom} ?`;
  $("enregistrer").disabled = etat.etoiles === 0 || !prenomSaisi(etat);
}

async function enregistrer(etat, deps) {
  const { lieu } = etat;
  const prenom = prenomSaisi(etat);
  const note = { etoiles: etat.etoiles, commentaire: $("commentaire").value.trim() || null, nomLieu: lieu.nom };
  $("enregistrer").disabled = true;
  try {
    await deps.api.enregistrerNote(lieu.type, lieu.id, prenom, note);
  } catch (erreur) {
    deps.afficherMessage(erreur.message);
    return rafraichirFormulaire(etat);
  }
  memoriserPrenom(etat, deps, prenom);
  deps.afficherMessage(NOTE_ENREGISTREE);
  await rechargerApresEnregistrement(etat, deps, lieu);
  rafraichirFormulaire(etat);
}

function memoriserPrenom(etat, deps, prenom) {
  ecrirePrenom(deps.stockage, prenom);
  etat.prenom = prenom;
}

async function rechargerApresEnregistrement(etat, deps, lieu) {
  try {
    const notes = await deps.api.listerNotes(lieu.type, lieu.id);
    if (etat.lieu === lieu) recevoirNotes(etat, notes);
    else Object.assign(lieu, moyenneDesNotes(notes));
    deps.surNoteEnregistree(lieu);
  } catch {
    // la note est enregistrée : on garde « Note enregistrée » et la fiche telle quelle
  }
}
