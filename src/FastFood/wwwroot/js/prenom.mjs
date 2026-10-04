const CLE = "fastfood.prenom";

export function lirePrenom(stockage) {
  try {
    return stockage?.getItem(CLE) ?? null;
  } catch {
    return null;
  }
}

export function ecrirePrenom(stockage, prenom) {
  try {
    stockage?.setItem(CLE, prenom);
  } catch {
    // stockage indisponible : le prénom ne sera simplement pas mémorisé
  }
}
