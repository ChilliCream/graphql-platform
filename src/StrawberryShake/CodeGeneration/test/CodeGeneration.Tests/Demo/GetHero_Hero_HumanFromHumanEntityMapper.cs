namespace Foo
{
    [global::System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class GetHero_Hero_HumanFromHumanEntityMapper
        : global::StrawberryShake.IEntityMapper<HumanEntity, GetHero_Hero_Human>
    {
        public GetHero_Hero_Human Map(HumanEntity entity)
        {
            return new GetHero_Hero_Human(
                entity.Name,
                entity.AppearsIn
            );
        }
    }
}
