using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Zeus.Abstractions;
using Zeus.Introspection;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OptimizedSelection
        : IOptimizedSelection
    {
        private readonly object _sync = new object();
        private readonly OperationContext _operationContext;
        private readonly SelectionContext _selectionContext;
        private readonly OptimizedSelectionHelper _selectionHelper;
        private ImmutableDictionary<NamedType, IImmutableList<IOptimizedSelection>> _selections
            = ImmutableDictionary<NamedType, IImmutableList<IOptimizedSelection>>.Empty;
        private ResolverDelegate _resolver;

        public OptimizedSelection(OperationContext operationContext, SelectionContext selectionContext)
        {
            _operationContext = operationContext
                ?? throw new ArgumentNullException(nameof(operationContext));

            _selectionContext = selectionContext
                ?? throw new ArgumentNullException(nameof(selectionContext));

            _selectionHelper = new OptimizedSelectionHelper(
                operationContext,
                selectionContext.FieldDefinition.Type.TypeName());
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_selectionContext.Field.Alias))
                {
                    return _selectionContext.Field.Name;
                }
                return _selectionContext.Field.Alias;
            }
        }

        public ObjectTypeDefinition TypeDefinition => _selectionContext.TypeDefinition;

        public FieldDefinition FieldDefinition => _selectionContext.FieldDefinition;

        public Field Field => _selectionContext.Field;

        public ResolverDelegate Resolver
        {
            get
            {
                if (_resolver == null)
                {
                    _resolver = GetResolverInternal();
                }
                return _resolver;
            }
        }

        public IEnumerable<IOptimizedSelection> GetSelections(IType type)
        {
            NamedType namedType = type.NamedType();
            if (_selections.TryGetValue(namedType, out var selections))
            {
                return selections;
            }

            ISelectionSet fieldSelectionSet = _selectionContext.Field.SelectionSet;
            selections = ResolveSelections(fieldSelectionSet, namedType).ToImmutableList();

            lock (_sync)
            {
                _selections = _selections.SetItem(namedType, selections);
            }

            return selections;
        }

        private IEnumerable<IOptimizedSelection> ResolveSelections(
            IEnumerable<ISelection> selections,
            NamedType typeCondition)
        {
            foreach (ISelection selection in selections)
            {
                if (selection is Field f
                    && _selectionHelper.TryCreateSelectionContext(f, out var sc))
                {
                    yield return new OptimizedSelection(_operationContext, sc);
                }

                if (selection is InlineFragment frag
                    && frag.TypeCondition.Equals(typeCondition))
                {
                    foreach (IOptimizedSelection optimizedSelection
                        in ResolveSelections(frag.SelectionSet, typeCondition))
                    {
                        yield return optimizedSelection;
                    }
                }

                if (selection is FragmentSpread fragSpread)
                {
                    foreach (IOptimizedSelection optimizedSelection
                        in ResolveFragmentSpread(fragSpread, typeCondition))
                    {
                        yield return optimizedSelection;
                    }
                }
            }
        }

        private IEnumerable<IOptimizedSelection> ResolveFragmentSpread(
            FragmentSpread fragmentSpread,
            NamedType typeCondition)
        {
            FragmentDefinition fragmentDefinition = _operationContext.QueryDocument
               .Fragments[fragmentSpread.Name]
               .FirstOrDefault(t => t.TypeCondition.Equals(typeCondition));

            if (fragmentDefinition != null)
            {
                return ResolveSelections(fragmentDefinition.SelectionSet, typeCondition);
            }

            return Enumerable.Empty<IOptimizedSelection>();
        }

        public IResolverContext CreateContext(
            IResolverContext parentContext,
            object parentResult)
        {
            return parentContext.Create(_selectionContext, parentResult);
        }

        private ResolverDelegate GetResolverInternal()
        {
            if (_operationContext.Schema.Resolvers.TryGetResolver(
                TypeDefinition.Name, FieldDefinition.Name, out var resolver))
            {
                return resolver;
            }

            if (TypeName.IsTypeName(_selectionContext.Field.Name))
            {
                return new ResolverDelegate((ctx, ct) => 
                    Task.FromResult<object>(_selectionContext.TypeDefinition.Name));
            }

            var memberResolver = new DynamicMemberResolver(FieldDefinition.Name);
            return new ResolverDelegate((ctx, ct) => memberResolver.ResolveAsync(ctx, ct));
        }
    }
}