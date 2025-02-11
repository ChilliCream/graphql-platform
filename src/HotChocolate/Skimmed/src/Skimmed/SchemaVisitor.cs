namespace HotChocolate.Types.Mutable;

public abstract class SchemaVisitor<TContext>
{
    public virtual void VisitSchema(SchemaDefinition schema, TContext context)
    {
        VisitTypes(schema.Types, context);
        VisitDirectiveDefinitions(schema.DirectiveDefinitions, context);
    }

    public virtual void VisitTypes(ITypeDefinitionCollection typesDefinition, TContext context)
    {
        foreach (var type in typesDefinition)
        {
            VisitType(type, context);
        }
    }

    public virtual void VisitType(ITypeDefinition type, TContext context)
    {
        switch (type.Kind)
        {
            case TypeKind.Enum:
                VisitEnumType((MutableEnumTypeDefinition)type, context);
                break;

            case TypeKind.InputObject:
                VisitInputObjectType((InputObjectTypeDefinition)type, context);
                break;

            case TypeKind.Interface:
                VisitInterfaceType((InterfaceTypeDefinition)type, context);
                break;

            case TypeKind.Object:
                VisitObjectType((ObjectTypeDefinition)type, context);
                break;

            case TypeKind.Scalar:
                if (type is MissingTypeDefinition)
                {
                    break;
                }

                VisitScalarType((ScalarTypeDefinition)type, context);
                break;

            case TypeKind.Union:
                VisitUnionType((UnionTypeDefinition)type, context);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void VisitDirectiveDefinitions(IDirectiveDefinitionCollection directiveTypes, TContext context)
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

    public virtual void VisitInputObjectType(InputObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitInputFields(type.Fields, context);
    }

    public virtual void VisitInterfaceType(InterfaceTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitObjectType(ObjectTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitUnionType(UnionTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitScalarType(ScalarTypeDefinition type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitEnumValues(IEnumValueCollection values, TContext context)
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

    public virtual void VisitDirectives(IDirectiveCollection directives, TContext context)
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

    public virtual void VisitInputFields(IFieldDefinitionCollection<MutableInputFieldDefinition> fields, TContext context)
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

    public virtual void VisitOutputFields(IFieldDefinitionCollection<OutputFieldDefinition> fields, TContext context)
    {
        foreach (var field in fields)
        {
            VisitOutputField(field, context);
        }
    }

    public virtual void VisitOutputField(OutputFieldDefinition field, TContext context)
    {
        VisitDirectives(field.Directives, context);
        VisitInputFields(field.Arguments, context);
    }

    public virtual void VisitDirectiveDefinition(MutableDirectiveDefinition mutableDirective, TContext context)
    {
        VisitInputFields(mutableDirective.Arguments, context);
    }
}
