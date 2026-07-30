namespace Chroniques.Simulation.Actions;

/// <summary>
/// Les états officiels d'une Action Instance (ACT-001-F, section 3) --- ni
/// renommés, ni réordonnés, ni complétés. Les transitions autorisées entre
/// ces états vivent dans <see cref="ActionInstance"/>, jamais ici : cet
/// enum ne décrit que les valeurs possibles, jamais le graphe.
/// </summary>
public enum ActionState
{
    Created,
    Validated,
    Planned,
    Prepared,
    Running,
    Suspended,
    Interrupted,
    Succeeded,
    Failed,
    Resolved,
    Committed,
    Archived
}
