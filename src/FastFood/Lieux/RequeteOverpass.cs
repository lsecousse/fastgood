using System.Globalization;

namespace FastFood.Lieux;

public static class RequeteOverpass
{
    private const string CuisinesRapides = "kebab|burger|pizza|sandwich|chicken|tacos";

    public static string Pour(Zone zone)
    {
        var boite = string.Create(CultureInfo.InvariantCulture, $"({zone.Sud},{zone.Ouest},{zone.Nord},{zone.Est})");
        return "[out:json][timeout:25];(" +
               $"nwr[\"amenity\"=\"fast_food\"]{boite};" +
               $"nwr[\"amenity\"=\"restaurant\"][\"cuisine\"~\"{CuisinesRapides}\"]{boite};" +
               $"nwr[\"shop\"=\"bakery\"]{boite};" +
               ");out tags center;";
    }
}
