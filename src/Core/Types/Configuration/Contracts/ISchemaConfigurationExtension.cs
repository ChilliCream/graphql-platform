using System;

namespace HotChocolate.Configuration
{
    public interface ISchemaConfigurationExtension
    {
        ISchemaConfiguration OnBeforeBuild(Action<ISchemaBuilder> build);
    }
}
