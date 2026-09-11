using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers;

internal static class BatchSelectionContext
{
    public static IResolverContext Create(ImmutableArray<IMiddlewareContext> contexts)
    {
        var first = contexts[0];
        var flags = first.IncludeConditionFlags;
        var word0 = flags.Word0;
        var length = flags.Overflow?.Length ?? 0;

        for (var i = 1; i < contexts.Length; i++)
        {
            var next = contexts[i].IncludeConditionFlags;
            word0 |= next.Word0;
            length = Math.Max(length, next.Overflow?.Length ?? 0);
        }

        ulong[]? overflow = null;

        for (var i = 1; i < contexts.Length; i++)
        {
            var next = contexts[i].IncludeConditionFlags.Overflow;

            if (next is null)
            {
                continue;
            }

            for (var j = 0; j < next.Length; j++)
            {
                var current = overflow is not null
                    ? overflow[j]
                    : j < (flags.Overflow?.Length ?? 0) ? flags.Overflow![j] : 0;

                if ((current | next[j]) != current)
                {
                    if (overflow is null)
                    {
                        overflow = new ulong[length];
                        flags.Overflow?.CopyTo(overflow, 0);
                    }

                    overflow[j] |= next[j];
                }
            }
        }

        if (word0 == flags.Word0 && overflow is null)
        {
            return first;
        }

        return new UnionContext(first, new ConditionFlags(word0, overflow ?? flags.Overflow));
    }

    private sealed class UnionContext(IResolverContext context, ConditionFlags flags) : IResolverContext
    {
        public Schema Schema => context.Schema;
        public ObjectType ObjectType => context.ObjectType;
        public Operation Operation => context.Operation;
        public Selection Selection => context.Selection;
        public IVariableValueCollection Variables => context.Variables;
        public Path Path => context.Path;
        public ulong IncludeFlags => flags.Word0;
        public ConditionFlags IncludeConditionFlags => flags;
        public IServiceProvider RequestServices => context.RequestServices;
        public string ResponseName => context.ResponseName;
        public bool HasErrors => context.HasErrors;
        public CancellationToken RequestAborted => context.RequestAborted;
        public IDictionary<string, object?> ContextData => context.ContextData;
        public IFeatureCollection Features => context.Features;

        public IServiceProvider Services
        {
            get => context.Services;
            set => context.Services = value;
        }

        public IImmutableDictionary<string, object?> ScopedContextData
        {
            get => context.ScopedContextData;
            set => context.ScopedContextData = value;
        }

        public IImmutableDictionary<string, object?> LocalContextData
        {
            get => context.LocalContextData;
            set => context.LocalContextData = value;
        }

        public T Parent<T>() => context.Parent<T>();
        public T ArgumentValue<T>(string name) => context.ArgumentValue<T>(name);
        public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
            => context.ArgumentLiteral<TValueNode>(name);
        public Optional<T> ArgumentOptional<T>(string name) => context.ArgumentOptional<T>(name);
        public ValueKind ArgumentKind(string name) => context.ArgumentKind(name);
        public T Service<T>() where T : notnull => context.Service<T>();
        public T Service<T>(object key) where T : notnull => context.Service<T>(key);
        public object Service(Type service) => context.Service(service);
        public T Resolver<T>() => context.Resolver<T>();
        public void ReportError(string errorMessage) => context.ReportError(errorMessage);
        public void ReportError(IError error) => context.ReportError(error);
        public void ReportError(Exception exception, Action<ErrorBuilder>? configure)
            => context.ReportError(exception, configure);

        public SelectionEnumerator GetSelections(
            ObjectType typeContext,
            Selection? selection,
            bool allowInternals)
        {
            selection ??= Selection;
            return selection.IsLeaf
                ? default
                : new SelectionEnumerator(Operation.GetSelectionSet(selection, typeContext), flags);
        }

        public ISelectionCollection Select()
            => new SelectionCollection(Schema, Operation, [Selection], flags);
        public ISelectionCollection Select(string fieldName) => Select().Select(fieldName);
        public T GetQueryRoot<T>() => context.GetQueryRoot<T>();
        public IResolverContext Clone() => new UnionContext(context.Clone(), flags);
    }
}
