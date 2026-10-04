namespace FastFood.Lieux;

public static class Categories
{
    public const string FastFood = "Fast-food";
    public const string RestaurantRapide = "Restaurant rapide";
    public const string Boulangerie = "Boulangerie";
}

public sealed record Lieu(string Type, long Id, string Nom, string Categorie, string? Cuisine, double Lat, double Lon);
