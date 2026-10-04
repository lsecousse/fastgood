using Microsoft.Extensions.Caching.Memory;

namespace FastFood.Lieux;

public interface ICatalogueLieux
{
    Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct);
}

public sealed class CatalogueLieux(IClientOverpass client, IMemoryCache cache) : ICatalogueLieux
{
    private static readonly TimeSpan DureeDeCache = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct)
    {
        var zoneArrondie = zone.ArrondieVersExterieur();
        if (cache.TryGetValue(zoneArrondie.Cle(), out IReadOnlyList<Lieu>? enCache) && enCache is not null) return enCache;

        var lieux = await client.Chercher(zoneArrondie, ct);
        cache.Set(zoneArrondie.Cle(), lieux, DureeDeCache);
        return lieux;
    }
}
