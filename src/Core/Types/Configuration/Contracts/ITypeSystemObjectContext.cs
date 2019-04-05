using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration
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
