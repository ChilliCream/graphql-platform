namespace StrawberryShake.Integration.Mappers
{
    public class HumanMapper : IEntityMapper<HumanEntity, Human>
    {
        public Human Map(HumanEntity entity, IEntityStoreSnapshot? snapshot = null) =>
            new(entity.Name);
    }
}
