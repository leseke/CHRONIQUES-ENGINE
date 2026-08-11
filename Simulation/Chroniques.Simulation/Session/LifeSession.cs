namespace Chroniques.Simulation.Session;

using Chroniques.Simulation.Kernel;
using Chroniques.Simulation.Systems;

/// <summary>
/// État minimal d'une session de vie (ENGINE-009).
/// </summary>
public enum LifeSessionState
{
    Active,
    EndedWithoutSuccessor
}

/// <summary>
/// Orchestre la continuité du personnage contrôlé au-dessus du World et du Scheduler.
///
/// Cette classe ne contient aucune logique métier :
/// - elle ne modifie aucun Component ;
/// - elle ne désigne jamais elle-même un héritier ;
/// - elle ne déclenche aucune Action joueur ou PNJ ;
/// - elle n'utilise World.Events que comme journal observable après un Tick.
/// </summary>
public sealed class LifeSession
{
    private const string EtatMort = "mort";
    private const string HeritageTransmission = "heritage.transmission";
    private const string HeritageAbsenceSuccesseur = "heritage.absence-successeur";

    private readonly Scheduler _scheduler;

    public World World { get; }

    public EntityId ActiveCharacterId { get; private set; }

    public LifeSessionState State { get; private set; }

    public LifeSession(
        World world,
        Scheduler scheduler,
        EntityId activeCharacterId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(scheduler);

        if (!world.TryGetEntity(activeCharacterId, out _))
        {
            throw new ArgumentException(
                "Le personnage actif doit exister dans le World.",
                nameof(activeCharacterId));
        }

        World = world;
        _scheduler = scheduler;
        ActiveCharacterId = activeCharacterId;
        State = LifeSessionState.Active;
    }

    /// <summary>
    /// Fait avancer exactement un Tick via le Scheduler puis synchronise
    /// le personnage actif avec l'état du World et le résultat observable
    /// produit par HeritageSystem pendant ce Tick.
    /// </summary>
    public void AdvanceTime()
    {
        if (State != LifeSessionState.Active)
        {
            return;
        }

        var personnageAvantTick = ActiveCharacterId;

        _scheduler.Tick(World);

        SynchroniserApresTick(personnageAvantTick, World.CurrentTick);
    }

    private void SynchroniserApresTick(
        EntityId personnageAvantTick,
        Tick tickCourant)
    {
        if (!World.TryGetEntity(personnageAvantTick, out var personnage))
        {
            State = LifeSessionState.EndedWithoutSuccessor;
            return;
        }

        if (!string.Equals(
                personnage.Lifecycle.CurrentState.Name,
                EtatMort,
                StringComparison.Ordinal))
        {
            return;
        }

        var transmission =
            World.Events
                .LastOrDefault(
                    evt =>
                        evt.OccurredAt == tickCourant
                        && evt.Kind == HeritageTransmission
                        && evt.Source == personnageAvantTick);

        if (transmission is not null
            && transmission.Target is EntityId heritierId
            && World.TryGetEntity(heritierId, out _))
        {
            ActiveCharacterId = heritierId;
            State = LifeSessionState.Active;
            return;
        }

        var absenceSuccesseur =
            World.Events.Any(
                evt =>
                    evt.OccurredAt == tickCourant
                    && evt.Kind == HeritageAbsenceSuccesseur
                    && evt.Source == personnageAvantTick);

        if (absenceSuccesseur || transmission is not null)
        {
            State = LifeSessionState.EndedWithoutSuccessor;
        }
    }
}
