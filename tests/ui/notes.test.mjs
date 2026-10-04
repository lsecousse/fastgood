import { test } from "node:test";
import assert from "node:assert/strict";
import { dateEnFrancais, trouverMaNote, compteur, etoilesEnTexte, moyenneDesNotes, libelleCategorie } from "../../src/FastFood/wwwroot/js/notes.mjs";

const notes = [
  { prenom: "Marie", etoiles: 3, commentaire: null, modifieeLe: "2026-10-04T10:00:00+00:00" },
  { prenom: "Lionel", etoiles: 5, commentaire: "Top", modifieeLe: "2026-10-03T10:00:00+00:00" },
];

test("dateEnFrancais écrit le jour, le mois en toutes lettres et l'année", () => {
  assert.equal(dateEnFrancais("2026-10-04T10:00:00+00:00"), "4 octobre 2026");
});

test("dateEnFrancais suit l'heure de Paris", () => {
  assert.equal(dateEnFrancais("2026-12-31T23:30:00+00:00"), "1 janvier 2027");
});

test("trouverMaNote rend la note du prénom", () => {
  assert.equal(trouverMaNote(notes, "Lionel"), notes[1]);
});

test("trouverMaNote ignore la casse et les espaces du prénom", () => {
  assert.equal(trouverMaNote(notes, " lionel "), notes[1]);
});

test("trouverMaNote rend null sans prénom", () => {
  assert.equal(trouverMaNote(notes, null), null);
});

test("trouverMaNote rend null quand le prénom n'a pas noté", () => {
  assert.equal(trouverMaNote(notes, "Paul"), null);
});

test("compteur affiche la longueur sur 280", () => {
  assert.equal(compteur("Bon"), "3 / 280");
});

test("etoilesEnTexte remplit autant d'étoiles que la note sur cinq", () => {
  assert.equal(etoilesEnTexte(3), "★★★☆☆");
});

test("moyenneDesNotes calcule la moyenne et le nombre de notes", () => {
  assert.deepEqual(moyenneDesNotes(notes), { moyenne: 4, nombreNotes: 2 });
});

test("moyenneDesNotes rend une moyenne nulle sans note", () => {
  assert.deepEqual(moyenneDesNotes([]), { moyenne: null, nombreNotes: 0 });
});

test("libelleCategorie ajoute la cuisine lisible après la catégorie", () => {
  assert.equal(libelleCategorie("Fast-food", "burger;ice_cream"), "Fast-food · burger, ice cream");
});

test("libelleCategorie se limite à la catégorie sans cuisine", () => {
  assert.equal(libelleCategorie("Boulangerie", null), "Boulangerie");
});
