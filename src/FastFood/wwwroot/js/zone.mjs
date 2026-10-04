const RAYON_TERRE_EN_METRES = 6_371_000;
export const COTE_MAX_METRES = 3000;

export function versParametres({ sud, ouest, nord, est }) {
  const p = (v) => v.toFixed(5);
  return `sud=${p(sud)}&ouest=${p(ouest)}&nord=${p(nord)}&est=${p(est)}`;
}

export function plusGrandCoteEnMetres({ sud, ouest, nord, est }) {
  const latitudeMoyenne = (sud + nord) / 2;
  const largeur = haversine(latitudeMoyenne, ouest, latitudeMoyenne, est);
  const hauteur = haversine(sud, ouest, nord, ouest);
  return Math.max(largeur, hauteur);
}

export function estTropGrande(zone) {
  return plusGrandCoteEnMetres(zone) > COTE_MAX_METRES;
}

function haversine(lat1, lon1, lat2, lon2) {
  const dLat = radians(lat2 - lat1);
  const dLon = radians(lon2 - lon1);
  const a = Math.sin(dLat / 2) ** 2
    + Math.cos(radians(lat1)) * Math.cos(radians(lat2)) * Math.sin(dLon / 2) ** 2;
  return 2 * RAYON_TERRE_EN_METRES * Math.asin(Math.sqrt(a));
}

function radians(degres) {
  return (degres * Math.PI) / 180;
}
