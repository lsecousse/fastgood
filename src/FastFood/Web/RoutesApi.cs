using FastFood.Lieux;
using FastFood.Notes;

namespace FastFood.Web;

public static class RoutesApi
{
    private static readonly string[] TypesOsm = ["node", "way", "relation"];
    private const int NomLieuMax = 200;

    public sealed record CorpsNote(int Etoiles, string? Commentaire, string? NomLieu);

    public sealed record LieuNote(string Type, long Id, string Nom, string Categorie, string? Cuisine,
        double Lat, double Lon, double? Moyenne, int NombreNotes);

    public static void MapApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sante", () => Results.Text("ok"));
        app.MapGet("/api/lieux", RechercherLieux);
        app.MapGet("/api/lieux/{type}/{id:long}/notes", ListerNotes);
        app.MapPut("/api/lieux/{type}/{id:long}/notes/{prenom}", EnregistrerNote);
    }

    private static IResult Erreur(int statut, string code, string message) =>
        Results.Json(new { code, message }, statusCode: statut);

    private static async Task<IResult> RechercherLieux(double? sud, double? ouest, double? nord, double? est,
        ICatalogueLieux catalogue, IDepotNotes depot, CancellationToken ct)
    {
        if (!Zone.TryCreer(sud, ouest, nord, est, out var zone))
            return Erreur(400, "zone_invalide", "La zone demandée est invalide.");
        if (zone!.EstTropGrande())
            return Erreur(422, "zone_trop_grande", "Zoome pour chercher.");
        try
        {
            var lieux = await catalogue.Chercher(zone, ct);
            return Results.Ok(AvecMoyennes(lieux, depot));
        }
        catch (OverpassIndisponible)
        {
            return Erreur(503, "osm_indisponible", "OpenStreetMap ne répond pas, réessaie dans une minute.");
        }
    }

    private static IEnumerable<LieuNote> AvecMoyennes(IReadOnlyList<Lieu> lieux, IDepotNotes depot)
    {
        var moyennes = depot.Moyennes(lieux.Select(l => (l.Type, l.Id)));
        return lieux.Select(l => moyennes.TryGetValue((l.Type, l.Id), out var m)
            ? new LieuNote(l.Type, l.Id, l.Nom, l.Categorie, l.Cuisine, l.Lat, l.Lon, m.Valeur, m.Nombre)
            : new LieuNote(l.Type, l.Id, l.Nom, l.Categorie, l.Cuisine, l.Lat, l.Lon, null, 0));
    }

    private static string NomLieu(string? saisi)
    {
        var nom = saisi?.Trim() ?? "";
        return nom.Length > NomLieuMax ? nom[..NomLieuMax] : nom;
    }

    private static IResult ListerNotes(string type, long id, IDepotNotes depot) =>
        TypesOsm.Contains(type)
            ? Results.Ok(depot.Lister(type, id))
            : Erreur(400, "type_invalide", "Type de lieu inconnu.");

    private static IResult EnregistrerNote(string type, long id, string prenom, CorpsNote corps, IDepotNotes depot)
    {
        if (!TypesOsm.Contains(type)) return Erreur(400, "type_invalide", "Type de lieu inconnu.");
        var message = Note.Valider(prenom, corps.Etoiles, corps.Commentaire);
        if (message is not null) return Erreur(400, "note_invalide", message);

        depot.Enregistrer(type, id, NomLieu(corps.NomLieu), prenom.Trim(), corps.Etoiles, corps.Commentaire);
        return Results.NoContent();
    }
}
