using HotChocolate.Configuration;

namespace HotChocolate
{
    public interface ISchemaConfiguration
        : ISchemaFirstConfiguration
        , ICodeFirstConfiguration
    {
        ISchemaOptions Options { get; }
    }


}
