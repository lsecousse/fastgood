using System.Text.Json;

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
                var lieux = await EssayerInstance(instance, requete, ct);
                if (lieux is not null) return lieux;
            }
            throw new OverpassIndisponible();
        }
        finally
        {
            _unAppelALaFois.Release();
        }
    }

    private async Task<IReadOnlyList<Lieu>?> EssayerInstance(Uri instance, string requete, CancellationToken ct)
    {
        try
        {
            using var corps = new StringContent("data=" + Uri.EscapeDataString(requete), null, "application/x-www-form-urlencoded");
            using var reponse = await http.PostAsync(instance, corps, ct);
            return reponse.IsSuccessStatusCode ? LireSiComplete(await reponse.Content.ReadAsStringAsync(ct)) : null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private static IReadOnlyList<Lieu>? LireSiComplete(string json)
    {
        using var document = JsonDocument.Parse(json);
        var remarque = document.RootElement.TryGetProperty("remark", out var r) ? r.GetString() : null;
        var erreurDExecution = remarque?.StartsWith("runtime error", StringComparison.Ordinal) ?? false;
        return erreurDExecution ? null : LecteurOverpass.Lire(json);
    }
}
