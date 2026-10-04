import { test } from "node:test";
import assert from "node:assert/strict";
import { resumeMoyenne, libelleEpingle } from "../../src/FastFood/wwwroot/js/etoiles.mjs";

test("resumeMoyenne indique pas encore noté pour une moyenne nulle", () => {
  assert.equal(resumeMoyenne(null, 0), "Pas encore noté");
});

test("resumeMoyenne indique pas encore noté quand il n'y a aucune note", () => {
  assert.equal(resumeMoyenne(3, 0), "Pas encore noté");
});

test("resumeMoyenne affiche la virgule française et le pluriel", () => {
  assert.equal(resumeMoyenne(3.5, 2), "3,5★ · 2 notes");
});

test("resumeMoyenne affiche le singulier pour une seule note", () => {
  assert.equal(resumeMoyenne(4, 1), "4★ · 1 note");
});

test("libelleEpingle affiche la virgule française", () => {
  assert.equal(libelleEpingle(3.5), "3,5");
});

test("libelleEpingle n'affiche pas de décimale pour un entier", () => {
  assert.equal(libelleEpingle(4), "4");
});

test("libelleEpingle est vide sans moyenne", () => {
  assert.equal(libelleEpingle(null), "");
});
