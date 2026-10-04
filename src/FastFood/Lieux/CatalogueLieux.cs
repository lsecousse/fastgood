using Microsoft.Extensions.Caching.Memory;

namespace FastFood.Lieux;

public interface ICatalogueLieux
{
    Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct);
}

public sealed class CatalogueLieux(IClientOverpass client, IMemoryCache cache) : ICatalogueLieux
{
    private static readonly TimeSpan DureeDeCache = TimeSpan.FromHours(24);

    private readonly SemaphoreSlim _uneRechercheALaFois = new(1, 1);

    public async Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct)
    {
        var zoneArrondie = zone.ArrondieVersExterieur();
        var cle = zoneArrondie.Cle();
        if (EnCache(cle) is { } lieux) return lieux;

        await _uneRechercheALaFois.WaitAsync(ct);
        try
        {
            return EnCache(cle) ?? Memoriser(cle, await client.Chercher(zoneArrondie, ct));
        }
        finally
        {
            _uneRechercheALaFois.Release();
        }
    }

    private IReadOnlyList<Lieu>? EnCache(string cle) => cache.Get<IReadOnlyList<Lieu>>(cle);

    private IReadOnlyList<Lieu> Memoriser(string cle, IReadOnlyList<Lieu> lieux) => cache.Set(cle, lieux, DureeDeCache);
}
