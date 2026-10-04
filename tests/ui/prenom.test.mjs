import { test } from "node:test";
import assert from "node:assert/strict";
import { lirePrenom, ecrirePrenom } from "../../src/FastFood/wwwroot/js/prenom.mjs";

const stockageEnMemoire = () => {
  const donnees = new Map();
  return { getItem: (k) => donnees.get(k) ?? null, setItem: (k, v) => donnees.set(k, v) };
};
const stockageQuiLeve = () => ({
  getItem: () => { throw new Error("interdit"); },
  setItem: () => { throw new Error("interdit"); },
});

test("lirePrenom renvoie null quand rien n'est stocké", () => {
  assert.equal(lirePrenom(stockageEnMemoire()), null);
});

test("lirePrenom relit le prénom écrit sous la clé fastfood.prenom", () => {
  const stockage = stockageEnMemoire();
  ecrirePrenom(stockage, "Zoé");
  assert.equal(stockage.getItem("fastfood.prenom"), "Zoé");
  assert.equal(lirePrenom(stockage), "Zoé");
});

test("lirePrenom renvoie null quand le stockage lève", () => {
  assert.equal(lirePrenom(stockageQuiLeve()), null);
});

test("lirePrenom renvoie null quand le stockage est absent", () => {
  assert.equal(lirePrenom(undefined), null);
});

test("ecrirePrenom est sans effet quand le stockage lève", () => {
  assert.doesNotThrow(() => ecrirePrenom(stockageQuiLeve(), "Zoé"));
});

test("ecrirePrenom est sans effet quand le stockage est absent", () => {
  assert.doesNotThrow(() => ecrirePrenom(undefined, "Zoé"));
});
