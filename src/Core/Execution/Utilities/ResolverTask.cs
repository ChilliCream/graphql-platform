using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ResolverTask
    {
        public ResolverTask(
            IExecutionContext executionContext,
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
            ResolverContext = new ResolverContext(executionContext, this);

            Directives = executionContext.CollectDirectives(
                objectType, fieldSelection, DirectiveScope.All);

            if (Directives.Count > 0)
            {
                ExecutableDirectives =
                    Directives.Where(t => t.IsExecutable).ToArray();
                HasExecutableDirectives = ExecutableDirectives.Count > 0;
            }
        }

        public ImmutableStack<object> Source { get; }

        public ObjectType ObjectType { get; }

        public FieldSelection FieldSelection { get; }

        public IType FieldType { get; }

        public Path Path { get; }

        private OrderedDictionary Result { get; }

        public IResolverContext ResolverContext { get; }

        public object ResolverResult { get; set; }

        public IReadOnlyCollection<IDirective> Directives { get; }

        public IReadOnlyCollection<IDirective> ExecutableDirectives { get; }

        public bool HasExecutableDirectives { get; }

        public void IntegrateResult(object value)
        {
            Result[FieldSelection.ResponseName] = value;
        }

        public FieldError CreateError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "A field error mustn't be null or empty.",
                    nameof(message));
            }

            return new FieldError(message, FieldSelection.Selection);
        }
    }
}
