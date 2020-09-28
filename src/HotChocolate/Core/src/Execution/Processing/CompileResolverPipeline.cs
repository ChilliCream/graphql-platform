using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    public delegate FieldDelegate CompileResolverPipeline(
        IObjectField field,
        FieldNode selection);
}
