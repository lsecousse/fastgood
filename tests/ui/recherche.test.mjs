import { test } from "node:test";
import assert from "node:assert/strict";
import { creerRechercheUnique, estAnnulation } from "../../src/FastFood/wwwroot/js/recherche.mjs";

const chercherEnregistre = () => {
  const signaux = [];
  const fn = async (zone, signal) => { signaux.push(signal); return zone; };
  fn.signaux = signaux;
  return fn;
};

test("RechercheUniqueShould.AnnulerLaRecherchePrecedente", async () => {
  const chercher = chercherEnregistre();
  const rechercher = creerRechercheUnique(chercher);
  await rechercher("zone 1");
  await rechercher("zone 2");
  assert.equal(chercher.signaux[0].aborted, true);
});

test("RechercheUniqueShould.LaisserLaDerniereRechercheAboutir", async () => {
  const chercher = chercherEnregistre();
  const rechercher = creerRechercheUnique(chercher);
  await rechercher("zone 1");
  assert.equal(await rechercher("zone 2"), "zone 2");
  assert.equal(chercher.signaux[1].aborted, false);
});

test("EstAnnulationShould.ReconnaitreUneAbortError", () => {
  assert.equal(estAnnulation(new DOMException("annulée", "AbortError")), true);
});

test("EstAnnulationShould.IgnorerUneAutreErreur", () => {
  assert.equal(estAnnulation(new Error("Connexion impossible, réessaie.")), false);
});
