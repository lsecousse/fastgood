import { versParametres } from "./zone.mjs";

const ERREUR_RESEAU = "Connexion impossible, réessaie.";
const ERREUR_SERVEUR = "Erreur du serveur, réessaie.";

export function creerApi(fetchFn) {
  const lieu = (type, id) => `/api/lieux/${type}/${id}/notes`;

  return {
    chercherLieux: (zone) => appeler(fetchFn, `/api/lieux?${versParametres(zone)}`),
    listerNotes: (type, id) => appeler(fetchFn, lieu(type, id)),
    enregistrerNote: (type, id, prenom, note) =>
      appeler(fetchFn, `${lieu(type, id)}/${encodeURIComponent(prenom)}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(note),
      }),
  };
}

async function appeler(fetchFn, url, options) {
  const reponse = await envoyer(fetchFn, url, options);
  if (!reponse.ok) throw new Error(await messageDErreur(reponse));
  return reponse.status === 204 ? undefined : reponse.json();
}

async function envoyer(fetchFn, url, options) {
  try {
    return await fetchFn(url, options);
  } catch {
    throw new Error(ERREUR_RESEAU);
  }
}

async function messageDErreur(reponse) {
  try {
    return (await reponse.json()).message || ERREUR_SERVEUR;
  } catch {
    return ERREUR_SERVEUR;
  }
}
