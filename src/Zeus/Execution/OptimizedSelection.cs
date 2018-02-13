using System;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    public class OptimizedSelection
        : IOptimizedSelection
    {
        private readonly SelectionContext _selectionContext;

        public OptimizedSelection(
            SelectionContext selectionContext,
            IResolver resolver,
            IEnumerable<IOptimizedSelection> selections)
        {
            _selectionContext = selectionContext ?? throw new ArgumentNullException(nameof(selectionContext));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            Selections = selections == null
                ? Array.Empty<IOptimizedSelection>()
                : selections.ToArray();
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

        public IResolver Resolver { get; }

        public IReadOnlyCollection<IOptimizedSelection> Selections { get; }

        public IResolverContext CreateContext(
            IResolverContext parentContext,
            object parentResult)
        {
            return parentContext.Create(_selectionContext, parentResult);
        }
    }
}