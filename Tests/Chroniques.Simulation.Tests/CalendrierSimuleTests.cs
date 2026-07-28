using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie la convention retenue en équipe : un Tick = un mois simulé,
/// trois mois par saison, quatre saisons par an (GDB-008A).
/// </summary>
public class CalendrierSimuleTests
{
    [Theory]
    [InlineData(1, "printemps")]
    [InlineData(2, "printemps")]
    [InlineData(3, "printemps")]
    [InlineData(4, "ete")]
    [InlineData(5, "ete")]
    [InlineData(6, "ete")]
    [InlineData(7, "automne")]
    [InlineData(8, "automne")]
    [InlineData(9, "automne")]
    [InlineData(10, "hiver")]
    [InlineData(11, "hiver")]
    [InlineData(12, "hiver")]
    [InlineData(13, "printemps")] // bascule dans l'année suivante
    [InlineData(24, "hiver")]
    [InlineData(25, "printemps")]
    public void Chaque_saison_dure_exactement_trois_ticks(long tickValue, string saisonAttendue)
    {
        Assert.Equal(saisonAttendue, CalendrierSimule.SaisonAu(new Tick(tickValue)));
    }

    [Fact]
    public void Avant_le_premier_tick_la_saison_par_convention_est_le_printemps()
    {
        Assert.Equal("printemps", CalendrierSimule.SaisonAu(Tick.Zero));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(11, 0)]
    [InlineData(12, 1)]
    [InlineData(13, 1)]
    [InlineData(23, 1)]
    [InlineData(24, 2)]
    public void Lannee_ne_progresse_quaux_multiples_de_douze_ticks(long tickValue, int anneeAttendue)
    {
        Assert.Equal(anneeAttendue, CalendrierSimule.AnneeAu(new Tick(tickValue)));
    }

    [Fact]
    public void Douze_mois_font_toujours_une_annee()
    {
        Assert.Equal(12, CalendrierSimule.MoisParAn);
        Assert.Equal(3, CalendrierSimule.MoisParSaison);
        Assert.Equal(4, CalendrierSimule.SaisonsParAn);
    }
}
