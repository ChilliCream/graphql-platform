namespace HotChocolate.Types;

public abstract class SchemaDefinitionVisitor<TContext>
{
    public virtual void VisitSchema(ISchemaDefinition schema, TContext context)
    {
        VisitTypes(schema.Types, context);
        VisitDirectiveDefinitions(schema.DirectiveDefinitions, context);
    }

    public virtual void VisitTypes(
        IReadOnlyTypeDefinitionCollection typesDefinition,
        TContext context)
    {
        foreach (var type in typesDefinition)
        {
            if (type.IsIntrospectionType)
            {
                continue;
            }

            VisitType(type, context);
        }
    }

    public virtual void VisitType(IType type, TContext context)
    {
        switch (type.Kind)
        {
            case TypeKind.Enum:
                VisitEnumType((IEnumTypeDefinition)type, context);
                break;

            case TypeKind.InputObject:
                VisitInputObjectType((IInputObjectTypeDefinition)type, context);
                break;

            case TypeKind.Interface:
                VisitInterfaceType((IInterfaceTypeDefinition)type, context);
                break;

            case TypeKind.Object:
                VisitObjectType((IObjectTypeDefinition)type, context);
                break;

            case TypeKind.Scalar:
                VisitScalarType((IScalarTypeDefinition)type, context);
                break;

            case TypeKind.Union:
                VisitUnionType((IUnionTypeDefinition)type, context);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void VisitDirectiveDefinitions(
        IReadOnlyDirectiveDefinitionCollection directiveTypes,
        TContext context)
    {
        foreach (var type in directiveTypes)
        {
            VisitDirectiveDefinition(type, context);
        }
    }

    public virtual void VisitEnumType(IEnumTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitEnumValues(type.Values, context);
    }

    public virtual void VisitInputObjectType(IInputObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitInputFields(type.Fields, context);
    }

    public virtual void VisitInterfaceType(IInterfaceTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitObjectType(IObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitUnionType(IUnionTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitScalarType(IScalarTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitEnumValues(IReadOnlyEnumValueCollection values, TContext context)
    {
        foreach (var value in values)
        {
            VisitEnumValue(value, context);
        }
    }

    public virtual void VisitEnumValue(IEnumValue value, TContext context)
    {
        VisitDirectives(value.Directives, context);
    }

    public virtual void VisitDirectives(IReadOnlyDirectiveCollection directives, TContext context)
    {
        foreach (var directive in directives)
        {
            VisitDirective(directive, context);
        }
    }

    public virtual void VisitDirective(IDirective directive, TContext context)
    {
        VisitArguments(directive.Arguments, context);
    }

    public virtual void VisitArguments(ArgumentAssignmentCollection arguments, TContext context)
    {
        foreach (var argument in arguments)
        {
            VisitArgument(argument, context);
        }
    }

    public virtual void VisitArgument(ArgumentAssignment argument, TContext context)
    {
    }

    public virtual void VisitInputFields(
        IReadOnlyFieldDefinitionCollection<IInputValueDefinition> fields,
        TContext context)
    {
        foreach (var field in fields)
        {
            VisitInputField(field, context);
        }
    }

    public virtual void VisitInputField(IInputValueDefinition field, TContext context)
    {
        VisitDirectives(field.Directives, context);
    }

    public virtual void VisitOutputFields(
        IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> fields,
        TContext context)
    {
        foreach (var field in fields)
        {
            if (field.IsIntrospectionField)
            {
                continue;
            }

            VisitOutputField(field, context);
        }
    }

    public virtual void VisitOutputField(IOutputFieldDefinition field, TContext context)
    {
        VisitDirectives(field.Directives, context);
        VisitInputFields(field.Arguments, context);
    }

    public virtual void VisitDirectiveDefinition(
        IDirectiveDefinition mutableDirective,
        TContext context)
    {
        VisitInputFields(mutableDirective.Arguments, context);
    }
}
