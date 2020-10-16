using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing
{
    public class DefaultFetchConfiguration : IFetchConfiguration
    {
        private readonly MatchSelectionsVisitor _matchSelections = new MatchSelectionsVisitor();
        private readonly ISchema _schema;

        public DefaultFetchConfiguration(
            ISchema schema,
            NameString schemaName,
            NameString typeName,
            IReadOnlyList<Field> requires,
            SelectionSetNode provides)
        {
            _schema = schema;
            SchemaName = schemaName;
            TypeName = typeName;
            Requires = requires;
            Provides = provides;
        }

        public NameString SchemaName { get; }

        public NameString TypeName { get; }

        public IReadOnlyList<Field> Requires { get; }

        public SelectionSetNode Provides { get; }

        public bool CanHandleSelections(
            IPreparedOperation operation,
            ISelectionSet selectionSet,
            IObjectType objectType,
            out IReadOnlyList<ISelection> handledSelections)
        {
            var context = new MatchSelectionsContext(_schema, operation, selection);
            _matchSelections.Visit(Provides, context);
        }
    }
}
