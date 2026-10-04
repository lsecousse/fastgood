const DUREE_MS = 2500;

export function creerToast(element) {
  let minuteur;
  return (message) => {
    element.textContent = message;
    element.hidden = false;
    clearTimeout(minuteur);
    minuteur = setTimeout(() => { element.hidden = true; }, DUREE_MS);
  };
}
