using System;
using HotChocolate.Configuration;

namespace HotChocolate
{
    public interface ISchemaConfiguration
        : ISchemaFirstConfiguration
        , ICodeFirstConfiguration
        , IDataLoaderConfiguration
        , IUserStateConfiguration
    {
        ISchemaOptions Options { get; }

        void RegisterServiceProvider(IServiceProvider serviceProvider);
    }

    public interface IUserStateConfiguration
    {

    }
}
