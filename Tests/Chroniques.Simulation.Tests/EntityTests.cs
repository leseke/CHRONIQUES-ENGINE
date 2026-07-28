using Chroniques.Simulation.Kernel;
using Xunit;

namespace Chroniques.Simulation.Tests;

/// <summary>
/// Vérifie CORE-002-C : deux Entity distinctes ne peuvent jamais partager
/// la même identité, et l'identité reste stable pendant toute l'existence
/// de l'Entity.
/// </summary>
public class EntityTests
{
    [Fact]
    public void Deux_entities_creees_separement_ont_des_identites_distinctes()
    {
        var a = Entity.Create();
        var b = Entity.Create();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Une_entity_reste_identifiable_apres_ajout_dun_component()
    {
        var entity = Entity.Create();
        var idAvant = entity.Id;

        entity.Set(new FakeComponent(42));

        Assert.Equal(idAvant, entity.Id);
    }

    [Fact]
    public void Une_entity_peut_exister_sans_aucun_component()
    {
        // CORE-002-B, section 4 : l'absence d'un Component ne remet pas en
        // cause l'existence de l'Entity.
        var entity = Entity.Create();

        Assert.Empty(entity.Components);
        Assert.False(entity.Has<FakeComponent>());
    }

    private readonly record struct FakeComponent(int Value) : IComponent;
}
