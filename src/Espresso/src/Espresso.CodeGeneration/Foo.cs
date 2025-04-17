using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace Espresso.CodeGeneration;


// @runtimeType(fullTypeName: "Espresso.CodeGeneration.ModelBuilder", isValueType: false)
// @serializationType(fullTypeName: "Espresso.CodeGeneration.ModelBuilder", isValueType: false)

public static class Directives
{
    private const string _runtimeTypeName = "runtimeType";
    private const string _serializationTypeName = "serializationType";
    private const string _fullTypeName = "fullTypeName";
    private const string _isValueType = "isValueType";

    public static MutableDirectiveDefinition CreateRuntimeType(
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition booleanType)
        => new MutableDirectiveDefinition(_runtimeTypeName)
        {
            IsRepeatable = false,
            IsSpecDirective = false,
            Locations = DirectiveLocation.Scalar,
            Arguments =
            {
                new MutableInputFieldDefinition(_fullTypeName, new NonNullType(stringType)),
                new MutableInputFieldDefinition(_isValueType, new NonNullType(booleanType))
                {
                    DefaultValue = BooleanValueNode.False
                }
            }
        };

    public static MutableDirectiveDefinition CreateSerializationType(
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition booleanType)
        => new MutableDirectiveDefinition(_serializationTypeName)
        {
            IsRepeatable = false,
            IsSpecDirective = false,
            Locations = DirectiveLocation.Scalar,
            Arguments =
            {
                new MutableInputFieldDefinition(_fullTypeName, new NonNullType(stringType)),
                new MutableInputFieldDefinition(_isValueType, new NonNullType(booleanType))
                {
                    DefaultValue = BooleanValueNode.False
                }
            }
        };

    public static RuntimeTypeInfo? GetRuntimeTypeInfo(
        MutableScalarTypeDefinition scalarType)
    {
        var directive = scalarType.Directives.FirstOrDefault(_runtimeTypeName);

        if (directive is null)
        {
            return null;
        }

        if (!directive.Arguments.TryGetValue(_fullTypeName, out var value))
        {
            throw new GeneratorException(
                $"The directive '{_runtimeTypeName}' is missing the required "
                + $"argument '{_fullTypeName}' on scalar type `{scalarType.Name}`.");
        }

        if (value is not StringValueNode fullTypeName)
        {
            throw new GeneratorException(
                $"The directive '{_runtimeTypeName}' on scalar type `{scalarType.Name}` "
                + $"has an invalid value for the argument '{_fullTypeName}'."
                + "The argument value must be a string.");
        }

        var isValueType = BooleanValueNode.False;

        if (directive.Arguments.TryGetValue(_isValueType, out value))
        {
            if (value is not BooleanValueNode booleanValue)
            {
                throw new GeneratorException(
                    $"The directive '{_runtimeTypeName}' on scalar type `{scalarType.Name}` "
                    + $"has an invalid value for the argument '{_isValueType}'."
                    + "The argument value must be a boolean.");
            }

            isValueType = booleanValue;
        }

        return new RuntimeTypeInfo(fullTypeName.Value, isValueType.Value);
    }

    public static SerializationTypeInfo? GeSerializationTypeInfo(
        MutableScalarTypeDefinition scalarType)
    {
        var directive = scalarType.Directives.FirstOrDefault(_runtimeTypeName);

        if (directive is null)
        {
            return null;
        }

        if (!directive.Arguments.TryGetValue(_fullTypeName, out var value))
        {
            throw new GeneratorException(
                $"The directive '{_serializationTypeName}' is missing the required "
                + $"argument '{_fullTypeName}' on scalar type `{scalarType.Name}`.");
        }

        if (value is not StringValueNode fullTypeName)
        {
            throw new GeneratorException(
                $"The directive '{_serializationTypeName}' on scalar type `{scalarType.Name}` "
                + $"has an invalid value for the argument '{_fullTypeName}'."
                + "The argument value must be a string.");
        }

        var isValueType = BooleanValueNode.False;

        if (directive.Arguments.TryGetValue(_isValueType, out value))
        {
            if (value is not BooleanValueNode booleanValue)
            {
                throw new GeneratorException(
                    $"The directive '{_serializationTypeName}' on scalar type `{scalarType.Name}` "
                    + $"has an invalid value for the argument '{_isValueType}'."
                    + "The argument value must be a boolean.");
            }

            isValueType = booleanValue;
        }

        return new SerializationTypeInfo(fullTypeName.Value, isValueType.Value);
    }
}


public class ModelBuilder
{
    private List<DocumentNode> _documents = [];

    public static ModelBuilder New() => new();

    public ModelBuilder AddDocument(byte[] document)
    {
        _documents.Add(Utf8GraphQLParser.Parse(document));
        return this;
    }

    public ModelBuilder AddDocument([StringSyntax("graphql")]string document)
    {
        AddDocument(Encoding.UTF8.GetBytes(document));
        return this;
    }

    public ISchemaDefinition Build()
    {
        var schema = new MutableSchemaDefinition();

        foreach (var document in _documents)
        {
            SchemaParser.Parse(schema, document);
        }

        if (!schema.Types.TryGetType(SpecScalarNames.String, out var stringType))
        {
            stringType = BuiltIns.String.Create();
            schema.Types.Add(stringType);
        }

        if (!schema.Types.TryGetType(SpecScalarNames.Boolean, out var booleanType))
        {
            booleanType = BuiltIns.Boolean.Create();
            schema.Types.Add(booleanType);
        }

        foreach (var type in schema.Types.OfType<MutableScalarTypeDefinition>())
        {
            TryApplyTypeBinding(type);

            if (type.Features.Get<RuntimeTypeInfo>() is null
                && type.Features.Get<SerializationTypeInfo>() is null)
            {
                ApplyDefaultTypeBinding(type);
            }

            ApplyFallbackTypeBinding(type);
        }

        return schema;
    }

    private static void TryApplyTypeBinding(MutableScalarTypeDefinition type)
    {
        var runtimeTypeInfo = Directives.GetRuntimeTypeInfo(type);
        if (runtimeTypeInfo is not null)
        {
            type.Features.Set(runtimeTypeInfo);
        }

        var serializationTypeInfo = Directives.GeSerializationTypeInfo(type);
        if (serializationTypeInfo is not null)
        {
            type.Features.Set(serializationTypeInfo);
        }
    }

    private static void ApplyDefaultTypeBinding(MutableScalarTypeDefinition type)
    {
        switch (type.Name)
        {
            case SpecScalarNames.String:
                type.Features.Set(
                    new RuntimeTypeInfo(
                        FullTypeName: "System.String",
                        IsValueType: false));
                type.Features.Set(
                    new SerializationTypeInfo(
                        FullTypeName: "System.String",
                        IsValueType: false));
                break;

            case SpecScalarNames.Int:
                type.Features.Set(
                    new RuntimeTypeInfo(
                        FullTypeName: "System.Int32",
                        IsValueType: true));
                type.Features.Set(
                    new SerializationTypeInfo(
                        FullTypeName: "System.Int32",
                        IsValueType: true));
                break;

            case SpecScalarNames.Float:
                type.Features.Set(
                    new RuntimeTypeInfo(
                        FullTypeName: "System.Double",
                        IsValueType: true));
                type.Features.Set(
                    new SerializationTypeInfo(
                        FullTypeName: "System.Double",
                        IsValueType: true));
                break;

            case SpecScalarNames.Boolean:
                type.Features.Set(
                    new RuntimeTypeInfo(
                        FullTypeName: "System.Boolean",
                        IsValueType: true));
                type.Features.Set(
                    new SerializationTypeInfo(
                        FullTypeName: "System.Boolean",
                        IsValueType: true));
                break;

            case SpecScalarNames.ID:
                type.Features.Set(
                    new RuntimeTypeInfo(
                        FullTypeName: "System.String",
                        IsValueType: false));
                type.Features.Set(
                    new SerializationTypeInfo(
                        FullTypeName: "System.Text.Json.JsonElement",
                        IsValueType: false));
                break;
        }
    }

    private static void ApplyFallbackTypeBinding(MutableScalarTypeDefinition type)
    {
        if (type.Features.Get<RuntimeTypeInfo>() is null)
        {
            type.Features.Set(
                new RuntimeTypeInfo(
                    FullTypeName: "System.String",
                    IsValueType: false));
        }

        if (type.Features.Get<SerializationTypeInfo>() is null)
        {
            type.Features.Set(
                new SerializationTypeInfo(
                    FullTypeName: "System.Text.Json.JsonElement",
                    IsValueType: false));
        }
    }
}

public sealed record RuntimeTypeInfo(string FullTypeName, bool IsValueType);

public sealed record SerializationTypeInfo(string FullTypeName, bool IsValueType);

public class GeneratorException : Exception
{
    public GeneratorException(string message) : base(message)
    {
    }

    public GeneratorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class OperationInspector(ISchemaDefinition schema) : SyntaxWalker<CSharpCodeGeneratorContext>
{
    public IReadOnlyList<ITypeModel> Inspect(DocumentNode document)
    {
        var context = new CSharpCodeGeneratorContext();
        Visit(document, context);
        return context.AllModels;
    }

    protected override ISyntaxVisitorAction Enter(OperationDefinitionNode node, CSharpCodeGeneratorContext context)
    {
        if (node.Name is null)
        {
            throw new GeneratorException(
                "The operation definition node is missing a name. "
                + "Please provide a name for the operation.");
        }

        var model = new EntityModel(node.Name.Value + node.Operation)
        {
            OperationDefinition = node
        };

        context.Entities.Push(model);
        context.AllModels.Add(model);
        context.Types.Push(schema.GetOperationType(node.Operation));

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(FieldNode node, CSharpCodeGeneratorContext context)
    {
        if (context.IsAlwaysSkipped(node))
        {
            return Skip;
        }

        var type = context.Types.Peek().AsTypeDefinition();
        var model = context.Entities.Peek();

        if (type is not IComplexTypeDefinition complexType)
        {
            throw new GeneratorException($"The type '{type.Name}' is not a complex type.");
        }

        if(!complexType.Fields.TryGetField(node.Name.Value, out var field))
        {
            throw new GeneratorException($"The field '{node.Name.Value}' does not exist on type '{type.Name}'.");
        }

        context.Fields.Push(node);
        context.Types.Push(field.Type);

        if (model.Properties.TryGetValue(node.Name.Value, out var property)
            && property.Nullability is not Nullability.Nullable
            && !context.Nullable.Peek()
            && !context.IsNullable(node, field.Type))
        {
            property = context.CreatePropertyModel(node, type);
            model.Properties[node.Name.Value] = property;
        }
        else
        {
            property = context.CreatePropertyModel(node, type);
            model.Properties.Add(node.Name.Value, property);
        }

        if (node.SelectionSet is not null)
        {
            if (context.IsAbstract(node, type))
            {
                var nextModel = new EntityInterfaceModel(property.TypeName);
                context.EntityInterfaces.Push(nextModel);
                context.AllModels.Add(nextModel);
                context.Abstract.Push(true);
            }
            else
            {
                var nextModel = new EntityModel(property.TypeName);
                context.Entities.Push(nextModel);
                context.AllModels.Add(nextModel);
                context.Abstract.Push(false);
            }

            context.Nullable.Push(false);
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, CSharpCodeGeneratorContext context)
    {
        if (context.IsAlwaysSkipped(node))
        {
            return Skip;
        }

        context.Fields.Pop();
        context.Types.Pop();

        if (node.SelectionSet is not null)
        {
            context.Nullable.Pop();

            if (context.Abstract.Peek())
            {
                context.EntityInterfaces.Pop();
            }
            else
            {
                context.Entities.Pop();
            }

            context.Abstract.Pop();
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(FragmentSpreadNode node, CSharpCodeGeneratorContext context)
    {
        // TODO: throw when the fragment spread has an interface marker and the type is not inlined.

        if (context.IsAlwaysSkipped(node))
        {
            return Skip;
        }

        if (!context.IsInlined(node))
        {
            var model = context.Entities.Peek();
            if (!model.Properties.TryGetValue(node.Name.Value, out var property))
            {
                property = context.CreatePropertyModel(node);
                model.Properties.Add(node.Name.Value, property);
            }
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, CSharpCodeGeneratorContext context)
    {
        if (context.IsAlwaysSkipped(node))
        {
            return Skip;
        }

        if (node.TypeCondition is null)
        {
            context.Nullable.Push(context.IsMaybeSkipped(node));
        }
        else
        {
            context.Nullable.Push(false);

            if (context.IsInlined(node))
            {
                var entityModel = new EntityModel(context.GetTypeName(node))
                {
                    InlineFragment = node
                };

                context.Entities.Push(entityModel);
            }
            else
            {
                var propertyName = context.GetPropertyName(node);
                var model = context.Entities.Peek();

                if (!model.Properties.TryGetValue(propertyName, out var property))
                {
                    property = context.CreatePropertyModel(node);
                    model.Properties.Add(propertyName, property);
                }

                var entityModel = new EntityModel(property.TypeName)
                {
                    InlineFragment = node
                };

                context.Entities.Push(entityModel);
                context.AllModels.Add(entityModel);
            }
        }

        return base.Leave(node, context);
    }
}

public class CSharpCodeGeneratorContext
{
    public string Namespace { get; set; } = "Espresso.CodeGeneration";

    public Stack<bool> Nullable { get; } = new();

    public Stack<bool> Abstract { get; } = new();

    public Stack<FieldNode> Fields { get; } = new();

    public Stack<IType> Types { get; } = new();

    public Stack<EntityModel> Entities { get; } = new();

    public Stack<EntityInterfaceModel> EntityInterfaces { get; } = new();

    public List<ITypeModel> AllModels { get; } = new();

    public PropertyModel CreatePropertyModel(FieldNode field, IType type)
    {
        var typeName = GetTypeName(field, type);
        return new PropertyModel(
            field.Name.Value,
            typeName,
            type,
            type.Kind is TypeKind.NonNull ? Nullability.NonNull : Nullability.Nullable,
            field.Alias?.Value ?? field.Name.Value);
    }

    public PropertyModel CreatePropertyModel(FragmentSpreadNode node)
    {
        throw new NotImplementedException();
    }

    public PropertyModel CreatePropertyModel(InlineFragmentNode node)
    {
        throw new NotImplementedException();
    }

    public string GetTypeName(InlineFragmentNode node)
    {
        throw new NotImplementedException();
    }

    public string GetPropertyName(InlineFragmentNode node)
    {
        throw new NotImplementedException();
    }

    public bool IsInlined(FragmentSpreadNode fragmentSpread)
        => false;


    public bool IsInlined(InlineFragmentNode fragmentSpread)
        => false;

    public bool IsAbstract(FieldNode field, IType type) => false;

    public bool IsNullable(FieldNode field, IType type) => false;

    public bool IsAlwaysSkipped(ISelectionNode node) => false;

    public bool IsMaybeSkipped(ISelectionNode node) => false;

    private string GetTypeName(FieldNode field, IType type)
    {
        var typeDefinition = type.AsTypeDefinition();

        if (typeDefinition.Kind is TypeKind.Scalar)
        {
            var runtimeType = ((IFeatureProvider)typeDefinition).Features.GetRequired<RuntimeTypeInfo>();
            return runtimeType.FullTypeName;
        }
        else if (typeDefinition.Kind is TypeKind.Enum)
        {
            return $"{Namespace}.{typeDefinition.Name}";
        }
        else
        {
            // TODO : needs inspection to build the name
            return "";
        }
    }
}

public interface ITypeModel
{
    string Name { get; }
}

public record EntityModel(string Name) : ITypeModel
{
    public Dictionary<string, PropertyModel> Properties { get; } = new();

    public List<EntityInterfaceModel> Interfaces { get; } = new();

    public InlineFragmentNode? InlineFragment { get; init; }

    public OperationDefinitionNode? OperationDefinition { get; init; }
}

public record PropertyModel(
    string Name,
    string TypeName,
    IType Type,
    Nullability Nullability,
    string? FieldName = null,
    bool IsFragment = false);

public class EntityInterfaceModel(string name) : ITypeModel
{
    public string Name { get; } = name;

    public List<PropertyModel> Properties { get; } = new();
}


public enum Nullability
{
    Nullable,
    NonNull,
    StrictNonNull
}

public static class SyntaxNodeExtensions
{
    public static Skipped GetSkipValue(this ISelectionNode selection)
    {
        var skipValue = selection.Directives
            .Where(t => t.Name.Value.Equals("skip"))
            .Select(t => t.Arguments.Single(a => a.Name.Value.Equals("if")).Value)
            .FirstOrDefault();

        switch (skipValue)
        {
            case BooleanValueNode { Value: true }:
                return Skipped.Always;

            case NullValueNode:
                return Skipped.Maybe;
        }

        var includeValue = selection.Directives
            .Where(t => t.Name.Value.Equals("include"))
            .Select(t => t.Arguments.Single(a => a.Name.Value.Equals("if")).Value)
            .FirstOrDefault();

        switch (includeValue)
        {
            case BooleanValueNode { Value: false }:
                return Skipped.Never;

            case NullValueNode:
                return Skipped.Maybe;

            default:
                return Skipped.Never;
        }
    }
}

public enum Skipped
{
    Always,
    Maybe,
    Never
}

public class CSharpCodeGenerator
{

    public string Namespace { get; set; } = "Espresso.CodeGeneration";

    public IEnumerable<(string FileName, string Content)> GenerateCode(IReadOnlyList<ITypeModel> models)
    {
        var sb = new StringBuilder();
        var writer = new CodeWriter(sb);

        foreach (var model in models)
        {
            sb.Clear();

            switch (model)
            {
                case EntityModel { OperationDefinition: not null } operationModel:
                {
                    WriteOperation(writer, operationModel);
                    yield return ($"{model.Name}.g.cs", sb.ToString());
                    break;
                }
                case EntityModel entityModel:
                {
                    WriteEntity(writer, entityModel);
                    yield return ($"{model.Name}.g.cs", sb.ToString());
                    break;
                }
            }
        }
    }

    private void WriteOperation(CodeWriter writer, EntityModel model)
    {
        writer.WriteIndentedLine("namespace {0}", Namespace);
        writer.WriteLine();

        writer.WriteIndentedLine("public record {0} : global::Espresso.OperationResult", model.Name);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();

        foreach (var property in model.Properties)
        {
            writer.WriteIndentedLine(
                "public required {0} {1} {{ get; init; }}",
                property.Value.TypeName,
                property.Value.FieldName);
        }

        writer.DecreaseIndent();
        writer.WriteIndentedLine("}");
    }

    private void WriteEntity(CodeWriter writer, EntityModel model)
    {
        writer.WriteIndentedLine("namespace {0}", Namespace);
        writer.WriteLine();

        writer.WriteIndentedLine("public record {0}", model.Name);
        writer.WriteIndentedLine("{");
        writer.IncreaseIndent();

        foreach (var property in model.Properties)
        {
            writer.WriteIndentedLine(
                "public required {0} {1} {{ get; init; }}",
                property.Value.TypeName,
                property.Value.FieldName);
        }

        writer.DecreaseIndent();
        writer.WriteIndentedLine("}");
    }
}
