using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Processing
{
    public class DefaultFetchConfiguration : IFetchConfiguration
    {
        public DefaultFetchConfiguration(
            NameString schemaName, 
            NameString typeName, 
            IReadOnlyList<Field> requires, 
            SelectionSetNode provides)
        {
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
            ISelection selection, 
            out IReadOnlyList<ISelection> handledSelections)
        {
            throw new System.Exception();
        }
    }
}
