namespace HotChocolate.Skimmed;

public abstract class SchemaVisitor<TContext>
{
    public virtual void VisitSchema(Schema schema, TContext context)
    {
        VisitTypes(schema.Types, context);
        VisitDirectiveTypes(schema.DirectiveTypes, context);
    }

    public virtual void VisitTypes(TypeCollection types, TContext context)
    {
        foreach (var type in types)
        {
            VisitType(type, context);
        }
    }

    public virtual void VisitType(IType type, TContext context)
    {
        switch (type.Kind)
        {
            case TypeKind.Enum:
                VisitEnumType((EnumType)type, context);
                break;

            case TypeKind.InputObject:
                VisitInputObjectType((InputObjectType)type, context);
                break;

            case TypeKind.Interface:
                VisitInterfaceType((InterfaceType)type, context);
                break;

            case TypeKind.Object:
                VisitObjectType((ObjectType)type, context);
                break;

            case TypeKind.Scalar:
                if (type is MissingType)
                {
                    break;
                }
                
                VisitScalarType((ScalarType)type, context);
                break;

            case TypeKind.Union:
                VisitUnionType((UnionType)type, context);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void VisitDirectiveTypes(DirectiveTypeCollection directiveTypes, TContext context)
    {
        foreach (var type in directiveTypes)
        {
            VisitDirectiveType(type, context);
        }
    }

    public virtual void VisitEnumType(EnumType type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitEnumValues(type.Values, context);
    }

    public virtual void VisitInputObjectType(InputObjectType type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitInputFields(type.Fields, context);
    }

    public virtual void VisitInterfaceType(InterfaceType type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitObjectType(ObjectType type, TContext context)
    {
        VisitDirectives(type.Directives, context);
        VisitOutputFields(type.Fields, context);
    }

    public virtual void VisitUnionType(UnionType type, TContext context)
    {
        VisitDirectives(type.Directives, context);
    }

    public virtual void VisitScalarType(ScalarType type, TContext context)
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

    public virtual void VisitEnumValue(EnumValue value, TContext context)
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

    public virtual void VisitArguments(ArgumentCollection arguments, TContext context)
    {
        foreach (var argument in arguments)
        {
            VisitArgument(argument, context);
        }
    }

    public virtual void VisitArgument(Argument argument, TContext context)
    {

    }

    public virtual void VisitInputFields(FieldCollection<InputField> fields, TContext context)
    {
        foreach (var field in fields)
        {
            VisitInputField(field, context);
        }
    }

    public virtual void VisitInputField(InputField field, TContext context)
    {
        VisitDirectives(field.Directives, context);
    }

    public virtual void VisitOutputFields(FieldCollection<OutputField> fields, TContext context)
    {
        foreach (var field in fields)
        {
            VisitOutputField(field, context);
        }
    }

    public virtual void VisitOutputField(OutputField field, TContext context)
    {
        VisitDirectives(field.Directives, context);
        VisitInputFields(field.Arguments, context);
    }

    public virtual void VisitDirectiveType(DirectiveType directive, TContext context)
    {
        VisitInputFields(directive.Arguments, context);
    }
}
