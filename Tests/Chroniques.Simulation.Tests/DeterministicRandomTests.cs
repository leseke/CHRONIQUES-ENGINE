using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie le Principe 10 (MASTER-002) : à état identique et mêmes entrées,
/// résultat identique.
/// </summary>
public class DeterministicRandomTests
{
    [Fact]
    public void Meme_graine_produit_toujours_la_meme_sequence()
    {
        var a = new DeterministicRandom(42);
        var b = new DeterministicRandom(42);

        var sequenceA = Enumerable.Range(0, 20).Select(_ => a.Next(0, 1000)).ToList();
        var sequenceB = Enumerable.Range(0, 20).Select(_ => b.Next(0, 1000)).ToList();

        Assert.Equal(sequenceA, sequenceB);
    }

    [Fact]
    public void Graines_differentes_produisent_des_sequences_differentes()
    {
        var a = new DeterministicRandom(1);
        var b = new DeterministicRandom(2);

        var sequenceA = Enumerable.Range(0, 20).Select(_ => a.Next(0, 1_000_000)).ToList();
        var sequenceB = Enumerable.Range(0, 20).Select(_ => b.Next(0, 1_000_000)).ToList();

        Assert.NotEqual(sequenceA, sequenceB);
    }
}
