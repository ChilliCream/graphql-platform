namespace StrawberryShake.Remove.Mappers
{
    public class HumanMapper : IEntityMapper<HumanEntity, Human>
    {
        public Human Map(HumanEntity entity) => new(entity.Name);
    }
}
