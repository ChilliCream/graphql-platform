using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding.Rewriters;

internal delegate T RewriteFieldsDelegate<out T>(
    IReadOnlyList<FieldDefinitionNode> fields)
    where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode;
