using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class ResolverTask
    {
        public ResolverTask(
            ObjectType objectType,
            FieldSelection fieldSelection,
            Path path,
            ImmutableStack<object> source,
            OrderedDictionary result)
        {
            Source = source;
            ObjectType = objectType;
            FieldSelection = fieldSelection;
            FieldType = fieldSelection.Field.Type;
            Path = path;
            Result = result;
        }

        public ImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        public OrderedDictionary Result { get; }

        public IResolverContext ResolverContext { get; }

        public object ResolverResult { get; set; }

        public void IntegrateResult(object value)
        {
            Result[FieldSelection.ResponseName] = value;
        }

        public FieldError CreateError(string message)
        {
            throw new NotImplementedException();
        }
    }
}
