namespace StrawberryShake.Remove.Mappers
{
    public class DroidMapper : IEntityMapper<DroidEntity, Droid>
    {
        public Droid Map(DroidEntity entity) => new(entity.Name);
    }
}
