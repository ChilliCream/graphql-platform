using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        IDocumentValidatorContext context)
    {
        if (!context.ContextData.TryGetValue(AuthContextData.Directives, out var value) ||
            value is not HashSet<AuthorizeDirective> authDirectives)
        {
            authDirectives = [];
            context.ContextData[AuthContextData.Directives] = authDirectives;
        }

        authDirectives.Clear();

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        var result = base.Enter(node, context);

        var root = (ObjectType)context.Types.Peek();
        CollectAuthorizeDirective(root.Directives, GetAuthDirectives(context));

        return result;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        if (IntrospectionFields.TypeName.EqualsOrdinal(node.Name.Value))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is IComplexOutputType ct &&
            ct.Fields.TryGetField(node.Name.Value, out var of))
        {
            var authDirectives = GetAuthDirectives(context);

            CollectAuthorizeDirective(of.Directives, authDirectives);
            CollectAuthorizeDirective(context.Schema, of.Type.NamedType(), authDirectives);

            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    private static void CollectAuthorizeDirective(
        ISchema schema,
        IType type,
        HashSet<AuthorizeDirective> authDirectives)
    {
        switch (type)
        {
            case ObjectType objectType:
                CollectAuthorizeDirective(objectType.Directives, authDirectives);
                break;

            case InterfaceType interfaceType:
                foreach (var objectType in schema.GetPossibleTypes(interfaceType))
                {
                    CollectAuthorizeDirective(objectType.Directives, authDirectives);
                }
                break;

            case UnionType unionType:
                foreach (var objectType in schema.GetPossibleTypes(unionType))
                {
                    CollectAuthorizeDirective(objectType.Directives, authDirectives);
                }
                break;
        }
    }

    private static void CollectAuthorizeDirective(
        IDirectiveCollection directives,
        HashSet<AuthorizeDirective> authDirectives)
    {
        var length = directives.Count;

        if (length == 0)
        {
            return;
        }

        ref var start = ref ((DirectiveCollection)directives).GetReference();

        for (var i = 0; i < length; i++)
        {
            var directive = Unsafe.Add(ref start, i);

            if (directive.Type is AuthorizeDirectiveType)
            {
                var authDirective = directive.AsValue<AuthorizeDirective>();
                if (authDirective.Apply is ApplyPolicy.Validation)
                {
                    authDirectives.Add(authDirective);
                }
            }
        }
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        context.OutputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectionSetNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is { Kind: TypeKind.Union, } &&
            HasFields(node))
        {
            return Skip;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        if (context.Fragments.TryGetValue(node.Name.Value, out var fragment) &&
            context.VisitedFragments.Add(fragment.Name.Value))
        {
            var result = Visit(fragment, node, context);
            context.VisitedFragments.Remove(fragment.Name.Value);

            if (result.IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    private static bool HasFields(SelectionSetNode selectionSet)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection.Kind is SyntaxKind.Field)
            {
                if (!IsTypeNameField(((FieldNode)selection).Name.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeNameField(string fieldName)
        => fieldName.EqualsOrdinal(IntrospectionFields.TypeName);

    private static HashSet<AuthorizeDirective> GetAuthDirectives(
        IDocumentValidatorContext context)
        => (HashSet<AuthorizeDirective>)context.ContextData[AuthContextData.Directives]!;
}
