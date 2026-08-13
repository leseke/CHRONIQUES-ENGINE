namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

public sealed class LineageWorldMemoryGenerationResolver : IWorldMemoryGenerationResolver
{
    private readonly string _continuityId;

    public LineageWorldMemoryGenerationResolver(string continuityId)
    {
        if (string.IsNullOrWhiteSpace(continuityId))
            throw new ArgumentException("continuityId");
        _continuityId = continuityId;
    }

    public long ResolveGeneration(World world, Tick currentTick)
        => GenerationContinuityRegistry.Get(world, _continuityId).GenerationIndex;
}
