namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

public enum WorldMemoryTier
{
    Anecdote,
    Memory,
    Legend,
    Tradition,
}

public sealed record WorldMemoryTransitionTrace(
    long Generation,
    WorldMemoryTier PreviousTier,
    WorldMemoryTier NewTier,
    bool BecameForgotten,
    IReadOnlyList<string> EvidenceSourceRefs);

public sealed class WorldMemoryComponent : IComponent
{
    public string MemoryTypeId { get; set; } = string.Empty;
    public string MemoryKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public WorldMemoryTier Tier { get; set; } = WorldMemoryTier.Anecdote;
    public bool IsForgotten { get; set; }
    public List<string> SourceRefs { get; set; } = new();
    public long CreatedGeneration { get; set; }
    public long LastEvaluatedGeneration { get; set; }
    public int ConsecutiveReferencedGenerations { get; set; }
    public int ConsecutiveUnreferencedGenerations { get; set; }
    public List<WorldMemoryTransitionTrace> Transitions { get; set; } = new();
}
