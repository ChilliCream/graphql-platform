using System.Runtime.CompilerServices;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// Fragment definitions are referenced in fragment spreads by name.
/// To avoid ambiguity, each fragment’s name must be unique within a
/// document.
///
/// https://spec.graphql.org/June2018/#sec-Fragment-Name-Uniqueness
///
/// AND
///
/// Defined fragments must be used within a document.
///
/// https://spec.graphql.org/June2018/#sec-Fragments-Must-Be-Used
///
/// AND
///
/// Fragments can only be declared on unions, interfaces, and objects.
/// They are invalid on scalars.
/// They can only be applied on non‐leaf fields.
/// This rule applies to both inline and named fragments.
///
/// https://spec.graphql.org/June2018/#sec-Fragments-On-Composite-Types
///
/// AND
///
/// Fragments are declared on a type and will only apply when the
/// runtime object type matches the type condition.
///
/// They also are spread within the context of a parent type.
///
/// A fragment spread is only valid if its type condition could ever
/// apply within the parent type.
///
/// https://spec.graphql.org/June2018/#sec-Fragment-spread-is-possible
///
/// AND
///
/// Named fragment spreads must refer to fragments defined within the
/// document.
///
/// It is a validation error if the target of a spread is not defined.
///
/// https://spec.graphql.org/June2018/#sec-Fragment-spread-target-defined
///
/// AND
///
/// The graph of fragment spreads must not form any cycles including
/// spreading itself. Otherwise, an operation could infinitely spread or
/// infinitely execute on cycles in the underlying data.
///
/// https://spec.graphql.org/June2018/#sec-Fragment-spreads-must-not-form-cycles
///
/// AND
///
/// Fragments must be specified on types that exist in the schema.
/// This applies for both named and inline fragments.
/// If they are not defined in the schema, the query does not validate.
///
/// https://spec.graphql.org/June2018/#sec-Fragment-Spread-Type-Existence
/// </summary>
internal sealed class FragmentVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        var fragmentNames = context.Features.GetOrSet<FragmentVisitorFeature>().FragmentNames;
        fragmentNames.Clear();

        for (var i = 0; i < node.Definitions.Count; i++)
        {
            var definition = node.Definitions[i];
            if (definition.Kind == SyntaxKind.FragmentDefinition)
            {
                var fragment = Unsafe.As<FragmentDefinitionNode>(definition);
                if (!fragmentNames.Add(fragment.Name.Value))
                {
                    context.ReportError(context.FragmentNameNotUnique(fragment));
                }
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        var fragmentNames = context.Features.GetRequired<FragmentVisitorFeature>().FragmentNames;

        foreach (var fragmentName in context.Fragments.Names)
        {
            if (!fragmentNames.Add(fragmentName))
            {
                context.ReportError(context.FragmentNotUsed(context.Fragments[fragmentName]));
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            return Skip;
        }

        if (context.Types.TryPeek(out var type)
            && type.NamedType() is IComplexTypeDefinition ot
            && ot.Fields.TryGetField(node.Name.Value, out var of))
        {
            context.OutputFields.Push(of);
            context.Types.Push(of.Type);
            return Continue;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.Types.Pop();
        context.OutputFields.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        var fragmentNames = context.Features.GetRequired<FragmentVisitorFeature>().FragmentNames;
        fragmentNames.Remove(node.Name.Value);

        if (context.Schema.Types.TryGetType<IOutputTypeDefinition>(
            node.TypeCondition.Name.Value,
            out var type))
        {
            if (type.IsCompositeType())
            {
                ValidateFragmentSpreadIsPossible(
                    node, context,
                    context.Types.Peek().NamedType(),
                    type);
                context.Types.Push(type);
                return Continue;
            }

            context.ReportError(context.FragmentOnlyCompositeType(node, type.NamedType()));
            return Skip;
        }

        context.ReportError(context.FragmentTypeConditionUnknown(node, node.TypeCondition));
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.Fragments.Leave(node);
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        DocumentValidatorContext context)
    {
        if (node.TypeCondition is null)
        {
            return Continue;
        }

        if (context.Schema.Types.TryGetType<IOutputTypeDefinition>(node.TypeCondition.Name.Value, out var type))
        {
            if (type.IsCompositeType())
            {
                ValidateFragmentSpreadIsPossible(
                    node, context,
                    context.Types.Peek().NamedType(),
                    type);
                context.Types.Push(type);
                return Continue;
            }

            context.ReportError(context.FragmentOnlyCompositeType(node, type.NamedType()));
            return Skip;
        }

        context.ReportError(context.FragmentTypeConditionUnknown(node, node.TypeCondition));
        return Skip;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        if (context.Fragments.TryGet(node, out var fragment))
        {
            if (context.Path.Contains(fragment))
            {
                context.ReportError(context.FragmentCycleDetected(node));
            }
        }
        else
        {
            context.ReportError(context.FragmentDoesNotExist(node));
        }

        return Continue;
    }

    private static void ValidateFragmentSpreadIsPossible(
        ISyntaxNode node,
        DocumentValidatorContext context,
        ITypeDefinition parentType,
        ITypeDefinition typeCondition)
    {
        if (!IsCompatibleType(context, parentType, typeCondition))
        {
            context.ReportError(context.FragmentNotPossible(node, typeCondition, parentType));
        }
    }

    private static bool IsCompatibleType(
        DocumentValidatorContext context,
        ITypeDefinition parentType,
        ITypeDefinition typeCondition)
    {
        if (parentType.IsAssignableFrom(typeCondition))
        {
            return true;
        }

        var types1 = context.Schema.GetPossibleTypes(parentType);
        var types2 = context.Schema.GetPossibleTypes(typeCondition);

        foreach (var a in types1)
        {
            foreach (var b in types2)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private sealed class FragmentVisitorFeature : ValidatorFeature
    {
        public HashSet<string> FragmentNames { get; } = [];

        protected internal override void Reset() => FragmentNames.Clear();
    }
}
