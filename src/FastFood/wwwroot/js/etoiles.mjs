const enFrancais = (nombre) => String(nombre).replace(".", ",");

export function resumeMoyenne(moyenne, nombre) {
  if (moyenne == null || !nombre) return "Pas encore noté";
  return `${enFrancais(moyenne)}★ · ${nombre} ${nombre > 1 ? "notes" : "note"}`;
}

export function libelleEpingle(moyenne) {
  return moyenne == null ? "" : enFrancais(moyenne);
}
