export const COMMENTAIRE_MAX = 280;

const FORMAT_DATE = new Intl.DateTimeFormat("fr-FR", {
  day: "numeric", month: "long", year: "numeric", timeZone: "Europe/Paris",
});

export function dateEnFrancais(iso) {
  return FORMAT_DATE.format(new Date(iso));
}

export function trouverMaNote(notes, prenom) {
  const cle = normaliser(prenom);
  if (!cle) return null;
  return notes.find((note) => normaliser(note.prenom) === cle) ?? null;
}

export function compteur(texte) {
  return `${texte.length} / ${COMMENTAIRE_MAX}`;
}

export function etoilesEnTexte(etoiles) {
  return "★".repeat(etoiles) + "☆".repeat(5 - etoiles);
}

export function moyenneDesNotes(notes) {
  if (notes.length === 0) return { moyenne: null, nombreNotes: 0 };
  const total = notes.reduce((somme, note) => somme + note.etoiles, 0);
  return { moyenne: total / notes.length, nombreNotes: notes.length };
}

export function libelleCategorie(categorie, cuisine) {
  if (!cuisine) return categorie;
  return `${categorie} · ${cuisine.replaceAll(";", ", ").replaceAll("_", " ")}`;
}

function normaliser(prenom) {
  return (prenom ?? "").trim().toLowerCase();
}
