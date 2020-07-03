using System;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    public interface ITypeSystemObjectContext
    {
        ITypeSystemObject Type { get; }

        bool IsType { get; }

        bool IsIntrospectionType { get; }

        bool IsDirective { get; }

        bool IsSchema { get; }

        IServiceProvider Services { get; }

        IDictionary<string, object> ContextData { get; }

        IDescriptorContext DescriptorContext { get; }

        ITypeInitializationInterceptor Interceptor { get; }

        void ReportError(ISchemaError error);
    }
}
