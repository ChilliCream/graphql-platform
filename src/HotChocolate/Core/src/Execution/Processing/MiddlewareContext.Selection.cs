using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private readonly PureResolverContext _childContext;
    private ISelection _selection = default!;

    public IObjectType ObjectType => _selection.DeclaringType;

    public IObjectField Field => _selection.Field;

    [Obsolete("Use Selection.SyntaxNode.")]
    public FieldNode FieldSelection => _selection.SyntaxNode;

    public ISelection Selection => _selection;

    public string ResponseName => _selection.ResponseName;

    public int ResponseIndex { get; private set; }

    public FieldDelegate? ResolverPipeline => _selection.ResolverPipeline;

    public PureFieldDelegate? PureResolver => _selection.PureResolver;

    public bool TryCreatePureContext(
        ISelection selection,
        ObjectType parentType,
        ObjectResult parentResult,
        object? parent,
        [NotNullWhen(true)] out IPureResolverContext? context)
    {
        if (_childContext.Initialize(selection, parentType, parentResult, parent))
        {
            context = _childContext;
            return true;
        }

        context = null;
        return false;
    }
}
