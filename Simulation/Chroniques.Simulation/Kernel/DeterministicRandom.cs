namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Générateur de nombres pseudo-aléatoires déterministe à graine.
///
/// Condition du Principe 10 (MASTER-002) : à état identique et mêmes
/// entrées, résultat identique. <see cref="System.Random"/> initialisé avec
/// une graine fixe produit toujours la même séquence au sein d'une même
/// version majeure de .NET --- une portabilité inter-runtime plus stricte
/// (si elle s'avère nécessaire pour le multijoueur, MASTER-002 Principe 10)
/// exigerait un PRNG maison, décision à ajourner tant qu'aucun enseignement
/// concret ne l'exige (MASTER-006).
/// </summary>
public sealed class DeterministicRandom
{
    private readonly Random _random;

    public long Seed { get; }

    public DeterministicRandom(long seed)
    {
        Seed = seed;
        _random = new Random(seed.GetHashCode());
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
