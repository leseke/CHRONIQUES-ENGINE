namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Time (CORE-008) sous la forme d'un Tick.
///
/// CORE-000-G est explicite : « Time fournit un ordre ». Un Tick ne
/// représente donc pas une durée réelle (secondes, minutes) mais un rang
/// dans la séquence de la simulation --- la conversion vers un temps de jeu
/// habité (jours, saisons, années) relève de la GDB [réf: GDB-008A], pas du
/// Kernel.
/// </summary>
public readonly record struct Tick(long Value) : IComparable<Tick>
{
    public static Tick Zero => new(0);

    public Tick Next() => new(Value + 1);

    public int CompareTo(Tick other) => Value.CompareTo(other.Value);

    public static bool operator <(Tick a, Tick b) => a.Value < b.Value;
    public static bool operator >(Tick a, Tick b) => a.Value > b.Value;
    public static bool operator <=(Tick a, Tick b) => a.Value <= b.Value;
    public static bool operator >=(Tick a, Tick b) => a.Value >= b.Value;

    public override string ToString() => $"t{Value}";
}
