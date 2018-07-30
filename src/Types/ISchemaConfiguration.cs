using System;
using HotChocolate.Configuration;

namespace HotChocolate
{
    public interface ISchemaConfiguration
        : ISchemaFirstConfiguration
        , ICodeFirstConfiguration
        , IDataLoaderConfiguration
        , ICustomContextConfiguration
    {
        ISchemaOptions Options { get; }

        void RegisterServiceProvider(IServiceProvider serviceProvider);
    }
}
