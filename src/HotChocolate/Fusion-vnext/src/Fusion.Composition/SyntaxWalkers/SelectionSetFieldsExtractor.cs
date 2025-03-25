using System.Collections.Immutable;
using HotChocolate.Fusion.Info;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.SyntaxWalkers;

/// <summary>
/// This class is used to extract the fields from a selection set.
/// </summary>
/// <param name="schema">The schema from which to resolve types.</param>
internal sealed class SelectionSetFieldsExtractor(MutableSchemaDefinition schema)
    : SyntaxWalker<SelectionSetFieldsExtractorContext>
{
    public ImmutableArray<OutputFieldInfo> ExtractFields(
        SelectionSetNode selectionSet,
        ITypeDefinition type)
    {
        var context = new SelectionSetFieldsExtractorContext(type);

        Visit(selectionSet, context);

        return [.. context.Fields];
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        SelectionSetFieldsExtractorContext context)
    {
        var type = context.TypeContext.Peek();

        if (type is MutableComplexTypeDefinition complexType)
        {
            if (complexType.Fields.TryGetField(node.Name.Value, out var field))
            {
                context.Fields.Add(new OutputFieldInfo(field, complexType, schema));

                var fieldType = field.Type.NullableType();

                if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
                {
                    if (node.SelectionSet?.Selections.Any() != true)
                    {
                        // The field returns a composite type and must have subselections.
                        return Break;
                    }

                    if (fieldType is MutableUnionTypeDefinition
                        && node.SelectionSet.Selections.Any(s => s is not InlineFragmentNode))
                    {
                        // The field returns a union type and must only include inline fragments.
                        return Break;
                    }

                    context.TypeContext.Push(fieldType.AsTypeDefinition());
                }
                else if (node.SelectionSet is not null)
                {
                    // The field does not return a composite type and cannot have subselections.
                    return Skip;
                }
                else
                {
                    return Skip;
                }
            }
            else
            {
                // The field does not exist on the type.
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        SelectionSetFieldsExtractorContext context)
    {
        context.TypeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        SelectionSetFieldsExtractorContext context)
    {
        if (node.TypeCondition is { } typeCondition)
        {
            if (!schema.Types.TryGetType(typeCondition.Name.Value, out var concreteType))
            {
                // The type condition in the selection set is invalid. The type does not exist.
                return Break;
            }

            if (ValidateConcreteType(concreteType, context))
            {
                context.TypeContext.Push(concreteType);
            }
            else
            {
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        InlineFragmentNode node,
        SelectionSetFieldsExtractorContext context)
    {
        if (node.TypeCondition is not null)
        {
            context.TypeContext.Pop();
        }

        return Continue;
    }

    private static bool ValidateConcreteType(
        ITypeDefinition concreteType,
        SelectionSetFieldsExtractorContext context)
    {
        var type = context.TypeContext.Peek();

        switch (type)
        {
            case MutableInterfaceTypeDefinition interfaceType:
                if (concreteType is MutableComplexTypeDefinition complexType
                    && complexType.Implements.Contains(interfaceType))
                {
                    return true;
                }

                break;
            case MutableObjectTypeDefinition:
            case MutableUnionTypeDefinition:
                var possibleTypes = GetPossibleTypes(type);

                if (possibleTypes.Contains(concreteType))
                {
                    return true;
                }

                break;
        }

        return false;
    }

    private static ImmutableHashSet<MutableComplexTypeDefinition> GetPossibleTypes(
        ITypeDefinition type)
    {
        switch (type)
        {
            case MutableObjectTypeDefinition objectType:
                return [objectType];

            case MutableUnionTypeDefinition unionType:
                var builder = ImmutableHashSet.CreateBuilder<MutableComplexTypeDefinition>();

                foreach (var memberType in unionType.Types)
                {
                    builder.Add(memberType);

                    foreach (var memberInterface in memberType.Implements)
                    {
                        builder.Add(memberInterface);
                    }
                }

                return builder.ToImmutable();

            default:
                throw new InvalidOperationException();
        }
    }
}

internal sealed class SelectionSetFieldsExtractorContext
{
    public SelectionSetFieldsExtractorContext(ITypeDefinition type)
    {
        TypeContext.Push(type);
    }

    public Stack<ITypeDefinition> TypeContext { get; } = [];

    public List<OutputFieldInfo> Fields { get; } = [];
}
