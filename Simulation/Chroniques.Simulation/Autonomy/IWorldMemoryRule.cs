namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed record WorldMemoryCreationCandidate(
    string MemoryTypeId,
    string MemoryKey,
    string Payload,
    IReadOnlyList<string> SourceRefs);

public sealed record WorldMemoryGenerationEvidence(
    bool HasLinkOrTransmission,
    bool HasQualifyingReference,
    bool HasRegionalInfluence,
    bool HasEqualOrGreaterContradiction,
    bool HasQualifyingPractice,
    IReadOnlyList<string> SourceRefs);

public interface IWorldMemoryRule
{
    string MemoryTypeId { get; }

    IReadOnlyList<WorldMemoryCreationCandidate> FindCreationCandidates(
        World world,
        Tick currentTick,
        long currentGeneration);

    WorldMemoryGenerationEvidence EvaluateGeneration(
        Entity memoryEntity,
        WorldMemoryComponent memory,
        World world,
        long generation);
}
