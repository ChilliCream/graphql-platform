using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal partial class MiddlewareContext
    {
        private ISelection _selection = default!;

        public IObjectType ObjectType => _selection.DeclaringType;

        public IObjectField Field => _selection.Field;

        [Obsolete("Use Selection.SyntaxNode.")]
        public FieldNode FieldSelection => _selection.SyntaxNode;

        public IFieldSelection Selection => _selection;

        public NameString ResponseName => _selection.ResponseName;

        public int ResponseIndex { get; private set; }

        public FieldDelegate ResolverPipeline => _selection.ResolverPipeline;
    }
}
