using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    public delegate ITypeDefinitionNode RewriteTypeDefinitionDelegate(
        ISchemaInfo schema,
        ITypeDefinitionNode typeDefinition);

    internal class DelegateTypeRewriter
        : ITypeRewriter
    {
        private readonly RewriteTypeDefinitionDelegate _rewrite;

        public DelegateTypeRewriter(RewriteTypeDefinitionDelegate rewrite)
        {
            _rewrite = rewrite
                ?? throw new ArgumentNullException(nameof(rewrite));
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            return _rewrite.Invoke(schema, typeDefinition);
        }
    }
}
