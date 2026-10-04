// Enveloppe de l'app en cache ; l'API et les tuiles de carte passent toujours par le réseau.
// Changer VERSION à chaque livraison de fichiers statiques pour renouveler le cache.
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
  evenement.waitUntil(caches.open(VERSION).then((cache) => cache.addAll(ENVELOPPE)));
  self.skipWaiting();
});

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
  evenement.respondWith(caches.match(requete).then((enCache) => enCache || fetch(requete)));
});
