using System.Reflection;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

[InterfaceType]
public class Product
{
    public required string Id { get; set; }
}

[InterfaceType<Product>]
public static partial class ProductType
{
    public static string Kind() => "Product";
}

[ObjectType]
public class Book : Product
{
    public required string Title { get; set; }
}

public class Television : Product;

[ObjectType<Television>]
public static partial class TelevisionType
{
    public static string ArgumentWithExplicitType(
        [GraphQLType<NonNullType<VersionType>>]
        long arg)
        => throw new Exception();

    public static string NullableArgumentWithExplicitType(
        [GraphQLType<VersionType>] long? arg)
        => throw new Exception();
}

[QueryType]
public static partial class Query
{
    /// <summary>
    /// Gets the product.
    /// </summary>
    /// <returns>The only product.</returns>
    public static Product GetProduct()
        => new Book { Id = "1", Title = "GraphQL in Action" };

    [UsePaging]
    [RewriteAfterToVersion]
    public static IQueryable<Product> GetProducts([GraphQLType<NonNullType<VersionType>>] long after)
        => throw new Exception();

    [RewriteArgToVersion]
    public static string ArgumentWithExplicitType([GraphQLType<NonNullType<VersionType>>] long arg)
        => throw new Exception();

    [RewriteArgToVersion]
    public static string NullableArgumentWithExplicitType([GraphQLType<VersionType>] long? arg)
        => throw new Exception();

    public static string NullableArrayArgumentRef(string[]? items)
        => throw new Exception();

    public static string ArrayArgumentRef(string[] items)
        => throw new Exception();

    public static string ArrayNullableElementArgumentRef(string?[] items)
        => throw new Exception();

    public static string NullableArrayNullableElementArgumentRef(string?[]? items)
        => throw new Exception();

    public static string NullableListArgumentRef(List<string>? items)
        => throw new Exception();

    public static string ListArgumentRef(List<string> items)
        => throw new Exception();

    public static string ListNullableElementArgumentRef(List<string?> items)
        => throw new Exception();

    public static string NullableListNullableElementArgumentRef(List<string?>? items)
        => throw new Exception();
}

public class VersionType : ScalarType<long, StringValueNode>
{
    public VersionType() : base("Version", BindingBehavior.Explicit)
    {
    }

    protected override long OnCoerceInputLiteral(StringValueNode valueLiteral)
        => throw new NotImplementedException();

    protected override long OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => throw new NotImplementedException();

    protected override void OnCoerceOutputValue(long runtimeValue, ResultElement resultValue)
        => throw new NotImplementedException();

    protected override StringValueNode OnValueToLiteral(long runtimeValue)
        => throw new NotImplementedException();
}

public class Version2Type : ScalarType<long, StringValueNode>
{
    public Version2Type() : base("Version2", BindingBehavior.Explicit)
    {
    }

    protected override long OnCoerceInputLiteral(StringValueNode valueLiteral)
        => throw new NotImplementedException();

    protected override long OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => throw new NotImplementedException();

    protected override void OnCoerceOutputValue(long runtimeValue, ResultElement resultValue)
        => throw new NotImplementedException();

    protected override StringValueNode OnValueToLiteral(long runtimeValue)
        => throw new NotImplementedException();
}

public sealed class RewriteArgToVersionAttribute
    : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor
            .ExtendWith(static extension =>
            {
                var argument = extension.Configuration.Arguments.First(arg => arg.Name == "arg");
                argument.Type = extension.Context.TypeInspector.GetTypeRef(typeof(Version2Type), TypeContext.Input);
            });
    }
}

public sealed class RewriteAfterToVersionAttribute
    : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor
            .Extend()
            .OnBeforeCreate(static (context, field) =>
            {
                var argument = field.Arguments.First(arg => arg.Name == "after");
                argument.Type = context.TypeInspector.GetTypeRef(typeof(Version2Type), TypeContext.Input);
            });
    }
}
