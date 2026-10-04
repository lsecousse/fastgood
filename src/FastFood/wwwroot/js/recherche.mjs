// Une seule recherche de lieux à la fois : la nouvelle annule la précédente.
export function creerRechercheUnique(chercherFn) {
  let precedente;
  return (zone) => {
    precedente?.abort();
    precedente = new AbortController();
    return chercherFn(zone, precedente.signal);
  };
}

export const estAnnulation = (erreur) => erreur?.name === "AbortError";
