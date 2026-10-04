using System.Globalization;

namespace FastFood.Lieux;

public sealed record Zone(double Sud, double Ouest, double Nord, double Est)
{
    private const double RayonTerreEnMetres = 6_371_000;
    private const double PlusGrandCoteMaxEnMetres = 3000;
    private const double PasDeGrille = 0.005;

    public static bool TryCreer(double? sud, double? ouest, double? nord, double? est, out Zone? zone)
    {
        zone = null;
        if (sud is null || ouest is null || nord is null || est is null) return false;
        if (!(sud < nord) || !(ouest < est)) return false;
        if (!DansLesBornes(sud.Value, nord.Value, 90) || !DansLesBornes(ouest.Value, est.Value, 180)) return false;

        zone = new Zone(sud.Value, ouest.Value, nord.Value, est.Value);
        return true;
    }

    public double PlusGrandCoteEnMetres()
    {
        var latitudeMoyenne = (Sud + Nord) / 2;
        var largeur = Haversine(latitudeMoyenne, Ouest, latitudeMoyenne, Est);
        var hauteur = Haversine(Sud, Ouest, Nord, Ouest);
        return Math.Max(largeur, hauteur);
    }

    public bool EstTropGrande() => PlusGrandCoteEnMetres() > PlusGrandCoteMaxEnMetres;

    public Zone ArrondieVersExterieur() =>
        new(Grille(Sud, Math.Floor), Grille(Ouest, Math.Floor), Grille(Nord, Math.Ceiling), Grille(Est, Math.Ceiling));

    public string Cle() => string.Create(CultureInfo.InvariantCulture, $"{Sud:F3},{Ouest:F3},{Nord:F3},{Est:F3}");

    private static bool DansLesBornes(double min, double max, double limite) => min >= -limite && max <= limite;

    // Le quotient est arrondi à 6 décimales pour absorber le bruit binaire (48.85 / 0.005 = 9769.999…).
    private static double Grille(double valeur, Func<double, double> arrondi) =>
        Math.Round(arrondi(Math.Round(valeur / PasDeGrille, 6)) * PasDeGrille, 3);

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = Radians(lat2 - lat1);
        var dLon = Radians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2)
              + Math.Cos(Radians(lat1)) * Math.Cos(Radians(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * RayonTerreEnMetres * Math.Asin(Math.Sqrt(a));
    }

    private static double Radians(double degres) => degres * Math.PI / 180;
}
