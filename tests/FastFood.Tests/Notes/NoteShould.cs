namespace FastFood.Tests.Notes;

public class NoteShould
{
    [Fact]
    public void AccepterUnCommentaireDe280Caracteres()
    {
        var erreur = Note.Valider("Lionel", 4, new string('a', 280));

        Assert.Null(erreur);
    }

    [Fact]
    public void RefuserUnCommentaireDe281Caracteres()
    {
        var erreur = Note.Valider("Lionel", 4, new string('a', 281));

        Assert.Equal("Le commentaire fait 280 caractères au plus.", erreur);
    }

    [Fact]
    public void RefuserZeroEtoile()
    {
        var erreur = Note.Valider("Lionel", 0, null);

        Assert.Equal("La note va de 1 à 5 étoiles.", erreur);
    }

    [Fact]
    public void RefuserSixEtoiles()
    {
        var erreur = Note.Valider("Lionel", 6, null);

        Assert.Equal("La note va de 1 à 5 étoiles.", erreur);
    }

    [Fact]
    public void RefuserUnPrenomBlanc()
    {
        var erreur = Note.Valider("   ", 3, null);

        Assert.Equal("Le prénom est obligatoire (40 caractères au plus).", erreur);
    }

    [Fact]
    public void RefuserUnPrenomDe41Caracteres()
    {
        var erreur = Note.Valider(new string('a', 41), 3, null);

        Assert.Equal("Le prénom est obligatoire (40 caractères au plus).", erreur);
    }

    [Theory]
    [InlineData("Lio/nel")]
    [InlineData("Lio\\nel")]
    [InlineData(".")]
    [InlineData("..")]
    public void RefuserUnPrenomQuiCasseLeChemin(string prenom)
    {
        var erreur = Note.Valider(prenom, 3, null);

        Assert.Equal("Le prénom ne peut pas contenir / ni \\ ni n'être que des points.", erreur);
    }

    [Fact]
    public void AccepterUnPrenomAvecUnPoint()
    {
        var erreur = Note.Valider("Zoé B.", 3, null);

        Assert.Null(erreur);
    }
}
