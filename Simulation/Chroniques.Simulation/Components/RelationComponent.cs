namespace Chroniques.Simulation.Components;

using Chroniques.Simulation.Kernel;

/// <summary>
/// Implémente GDB-004C (Les Relations Sociales) : le réseau de relations
/// sociales d'un habitant.
///
/// Pure donnée, conformément à CORE-003-C : aucune logique n'est exécutée
/// ici. C'est <see cref="Chroniques.Simulation.Systems.RelationSystem"/>
/// qui fait évoluer les relations à chaque Tick (érosion) et qui enregistre
/// les interactions qualifiantes (création, Épisodes).
/// </summary>
public sealed class RelationComponent : IComponent
{
    private readonly List<Relation> _relations = new();

    public IReadOnlyList<Relation> Relations => _relations;

    internal void Ajouter(Relation relation) => _relations.Add(relation);

    internal void Retirer(Relation relation) => _relations.Remove(relation);
}

/// <summary>
/// Les sept types de relations définis par GDB-004C.
/// Une paire d'habitants peut entretenir plusieurs relations de nature
/// différente simultanément --- jamais deux relations du même type.
/// </summary>
public enum TypeRelation
{
    Familiale,
    Amicale,
    Professionnelle,
    Commerciale,
    Politique,
    Conflictuelle,
    Sentimentale
}

/// <summary>
/// Une interaction significative attachée à une relation (GDB-004C, ÉPISODES).
/// Créé uniquement si l'ampleur de l'interaction franchit le seuil
/// d'importance --- jamais pour une interaction ordinaire.
/// </summary>
public sealed record Episode(Tick Tick, string Description, double Impact);

/// <summary>
/// Lien entre deux habitants (GDB-004C, MODÈLE DE FORCE).
/// La Force va de 0 à 100. Le Type porte le signe narratif (positif /
/// négatif), jamais la Force elle-même.
///
/// Mutable uniquement par <see cref="Chroniques.Simulation.Systems.RelationSystem"/>.
/// </summary>
public sealed class Relation
{
    private readonly List<Episode> _episodes = new();

    public EntityId Cible { get; }
    public TypeRelation Type { get; }
    public double Force { get; internal set; }
    public Tick CreeeAu { get; }

    public IReadOnlyList<Episode> Episodes => _episodes;

    public Relation(EntityId cible, TypeRelation type, double forceInitiale, Tick creeeAu)
    {
        Cible = cible;
        Type = type;
        Force = Math.Clamp(forceInitiale, 0, 100);
        CreeeAu = creeeAu;
    }

    internal void AjouterEpisode(Episode episode, int capaciteMax)
    {
        if (_episodes.Count >= capaciteMax)
            _episodes.RemoveAt(0); // éviction du plus ancien (GDB-004C, ÉPISODES)

        _episodes.Add(episode);
    }
}
