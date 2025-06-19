using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Leave(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        context.SetAuthorizeDirectives([.. context.GetDirectives()]);
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        var result = base.Enter(node, context);

        var root = (IObjectTypeDefinition)context.Types.Peek();
        CollectAuthorizeDirective(root.Directives, context.GetDirectives());

        return result;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.EqualsOrdinal(node.Name.Value))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is IComplexTypeDefinition ct &&
            ct.Fields.TryGetField(node.Name.Value, out var of))
        {
            CollectAuthorizeDirective(of.Directives, context.GetDirectives());
            CollectAuthorizeDirective(context.Schema, of.Type.NamedType(), context.GetDirectives());

            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    private static void CollectAuthorizeDirective(
        ISchemaDefinition schema,
        IType type,
        HashSet<AuthorizeDirective> authDirectives)
    {
        switch (type)
        {
            case IObjectTypeDefinition objectType:
                CollectAuthorizeDirective(objectType.Directives, authDirectives);
                break;

            case IInterfaceTypeDefinition interfaceType:
                foreach (var objectType in schema.GetPossibleTypes(interfaceType))
                {
                    CollectAuthorizeDirective(objectType.Directives, authDirectives);
                }
                break;

            case IUnionTypeDefinition unionType:
                foreach (var objectType in schema.GetPossibleTypes(unionType))
                {
                    CollectAuthorizeDirective(objectType.Directives, authDirectives);
                }
                break;
        }
    }

    private static void CollectAuthorizeDirective(
        IReadOnlyDirectiveCollection directives,
        HashSet<AuthorizeDirective> authDirectives)
    {
        var length = directives.Count;

        if (length == 0)
        {
            return;
        }

        for (var i = 0; i < length; i++)
        {
            var directive = directives[i];
            if (directive.Definition is AuthorizeDirectiveType)
            {
                var authDirective = Unsafe.As<Directive>(directive).ToValue<AuthorizeDirective>();
                if (authDirective.Apply is ApplyPolicy.Validation)
                {
                    authDirectives.Add(authDirective);
                }
            }
        }
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.OutputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectionSetNode node,
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is { Kind: TypeKind.Union } &&
            HasFields(node))
        {
            return Skip;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        if (context.Fragments.TryEnter(node, out var fragment))
        {
            var result = Visit(fragment, node, context);
            context.Fragments.Leave(fragment);

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
        => fieldName.EqualsOrdinal(IntrospectionFieldNames.TypeName);
}

file sealed class VisitorFeature
{
    public HashSet<AuthorizeDirective> Directives { get; } = [];
}

file static class VisitorFeatureExtensions
{
    public static HashSet<AuthorizeDirective> GetDirectives(this DocumentValidatorContext context)
        => context.Features.GetOrSet<VisitorFeature>().Directives;
}
