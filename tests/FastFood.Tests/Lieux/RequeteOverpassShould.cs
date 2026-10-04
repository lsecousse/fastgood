using System.Globalization;

namespace FastFood.Tests.Lieux;

public class RequeteOverpassShould
{
    [Fact]
    public void ContenirLesTroisCategoriesDansLaZone()
    {
        var zone = new Zone(48.85, 2.33, 48.86, 2.34);

        var requete = RequeteOverpass.Pour(zone);

        Assert.Equal(
            "[out:json][timeout:25];(" +
            "nwr[\"amenity\"=\"fast_food\"](48.85,2.33,48.86,2.34);" +
            "nwr[\"amenity\"=\"restaurant\"][\"cuisine\"~\"kebab|burger|pizza|sandwich|chicken|tacos\"](48.85,2.33,48.86,2.34);" +
            "nwr[\"shop\"=\"bakery\"](48.85,2.33,48.86,2.34);" +
            ");out tags center;",
            requete);
    }

    [Fact]
    public void EcrireLesCoordonneesAvecUnPointDecimal()
    {
        var zone = new Zone(48.85, 2.33, 48.86, 2.34);
        var cultureInitiale = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        try
        {
            var requete = RequeteOverpass.Pour(zone);

            Assert.Contains("(48.85,2.33,48.86,2.34)", requete);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureInitiale;
        }
    }
}
