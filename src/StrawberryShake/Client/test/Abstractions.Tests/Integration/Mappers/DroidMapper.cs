namespace StrawberryShake.Integration.Mappers
{
    public class DroidMapper : IEntityMapper<DroidEntity, Droid>
    {
        public Droid Map(DroidEntity entity) => new(entity.Name);
    }
}
