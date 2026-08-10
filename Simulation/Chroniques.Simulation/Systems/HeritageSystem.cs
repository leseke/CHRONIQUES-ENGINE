namespace Chroniques.Simulation.Systems;

using Chroniques.Simulation.Components;
using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-004J (La Transmission).
///
/// Responsabilités :
///   - détecter la mort d'une Entity par inspection directe du Lifecycle ;
///   - désigner l'héritier selon l'algorithme déterministe de GDB-004J ;
///   - garantir qu'une Entity morte n'est traitée qu'une seule fois ;
///   - traiter les cas d'échec relevant de l'héritage ;
///   - constituer l'unique source de vérité pour la logique d'héritage.
///
/// World.Events reste uniquement un journal d'observabilité :
/// HeritageSystem ne lit jamais les événements du World pour décider d'agir.
///
/// HeritageSystem doit être enregistré après AgingSystem dans le Scheduler
/// afin qu'une Entity décédée pendant le Tick soit déjà dans l'état "mort"
/// lorsque Update est exécuté.
/// </summary>
public sealed class HeritageSystem : ISystem
{
    private const string EtatMort = "mort";

    private readonly HashSet<EntityId> _dejaTraitees = new();

    /// <summary>
    /// Inspecte le Lifecycle des Entities et déclenche une transmission
    /// pour toute Entity morte qui n'a pas encore été traitée.
    /// </summary>
    public void Update(World world, Tick currentTick)
    {
        foreach (var entity in world.Entities)
        {
            if (entity.Lifecycle.CurrentState.Name != EtatMort)
                continue;

            if (_dejaTraitees.Contains(entity.Id))
                continue;

            _dejaTraitees.Add(entity.Id);

            TraiterTransmission(
                world,
                entity,
                currentTick);
        }
    }

    /// <summary>
    /// Traite explicitement le refus d'un héritage.
    ///
    /// Cette méthode constitue l'unique point d'entrée métier du refus
    /// d'héritage. PopulationEffectApplicator ne contient aucune logique
    /// d'héritage : il se contente de dispatcher HeritageRefusalEffect ici.
    ///
    /// En Phase 1, aucun patrimoine matériel n'étant encore représenté,
    /// le traitement concret consiste à produire l'événement observable
    /// "heritage.refus".
    ///
    /// La redistribution éventuelle de la part refusée sera ajoutée lorsque
    /// le patrimoine disposera d'une représentation conforme à GDB-004J.
    /// </summary>
    public void RefuserHeritage(
        World world,
        Tick tick,
        EntityId heritier,
        EntityId defunt)
    {
        // Si l'une des deux Entities n'existe pas, aucune mutation ni
        // publication ne doit avoir lieu.
        if (!world.TryGetEntity(heritier, out _))
            return;

        if (!world.TryGetEntity(defunt, out _))
            return;

        world.Publish(
            GameEvent.Create(
                tick,
                "heritage.refus",
                source: heritier,
                target: defunt));
    }

    /// <summary>
    /// Traite la transmission initiale après le décès.
    /// </summary>
    private void TraiterTransmission(
        World world,
        Entity defunt,
        Tick tick)
    {
        var heritier =
            DesignerHeritier(
                world,
                defunt);

        if (heritier is null)
        {
            // Cas d'échec 1 :
            // absence de successeur (GDB-004J).
            world.Publish(
                GameEvent.Create(
                    tick,
                    "heritage.absence-successeur",
                    source: defunt.Id));

            return;
        }

        /*
         * Phase 1 :
         *
         * Le patrimoine matériel n'est pas encore représenté.
         * La transmission est donc actuellement matérialisée par
         * un événement observable.
         *
         * Un éventuel refus ultérieur passe exclusivement par
         * RefuserHeritage(), appelé via HeritageRefusalEffect.
         */
        world.Publish(
            GameEvent.Create(
                tick,
                "heritage.transmission",
                source: defunt.Id,
                target: heritier.Id));
    }

    /// <summary>
    /// Désigne l'héritier selon l'algorithme déterministe de GDB-004J :
    ///
    /// 1. priorité aux relations Familiales ;
    /// 2. Force la plus élevée ;
    /// 3. en cas d'égalité, relation la plus ancienne ;
    /// 4. si aucune relation Familiale, même règle sur les autres relations ;
    /// 5. si aucune relation exploitable, absence de successeur.
    /// </summary>
    private static Entity? DesignerHeritier(
        World world,
        Entity defunt)
    {
        if (!defunt.TryGet<RelationComponent>(out var relationComponent))
            return null;

        if (relationComponent.Relations.Count == 0)
            return null;

        var candidatsFamiliaux =
            relationComponent.Relations
                .Where(
                    relation =>
                        relation.Type == TypeRelation.Familiale)
                .ToList();

        var candidats =
            candidatsFamiliaux.Count > 0
                ? candidatsFamiliaux
                : relationComponent.Relations.ToList();

        var meilleureRelation =
            candidats
                .OrderByDescending(
                    relation =>
                        relation.Force)
                .ThenBy(
                    relation =>
                        relation.CreeeAu.Value)
                .First();

        return world.TryGetEntity(
            meilleureRelation.Cible,
            out var heritier)
                ? heritier
                : null;
    }
}
