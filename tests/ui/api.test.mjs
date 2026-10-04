import { test } from "node:test";
import assert from "node:assert/strict";
import { creerApi } from "../../src/FastFood/wwwroot/js/api.mjs";

const reponse = (status, corps) => ({
  ok: status >= 200 && status < 300,
  status,
  json: async () => { if (corps === undefined) throw new SyntaxError("pas de JSON"); return corps; },
});
const fetchEnregistre = (rep) => {
  const appels = [];
  const fn = async (url, options) => { appels.push({ url, options }); return rep; };
  fn.appels = appels;
  return fn;
};
const zone = { sud: 48.85, ouest: 2.3, nord: 48.9, est: 2.4 };

test("chercherLieux appelle /api/lieux avec les paramètres de zone", async () => {
  const f = fetchEnregistre(reponse(200, []));
  await creerApi(f).chercherLieux(zone);
  assert.equal(f.appels[0].url, "/api/lieux?sud=48.85000&ouest=2.30000&nord=48.90000&est=2.40000");
});

test("chercherLieux renvoie le JSON", async () => {
  const lieux = [{ type: "node", id: 1 }];
  assert.deepEqual(await creerApi(fetchEnregistre(reponse(200, lieux))).chercherLieux(zone), lieux);
});

test("listerNotes appelle l'URL des notes du lieu", async () => {
  const f = fetchEnregistre(reponse(200, []));
  await creerApi(f).listerNotes("node", 42);
  assert.equal(f.appels[0].url, "/api/lieux/node/42/notes");
});

test("enregistrerNote envoie un PUT JSON avec le prénom encodé", async () => {
  const f = fetchEnregistre(reponse(204));
  const note = { etoiles: 4, commentaire: "Bon", nomLieu: "Quick" };
  await creerApi(f).enregistrerNote("node", 42, "Zoé B.", note);
  assert.equal(f.appels[0].url, "/api/lieux/node/42/notes/Zo%C3%A9%20B.");
  assert.equal(f.appels[0].options.method, "PUT");
  assert.equal(f.appels[0].options.headers["Content-Type"], "application/json");
  assert.equal(f.appels[0].options.body, JSON.stringify(note));
});

test("enregistrerNote renvoie undefined sur 204", async () => {
  const r = await creerApi(fetchEnregistre(reponse(204))).enregistrerNote("node", 1, "Zoé", {});
  assert.equal(r, undefined);
});

test("remonte le message 422", async () => {
  const f = fetchEnregistre(reponse(422, { code: "zone_trop_grande", message: "Zoome pour chercher." }));
  await assert.rejects(creerApi(f).chercherLieux(zone), { message: "Zoome pour chercher." });
});

test("remonte un message générique quand l'erreur n'a pas de corps JSON", async () => {
  await assert.rejects(creerApi(fetchEnregistre(reponse(500))).chercherLieux(zone),
    { message: "Erreur du serveur, réessaie." });
});

test("signale l'erreur réseau", async () => {
  const f = async () => { throw new TypeError("fetch failed"); };
  await assert.rejects(creerApi(f).chercherLieux(zone), { message: "Connexion impossible, réessaie." });
});

test("ApiShould.TransmettreLeSignalDAnnulationAFetch", async () => {
  const f = fetchEnregistre(reponse(200, []));
  const { signal } = new AbortController();
  await creerApi(f).chercherLieux(zone, signal);
  assert.equal(f.appels[0].options.signal, signal);
});

test("ApiShould.LaisserPasserLAnnulationSansLaTraduireEnErreurReseau", async () => {
  const f = async () => { throw new DOMException("annulée", "AbortError"); };
  await assert.rejects(creerApi(f).chercherLieux(zone), { name: "AbortError" });
});
