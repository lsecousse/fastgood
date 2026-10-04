# FastFood — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** PWA « carte des fast-foods autour de moi », notée entre proches, servie par une app .NET sur le Mac mini.

**Architecture :** une app ASP.NET Core minimale (`src/FastFood`) sert l'API JSON et les fichiers statiques de la PWA (`wwwroot`). Les lieux viennent d'Overpass (2 instances, une requête à la fois, cache 24 h), les notes d'une base SQLite. Front en modules ES sans framework, carte Leaflet copiée dans le dépôt.

**Tech Stack :** .NET 10 (SDK 10.0.401), xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Data.Sqlite`, `Microsoft.Extensions.Caching.Memory` ; Node 24 (`node --test`) pour les modules front purs ; Leaflet 1.9.4 ; bash pour l'installateur ; GitHub Actions.

**Spec :** `docs/superpowers/specs/2026-10-04-fastfood-app-design.md` (à lire avant toute tâche).

## Global Constraints

- TDD : test rouge exécuté et vu échouer avant le code ; nommage des classes de test `XxxShould`, méthodes `DescriptionDuComportement` ; AAA ; pas de `if` dans les tests.
- Code, identifiants métier, messages : en français (`Zone`, `Lieu`, `Note`…), comme le Guichet.
- Port local **5080** ; jamais 5000.
- User-Agent Overpass : `FastFood/1.0 (app perso; lsecousse@linkeat.io)`.
- Instances Overpass, dans cet ordre : `https://lz4.overpass-api.de/api/interpreter`, `https://overpass-api.de/api/interpreter` ; délai 30 s par appel.
- Zone : plus grand côté > **3 000 m** → refus `zone_trop_grande`.
- Note : étoiles entières **1..5** ; prénom 1..40 caractères après `Trim()` ; commentaire ≤ **280** caractères après `Trim()`, vide → `null`.
- Catégories et libellés exacts : `Fast-food`, `Restaurant rapide`, `Boulangerie` ; cuisines rapides : `kebab`, `burger`, `pizza`, `sandwich`, `chicken`, `tacos`.
- Lieu sans nom : `"<Catégorie> sans nom"` (ex. « Boulangerie sans nom »).
- Front : uniquement des variables de `colors_and_type.css` (copie de `~/LinkEat/conception-ui/2-Design System/colors_and_type.css`) pour couleurs / polices / rayons / ombres ; `--accent` réservé à « Chercher ici » et « Enregistrer ma note ».
- Nombres affichés à la française (« 3,5★ »).
- Aucune dépendance npm : `node --test` sur des `.mjs` purs.

## Review Focus

1. Prénom saisi « Lionel » puis « lionel » ou « Lionel␠» → une seule note, remplacée (test DepotNotes + API, Tâche 3/5).
2. Overpass renvoie 504 puis la 2ᵉ instance répond → lieux affichés ; les deux échouent → 503 `osm_indisponible`, jamais 500 (Tâche 4/5).
3. Élément Overpass sans `lat/lon` ni `center`, ou sans `tags` → ignoré sans exception (Tâche 2).
4. Zone envoyée avec sud > nord, ou latitude hors [-90, 90] / paramètre manquant → 400 `zone_invalide` (Tâche 1/5).
5. Commentaire de 281 caractères ou étoiles 0 / 6 → 400 avec message français ; texte de 280 accepté (Tâche 3/5). Côté front, le commentaire et le nom d'un lieu sont insérés en `textContent`, jamais en HTML (Tâche 7).

---

## Structure des fichiers

```
FastFood.slnx
global.json                         # SDK 10.0.401, rollForward latestFeature
src/FastFood/FastFood.csproj
src/FastFood/Program.cs             # composition + routes (fin, testable via WebApplicationFactory<Program>)
src/FastFood/Lieux/Zone.cs
src/FastFood/Lieux/Lieu.cs          # record Lieu + enum/constantes Categorie
src/FastFood/Lieux/RequeteOverpass.cs
src/FastFood/Lieux/LecteurOverpass.cs
src/FastFood/Lieux/ClientOverpass.cs   # IClientOverpass + implémentation HTTP
src/FastFood/Lieux/CatalogueLieux.cs   # ICatalogueLieux + cache
src/FastFood/Notes/Note.cs             # record + validation
src/FastFood/Notes/DepotNotes.cs       # IDepotNotes + SQLite
src/FastFood/Web/RoutesApi.cs          # MapApi(this WebApplication)
src/FastFood/wwwroot/...               # PWA (Tâches 6-8)
tests/FastFood.Tests/...               # xUnit
tests/ui/*.test.mjs                    # node --test
deploy/installer.sh, deploy/lib.sh, deploy/tests/installer.test.sh
.github/workflows/ci.yml
```

---

### Task 1 : Socle de solution + `Zone`

**Files :** Create `global.json`, `FastFood.slnx`, `src/FastFood/FastFood.csproj` (Web SDK, `net10.0`, `Nullable` enable, `InternalsVisibleTo FastFood.Tests`), `src/FastFood/Program.cs` (vide : `app.Run()` + `public partial class Program;`), `src/FastFood/Lieux/Zone.cs`, `tests/FastFood.Tests/FastFood.Tests.csproj`, `tests/FastFood.Tests/Lieux/ZoneShould.cs`, `.gitignore` (ajouter `bin/ obj/ *.db`).

**Interfaces — Produces :**
- `public sealed record Zone(double Sud, double Ouest, double Nord, double Est)`
- `static bool Zone.TryCreer(double? sud, double? ouest, double? nord, double? est, out Zone? zone)` — faux si un paramètre manque, sud ≥ nord, ouest ≥ est, latitudes hors [-90, 90], longitudes hors [-180, 180].
- `double PlusGrandCoteEnMetres()` — haversine, max(largeur à la latitude moyenne, hauteur).
- `bool EstTropGrande()` → `PlusGrandCoteEnMetres() > 3000`.
- `Zone ArrondieVersExterieur()` — sud/ouest arrondis vers le bas, nord/est vers le haut, au pas de 0,005°.
- `string Cle()` — `"{Sud:F3},{Ouest:F3},{Nord:F3},{Est:F3}"` en culture invariante.

- [ ] Étape 1 : tests `ZoneShould` : `RefuserUneZoneDontLeSudDepasseLeNord`, `RefuserUnParametreManquant`, `RefuserUneLatitudeHorsLimites`, `MesurerEnviron1100MetresPour0Virgule01DegreDeLatitude` (zone 48.85→48.86 : hauteur ≈ 1 112 m ± 5), `EtreTropGrandeAuDelaDe3000Metres` (48.85,2.33,48.88,2.36 → vrai), `NePasEtreTropGrandePourUneZoneDe1Km`, `SArrondirVersLExterieurAuPasDe0Virgule005` (48.8534,2.3488,48.8561,2.3512 → 48.850,2.345,48.860,2.355), `DonnerLaMemeCleAPartirDeDeuxZonesVoisinesArrondies`.
- [ ] Étape 2 : `dotnet test` → échec de compilation (Zone absente). Le noter.
- [ ] Étape 3 : implémenter `Zone`.
- [ ] Étape 4 : `dotnet test` → vert.
- [ ] Étape 5 : commit `feat: socle .NET et Zone`.

### Task 2 : `Lieu`, `RequeteOverpass`, `LecteurOverpass`

**Files :** Create `src/FastFood/Lieux/Lieu.cs`, `RequeteOverpass.cs`, `LecteurOverpass.cs` ; tests `tests/FastFood.Tests/Lieux/RequeteOverpassShould.cs`, `LecteurOverpassShould.cs` ; la fixture réelle `tests/FastFood.Tests/Fixtures/overpass-paris.json` existe déjà (14 nœuds : 10 `fast_food`, 4 `restaurant`), à copier en sortie (`CopyToOutputDirectory`).

**Interfaces — Produces :**
- `public static class Categories { public const string FastFood = "Fast-food", RestaurantRapide = "Restaurant rapide", Boulangerie = "Boulangerie"; }`
- `public sealed record Lieu(string Type, long Id, string Nom, string Categorie, string? Cuisine, double Lat, double Lon)` ; `Type` ∈ `node|way|relation`.
- `static string RequeteOverpass.Pour(Zone zone)` — `[out:json][timeout:25];(nwr["amenity"="fast_food"](s,o,n,e);nwr["amenity"="restaurant"]["cuisine"~"kebab|burger|pizza|sandwich|chicken|tacos"](s,o,n,e);nwr["shop"="bakery"](s,o,n,e););out tags center;` — nombres en culture invariante.
- `static IReadOnlyList<Lieu> LecteurOverpass.Lire(string json)` — position = `lat/lon` sinon `center.lat/lon` sinon élément ignoré ; catégorie : `shop=bakery` → Boulangerie, `amenity=fast_food` → Fast-food, `amenity=restaurant` → Restaurant rapide ; sans `tags` ou sans catégorie reconnue → ignoré ; nom absent → `"<Catégorie> sans nom"`.

- [ ] Étape 1 : tests : `RequeteOverpassShould.ContenirLesTroisCategoriesDansLaZone`, `EcrireLesCoordonneesAvecUnPointDecimal` (exécuté sous `CultureInfo("fr-FR")`) ; `LecteurOverpassShould.LireLes14LieuxDeLaReponseReelle`, `ClasserMaisonDeGyrosEnRestaurantRapide`, `ClasserSubwayEnFastFoodAvecCuisineSandwich`, `PrendreLeCentreDUnWay` (JSON en ligne : `way` 456 avec `center`), `ClasserUneBoulangerie`, `NommerUnLieuSansNom` (« Boulangerie sans nom »), `IgnorerUnElementSansPosition`, `IgnorerUnElementSansTags`.
- [ ] Étape 2 : rouge constaté.
- [ ] Étape 3 : implémenter (`System.Text.Json`, `JsonDocument`).
- [ ] Étape 4 : vert.
- [ ] Étape 5 : commit `feat: lecture des lieux OpenStreetMap`.

### Task 3 : `Note` et `DepotNotes` (SQLite)

**Files :** Create `src/FastFood/Notes/Note.cs`, `DepotNotes.cs` ; tests `tests/FastFood.Tests/Notes/NoteShould.cs`, `DepotNotesShould.cs` (base dans un fichier temporaire unique par test, supprimé au `Dispose`).

**Interfaces — Produces :**
- `public sealed record Note(string Prenom, int Etoiles, string? Commentaire, DateTimeOffset ModifieeLe)`
- `static string? Note.Valider(string? prenom, int etoiles, string? commentaire)` → `null` si valide, sinon message : « Le prénom est obligatoire (40 caractères au plus). », « La note va de 1 à 5 étoiles. », « Le commentaire fait 280 caractères au plus. »
- `public sealed record Moyenne(double Valeur, int Nombre)`
- `public interface IDepotNotes { void Enregistrer(string type, long id, string nomLieu, string prenom, int etoiles, string? commentaire); IReadOnlyList<Note> Lister(string type, long id); IReadOnlyDictionary<(string Type, long Id), Moyenne> Moyennes(IEnumerable<(string Type, long Id)> lieux); }`
- `public sealed class DepotNotes(string cheminBase, TimeProvider horloge) : IDepotNotes` — crée la table au premier usage (schéma de la spec §5, clé `(type, osm_id, prenom COLLATE NOCASE)`), `INSERT … ON CONFLICT DO UPDATE` ; prénom et commentaire `Trim()`és, commentaire vide → `NULL` ; garde la casse de la dernière saisie ; `Lister` trié par `modifiee_le` décroissant.

- [ ] Étape 1 : tests : `NoteShould.AccepterUnCommentaireDe280Caracteres`, `RefuserUnCommentaireDe281Caracteres`, `RefuserZeroEtoile`, `RefuserSixEtoiles`, `RefuserUnPrenomBlanc`, `RefuserUnPrenomDe41Caracteres` ; `DepotNotesShould.RetrouverUneNoteEnregistree`, `RemplacerLaNoteDuMemePrenomSansTenirCompteDeLaCasse` (« Lionel » 2★ puis « lionel » 4★ → 1 note, 4★), `RetirerLesEspacesDeBordDuPrenom`, `StockerUnCommentaireVideCommeAbsent`, `ListerLaPlusRecenteDAbord` (`FakeTimeProvider` ou `TimeProvider` maison), `CalculerMoyenneEtNombreParLieu` (4★ + 3★ → 3,5 / 2), `NeRienRendrePourUnLieuSansNote`.
- [ ] Étape 2 : rouge constaté.
- [ ] Étape 3 : implémenter.
- [ ] Étape 4 : vert.
- [ ] Étape 5 : commit `feat: notes en SQLite`.

### Task 4 : `ClientOverpass` et `CatalogueLieux`

**Files :** Create `src/FastFood/Lieux/ClientOverpass.cs`, `CatalogueLieux.cs` ; tests `tests/FastFood.Tests/Lieux/ClientOverpassShould.cs`, `CatalogueLieuxShould.cs` (handler HTTP factice enregistrant les requêtes).

**Interfaces — Produces :**
- `public sealed class OverpassIndisponible : Exception`
- `public interface IClientOverpass { Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct); }`
- `public sealed class ClientOverpass(HttpClient http, IReadOnlyList<Uri> instances) : IClientOverpass` — POST `data=<requête>` en `application/x-www-form-urlencoded` ; essaie chaque instance dans l'ordre ; réponse non-2xx, exception HTTP ou délai dépassé → instance suivante ; toutes en échec → `OverpassIndisponible` ; un `SemaphoreSlim(1)` statique à l'instance garantit **un seul appel Overpass à la fois**. `ClientOverpass.Instances` expose les deux URI des Global Constraints. User-Agent et délai 30 s posés sur le `HttpClient` à l'enregistrement (Tâche 5).
- `public interface ICatalogueLieux { Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct); }`
- `public sealed class CatalogueLieux(IClientOverpass client, IMemoryCache cache) : ICatalogueLieux` — cherche la zone `ArrondieVersExterieur()`, mise en cache 24 h sous `Cle()` ; une erreur n'est pas mise en cache.

- [ ] Étape 1 : tests : `ClientOverpassShould.EnvoyerLaRequeteALaPremiereInstance`, `PasserALaSecondeInstanceApresUn504`, `PasserALaSecondeInstanceApresUneErreurReseau`, `LeverOverpassIndisponibleQuandToutesEchouent`, `NeJamaisLancerDeuxAppelsSimultanes` (handler qui compte le maximum d'appels concurrents sur 3 recherches parallèles → 1) ; `CatalogueLieuxShould.NAppelerOverpassQuUneFoisPourDeuxZonesVoisines`, `RappelerOverpassApresUnEchec`.
- [ ] Étape 2 : rouge constaté.
- [ ] Étape 3 : implémenter.
- [ ] Étape 4 : vert.
- [ ] Étape 5 : commit `feat: client Overpass avec repli et cache`.

### Task 5 : API HTTP

**Files :** Create `src/FastFood/Web/RoutesApi.cs` ; Modify `src/FastFood/Program.cs` (DI, `UseDefaultFiles`, `UseStaticFiles`, `MapApi`, Kestrel sur `http://127.0.0.1:5080` par défaut via `appsettings.json` `Urls`, base `FastFood:Base` défaut `fastfood.db`) ; Create `src/FastFood/appsettings.json` ; tests `tests/FastFood.Tests/Web/ApiShould.cs` (`WebApplicationFactory<Program>`, `ICatalogueLieux` remplacé par un faux, base temporaire).

**Interfaces :** Consumes Tâches 1-4. Produces les routes de la spec §5, JSON camelCase :
- `GET /api/lieux?sud&ouest&nord&est` → `[{ type, id, nom, categorie, cuisine, lat, lon, moyenne, nombreNotes }]` (`moyenne` `null` et `nombreNotes` 0 si aucune note) ; erreurs `{ code, message }` : 400 `zone_invalide`, 422 `zone_trop_grande` « Zoome pour chercher. », 503 `osm_indisponible` « OpenStreetMap ne répond pas, réessaie dans une minute. »
- `GET /api/lieux/{type}/{id}/notes` → `[{ prenom, etoiles, commentaire, modifieeLe }]` ; `type` hors `node|way|relation` → 400.
- `PUT /api/lieux/{type}/{id}/notes/{prenom}` corps `{ etoiles, commentaire, nomLieu }` → 204 ; invalide → 400 `{ code: "note_invalide", message }`.
- `GET /api/sante` → 200 `ok`.

- [ ] Étape 1 : tests `ApiShould` : `RendreLesLieuxAvecLeurMoyenne`, `RendreMoyenneNulleSansNote`, `Repondre400SiLaZoneEstInvalide`, `Repondre422SiLaZoneEstTropGrande`, `Repondre503SiOpenStreetMapEstIndisponible`, `EnregistrerPuisListerUneNote`, `RemplacerLaNoteDeLionelSaisieEnMinuscules`, `Repondre400PourUnCommentaireTropLong`, `Repondre400PourUnTypeDeLieuInconnu`, `RepondreOkSurSante`, `ServirLaPageDAccueil` (GET `/` → 200 `text/html`, nécessite un `wwwroot/index.html` minimal créé ici).
- [ ] Étape 2 : rouge constaté.
- [ ] Étape 3 : implémenter.
- [ ] Étape 4 : vert ; puis `dotnet run --project src/FastFood` et `curl 'http://127.0.0.1:5080/api/lieux?sud=48.852&ouest=2.345&nord=48.855&est=2.350'` → JSON avec des lieux réels (Overpass peut répondre 503 : réessayer après une minute, ne pas marteler).
- [ ] Étape 5 : commit `feat: API lieux et notes`.

### Task 6 : Modules front purs

**Files :** Create `src/FastFood/wwwroot/js/zone.mjs`, `etoiles.mjs`, `prenom.mjs`, `api.mjs` ; tests `tests/ui/zone.test.mjs`, `etoiles.test.mjs`, `prenom.test.mjs`, `api.test.mjs`.

**Interfaces — Produces :**
- `zone.mjs` : `export function versParametres({sud, ouest, nord, est}) : string` (`sud=…&ouest=…&nord=…&est=…`, 5 décimales) ; `export function plusGrandCoteEnMetres(zone) : number` (même formule que `Zone`) ; `export const COTE_MAX_METRES = 3000` ; `export function estTropGrande(zone) : boolean`.
- `etoiles.mjs` : `export function resumeMoyenne(moyenne, nombre) : string` → `null`/0 → « Pas encore noté » ; 3.5, 2 → « 3,5★ · 2 notes » ; 4, 1 → « 4★ · 1 note » ; `export function libelleEpingle(moyenne) : string` → « 3,5 », « 4 », « » si null.
- `prenom.mjs` : `export function lirePrenom(stockage) : string|null`, `export function ecrirePrenom(stockage, prenom)` — clé `fastfood.prenom` ; `stockage` absent ou qui lève → `null` / sans effet.
- `api.mjs` : `export function creerApi(fetchFn)` → `{ chercherLieux(zone), listerNotes(type, id), enregistrerNote(type, id, prenom, {etoiles, commentaire, nomLieu}) }` ; réponse non-2xx avec `{message}` → `Error(message)` ; erreur réseau → `Error("Connexion impossible, réessaie.")` ; prénom encodé par `encodeURIComponent`.

- [ ] Étape 1 : tests `node:test` + `node:assert/strict` couvrant chaque exemple ci-dessus, plus `prenom.test.mjs` : `stockage qui lève → null`, `api.test.mjs` : `encode « Zoé B. » dans l'URL`, `remonte le message 422`.
- [ ] Étape 2 : `node --test tests/ui/` → rouge constaté.
- [ ] Étape 3 : implémenter.
- [ ] Étape 4 : vert.
- [ ] Étape 5 : commit `feat: modules front purs`.

### Task 7 : PWA — carte, fiche, notation

**Files :** Create `src/FastFood/wwwroot/index.html`, `css/app.css`, `css/colors_and_type.css` (copie exacte du DS), `vendor/leaflet/leaflet.js|leaflet.css|images/*` (Leaflet 1.9.4 depuis `https://unpkg.com/leaflet@1.9.4/dist/`, avec son `LICENSE`), `js/carte.mjs`, `js/fiche.mjs`, `js/app.mjs`.

**Interfaces :** Consumes Tâche 6. Comportement exigé (spec §3, §5, §6) :
- Carte plein écran, fond `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png` avec attribution « © OpenStreetMap ». Démarrage : `navigator.geolocation.getCurrentPosition` (délai 10 s) → zoom 16 sur la position et recherche automatique ; refus/erreur → centre 48.8534, 2.3488, zoom 16, toast « Position indisponible : déplace la carte », recherche automatique.
- Après `moveend` : bouton « Chercher ici » (`--accent`) ; si zone trop grande, il est remplacé par l'indication « Zoome pour chercher » et aucune requête ne part.
- Épingles `L.divIcon` rondes, couleur par catégorie (`--tag-1` Fast-food, `--tag-2` Restaurant rapide, `--tag-3` Boulangerie), `libelleEpingle(moyenne)` dedans ; légende compacte des 3 couleurs.
- Bouton « ma position » (bas droite) recentre.
- Fiche (feuille du bas) : nom, catégorie · cuisine, `resumeMoyenne`, notes (prénom, étoiles, commentaire, date fr), formulaire « Ma note » prérempli avec la note existante du prénom ; 5 étoiles boutons 44 px, `textarea maxlength=280` + compteur « n / 280 » ; « Enregistrer ma note » (`--accent`). Sans prénom mémorisé : champ « Ton prénom ? » dans la fiche avant d'enregistrer ; lien « Pas Lionel ? » pour changer de prénom. Après enregistrement : toast « Note enregistrée », fiche et épingle mises à jour.
- Toute donnée venue de l'API insérée par `textContent`.
- Erreurs : toast DS (fond `--ink-900`, texte blanc, bas, 2,5 s) avec le message d'`api.mjs`.
- `env(safe-area-inset-*)` respecté ; `<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">`.

- [ ] Étape 1 : le test `ApiShould.ServirLaPageDAccueil` reste vert ; ajouter `ApiShould.ServirLeCssDuDesignSystem` (GET `/css/colors_and_type.css` → 200) et le voir rouge avant la copie.
- [ ] Étape 2 : implémenter.
- [ ] Étape 3 : lancer l'app, ouvrir `http://127.0.0.1:5080/` en vue iPhone 390×844 (Playwright via `npx playwright` ou Chrome headless avec géolocalisation simulée 48.8534, 2.3488), capturer : carte avec épingles, fiche ouverte, note enregistrée. **Regarder** les captures (outil Read) et corriger ce qui cloche.
- [ ] Étape 4 : commit `feat: carte et notation`.

### Task 8 : Installable (PWA)

**Files :** Create `src/FastFood/wwwroot/manifest.webmanifest` (`name` « FastFood », `short_name` « FastFood », `display` `standalone`, `start_url` `/`, `theme_color`/`background_color` = valeurs hex de `--brand` et `--bg-0` du DS), `icons/icon-180.png`, `icon-192.png`, `icon-512.png` (générées : pastille `--accent` + burger stylisé ou « FF », lisibles), `sw.js` (met en cache l'enveloppe : `/`, css, js, vendor ; réseau d'abord pour `/api/` sans mise en cache ; version de cache dans une constante) ; Modify `index.html` (`link rel=manifest`, `apple-touch-icon`, `apple-mobile-web-app-capable`, `apple-mobile-web-app-title`, enregistrement du SW).

- [ ] Étape 1 : test `ApiShould.ServirLeManifesteAvecLeBonType` (GET `/manifest.webmanifest` → 200, `Content-Type` `application/manifest+json`) rouge.
- [ ] Étape 2 : implémenter (mapping MIME `.webmanifest` dans `Program.cs`).
- [ ] Étape 3 : vert ; capture de l'icône regardée.
- [ ] Étape 4 : commit `feat: PWA installable`.

### Task 9 : Installateur Mac mini + CI

**Files :** Create `deploy/lib.sh` (fonctions pures : `charger_configuration` avec `FASTFOOD_LABEL` défaut `net.linkeat.fastfood`, `FASTFOOD_PORT` défaut 5080, `FASTFOOD_DONNEES` défaut `$HOME/Library/Application Support/FastFood`, `FASTFOOD_JOURNAUX` défaut `$HOME/Library/Logs/FastFood` ; `rid_pour_arch` ; `echapper_xml` ; `generer_plist label app base port journal` → plist avec `ProgramArguments` = `$app/FastFood`, `EnvironmentVariables` `ASPNETCORE_URLS=http://127.0.0.1:$port`, `FastFood__Base=$base`, `RunAtLoad`, `KeepAlive`, `StandardOutPath`/`StandardErrorPath` = journal), `deploy/installer.sh` (préalables, refus si le port est tenu par un autre processus, `dotnet publish src/FastFood -c Release -r $RID --self-contained -o app.nouveau`, bascule bootout/bootstrap, santé `GET /api/sante` 60 s, affiche à la fin la commande `tailscale serve --bg --https=8443 http://127.0.0.1:5080`), `deploy/tests/installer.test.sh` (tests bash des fonctions pures : plist contient le port 5080, échappe `&`, rid arm64/x86_64, refus d'un chemin relatif), `.github/workflows/ci.yml` (`on: pull_request` et `push` sur `main` ; ubuntu-latest ; `actions/setup-dotnet` 10.0.x ; `actions/setup-node` 24 ; `dotnet build -c Release`, `dotnet test -c Release`, `node --test tests/ui/`, `bash deploy/tests/installer.test.sh`), `README.md` (court : lancer, tester, installer).

- [ ] Étape 1 : `deploy/tests/installer.test.sh` écrit et exécuté → rouge (lib absente).
- [ ] Étape 2 : implémenter `lib.sh`, `installer.sh`, CI, README.
- [ ] Étape 3 : `bash deploy/tests/installer.test.sh` vert ; `bash -n deploy/installer.sh` ; `FASTFOOD_DONNEES=<scratch> FASTFOOD_JOURNAUX=<scratch> FASTFOOD_LABEL=net.linkeat.fastfood.essai FASTFOOD_PORT=5081` **ne pas lancer** (launchctl refusé en mode auto) — l'installation réelle est lancée par Lionel.
- [ ] Étape 4 : commit `feat: installateur Mac mini et CI`.
