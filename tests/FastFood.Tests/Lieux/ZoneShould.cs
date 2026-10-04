namespace FastFood.Tests.Lieux;

public class ZoneShould
{
    [Fact]
    public void RefuserUneZoneDontLeSudDepasseLeNord()
    {
        var creee = Zone.TryCreer(48.86, 2.33, 48.85, 2.34, out var zone);

        Assert.False(creee);
        Assert.Null(zone);
    }

    [Fact]
    public void RefuserUnParametreManquant()
    {
        var creee = Zone.TryCreer(48.85, null, 48.86, 2.34, out _);

        Assert.False(creee);
    }

    [Fact]
    public void RefuserUneLatitudeHorsLimites()
    {
        var creee = Zone.TryCreer(89, 2.33, 91, 2.34, out _);

        Assert.False(creee);
    }

    [Fact]
    public void MesurerEnviron1100MetresPour0Virgule01DegreDeLatitude()
    {
        var zone = new Zone(48.85, 2.330, 48.86, 2.331);

        Assert.InRange(zone.PlusGrandCoteEnMetres(), 1107, 1117);
    }

    [Fact]
    public void EtreTropGrandeAuDelaDe3000Metres()
    {
        var zone = new Zone(48.85, 2.33, 48.88, 2.36);

        Assert.True(zone.EstTropGrande());
    }

    [Fact]
    public void NePasEtreTropGrandePourUneZoneDe1Km()
    {
        var zone = new Zone(48.85, 2.33, 48.859, 2.34);

        Assert.False(zone.EstTropGrande());
    }

    [Fact]
    public void SArrondirVersLExterieurAuPasDe0Virgule005()
    {
        var zone = new Zone(48.8534, 2.3488, 48.8561, 2.3512);

        var arrondie = zone.ArrondieVersExterieur();

        Assert.Equal(new Zone(48.850, 2.345, 48.860, 2.355), arrondie);
    }

    [Fact]
    public void DonnerLaMemeCleAPartirDeDeuxZonesVoisinesArrondies()
    {
        var a = new Zone(48.8534, 2.3488, 48.8561, 2.3512);
        var b = new Zone(48.8512, 2.3461, 48.8590, 2.3540);

        Assert.Equal(a.ArrondieVersExterieur().Cle(), b.ArrondieVersExterieur().Cle());
    }
}
