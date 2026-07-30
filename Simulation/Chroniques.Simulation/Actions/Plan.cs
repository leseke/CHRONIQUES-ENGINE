using Chroniques.Simulation.Kernel;

namespace Chroniques.Simulation.Actions;

/// <summary>
/// Stratégie d'exécution organisant des Actions en réponse à un Intent
/// (ACT-002-I).
///
/// Le Plan organise, il ne décide ni n'exécute (ACT-002-I, section 9) ---
/// ce type ne contient donc aucune méthode d'exécution, seulement des
/// méthodes de réévaluation (ACT-002-I, section 7).
/// </summary>
public sealed class Plan
{
    private IReadOnlyList<PlanStep> _steps;

    public Intent Intent { get; }
    public IReadOnlyList<PlanStep> Steps => _steps;
    public PlanStatus Status { get; private set; } = PlanStatus.Actif;

    public Plan(Intent intent, IReadOnlyList<PlanStep> steps)
    {
        ValiderAcyclique(steps);
        Intent = intent;
        _steps = steps;
    }

    /// <summary>
    /// Suspend le Plan --- la progression est conservée, il pourra reprendre
    /// (ACT-002-I, section 7).
    /// </summary>
    public void Suspendre()
    {
        if (Status == PlanStatus.Abandonne)
        {
            throw new InvalidOperationException("Un Plan abandonné ne peut plus être suspendu.");
        }

        Status = PlanStatus.Suspendu;
    }

    /// <summary>
    /// Fait reprendre un Plan suspendu.
    /// </summary>
    public void Reprendre()
    {
        if (Status == PlanStatus.Abandonne)
        {
            throw new InvalidOperationException("Un Plan abandonné ne peut plus reprendre.");
        }

        Status = PlanStatus.Actif;
    }

    /// <summary>
    /// Adapte les étapes du Plan sans changer l'Intent d'origine (ACT-002-I,
    /// section 7).
    /// </summary>
    public void Adapter(IReadOnlyList<PlanStep> nouvellesEtapes)
    {
        if (Status == PlanStatus.Abandonne)
        {
            throw new InvalidOperationException("Un Plan abandonné ne peut plus être adapté.");
        }

        ValiderAcyclique(nouvellesEtapes);
        _steps = nouvellesEtapes;
    }

    /// <summary>
    /// Abandonne définitivement ce Plan (ACT-002-I, section 7). Contrairement
    /// aux états d'une Action Instance (ACT-001-F), rien n'interdit qu'un
    /// nouvel Intent relance un pipeline équivalent --- seul ce Plan-ci est
    /// abandonné, jamais l'Intent qui l'a motivé.
    /// </summary>
    public void Abandonner()
    {
        Status = PlanStatus.Abandonne;
    }

    /// <summary>
    /// Reconstruit entièrement les étapes de ce Plan à partir du même
    /// Intent, via le Planner fourni --- jamais un remplacement par un Plan
    /// entièrement nouveau (ACT-002-I, section 7 ; ENGINE-006, section 4.3).
    /// </summary>
    public void Reconstruire(IPlanner planner, World world)
    {
        if (Status == PlanStatus.Abandonne)
        {
            throw new InvalidOperationException("Un Plan abandonné ne peut plus être reconstruit.");
        }

        var planReconstruit = planner.CreatePlan(Intent, world);
        _steps = planReconstruit.Steps;
        Status = PlanStatus.Actif;
    }

    private static void ValiderAcyclique(IReadOnlyList<PlanStep> etapes)
    {
        foreach (var etape in etapes)
        {
            if (ContientUnCycle(etape))
            {
                throw new InvalidOperationException(
                    "Un Plan ne peut jamais contenir de dépendance circulaire entre ses étapes (ACT-002-I, section 6).");
            }
        }
    }

    /// <summary>
    /// Détecte un cycle par identité de référence, jamais par égalité
    /// structurelle --- <see cref="PlanStep"/> est un record : deux étapes
    /// indépendantes mais structurellement identiques (même Definition,
    /// même Mode, aucune dépendance) ne doivent jamais être confondues.
    /// </summary>
    private static bool ContientUnCycle(PlanStep depart)
    {
        var enCoursDeVisite = new HashSet<PlanStep>(ReferenceEqualityComparer.Instance);
        return Visite(depart, enCoursDeVisite);

        static bool Visite(PlanStep etape, HashSet<PlanStep> enCoursDeVisite)
        {
            if (!enCoursDeVisite.Add(etape))
            {
                return true;
            }

            foreach (var dependance in etape.DependsOn)
            {
                if (Visite(dependance, enCoursDeVisite))
                {
                    return true;
                }
            }

            enCoursDeVisite.Remove(etape);
            return false;
        }
    }
}
