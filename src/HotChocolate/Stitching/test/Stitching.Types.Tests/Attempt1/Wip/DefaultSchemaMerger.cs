using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public class DefaultSchemaMerger : ISchemaMerger
{
    //private readonly IEnumerable<IMergeRule> _rules;

    //public DefaultSchemaMerger(IEnumerable<IMergeRule> rules)
    //{
    //    _rules = rules;
    //}

    public ISchemaDocument Merge(IEnumerable<ISchemaDocument> schemas)
    {
        var schemaDocument = new DocumentNode(new List<IDefinitionNode>());
        var documentDefinition = new DocumentDefinition();
        foreach (ISchemaDocument schema in schemas)
        {
            MergeInto(schema, documentDefinition);
        }

        throw new NotImplementedException();
    }

    private void MergeInto(ISchemaDocument source, DocumentDefinition target)
    {
        //var visitor = new DefaultSyntaxNodeVisitor(target);
        //source.Accept(visitor);
    }
}
