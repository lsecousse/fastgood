namespace FastFood.Lieux;

public sealed class OverpassIndisponible : Exception
{
    public OverpassIndisponible() : base("Aucune instance Overpass n'a répondu.") { }
}

public interface IClientOverpass
{
    Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct);
}

public sealed class ClientOverpass(HttpClient http, IReadOnlyList<Uri> instances) : IClientOverpass
{
    public static IReadOnlyList<Uri> Instances { get; } =
    [
        new("https://lz4.overpass-api.de/api/interpreter"),
        new("https://overpass-api.de/api/interpreter"),
    ];

    public static readonly TimeSpan Delai = TimeSpan.FromSeconds(30);
    public const string UserAgent = "FastFood/1.0 (app perso; lsecousse@linkeat.io)";

    private readonly SemaphoreSlim _unAppelALaFois = new(1, 1);

    public async Task<IReadOnlyList<Lieu>> Chercher(Zone zone, CancellationToken ct)
    {
        var requete = RequeteOverpass.Pour(zone);
        await _unAppelALaFois.WaitAsync(ct);
        try
        {
            foreach (var instance in instances)
            {
                var json = await EssayerInstance(instance, requete, ct);
                if (json is not null) return LecteurOverpass.Lire(json);
            }
            throw new OverpassIndisponible();
        }
        finally
        {
            _unAppelALaFois.Release();
        }
    }

    private async Task<string?> EssayerInstance(Uri instance, string requete, CancellationToken ct)
    {
        try
        {
            using var corps = new StringContent("data=" + Uri.EscapeDataString(requete), null, "application/x-www-form-urlencoded");
            using var reponse = await http.PostAsync(instance, corps, ct);
            return reponse.IsSuccessStatusCode ? await reponse.Content.ReadAsStringAsync(ct) : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }
}
