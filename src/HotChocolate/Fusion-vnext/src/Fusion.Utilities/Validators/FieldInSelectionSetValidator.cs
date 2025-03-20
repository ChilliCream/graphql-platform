using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Validators;

/// <summary>
/// This class validates whether a field is present in a selection set.
/// </summary>
/// <param name="schema">The schema from which to resolve types.</param>
public sealed class FieldInSelectionSetValidator(MutableSchemaDefinition schema)
    : SyntaxWalker<FieldInSelectionSetValidatorContext>
{
    public bool Validate(
        SelectionSetNode selectionSet,
        ITypeDefinition type,
        MutableOutputFieldDefinition field,
        ITypeDefinition declaringType)
    {
        var context = new FieldInSelectionSetValidatorContext(type, field, declaringType);

        Visit(selectionSet, context);

        return context.IsSelected;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        FieldInSelectionSetValidatorContext context)
    {
        var type = context.TypeContext.Peek();

        if (context.DeclaringType == type && context.Field.Name == node.Name.Value)
        {
            context.IsSelected = true;
            return Break;
        }

        if (type is MutableComplexTypeDefinition complexType)
        {
            if (complexType.Fields.TryGetField(node.Name.Value, out var field))
            {
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
        FieldInSelectionSetValidatorContext context)
    {
        context.TypeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        FieldInSelectionSetValidatorContext context)
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
        FieldInSelectionSetValidatorContext context)
    {
        if (node.TypeCondition is not null)
        {
            context.TypeContext.Pop();
        }

        return Continue;
    }

    private static bool ValidateConcreteType(
        ITypeDefinition concreteType,
        FieldInSelectionSetValidatorContext context)
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

public sealed class FieldInSelectionSetValidatorContext
{
    public FieldInSelectionSetValidatorContext(
        ITypeDefinition type,
        MutableOutputFieldDefinition field,
        ITypeDefinition declaringType)
    {
        TypeContext.Push(type);
        Field = field;
        DeclaringType = declaringType;
    }

    public Stack<ITypeDefinition> TypeContext { get; } = [];

    public MutableOutputFieldDefinition Field { get; }

    public ITypeDefinition DeclaringType { get; }

    public bool IsSelected { get; set; }
}
