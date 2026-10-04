using Microsoft.Extensions.Caching.Memory;

namespace FastFood.Tests.Lieux;

public class CatalogueLieuxShould
{
    private sealed class ClientFactice : IClientOverpass
    {
        public List<Zone> Zones { get; } = [];
        public bool Echoue { get; set; }

        public Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct)
        {
            Zones.Add(zone);
            return Echoue
                ? throw new OverpassIndisponible()
                : Task.FromResult<IReadOnlyList<Lieu>>([new Lieu("node", 1, "Chez Test", Categories.FastFood, null, 48.85, 2.34)]);
        }
    }

    private readonly ClientFactice _client = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private CatalogueLieux Catalogue() => new(_client, _cache);

    [Fact]
    public async Task NAppelerOverpassQuUneFoisPourDeuxZonesVoisines()
    {
        var catalogue = Catalogue();

        await catalogue.Chercher(new Zone(48.8501, 2.3401, 48.8601, 2.3501), CancellationToken.None);
        await catalogue.Chercher(new Zone(48.8509, 2.3409, 48.8609, 2.3509), CancellationToken.None);

        Assert.Single(_client.Zones);
    }

    [Fact]
    public async Task ChercherLaZoneArrondieVersExterieur()
    {
        var zone = new Zone(48.8501, 2.3401, 48.8601, 2.3501);

        await Catalogue().Chercher(zone, CancellationToken.None);

        Assert.Equal(zone.ArrondieVersExterieur(), Assert.Single(_client.Zones));
    }

    [Fact]
    public async Task RappelerOverpassApresUnEchec()
    {
        var catalogue = Catalogue();
        var zone = new Zone(48.850, 2.340, 48.860, 2.350);
        _client.Echoue = true;
        await Assert.ThrowsAsync<OverpassIndisponible>(() => catalogue.Chercher(zone, CancellationToken.None));
        _client.Echoue = false;

        var lieux = await catalogue.Chercher(zone, CancellationToken.None);

        Assert.Single(lieux);
        Assert.Equal(2, _client.Zones.Count);
    }
}
