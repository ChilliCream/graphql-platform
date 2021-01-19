namespace HotChocolate.Types.Spatial.Configuration
{
    public interface ISpatialConventionDescriptor
    {
        ISpatialConventionDescriptor DefaultSrid(int srid);

        ISpatialConventionDescriptor AddCoordinateSystemFromString(int srid, string wellKnownText);
    }
}
