using System.Text.Json;

namespace FastFood.Lieux;

public static class LecteurOverpass
{
    public static IReadOnlyList<Lieu> Lire(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("elements").EnumerateArray()
            .Select(LireElement)
            .OfType<Lieu>()
            .ToList();
    }

    private static Lieu? LireElement(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags)) return null;
        var categorie = Categorie(tags);
        var position = Position(element);
        if (categorie is null || position is null) return null;

        var nom = Etiquette(tags, "name") ?? $"{categorie} sans nom";
        return new Lieu(
            element.GetProperty("type").GetString()!,
            element.GetProperty("id").GetInt64(),
            nom,
            categorie,
            Etiquette(tags, "cuisine"),
            position.Value.Lat,
            position.Value.Lon);
    }

    private static string? Categorie(JsonElement tags)
    {
        if (Etiquette(tags, "shop") == "bakery") return Categories.Boulangerie;
        return Etiquette(tags, "amenity") switch
        {
            "fast_food" => Categories.FastFood,
            "restaurant" => Categories.RestaurantRapide,
            _ => null
        };
    }

    private static (double Lat, double Lon)? Position(JsonElement element)
    {
        if (element.TryGetProperty("lat", out _)) return Coordonnees(element);
        if (element.TryGetProperty("center", out var centre)) return Coordonnees(centre);
        return null;
    }

    private static (double Lat, double Lon) Coordonnees(JsonElement source) =>
        (source.GetProperty("lat").GetDouble(), source.GetProperty("lon").GetDouble());

    private static string? Etiquette(JsonElement tags, string cle) =>
        tags.TryGetProperty(cle, out var valeur) ? valeur.GetString() : null;
}
