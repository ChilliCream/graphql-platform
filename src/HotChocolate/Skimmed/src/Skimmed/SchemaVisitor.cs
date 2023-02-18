namespace HotChocolate.Skimmed;

public abstract class SchemaVisitor<TContext>
{
    public virtual void Visit(Schema schema, TContext context)
    {
        Visit(schema.Types, context);
    }

    public virtual void Visit(TypeCollection types, TContext context)
    {
        foreach (var type in types)
        {
            Visit(type, context);
        }
    }

    public virtual void Visit(IType type, TContext context)
    {
        switch (type.Kind)
        {
            case TypeKind.Enum:
                break;

            case TypeKind.InputObject:
                Visit((InputObjectType)type, context);
                break;

            case TypeKind.Interface:
                Visit((InterfaceType)type, context);
                break;

            case TypeKind.Object:
                Visit((ObjectType)type, context);
                break;

            case TypeKind.Scalar:
                Visit((ScalarType)type, context);
                break;

            case TypeKind.Union:
                break;

            case TypeKind.List:
                Visit((ListType)type, context);
                break;

            case TypeKind.NonNull:
                Visit((NonNullType)type, context);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void Visit(InputObjectType type, TContext context)
    {
        Visit(type.Directives, context);
        Visit(type.Fields, context);
    }

    public virtual void Visit(InterfaceType type, TContext context)
    {
        Visit(type.Directives, context);
        Visit(type.Fields, context);
    }

    public virtual void Visit(ObjectType type, TContext context)
    {
        Visit(type.Directives, context);
        Visit(type.Fields, context);
    }

    public virtual void Visit(ScalarType type, TContext context) { }

    public virtual void Visit(ListType type, TContext context)
    {
        Visit(type.ElementType, context);
    }

    public virtual void Visit(NonNullType type, TContext context)
    {
        Visit(type.NullableType, context);
    }

    public virtual void Visit(DirectiveCollection directives, TContext context)
    {
        foreach (var directive in directives)
        {
            Visit(directive, context);
        }
    }

    public virtual void Visit(Directive directive, TContext context)
    {
        Visit(directive.Arguments, context);
    }

    public virtual void Visit(IReadOnlyList<Argument> arguments, TContext context)
    {
        foreach (var argument in arguments)
        {
            Visit(argument, context);
        }
    }

    public virtual void Visit(Argument argument, TContext context)
    {

    }

    public virtual void Visit(FieldCollection<InputField> fields, TContext context)
    {
        foreach (var field in fields)
        {
            Visit(field, context);
        }
    }

    public virtual void Visit(InputField field, TContext context)
    {
        Visit(field.Directives, context);
    }

    public virtual void Visit(FieldCollection<OutputField> fields, TContext context)
    {
        foreach (var field in fields)
        {
            Visit(field, context);
        }
    }

    public virtual void Visit(OutputField field, TContext context)
    {
        Visit(field.Directives, context);
        Visit(field.Arguments, context);
    }

    public virtual void Visit(DirectiveType directive, TContext context)
    {
        Visit(directive.Arguments, context);
    }
}
