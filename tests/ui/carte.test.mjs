import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

// carte.mjs dépend de window.L (Leaflet) : on vérifie sa source.
const source = readFileSync("src/FastFood/wwwroot/js/carte.mjs", "utf8");

test("CarteShould.CrediterLesContributeursOpenStreetMapAvecLeLienDeLicence", () => {
  assert.ok(source.includes(`'© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'`));
});

test("CarteShould.MasquerLePrefixeLeaflet", () => {
  assert.match(source, /setPrefix\(false\)/);
});
