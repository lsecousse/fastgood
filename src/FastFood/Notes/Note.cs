namespace FastFood.Notes;

public sealed record Note(string Prenom, int Etoiles, string? Commentaire, DateTimeOffset ModifieeLe)
{
    private const int PrenomMax = 40;
    private const int CommentaireMax = 280;

    public static string? Valider(string? prenom, int etoiles, string? commentaire)
    {
        var longueurPrenom = prenom?.Trim().Length ?? 0;
        if (longueurPrenom is 0 or > PrenomMax) return "Le prénom est obligatoire (40 caractères au plus).";
        if (etoiles is < 1 or > 5) return "La note va de 1 à 5 étoiles.";
        if ((commentaire?.Trim().Length ?? 0) > CommentaireMax) return "Le commentaire fait 280 caractères au plus.";
        return null;
    }
}

public sealed record Moyenne(double Valeur, int Nombre);
