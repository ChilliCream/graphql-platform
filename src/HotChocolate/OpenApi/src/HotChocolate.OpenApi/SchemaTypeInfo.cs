using HotChocolate.OpenApi.Helpers;
using HotChocolate.Types;
using Microsoft.OpenApi.Models;
using IType = HotChocolate.Skimmed.IType;
using ListType = HotChocolate.Skimmed.ListType;
using NonNullType = HotChocolate.Skimmed.NonNullType;
using ObjectType = HotChocolate.Skimmed.ObjectType;

namespace HotChocolate.OpenApi;

/// <summary>
/// Container class which exposes metadata and GraphQL related data
/// read of an <see cref="OpenApiSchema"/>
/// </summary>
public class SchemaTypeInfo
{
    public SchemaTypeInfo(OpenApiSchema schema)
    {
        Schema = schema;

        IsListType = schema.Items is not null;

        TypeName = IsListType ? schema.Items!.Type : schema.Type;
        TypeName ??= IsListType ? schema.Items!.Reference.Id : schema.Reference.Id;

        Format = IsListType ? schema.Items!.Format : schema.Format;
        GraphQLTypeName = GetGraphQLTypeName(TypeName, Format);
        IsScalar = Scalars.IsBuiltIn(GraphQLTypeName);
    }
    
    /// <summary>
    /// The schema the information is based on
    /// </summary>
    public OpenApiSchema Schema { get; }

    /// <summary>
    /// Is the base schema e.g. if the schema is an array it is the item schema
    /// </summary>
    public OpenApiSchema RootSchema => IsListType ? Schema.Items : Schema;

    /// <summary>
    /// Is the schema a list type
    /// </summary>
    public bool IsListType { get; }

    /// <summary>
    /// Is the schema scalar type
    /// </summary>
    public bool IsScalar { get; }

    /// <summary>
    /// A graphql name for the schema
    /// </summary>
    public string GraphQLTypeName { get; }

    /// <summary>
    /// Type name from open api
    /// </summary>
    public string TypeName { get;  }

    /// <summary>
    /// More detailed information about the type e.g
    /// int64
    /// </summary>
    public string? Format { get; }

    /// <summary>
    /// Creates a type node of this schema
    /// </summary>
    /// <param name="required"></param>
    /// <returns></returns>
    public IType GetGraphQLTypeNode(bool required)
    {
        var unwrappedType = new ObjectType(GraphQLTypeName);
        IType baseType = required
            ? new NonNullType(unwrappedType)
            : unwrappedType;

        return IsListType
            ? new ListType(baseType)
            : baseType;
    }

    private static string GetGraphQLTypeName(string openApiSchemaTypeName, string? format)
    {
        var typename = openApiSchemaTypeName switch
        {
            "string" => ScalarNames.String,
            "number" => ScalarNames.Float,
            "integer" => format == "int64" ? ScalarNames.Long : ScalarNames.Int,
            "boolean" => ScalarNames.Boolean,
            _ => OpenApiNamingHelper.GetTypeName(openApiSchemaTypeName),
        };
        return typename ?? throw new InvalidOperationException();
    }
}
