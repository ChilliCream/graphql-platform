using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    internal class SchemaMerger
    {

    }

    public interface ISchemaInfo
    {
        string Name { get; }
        DocumentNode Schema { get; }
    }

    public interface ITypeInfo
    {
        ITypeDefinitionNode Definition { get; }

        DocumentNode Schema { get; }

        string SchemaName { get; }
    }

    public interface ITypeMergerContext
    {
        void AddType(ITypeDefinitionNode type);
    }

    public interface ITypeMerger
    {
        void Merge(
            ITypeMergerContext context,
            IReadOnlyList<ITypeInfo> types);
    }
}
