namespace HotChocolate.Types.Mutable;

public abstract class SchemaVisitor<TContext>
{
    public virtual void VisitSchema(MutableSchemaDefinition schema, TContext context)
    {
        VisitTypes(schema.Types, context);
        VisitDirectiveDefinitions(schema.DirectiveDefinitions, context);
    }

    public virtual void VisitTypes(TypeDefinitionCollection typesDefinition, TContext context)
    {
        foreach (var type in typesDefinition)
        {
            VisitType(type, context);
        }
    }

    public virtual void VisitType(IType type, TContext context)
    {
        switch (type.Kind)
        {
            case TypeKind.Enum:
                VisitEnumType((MutableEnumTypeDefinition)type, context);
                break;

            case TypeKind.InputObject:
                VisitInputObjectType((MutableInputObjectTypeDefinition)type, context);
                break;

            case TypeKind.Interface:
                VisitInterfaceType((MutableInterfaceTypeDefinition)type, context);
                break;

            case TypeKind.Object:
                VisitObjectType((MutableObjectTypeDefinition)type, context);
                break;

            case TypeKind.Scalar:
                if (type is MissingType)
                {
                    break;
                }

                VisitScalarType((MutableScalarTypeDefinition)type, context);
                break;

            case TypeKind.Union:
                VisitUnionType((MutableUnionTypeDefinition)type, context);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void VisitDirectiveDefinitions(DirectiveDefinitionCollection directiveTypes, TContext context)
    {
        foreach (var type in directiveTypes)
        {
            VisitDirectiveDefinition(type, context);
        }
    }

    public virtual void VisitEnumType(MutableEnumTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitEnumValues(type.Values, context);
    }

    public virtual void VisitInputObjectType(MutableInputObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitInputFields(type.Fields, context);
    }

    public virtual void VisitInterfaceType(MutableInterfaceTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitObjectType(MutableObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitUnionType(MutableUnionTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitScalarType(MutableScalarTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitEnumValues(EnumValueCollection values, TContext context)
    {
        foreach (var value in values)
        {
            VisitEnumValue(value, context);
        }
    }

    public virtual void VisitEnumValue(MutableEnumValue value, TContext context)
    {
        VisitDirectives(value.Directives, context);
    }

    public virtual void VisitDirectives(DirectiveCollection directives, TContext context)
    {
        foreach (var directive in directives)
        {
            VisitDirective(directive, context);
        }
    }

    public virtual void VisitDirective(Directive directive, TContext context)
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

    public virtual void VisitInputFields(InputFieldDefinitionCollection fields, TContext context)
    {
        foreach (var field in fields)
        {
            VisitInputField(field, context);
        }
    }

    public virtual void VisitInputField(MutableInputFieldDefinition field, TContext context)
    {
        VisitDirectives(field.Directives, context);
    }

    public virtual void VisitOutputFields(OutputFieldDefinitionCollection fields, TContext context)
    {
        foreach (var field in fields)
        {
            VisitOutputField(field, context);
        }
    }

    public virtual void VisitOutputField(MutableOutputFieldDefinition field, TContext context)
    {
        VisitDirectives(field.Directives, context);
        VisitInputFields(field.Arguments, context);
    }

    public virtual void VisitDirectiveDefinition(MutableDirectiveDefinition mutableDirective, TContext context)
    {
        VisitInputFields(mutableDirective.Arguments, context);
    }
}
