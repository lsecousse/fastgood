import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const CSS = "src/FastFood/wwwroot/css";
const ds = readFileSync(`${CSS}/colors_and_type.css`, "utf8");
const app = readFileSync(`${CSS}/app.css`, "utf8");
const CATEGORIES = ["fast-food", "restaurant-rapide", "boulangerie"];

const variables = new Map([...ds.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map((m) => [m[1], m[2].trim()]));
const resoudre = (valeur) => {
  const reference = valeur.match(/^var\((--[\w-]+)\)$/);
  return reference ? resoudre(variables.get(reference[1])) : valeur;
};
const regle = (categorie) => app.match(new RegExp(`\\.categorie-${categorie} \\{([^}]*)\\}`))[1];
const propriete = (categorie, nom) => resoudre(regle(categorie).match(new RegExp(`(?:^|[\\s;])${nom}:\\s*([^;]+);`))[1].trim());

const lineaire = (c) => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
const depuisHex = (hex) => [1, 3, 5].map((i) => lineaire(parseInt(hex.slice(i, i + 2), 16) / 255));
const depuisOklch = (texte) => {
  const [l, c, h] = texte.match(/oklch\(([^)]+)\)/)[1].trim().split(/\s+/).map(Number);
  const a = c * Math.cos((h * Math.PI) / 180);
  const b = c * Math.sin((h * Math.PI) / 180);
  const [lc, mc, sc] = [l + 0.3963377774 * a + 0.2158037573 * b, l - 0.1055613458 * a - 0.0638541728 * b,
    l - 0.0894841775 * a - 1.291485548 * b].map((x) => x ** 3);
  return [4.0767416621 * lc - 3.3077115913 * mc + 0.2309699292 * sc,
    -1.2684380046 * lc + 2.6097574011 * mc - 0.3413193965 * sc,
    -0.0041960863 * lc - 0.7034186147 * mc + 1.707614701 * sc];
};
const luminance = (couleur) => {
  const [r, g, b] = couleur.startsWith("#") ? depuisHex(couleur) : depuisOklch(couleur);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};
const contraste = (x, y) => {
  const [claire, sombre] = [luminance(x), luminance(y)].sort((p, q) => q - p);
  return (claire + 0.05) / (sombre + 0.05);
};

for (const categorie of CATEGORIES) {
  test(`EpingleShould.OffrirUnContrasteDAuMoins4Virgule5PourLaCategorie_${categorie}`, () => {
    const ratio = contraste(propriete(categorie, "background"), propriete(categorie, "color"));
    assert.ok(ratio >= 4.5, `contraste ${ratio.toFixed(2)} pour ${categorie}`);
  });

  test(`EpingleShould.NePasReprendreLOrangeDeLActionPrincipale_${categorie}`, () => {
    assert.notEqual(propriete(categorie, "background"), resoudre("var(--accent)"));
  });
}
