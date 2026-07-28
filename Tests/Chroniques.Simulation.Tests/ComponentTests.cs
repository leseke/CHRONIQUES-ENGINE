using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie CORE-003-C : un même type de Component ne peut être présent
/// qu'une seule fois par Entity (responsabilité unique), et un Component
/// peut être ajouté, lu et retiré sans affecter les autres.
/// </summary>
public class ComponentTests
{
    [Fact]
    public void Ajouter_un_component_du_meme_type_remplace_le_precedent()
    {
        var entity = Entity.Create();

        entity.Set(new Position(1, 1));
        entity.Set(new Position(2, 2));

        entity.TryGet<Position>(out var position);
        Assert.Equal(new Position(2, 2), position);
        Assert.Single(entity.Components);
    }

    [Fact]
    public void Retirer_un_component_najoute_pas_dautre_component()
    {
        var entity = Entity.Create();
        entity.Set(new Position(1, 1));
        entity.Set(new Health(10));

        entity.Remove<Position>();

        Assert.False(entity.Has<Position>());
        Assert.True(entity.Has<Health>());
    }

    [Fact]
    public void Deux_types_de_component_coexistent_independamment()
    {
        var entity = Entity.Create();
        entity.Set(new Position(3, 4));
        entity.Set(new Health(10));

        Assert.Equal(2, entity.Components.Count);
    }

    private readonly record struct Position(int X, int Y) : IComponent;

    private readonly record struct Health(int Value) : IComponent;
}
