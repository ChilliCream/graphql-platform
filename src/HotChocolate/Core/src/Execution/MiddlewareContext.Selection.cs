using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private IPreparedSelection _selection = default!;
        
        public ObjectType ObjectType => _selection.DeclaringType;

        public ObjectField Field => _selection.Field;

        public FieldNode FieldSelection => _selection.Selection;

        public NameString ResponseName => _selection.ResponseName;
    }
}
