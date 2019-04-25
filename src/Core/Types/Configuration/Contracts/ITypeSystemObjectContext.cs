using System;
using System.Collections.Generic;
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

        IDictionary<string, object> ContextData { get; }

        void ReportError(ISchemaError error);
    }
}
