# FastFood

Petite application perso : une carte des fast-foods, restaurants rapides et boulangeries, notés entre proches. PWA pour iPhone, servie par une app .NET 10 (API + `wwwroot`) avec une base SQLite.

## Lancer en local

```bash
dotnet run --project src/FastFood
```

Puis ouvrir http://127.0.0.1:5080/. La base est `fastfood.db` dans le dossier courant (changer avec `FastFood__Base`).

## Tests

```bash
dotnet test
node --test "tests/ui/*.test.mjs"
bash deploy/tests/installer.test.sh
```

## Installer sur le Mac mini

```bash
bash deploy/installer.sh
```

L'installateur publie l'app, la charge comme LaunchAgent (`net.linkeat.fastfood`, port 5080), vérifie `/api/sante` et revient à la version précédente si la nouvelle ne répond pas. Pour l'exposer sur le tailnet :

```bash
tailscale serve --bg --https=8443 http://127.0.0.1:5080
```

Adresse : https://macmini.tail01f6f6.ts.net:8443/ — sur iPhone, Safari › Partager › Sur l'écran d'accueil.

## Données et licences

- Lieux : © OpenStreetMap contributors, licence ODbL.
- Carte : [Leaflet](https://leafletjs.com/), licence BSD-2.
