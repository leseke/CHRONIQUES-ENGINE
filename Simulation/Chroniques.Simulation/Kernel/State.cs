namespace Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente la primitive State (CORE-005) : représente une condition à un
/// instant donné (CORE-000-G : « les States représentent une condition »).
///
/// Un State ne provoque jamais d'action par lui-même --- il décrit, il
/// n'exécute pas. Il s'appuie sur une ou plusieurs <see cref="Value{T}"/>
/// pour porter ses données (CORE-000-D).
/// </summary>
public sealed class State
{
    private readonly Dictionary<string, object?> _values = new();

    public string Name { get; }

    public State(string name)
    {
        Name = name;
    }

    public void Set<T>(string key, Value<T> value) => _values[key] = value.Data;

    public bool TryGet<T>(string key, out T value)
    {
        if (_values.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    public IReadOnlyDictionary<string, object?> Values => _values;
}
