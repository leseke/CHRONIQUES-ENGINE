namespace Chroniques.Simulation.Autonomy;

using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// Orchestre l'initiation d'Actions par des habitants explicitement
/// enregistrés comme autonomes (ENGINE-010).
///
/// Le System ne choisit aucune règle métier et n'exécute aucune Action
/// directement : il demande éventuellement un Intent à la source injectée,
/// puis le remet à l'exécuteur injecté.
/// </summary>
public sealed class AutonomousActionSystem : ISystem
{
    private const string EtatMort = "mort";

    private readonly List<EntityId> _actors = new();
    private readonly IAutonomousIntentSource _intentSource;
    private readonly IAutonomousIntentExecutor _intentExecutor;

    public IReadOnlyList<EntityId> Actors => _actors;

    public AutonomousActionSystem(
        IAutonomousIntentSource intentSource,
        IAutonomousIntentExecutor intentExecutor)
    {
        ArgumentNullException.ThrowIfNull(intentSource);
        ArgumentNullException.ThrowIfNull(intentExecutor);

        _intentSource = intentSource;
        _intentExecutor = intentExecutor;
    }

    /// <summary>
    /// Enregistre un Acteur autonome en conservant l'ordre d'enregistrement.
    /// Un même Acteur n'est jamais enregistré deux fois afin de garantir au
    /// plus une demande d'Intent par Acteur et par Update.
    /// </summary>
    public void RegisterActor(EntityId actorId)
    {
        if (!_actors.Contains(actorId))
        {
            _actors.Add(actorId);
        }
    }

    public void Update(World world, Tick currentTick)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var actorId in _actors)
        {
            if (!world.TryGetEntity(actorId, out var actor))
            {
                continue;
            }

            if (string.Equals(
                    actor.Lifecycle.CurrentState.Name,
                    EtatMort,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var intent = _intentSource.CreateIntent(actor, world, currentTick);

            if (intent is null)
            {
                continue;
            }

            if (intent.Acteur != actor.Id)
            {
                throw new InvalidOperationException(
                    "Une source d'Intent autonome ne peut pas faire agir une autre Entity.");
            }

            _intentExecutor.Execute(intent, world);
        }
    }
}
