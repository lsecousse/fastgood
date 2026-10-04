using System.Net;

namespace FastFood.Tests.Lieux;

public class ClientOverpassShould
{
    private static readonly Zone ZoneParis = new(48.850, 2.340, 48.860, 2.350);
    private const string ReponseVide = "{\"elements\":[]}";
    private const string ReponseUnLieu =
        "{\"elements\":[{\"type\":\"node\",\"id\":1,\"lat\":48.85,\"lon\":2.34,\"tags\":{\"amenity\":\"fast_food\",\"name\":\"Chez Test\"}}]}";

    private static readonly IReadOnlyList<Uri> DeuxInstances = ClientOverpass.Instances;

    private sealed class HandlerFactice(Func<HttpRequestMessage, Task<HttpResponseMessage>> reponse) : HttpMessageHandler
    {
        public List<(Uri? Url, string Corps)> Requetes { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requete, CancellationToken ct)
        {
            var corps = requete.Content is null ? "" : await requete.Content.ReadAsStringAsync(ct);
            lock (Requetes) Requetes.Add((requete.RequestUri, corps));
            return await reponse(requete);
        }
    }

    private static HttpResponseMessage Ok(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private static ClientOverpass Client(HandlerFactice handler) => new(new HttpClient(handler), DeuxInstances);

    [Fact]
    public async Task EnvoyerLaRequeteALaPremiereInstance()
    {
        var handler = new HandlerFactice(_ => Task.FromResult(Ok(ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Equal("Chez Test", Assert.Single(lieux).Nom);
        var requete = Assert.Single(handler.Requetes);
        Assert.Equal(new Uri("https://lz4.overpass-api.de/api/interpreter"), requete.Url);
        Assert.Equal("data=" + Uri.EscapeDataString(RequeteOverpass.Pour(ZoneParis)), requete.Corps);
    }

    [Fact]
    public async Task PasserALaSecondeInstanceApresUn504()
    {
        var handler = new HandlerFactice(r => Task.FromResult(
            r.RequestUri!.Host.StartsWith("lz4") ? new HttpResponseMessage(HttpStatusCode.GatewayTimeout) : Ok(ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Single(lieux);
        Assert.Equal(new Uri("https://overpass-api.de/api/interpreter"), handler.Requetes[1].Url);
    }

    [Fact]
    public async Task PasserALaSecondeInstanceApresUneErreurReseau()
    {
        var handler = new HandlerFactice(r => r.RequestUri!.Host.StartsWith("lz4")
            ? throw new HttpRequestException("réseau coupé")
            : Task.FromResult(Ok(ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Single(lieux);
        Assert.Equal(2, handler.Requetes.Count);
    }

    [Fact]
    public async Task PasserALaSecondeInstanceQuandLeDelaiEstDepasse()
    {
        var handler = new HandlerFactice(r => r.RequestUri!.Host.StartsWith("lz4")
            ? throw new TaskCanceledException("délai dépassé")
            : Task.FromResult(Ok(ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Single(lieux);
    }

    [Fact]
    public async Task PropagerLAnnulationDeLAppelantSansEssayerLInstanceSuivante()
    {
        using var source = new CancellationTokenSource();
        var handler = new HandlerFactice(_ =>
        {
            source.Cancel();
            throw new TaskCanceledException();
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(handler).Chercher(ZoneParis, source.Token));

        Assert.Single(handler.Requetes);
    }

    [Fact]
    public async Task LeverOverpassIndisponibleQuandToutesEchouent()
    {
        var handler = new HandlerFactice(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.GatewayTimeout)));

        await Assert.ThrowsAsync<OverpassIndisponible>(() => Client(handler).Chercher(ZoneParis, CancellationToken.None));

        Assert.Equal(2, handler.Requetes.Count);
    }

    [Fact]
    public async Task PasserALaSecondeInstanceQuandLaReponseNEstPasDuJson()
    {
        var handler = new HandlerFactice(r => Task.FromResult(
            Ok(r.RequestUri!.Host.StartsWith("lz4") ? "<html>Surchargé</html>" : ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Single(lieux);
        Assert.Equal(2, handler.Requetes.Count);
    }

    [Fact]
    public async Task PasserALaSecondeInstanceQuandUnIdentifiantEstMalForme()
    {
        const string idDecimal =
            "{\"elements\":[{\"type\":\"node\",\"id\":1.5,\"lat\":48.85,\"lon\":2.34,\"tags\":{\"amenity\":\"fast_food\"}}]}";
        var handler = new HandlerFactice(r => Task.FromResult(
            Ok(r.RequestUri!.Host.StartsWith("lz4") ? idDecimal : ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Equal("Chez Test", Assert.Single(lieux).Nom);
    }

    [Fact]
    public async Task LeverOverpassIndisponibleQuandToutesRepondentDuJsonInvalide()
    {
        var handler = new HandlerFactice(_ => Task.FromResult(Ok("pas du json")));

        await Assert.ThrowsAsync<OverpassIndisponible>(() => Client(handler).Chercher(ZoneParis, CancellationToken.None));
    }

    [Fact]
    public async Task PasserALaSecondeInstanceQuandOverpassSignaleUneErreurDExecution()
    {
        const string partielle = "{\"remark\":\"runtime error: Query timed out in \\\"query\\\" at line 1 after 26 seconds.\",\"elements\":[]}";
        var handler = new HandlerFactice(r => Task.FromResult(
            Ok(r.RequestUri!.Host.StartsWith("lz4") ? partielle : ReponseUnLieu)));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Single(lieux);
    }

    [Fact]
    public async Task AccepterUneRemarqueQuiNEstPasUneErreurDExecution()
    {
        var handler = new HandlerFactice(_ => Task.FromResult(Ok("{\"remark\":\"info\",\"elements\":[]}")));

        var lieux = await Client(handler).Chercher(ZoneParis, CancellationToken.None);

        Assert.Empty(lieux);
        Assert.Single(handler.Requetes);
    }

    [Fact]
    public async Task NeJamaisLancerDeuxAppelsSimultanes()
    {
        var enCours = 0;
        var maximum = 0;
        var handler = new HandlerFactice(async _ =>
        {
            var maintenant = Interlocked.Increment(ref enCours);
            InterlockedMax(ref maximum, maintenant);
            await Task.Delay(50);
            Interlocked.Decrement(ref enCours);
            return Ok(ReponseVide);
        });
        var client = Client(handler);

        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.Chercher(ZoneParis, CancellationToken.None)));

        Assert.Equal(1, maximum);
    }

    private static void InterlockedMax(ref int cible, int valeur)
    {
        int courant;
        do { courant = Volatile.Read(ref cible); }
        while (valeur > courant && Interlocked.CompareExchange(ref cible, valeur, courant) != courant);
    }
}
