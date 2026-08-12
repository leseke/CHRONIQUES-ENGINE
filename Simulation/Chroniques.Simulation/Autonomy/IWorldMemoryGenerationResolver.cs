namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Kernel;

public interface IWorldMemoryGenerationResolver
{
    long ResolveGeneration(World world, Tick currentTick);
}
