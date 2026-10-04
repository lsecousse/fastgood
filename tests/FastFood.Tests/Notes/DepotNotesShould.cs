using Microsoft.Extensions.Time.Testing;

namespace FastFood.Tests.Notes;

public sealed class DepotNotesShould : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"fastfood-{Guid.NewGuid():N}.db");
    private readonly FakeTimeProvider _horloge = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly DepotNotes _sut;

    public DepotNotesShould() => _sut = new DepotNotes(_chemin, _horloge);

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_chemin);
    }

    [Fact]
    public void RetrouverUneNoteEnregistree()
    {
        _sut.Enregistrer("node", 1, "Subway", "Lionel", 4, "Bon");

        var note = Assert.Single(_sut.Lister("node", 1));

        Assert.Equal(new Note("Lionel", 4, "Bon", _horloge.GetUtcNow()), note);
    }

    [Fact]
    public void RemplacerLaNoteDuMemePrenomSansTenirCompteDeLaCasse()
    {
        _sut.Enregistrer("node", 1, "Subway", "Lionel", 2, null);
        _sut.Enregistrer("node", 1, "Subway", "lionel", 4, null);

        var note = Assert.Single(_sut.Lister("node", 1));

        Assert.Equal(("lionel", 4), (note.Prenom, note.Etoiles));
    }

    [Fact]
    public void RetirerLesEspacesDeBordDuPrenom()
    {
        _sut.Enregistrer("node", 1, "Subway", "  Lionel  ", 3, null);

        var note = Assert.Single(_sut.Lister("node", 1));

        Assert.Equal("Lionel", note.Prenom);
    }

    [Fact]
    public void StockerUnCommentaireVideCommeAbsent()
    {
        _sut.Enregistrer("node", 1, "Subway", "Lionel", 3, "   ");

        var note = Assert.Single(_sut.Lister("node", 1));

        Assert.Null(note.Commentaire);
    }

    [Fact]
    public void ListerLaPlusRecenteDAbord()
    {
        _sut.Enregistrer("node", 1, "Subway", "Ancien", 3, null);
        _horloge.Advance(TimeSpan.FromMinutes(5));
        _sut.Enregistrer("node", 1, "Subway", "Recent", 5, null);

        var prenoms = _sut.Lister("node", 1).Select(n => n.Prenom);

        Assert.Equal(["Recent", "Ancien"], prenoms);
    }

    [Fact]
    public void CalculerMoyenneEtNombreParLieu()
    {
        _sut.Enregistrer("node", 1, "Subway", "Lionel", 4, null);
        _sut.Enregistrer("node", 1, "Subway", "Marie", 3, null);

        var moyennes = _sut.Moyennes([("node", 1L)]);

        Assert.Equal(new Moyenne(3.5, 2), moyennes[("node", 1L)]);
    }

    [Fact]
    public void NeRienRendrePourUnLieuSansNote()
    {
        var moyennes = _sut.Moyennes([("node", 99L)]);

        Assert.Empty(moyennes);
    }
}
