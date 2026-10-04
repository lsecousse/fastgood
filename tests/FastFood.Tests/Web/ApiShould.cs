using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FastFood.Tests.Web;

public sealed class ApiShould : IDisposable
{
    private const string ZoneValide = "sud=48.852&ouest=2.345&nord=48.855&est=2.350";
    private const string ZoneTropGrande = "sud=48.80&ouest=2.30&nord=48.90&est=2.40";

    private static readonly Lieu Quick = new("node", 1, "Quick", Categories.FastFood, "burger", 48.853, 2.347);

    private readonly string _base = Path.Combine(Path.GetTempPath(), $"fastfood-api-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program>? _usine;

    private sealed class CatalogueFactice(Func<IReadOnlyList<Lieu>> reponse) : ICatalogueLieux
    {
        public Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct) => Task.FromResult(reponse());
    }

    private sealed class DepotEspion : IDepotNotes
    {
        public string? DernierNomLieu { get; private set; }

        public void Enregistrer(string type, long id, string nomLieu, string prenom, int etoiles, string? commentaire) =>
            DernierNomLieu = nomLieu;

        public IReadOnlyList<Note> Lister(string type, long id) => [];

        public IReadOnlyDictionary<(string Type, long Id), Moyenne> Moyennes(IEnumerable<(string Type, long Id)> lieux) =>
            new Dictionary<(string Type, long Id), Moyenne>();
    }

    private HttpClient Client(Func<IReadOnlyList<Lieu>>? catalogue = null, Action<IServiceCollection>? surcharger = null)
    {
        var lieux = catalogue ?? (() => [Quick]);
        _usine = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("FastFood:Base", _base);
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<ICatalogueLieux>(new CatalogueFactice(lieux));
                surcharger?.Invoke(s);
            });
        });
        return _usine.CreateClient();
    }

    private static StringContent Corps(string json) => new(json, System.Text.Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Json(HttpResponseMessage reponse) =>
        JsonDocument.Parse(await reponse.Content.ReadAsStringAsync()).RootElement;

    public void Dispose()
    {
        _usine?.Dispose();
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_base) + "*"))
            File.Delete(f);
    }

    [Fact]
    public async Task ServirLeManifesteAvecLeBonType()
    {
        var client = Client();

        var reponse = await client.GetAsync("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("application/manifest+json", reponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task RendreLesLieuxAvecLeurMoyenne()
    {
        var client = Client();
        await client.PutAsync("/api/lieux/node/1/notes/Lionel", Corps("""{"etoiles":4,"nomLieu":"Quick"}"""));
        await client.PutAsync("/api/lieux/node/1/notes/Marie", Corps("""{"etoiles":3,"nomLieu":"Quick"}"""));

        var reponse = await client.GetAsync($"/api/lieux?{ZoneValide}");

        var lieu = (await Json(reponse))[0];
        Assert.Equal(("node", 1, "Quick", 3.5, 2),
            (lieu.GetProperty("type").GetString(), lieu.GetProperty("id").GetInt64(), lieu.GetProperty("nom").GetString(),
             lieu.GetProperty("moyenne").GetDouble(), lieu.GetProperty("nombreNotes").GetInt32()));
    }

    [Fact]
    public async Task RendreMoyenneNulleSansNote()
    {
        var client = Client();

        var reponse = await client.GetAsync($"/api/lieux?{ZoneValide}");

        var lieu = (await Json(reponse))[0];
        Assert.Equal((JsonValueKind.Null, 0),
            (lieu.GetProperty("moyenne").ValueKind, lieu.GetProperty("nombreNotes").GetInt32()));
    }

    [Fact]
    public async Task Repondre400SiLaZoneEstInvalide()
    {
        var client = Client();

        var reponse = await client.GetAsync("/api/lieux?sud=48.9&ouest=2.3&nord=48.8&est=2.4");

        Assert.Equal((HttpStatusCode.BadRequest, "zone_invalide"),
            (reponse.StatusCode, (await Json(reponse)).GetProperty("code").GetString()));
    }

    [Fact]
    public async Task Repondre422SiLaZoneEstTropGrande()
    {
        var client = Client();

        var reponse = await client.GetAsync($"/api/lieux?{ZoneTropGrande}");

        var corps = await Json(reponse);
        Assert.Equal((HttpStatusCode.UnprocessableEntity, "zone_trop_grande", "Zoome pour chercher."),
            (reponse.StatusCode, corps.GetProperty("code").GetString(), corps.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Repondre503SiOpenStreetMapEstIndisponible()
    {
        var client = Client(() => throw new OverpassIndisponible());

        var reponse = await client.GetAsync($"/api/lieux?{ZoneValide}");

        var corps = await Json(reponse);
        Assert.Equal((HttpStatusCode.ServiceUnavailable, "osm_indisponible", "OpenStreetMap ne répond pas, réessaie dans une minute."),
            (reponse.StatusCode, corps.GetProperty("code").GetString(), corps.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task EnregistrerPuisListerUneNote()
    {
        var client = Client();

        var put = await client.PutAsync("/api/lieux/node/1/notes/Lionel", Corps("""{"etoiles":5,"commentaire":"Top","nomLieu":"Quick"}"""));
        var notes = await Json(await client.GetAsync("/api/lieux/node/1/notes"));

        Assert.Equal((HttpStatusCode.NoContent, "Lionel", 5, "Top"),
            (put.StatusCode, notes[0].GetProperty("prenom").GetString(), notes[0].GetProperty("etoiles").GetInt32(),
             notes[0].GetProperty("commentaire").GetString()));
    }

    [Fact]
    public async Task RemplacerLaNoteDeLionelSaisieEnMinuscules()
    {
        var client = Client();
        await client.PutAsync("/api/lieux/node/1/notes/Lionel", Corps("""{"etoiles":2,"nomLieu":"Quick"}"""));
        await client.PutAsync("/api/lieux/node/1/notes/lionel", Corps("""{"etoiles":4,"nomLieu":"Quick"}"""));

        var notes = await Json(await client.GetAsync("/api/lieux/node/1/notes"));

        Assert.Equal((1, 4), (notes.GetArrayLength(), notes[0].GetProperty("etoiles").GetInt32()));
    }

    [Fact]
    public async Task TronquerLeNomDuLieuA200Caracteres()
    {
        var depot = new DepotEspion();
        var client = Client(surcharger: s => s.AddSingleton<IDepotNotes>(depot));

        await client.PutAsJsonAsync("/api/lieux/node/1/notes/Lionel", new { etoiles = 3, nomLieu = "  " + new string('a', 250) });

        Assert.Equal(new string('a', 200), depot.DernierNomLieu);
    }

    [Fact]
    public async Task Repondre400PourUnCommentaireTropLong()
    {
        var client = Client();
        var commentaire = new string('a', 281);

        var reponse = await client.PutAsJsonAsync("/api/lieux/node/1/notes/Lionel", new { etoiles = 3, commentaire, nomLieu = "Quick" });

        Assert.Equal((HttpStatusCode.BadRequest, "note_invalide"),
            (reponse.StatusCode, (await Json(reponse)).GetProperty("code").GetString()));
    }

    [Fact]
    public async Task Repondre400PourUnTypeDeLieuInconnu()
    {
        var client = Client();

        var reponse = await client.GetAsync("/api/lieux/chose/1/notes");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task RepondreOkSurSante()
    {
        var client = Client();

        var reponse = await client.GetAsync("/api/sante");

        Assert.Equal((HttpStatusCode.OK, "ok"), (reponse.StatusCode, await reponse.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task ServirLaPageDAccueil()
    {
        var client = Client();

        var reponse = await client.GetAsync("/");

        Assert.Equal((HttpStatusCode.OK, "text/html"), (reponse.StatusCode, reponse.Content.Headers.ContentType?.MediaType));
    }

    [Fact]
    public async Task ServirLeCssDuDesignSystem()
    {
        var client = Client();

        var reponse = await client.GetAsync("/css/colors_and_type.css");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }
}
