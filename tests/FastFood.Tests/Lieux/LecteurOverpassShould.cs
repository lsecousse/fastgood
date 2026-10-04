namespace FastFood.Tests.Lieux;

public class LecteurOverpassShould
{
    private static string Reponse(string elements) => "{\"elements\":[" + elements + "]}";

    private static string Fixture() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "overpass-paris.json"));

    [Fact]
    public void LireLes14LieuxDeLaReponseReelle()
    {
        var lieux = LecteurOverpass.Lire(Fixture());

        Assert.Equal(14, lieux.Count);
    }

    [Fact]
    public void ClasserMaisonDeGyrosEnRestaurantRapide()
    {
        var lieux = LecteurOverpass.Lire(Fixture());

        var lieu = lieux.Single(l => l.Nom == "Maison de Gyros");

        Assert.Equal(Categories.RestaurantRapide, lieu.Categorie);
    }

    [Fact]
    public void ClasserSubwayEnFastFoodAvecCuisineSandwich()
    {
        var lieux = LecteurOverpass.Lire(Fixture());

        var lieu = lieux.Single(l => l.Nom == "Subway");

        Assert.Equal((Categories.FastFood, "sandwich"), (lieu.Categorie, lieu.Cuisine));
    }

    [Fact]
    public void PrendreLeCentreDUnWay()
    {
        var json = Reponse("{\"type\":\"way\",\"id\":456,\"center\":{\"lat\":48.1,\"lon\":2.1},\"tags\":{\"amenity\":\"fast_food\",\"name\":\"Chez Way\"}}");

        var lieu = Assert.Single(LecteurOverpass.Lire(json));

        Assert.Equal(new Lieu("way", 456, "Chez Way", Categories.FastFood, null, 48.1, 2.1), lieu);
    }

    [Fact]
    public void ClasserUneBoulangerie()
    {
        var json = Reponse("{\"type\":\"node\",\"id\":1,\"lat\":48.1,\"lon\":2.1,\"tags\":{\"shop\":\"bakery\",\"name\":\"Le Fournil\"}}");

        var lieu = Assert.Single(LecteurOverpass.Lire(json));

        Assert.Equal(Categories.Boulangerie, lieu.Categorie);
    }

    [Fact]
    public void NommerUnLieuSansNom()
    {
        var json = Reponse("{\"type\":\"node\",\"id\":1,\"lat\":48.1,\"lon\":2.1,\"tags\":{\"shop\":\"bakery\"}}");

        var lieu = Assert.Single(LecteurOverpass.Lire(json));

        Assert.Equal("Boulangerie sans nom", lieu.Nom);
    }

    [Fact]
    public void IgnorerUnElementSansPosition()
    {
        var json = Reponse("{\"type\":\"way\",\"id\":1,\"tags\":{\"amenity\":\"fast_food\",\"name\":\"Perdu\"}}");

        Assert.Empty(LecteurOverpass.Lire(json));
    }

    [Fact]
    public void IgnorerUnElementSansTags()
    {
        var json = Reponse("{\"type\":\"node\",\"id\":1,\"lat\":48.1,\"lon\":2.1}");

        Assert.Empty(LecteurOverpass.Lire(json));
    }
}
