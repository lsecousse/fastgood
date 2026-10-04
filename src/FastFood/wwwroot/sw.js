// Réseau d'abord, repli sur le cache hors ligne. L'API et les tuiles de carte ne sont jamais mises en cache.
// VERSION ne sert qu'à purger les anciens caches ; ENVELOPPE est vérifiée par tests/ui/sw.test.mjs.
const VERSION = "fastfood-v1";
const ENVELOPPE = [
  "/",
  "/css/colors_and_type.css",
  "/css/app.css",
  "/js/api.mjs",
  "/js/app.mjs",
  "/js/carte.mjs",
  "/js/etoiles.mjs",
  "/js/fiche.mjs",
  "/js/notes.mjs",
  "/js/prenom.mjs",
  "/js/toast.mjs",
  "/js/zone.mjs",
  "/vendor/leaflet/leaflet.css",
  "/vendor/leaflet/leaflet.js",
  "/manifest.webmanifest",
  "/icons/icon-192.png",
];

self.addEventListener("install", (evenement) => {
  evenement.waitUntil(precacher());
  self.skipWaiting();
});

// Un chemin manquant n'empêche pas l'installation : on met en cache ce qui répond.
async function precacher() {
  const cache = await caches.open(VERSION);
  await Promise.all(ENVELOPPE.map((url) =>
    cache.add(new Request(url, { cache: "reload" })).catch(() => {})));
}

self.addEventListener("activate", (evenement) => {
  evenement.waitUntil(
    caches.keys()
      .then((cles) => Promise.all(cles.filter((c) => c !== VERSION).map((c) => caches.delete(c))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener("fetch", (evenement) => {
  const requete = evenement.request;
  const url = new URL(requete.url);
  const memeOrigine = url.origin === self.location.origin;
  if (requete.method !== "GET" || !memeOrigine || url.pathname.startsWith("/api/")) return;
  evenement.respondWith(reseauPuisCache(requete));
});

async function reseauPuisCache(requete) {
  try {
    const reponse = await fetch(requete);
    if (reponse.ok) {
      const copie = reponse.clone();
      caches.open(VERSION).then((cache) => cache.put(requete, copie));
    }
    return reponse;
  } catch (erreur) {
    const enCache = await caches.match(requete);
    if (enCache) return enCache;
    if (requete.mode === "navigate") return caches.match("/");
    throw erreur;
  }
}
