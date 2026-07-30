namespace Chroniques.Simulation.Actions;

/// <summary>
/// Nature d'une étape de <see cref="Plan"/> (ACT-002-I, section 5) :
/// « une étape peut être séquentielle, parallèle, optionnelle,
/// conditionnelle ».
/// </summary>
public enum PlanStepMode
{
    Sequentiel,
    Parallele,
    Optionnel,
    Conditionnel
}
