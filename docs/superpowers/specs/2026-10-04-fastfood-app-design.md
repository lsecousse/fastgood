# FastFood — carte des fast-foods autour de moi, notée entre proches

Date : 2026-10-04 · Auteur : exécutant `fastfood-app` · Statut : à valider par Lionel

## 1. Intention

Lionel veut, sur son iPhone, voir les fast-foods autour de lui sur une carte et les noter ; ses proches (présents sur son tailnet) voient et ajoutent leurs notes. Application **personnelle**, hors produit LinkEat.

Réussi quand : depuis l'iPhone, l'icône FastFood sur l'écran d'accueil ouvre une carte centrée sur sa position, les épingles des fast-foods de la zone visible apparaissent, un toucher ouvre la fiche, il pose 4★ + un commentaire, et un proche voit la moyenne et la note de Lionel.

## 2. Décisions de Lionel (réponses `demander`, texte exact)

| # | Question | Réponse |
|---|---|---|
| 268 | Notes pour lui seul ou partagées ? | « Partagées, chacun tape son prénom une fois (mémorisé sur le téléphone) : moyenne + note de chacun, aucune sécurité (n'importe qui sur le tailnet peut prendre un prénom) » |
| 269 | Étoiles, commentaire ? | « Étoiles (1 à 5) + commentaire facultatif (texte court, 280 caractères max) » |
| 270 | Liste, carte, rayon ? | « carte juste » |
| 271 | Quels lieux sur la carte ? | « La zone visible à l'écran : tu déplaces ou dézoomes la carte, les épingles se chargent pour la nouvelle zone (bouton « Chercher ici ») ; au-delà d'un certain dézoom (~3 km de large), on te demande de zoomer » |
| 272 | Qu'est-ce qu'un fast-food ? | « Fast-food + restaurants rapides + boulangeries : 58 lieux à 500 m » |

Décisions de cadrage (orchestrateur) : PWA installable sans App Store ; données OpenStreetMap via Overpass sans clé, politique d'usage respectée ; hébergement sur le Mac mini derrière Tailscale, sans toucher au `/` du Guichet (port 5000) ; port local 5080.

Mesures réelles (Overpass, 2026-10-04, centre de Paris 48.8534, 2.3488, position IP approximative du Mac mini) : fast-food 32 / 248 / 1 599 lieux à 500 m / 1 km / 3 km ; restaurants rapides +14 / +61 / +329 ; boulangeries +12 / +70 / +521. L'instance principale `overpass-api.de` a répondu **504 à 4 requêtes sur 6** ; `lz4.overpass-api.de` a répondu.

## 3. Périmètre

Dans le périmètre :
- Carte plein écran centrée sur la position de l'iPhone ; épingles des lieux de la zone visible ; bouton « Chercher ici » après déplacement.
- Zone trop grande (plus de 3 km sur son plus grand côté) : pas de requête, message « Zoome pour chercher ».
- Fiche d'un lieu : nom, catégorie, cuisine, moyenne et nombre de notes, liste des notes (prénom, étoiles, commentaire, date), formulaire « Ma note ».
- Prénom saisi une fois, mémorisé sur le téléphone, modifiable.
- Une note par (lieu, prénom), modifiable ; 1 à 5 étoiles ; commentaire facultatif ≤ 280 caractères.
- Épingle d'un lieu déjà noté : affiche sa moyenne.
- Installation sur l'écran d'accueil (manifeste, icônes, service worker de l'enveloppe).

Hors périmètre (YAGNI) : liste, recherche par nom, filtres de catégorie, photos, suppression de note, comptes / mot de passe, mode hors ligne des données, notification.

## 4. Catégories (règle métier)

| Catégorie affichée | Requête OSM |
|---|---|
| Fast-food | `amenity=fast_food` |
| Restaurant rapide | `amenity=restaurant` et `cuisine` contient l'un de : `kebab`, `burger`, `pizza`, `sandwich`, `chicken`, `tacos` |
| Boulangerie | `shop=bakery` |

Un lieu sans nom reçoit le libellé de sa catégorie (« Fast-food sans nom »). Identifiant d'un lieu = type OSM + id (`node/123`, `way/456`) ; position d'un `way` = son centre.

## 5. Architecture

Une seule application **.NET 10** (comme le Guichet) : API minimale + fichiers statiques, base **SQLite**. Le front est une PWA en JavaScript sans framework (modules ES), carte **Leaflet** (copiée dans le dépôt, licence BSD-2) et fond de carte OpenStreetMap (`tile.openstreetmap.org`, attribution affichée).

```
iPhone (Safari / PWA) ──HTTPS──► tailscale serve :8443 ──► 127.0.0.1:5080  FastFood (.NET)
                                                                 ├── wwwroot/ (PWA)
                                                                 ├── SQLite  notes
                                                                 └── Overpass (lz4 → principal), 1 requête à la fois, cache 24 h
```

### Unités (back)

| Unité | Rôle |
|---|---|
| `Zone` | rectangle sud/ouest/nord/est ; valide les bornes ; calcule son plus grand côté en mètres ; s'arrondit vers l'extérieur sur une grille de 0,005° (clé de cache). |
| `RequeteOverpass` | construit le texte de la requête Overpass QL pour une zone (les 3 catégories, `out tags center`). |
| `LecteurOverpass` | transforme la réponse JSON en `Lieu` (id, nom, catégorie, cuisine, lat, lon) ; écarte les éléments sans position. |
| `ClientOverpass` | appelle les instances dans l'ordre (`lz4.overpass-api.de`, puis `overpass-api.de`), User-Agent identifiant l'app et un contact, délai 30 s, **une requête à la fois** (sémaphore). Échec de toutes → `OverpassIndisponible`. |
| `CatalogueLieux` | cache 24 h par zone arrondie devant `ClientOverpass`. |
| `DepotNotes` | SQLite : enregistrer (upsert) une note, lister les notes d'un lieu, moyennes pour un ensemble de lieux. |
| `Note` | validation : étoiles 1..5, prénom 1..40 caractères non blancs (espaces de bord retirés), commentaire ≤ 280 (vide → absent). |

### API

| Méthode | Route | Effet |
|---|---|---|
| GET | `/api/lieux?sud&ouest&nord&est` | lieux de la zone + `moyenne` et `nombreNotes`. 400 `zone_invalide` ; 422 `zone_trop_grande` (> 3 000 m) ; 503 `osm_indisponible`. |
| GET | `/api/lieux/{type}/{id}/notes` | notes du lieu, plus récentes d'abord. |
| PUT | `/api/lieux/{type}/{id}/notes/{prenom}` | corps `{ etoiles, commentaire, nomLieu }` ; crée ou remplace ; 400 avec message si invalide. |
| GET | `/api/sante` | 200 (contrôle de l'installateur). |

Table `notes(type, osm_id, prenom, etoiles, commentaire, nom_lieu, modifiee_le)`, clé primaire `(type, osm_id, prenom COLLATE NOCASE)` : « Lionel » et « lionel » sont la même personne.

### Front

| Module | Rôle (testé avec `node --test` quand il est pur) |
|---|---|
| `zone.mjs` | zone visible → paramètres de requête ; plus grand côté en mètres ; « trop grande ? ». |
| `etoiles.mjs` | rendu texte d'une moyenne (« 3,5★ · 2 notes », virgule française). |
| `prenom.mjs` | lecture / écriture du prénom (stockage local, tolérant à son absence). |
| `api.mjs` | appels HTTP, erreurs traduites en messages. |
| `carte.mjs`, `fiche.mjs`, `app.mjs` | Leaflet, fiche en panneau bas, orchestration. |

Géolocalisation refusée ou indisponible : la carte s'ouvre sur le centre de Paris avec le message « Position indisponible : déplace la carte ». Erreurs réseau / OSM : bandeau « OpenStreetMap ne répond pas, réessaie dans une minute ».

## 6. Interface

App personnelle : le Design System LinkEat (`~/LinkEat/conception-ui/2-Design System/README.md`, `colors_and_type.css`) s'applique pour les couleurs, typographies, rayons, ombres et le toast (`ink-900`, bas d'écran) ; les composants de tableau ne servent pas. `colors_and_type.css` est copié tel quel dans `wwwroot/` ; aucune valeur visuelle hors de ses variables. L'orange (`--accent`) est réservé à l'action principale de l'écran (« Enregistrer ma note » dans la fiche, « Chercher ici » sur la carte).

- Carte plein écran ; bouton « Chercher ici » flottant en haut, visible après un déplacement ; bouton « ma position » en bas à droite.
- Épingles : pastille ronde par catégorie (couleurs catégorielles non sémantiques : `--tag-1` Fast-food, `--tag-2` Restaurant rapide, `--tag-4` Boulangerie — `--tag-3` écarté car identique à `--accent`, réservé à l'action principale), moyenne affichée dedans si le lieu est noté.
- Fiche : panneau qui monte du bas (feuille), fermeture par croix 38 px ; étoiles tactiles 44 px ; zone de texte avec compteur « 120 / 280 ».
- Premier usage : demande « Ton prénom ? » avant la première note (pas au lancement).
- Respect de l'encoche iPhone (`env(safe-area-inset-*)`), `apple-mobile-web-app-capable`, icône 180 px.

## 7. Hébergement

- `deploy/installer.sh` (modèle du Guichet, simplifié) : `dotnet publish` autonome dans `~/Library/Application Support/FastFood/app`, base `fastfood.db` à côté, journal `~/Library/Logs/FastFood/fastfood.log`, LaunchAgent `net.linkeat.fastfood` (KeepAlive), port 5080, contrôle `/api/sante`.
- Exposition : `tailscale serve --bg --https=8443 http://127.0.0.1:5080` → **https://macmini.tail01f6f6.ts.net:8443/**. Le `/` (port 443 → 5000) du Guichet n'est pas modifié. Un port plutôt qu'un chemin : l'app reste à la racine de son origine (service worker, manifeste, géolocalisation sans réécriture de chemins).
- L'installation et `tailscale serve` sont lancés par Lionel (commande `!` fournie).

## 8. Tests et vérification

- xUnit : `Zone`, `RequeteOverpass`, `LecteurOverpass` (sur une réponse Overpass réelle enregistrée), `Note`, `DepotNotes` (SQLite en fichier temporaire), `ClientOverpass` (repli sur la 2ᵉ instance, échec total, sérialisation ; `HttpMessageHandler` factice), API par `WebApplicationFactory` (codes 400/422/503, upsert insensible à la casse du prénom).
- `node --test` pour les modules front purs.
- Tests du script d'installation : fonctions pures (plist), comme `deploy/tests` du Guichet.
- GitHub Actions : `dotnet test` + `dotnet build` + `node --test` sur chaque PR et sur `main`.
- Vérification réelle : lancement local, appel `/api/lieux` sur une zone de Paris, capture en vue iPhone (390×844) regardée : carte, épingles, fiche, note enregistrée.

## 9. Risques

- Overpass surchargé (504 observés) : repli sur 2 instances + cache 24 h ; si tout échoue, message clair, rien ne casse.
- Tuiles OSM : usage personnel faible, attribution affichée, conforme à la politique d'usage ; à remplacer si l'usage grandit.
- Pas d'authentification (choix de Lionel) : l'accès est borné au tailnet.
