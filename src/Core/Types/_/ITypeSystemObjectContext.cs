using System;
using System.Linq;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    public interface ITypeSystemObjectContext
    {
        ITypeSystemObject Type { get; }

        bool IsType { get; }

        bool IsIntrospectionType { get; }

        bool IsDirective { get; }

        IServiceProvider Services { get; }

        void ReportError(ISchemaError error);
    }
}
