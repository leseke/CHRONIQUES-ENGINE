namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive Value (CORE-004) : un conteneur typé pour une
/// information simple, sans aucun comportement propre (CORE-000-G : aucune
/// primitive ne provoque une action).
///
/// Un <see cref="State"/> s'appuie sur une ou plusieurs Value pour
/// représenter une condition (CORE-000-D).
/// </summary>
public readonly record struct Value<T>(T Data)
{
    public static implicit operator T(Value<T> value) => value.Data;

    public static implicit operator Value<T>(T data) => new(data);
}
