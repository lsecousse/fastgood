import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync, readdirSync } from "node:fs";
import { join } from "node:path";

const WWWROOT = "src/FastFood/wwwroot";
const source = readFileSync(join(WWWROOT, "sw.js"), "utf8");
const enveloppe = [...source.match(/const ENVELOPPE = \[([\s\S]*?)\];/)[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]);

const fichiers = (dossier) =>
  readdirSync(join(WWWROOT, dossier), { withFileTypes: true }).flatMap((e) =>
    e.isDirectory() ? fichiers(join(dossier, e.name)) : [join(dossier, e.name)]);

test("ServiceWorkerShould.ListerDesChemins", () => {
  assert.ok(enveloppe.length > 0);
});

test("ServiceWorkerShould.PrecacherDesFichiersQuiExistent", () => {
  const absents = enveloppe.filter((c) => c !== "/" && !existsSync(join(WWWROOT, c)));
  assert.deepEqual(absents, []);
});

test("ServiceWorkerShould.PrecacherChaqueModuleEtFeuilleDeStyle", () => {
  const attendus = ["css", "js"].flatMap(fichiers)
    .filter((f) => /\.(mjs|css)$/.test(f)).map((f) => "/" + f.replaceAll("\\", "/"));
  assert.deepEqual(attendus.filter((f) => !enveloppe.includes(f)), []);
});

test("ServiceWorkerShould.PreferLeReseauAuCache", () => {
  assert.match(source, /fetch\(requete, [^)]*\)[\s\S]*caches\.match/);
  assert.match(source, /cache: "reload"/);
});

test("ServiceWorkerShould.RevaliderChaqueFichierAupresDuServeur", () => {
  assert.match(source, /fetch\(requete, \{ cache: "no-cache" \}\)/);
});

test("ServiceWorkerShould.GarderLeWorkerEnVieJusquALaMiseEnCache", () => {
  assert.match(source, /waitUntil\([\s\S]*?cache\.put\([\s\S]*?\.catch\(\(\) => \{\}\)/);
});
