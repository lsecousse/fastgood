using System.Globalization;
using Microsoft.Data.Sqlite;

namespace FastFood.Notes;

public interface IDepotNotes
{
    void Enregistrer(string type, long id, string nomLieu, string prenom, int etoiles, string? commentaire);
    IReadOnlyList<Note> Lister(string type, long id);
    IReadOnlyDictionary<(string Type, long Id), Moyenne> Moyennes(IEnumerable<(string Type, long Id)> lieux);
}

public sealed class DepotNotes(string cheminBase, TimeProvider horloge) : IDepotNotes
{
    private const string CreationTable = """
        CREATE TABLE IF NOT EXISTS notes (
            type TEXT NOT NULL,
            osm_id INTEGER NOT NULL,
            prenom TEXT NOT NULL COLLATE NOCASE,
            etoiles INTEGER NOT NULL,
            commentaire TEXT NULL,
            nom_lieu TEXT NOT NULL,
            modifiee_le TEXT NOT NULL,
            PRIMARY KEY (type, osm_id, prenom)
        )
        """;

    private const string Upsert = """
        INSERT INTO notes (type, osm_id, prenom, etoiles, commentaire, nom_lieu, modifiee_le)
        VALUES ($type, $id, $prenom, $etoiles, $commentaire, $nomLieu, $modifieeLe)
        ON CONFLICT (type, osm_id, prenom) DO UPDATE SET
            prenom = excluded.prenom,
            etoiles = excluded.etoiles,
            commentaire = excluded.commentaire,
            nom_lieu = excluded.nom_lieu,
            modifiee_le = excluded.modifiee_le
        """;

    public void Enregistrer(string type, long id, string nomLieu, string prenom, int etoiles, string? commentaire)
    {
        var texte = string.IsNullOrWhiteSpace(commentaire) ? null : commentaire.Trim();
        using var connexion = Ouvrir();
        using var commande = Commande(connexion, Upsert, ("$type", type), ("$id", id), ("$prenom", prenom.Trim()),
            ("$etoiles", etoiles), ("$commentaire", texte), ("$nomLieu", nomLieu),
            ("$modifieeLe", horloge.GetUtcNow().ToString("O", CultureInfo.InvariantCulture)));
        commande.ExecuteNonQuery();
    }

    public IReadOnlyList<Note> Lister(string type, long id)
    {
        using var connexion = Ouvrir();
        using var commande = Commande(connexion,
            "SELECT prenom, etoiles, commentaire, modifiee_le FROM notes WHERE type = $type AND osm_id = $id ORDER BY modifiee_le DESC",
            ("$type", type), ("$id", id));
        using var lecteur = commande.ExecuteReader();
        var notes = new List<Note>();
        while (lecteur.Read()) notes.Add(LireNote(lecteur));
        return notes;
    }

    public IReadOnlyDictionary<(string Type, long Id), Moyenne> Moyennes(IEnumerable<(string Type, long Id)> lieux)
    {
        using var connexion = Ouvrir();
        var moyennes = new Dictionary<(string Type, long Id), Moyenne>();
        foreach (var lieu in lieux.Distinct())
        {
            using var commande = Commande(connexion,
                "SELECT AVG(etoiles), COUNT(*) FROM notes WHERE type = $type AND osm_id = $id",
                ("$type", lieu.Type), ("$id", lieu.Id));
            using var lecteur = commande.ExecuteReader();
            lecteur.Read();
            if (lecteur.GetInt32(1) > 0) moyennes[lieu] = new Moyenne(lecteur.GetDouble(0), lecteur.GetInt32(1));
        }
        return moyennes;
    }

    private static Note LireNote(SqliteDataReader lecteur) => new(
        lecteur.GetString(0),
        lecteur.GetInt32(1),
        lecteur.IsDBNull(2) ? null : lecteur.GetString(2),
        DateTimeOffset.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture));

    private SqliteConnection Ouvrir()
    {
        var connexion = new SqliteConnection($"Data Source={cheminBase}");
        connexion.Open();
        using var creation = Commande(connexion, CreationTable);
        creation.ExecuteNonQuery();
        return connexion;
    }

    private static SqliteCommand Commande(SqliteConnection connexion, string sql, params (string Nom, object? Valeur)[] parametres)
    {
        var commande = connexion.CreateCommand();
        commande.CommandText = sql;
        foreach (var (nom, valeur) in parametres) commande.Parameters.AddWithValue(nom, valeur ?? DBNull.Value);
        return commande;
    }
}
