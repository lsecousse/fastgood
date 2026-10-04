import { test } from "node:test";
import assert from "node:assert/strict";
import { versParametres, plusGrandCoteEnMetres, estTropGrande, COTE_MAX_METRES } from "../../src/FastFood/wwwroot/js/zone.mjs";

const petite = { sud: 48.85, ouest: 2.35, nord: 48.855, est: 2.356 };
const grande = { sud: 48.8, ouest: 2.3, nord: 48.9, est: 2.4 };

test("versParametres formate la zone avec 5 décimales", () => {
  assert.equal(versParametres({ sud: 48.85, ouest: 2.3, nord: 48.9, est: 2.4 }),
    "sud=48.85000&ouest=2.30000&nord=48.90000&est=2.40000");
});

test("plusGrandCoteEnMetres mesure la hauteur d'un degré de latitude", () => {
  const metres = plusGrandCoteEnMetres({ sud: 0, ouest: 0, nord: 1, est: 0.001 });
  assert.ok(Math.abs(metres - 111194.9) < 1);
});

test("plusGrandCoteEnMetres mesure la largeur à la latitude moyenne", () => {
  const metres = plusGrandCoteEnMetres({ sud: 59.999, ouest: 0, nord: 60.001, est: 1 });
  assert.ok(Math.abs(metres - 55597.5) < 5);
});

test("COTE_MAX_METRES vaut 3000", () => {
  assert.equal(COTE_MAX_METRES, 3000);
});

test("estTropGrande est faux pour une petite zone", () => {
  assert.equal(estTropGrande(petite), false);
});

test("estTropGrande est vrai pour une grande zone", () => {
  assert.equal(estTropGrande(grande), true);
});
