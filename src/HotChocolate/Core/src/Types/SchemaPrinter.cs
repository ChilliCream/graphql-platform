using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using static HotChocolate.Types.SpecifiedByDirectiveType.Names;
using static HotChocolate.WellKnownDirectives;

namespace HotChocolate;

public static class SchemaPrinter
{
    public static string Print(ISchema schema)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var document = PrintSchema(schema);
        return document.Print();
    }

    public static void Serialize(ISchema schema, TextWriter textWriter)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (textWriter is null)
        {
            throw new ArgumentNullException(nameof(textWriter));
        }

        var document = PrintSchema(schema);
        textWriter.Write(document.Print());
    }

    public static async ValueTask PrintAsync(
        ISchema schema,
        Stream stream,
        bool indented = true,
        CancellationToken cancellationToken = default)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var document = PrintSchema(schema);
        await document.PrintToAsync(stream, indented, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask PrintAsync(
        IEnumerable<INamedType> namedTypes,
        Stream stream,
        bool indented = true,
        CancellationToken cancellationToken = default)
    {
        if (namedTypes is null)
        {
            throw new ArgumentNullException(nameof(namedTypes));
        }

        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var list = new List<IDefinitionNode>();

        foreach (var namedType in namedTypes)
        {
            var typeDefinition =
                namedType is ScalarType scalarType
                    ? PrintScalarType(scalarType)
                    : PrintNonScalarTypeDefinition(namedType, false);
            list.Add(typeDefinition);
        }

        await new DocumentNode(list)
            .PrintToAsync(stream, indented, cancellationToken)
            .ConfigureAwait(false);
    }

    public static DocumentNode PrintSchema(
        ISchema schema,
        bool includeSpecScalars = false,
        bool printResolverKind = false)
    {
        if (schema is null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        var typeDefinitions = GetNonScalarTypes(schema)
            .Select(t => PrintNonScalarTypeDefinition(t, printResolverKind))
            .OfType<IDefinitionNode>()
            .ToList();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (schema.QueryType is not null
            || schema.MutationType is not null
            || schema.SubscriptionType is not null)
        {
            typeDefinitions.Insert(0, PrintSchemaTypeDefinition(schema));
        }

        var builtInDirectives = new HashSet<string> { Skip, Include, Deprecated, };

        var directiveTypeDefinitions =
            schema.DirectiveTypes
                .Where(directive => !builtInDirectives.Contains(directive.Name))
                .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
                .Select(PrintDirectiveTypeDefinition);

        typeDefinitions.AddRange(directiveTypeDefinitions);

        var scalarTypeDefinitions =
            schema.Types
            .OfType<ScalarType>()
            .Where(t => includeSpecScalars || !BuiltInTypes.IsBuiltInType(t.Name))
            .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
            .Select(PrintScalarType);

        typeDefinitions.AddRange(scalarTypeDefinitions);

        return new DocumentNode(null, typeDefinitions);
    }

    private static IEnumerable<INamedType> GetNonScalarTypes(
        ISchema schema)
    {
        return schema.Types
           .Where(IsPublicAndNoScalar)
           .OrderBy(t => t.Name.ToString(), StringComparer.Ordinal)
           .GroupBy(t => (int)t.Kind)
           .OrderBy(t => t.Key)
           .SelectMany(t => t);
    }

    private static bool IsPublicAndNoScalar(INamedType type)
    {
        if (type.IsIntrospectionType() || type is ScalarType)
        {
            return false;
        }

        return true;
    }

    private static DirectiveDefinitionNode PrintDirectiveTypeDefinition(
        DirectiveType directiveType)
    {
        var arguments = directiveType.Arguments
            .Select(PrintInputField)
            .ToList();

        var locations = directiveType.Locations
            .AsEnumerable()
            .Select(l => new NameNode(l.MapDirectiveLocation().ToString()))
            .ToList();

        return new DirectiveDefinitionNode
        (
            null,
            new NameNode(directiveType.Name),
            PrintDescription(directiveType.Description),
            directiveType.IsRepeatable,
            arguments,
            locations
        );
    }

    private static SchemaDefinitionNode PrintSchemaTypeDefinition(ISchema schema)
    {
        var operations = new List<OperationTypeDefinitionNode>();

        if (schema.QueryType is not null)
        {
            operations.Add(PrintOperationType(
                schema.QueryType,
                OperationType.Query));
        }

        if (schema.MutationType is not null)
        {
            operations.Add(PrintOperationType(
                schema.MutationType,
                OperationType.Mutation));
        }

        if (schema.SubscriptionType is not null)
        {
            operations.Add(PrintOperationType(
                schema.SubscriptionType,
                OperationType.Subscription));
        }

        var directives = schema.Directives
            .Select(PrintDirective)
            .ToList();

        return new SchemaDefinitionNode
        (
            null,
            PrintDescription(schema.Description),
            directives,
            operations
        );
    }

    private static OperationTypeDefinitionNode PrintOperationType(
       ObjectType type,
       OperationType operation)
    {
        return new(
            null,
            operation,
            PrintNamedType(type));
    }

    private static ITypeDefinitionNode PrintNonScalarTypeDefinition(
        INamedType namedType,
        bool printResolverKind) =>
        namedType switch
        {
            ObjectType type => PrintObjectType(type, printResolverKind),
            InterfaceType type => PrintInterfaceType(type),
            InputObjectType type => PrintInputObjectType(type),
            UnionType type => PrintUnionType(type),
            EnumType type => PrintEnumType(type),
            _ => throw new NotSupportedException(),
        };

    private static ObjectTypeDefinitionNode PrintObjectType(
        ObjectType objectType,
        bool printResolverKind)
    {
        var directives = objectType.Directives
            .Select(PrintDirective)
            .ToList();

        var interfaces = objectType.Implements
            .Select(PrintNamedType)
            .ToList();

        var fields = objectType.Fields
            .Where(t => !t.IsIntrospectionField)
            .Select(f => PrintObjectField(f, printResolverKind))
            .ToList();

        return new ObjectTypeDefinitionNode
        (
            null,
            new NameNode(objectType.Name),
            PrintDescription(objectType.Description),
            directives,
            interfaces,
            fields
        );
    }

    private static InterfaceTypeDefinitionNode PrintInterfaceType(
        InterfaceType interfaceType)
    {
        var directives = interfaceType.Directives
            .Select(PrintDirective)
            .ToList();

        var interfaces = interfaceType.Implements
            .Select(PrintNamedType)
            .ToList();

        var fields = interfaceType.Fields
            .Select(PrintInterfaceField)
            .ToList();

        return new InterfaceTypeDefinitionNode
        (
            null,
            new NameNode(interfaceType.Name),
            PrintDescription(interfaceType.Description),
            directives,
            interfaces,
            fields
        );
    }

    private static InputObjectTypeDefinitionNode PrintInputObjectType(
        InputObjectType inputObjectType)
    {
        var directives = inputObjectType.Directives
            .Select(PrintDirective)
            .ToList();

        var fields = inputObjectType.Fields
            .Select(PrintInputField)
            .ToList();

        return new InputObjectTypeDefinitionNode
        (
            null,
            new NameNode(inputObjectType.Name),
            PrintDescription(inputObjectType.Description),
            directives,
            fields
        );
    }

    private static UnionTypeDefinitionNode PrintUnionType(UnionType unionType)
    {
        var directives = unionType.Directives
            .Select(PrintDirective)
            .ToList();

        var types = unionType.Types.Values
            .Select(PrintNamedType)
            .ToList();

        return new UnionTypeDefinitionNode
        (
            null,
            new NameNode(unionType.Name),
            PrintDescription(unionType.Description),
            directives,
            types
        );
    }

    private static EnumTypeDefinitionNode PrintEnumType(EnumType enumType)
    {
        var directives = enumType.Directives
            .Select(PrintDirective)
            .ToList();

        var values = enumType.Values
            .Select(PrintEnumValue)
            .ToList();

        return new EnumTypeDefinitionNode
        (
            null,
            new NameNode(enumType.Name),
            PrintDescription(enumType.Description),
            directives,
            values
        );
    }

    private static EnumValueDefinitionNode PrintEnumValue(IEnumValue enumValue)
    {
        var directives = enumValue.Directives
            .Select(PrintDirective)
            .ToList();

        PrintDeprecationDirective(
            directives,
            enumValue.IsDeprecated,
            enumValue.DeprecationReason);

        return new EnumValueDefinitionNode
        (
            null,
            new NameNode(enumValue.Name),
            PrintDescription(enumValue.Description),
            directives
        );
    }

    private static ScalarTypeDefinitionNode PrintScalarType(
        ScalarType scalarType)
    {
        var directives = scalarType.Directives
            .Select(PrintDirective)
            .ToList();

        if (scalarType.SpecifiedBy is not null)
        {
            directives.Add(
                new DirectiveNode(
                    SpecifiedBy,
                    new ArgumentNode(
                        Url,
                        new StringValueNode(scalarType.SpecifiedBy.ToString()))));
        }

        return new(
            null,
            new NameNode(scalarType.Name),
            PrintDescription(scalarType.Description),
            directives);
    }

    private static FieldDefinitionNode PrintObjectField(
        ObjectField field,
        bool printResolverKind)
    {
        var arguments = field.Arguments
            .Select(PrintInputField)
            .ToList();

        var directives = field.Directives
            .Select(PrintDirective)
            .ToList();

        PrintDeprecationDirective(
            directives,
            field.IsDeprecated,
            field.DeprecationReason);

        if (printResolverKind && field.PureResolver is not null)
        {
            directives.Add(new DirectiveNode("pureResolver"));
        }

        return new FieldDefinitionNode
        (
            null,
            new NameNode(field.Name),
            PrintDescription(field.Description),
            arguments,
            PrintType(field.Type),
            directives
        );
    }

    private static FieldDefinitionNode PrintInterfaceField(
        InterfaceField field)
    {
        var arguments = field.Arguments
            .Select(PrintInputField)
            .ToList();

        var directives = field.Directives
            .Select(PrintDirective)
            .ToList();

        PrintDeprecationDirective(
            directives,
            field.IsDeprecated,
            field.DeprecationReason);

        return new FieldDefinitionNode
        (
            null,
            new NameNode(field.Name),
            PrintDescription(field.Description),
            arguments,
            PrintType(field.Type),
            directives
        );
    }

    private static void PrintDeprecationDirective(
        ICollection<DirectiveNode> directives,
        bool isDeprecated,
        string deprecationReason)
    {
        if (isDeprecated)
        {
            if (DeprecationDefaultReason.EqualsOrdinal(deprecationReason))
            {
                directives.Add(new DirectiveNode(Deprecated));
            }
            else
            {
                directives.Add(new DirectiveNode(
                    Deprecated,
                    new ArgumentNode("reason", deprecationReason)));
            }
        }
    }

    private static InputValueDefinitionNode PrintInputField(
        IInputField inputValue)
    {
        var directives = inputValue.Directives
            .Select(PrintDirective)
            .ToList();

        PrintDeprecationDirective(
            directives,
            inputValue.IsDeprecated,
            inputValue.DeprecationReason);

        return new(
            null,
            new NameNode(inputValue.Name),
            PrintDescription(inputValue.Description),
            PrintType(inputValue.Type),
            inputValue.DefaultValue,
            directives);
    }

    private static ITypeNode PrintType(IType type)
    {
        if (type is NonNullType nt)
        {
            return new NonNullTypeNode(null, (INullableTypeNode)PrintType(nt.Type));
        }

        if (type is ListType lt)
        {
            return new ListTypeNode(null, PrintType(lt.ElementType));
        }

        if (type is INamedType namedType)
        {
            return PrintNamedType(namedType);
        }

        throw new NotSupportedException();
    }

    private static NamedTypeNode PrintNamedType(INamedType namedType)
        => new(null, new NameNode(namedType.Name));

    private static DirectiveNode PrintDirective(Directive directive)
        => directive.AsSyntaxNode(true);

    private static StringValueNode PrintDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        // Get rid of any unnecessary whitespace.
        description = description.Trim();

        var isBlock = description.Contains("\n");

        return new StringValueNode(null, description, isBlock);
    }
}
