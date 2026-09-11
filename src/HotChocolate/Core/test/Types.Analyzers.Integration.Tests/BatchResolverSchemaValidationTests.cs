using System.Reflection;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class BatchResolverSchemaValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schema_Should_RejectCustomPerParentAttribute_When_ResolverIsGenerated(bool validateOrder)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .ModifyOptions(o => o.ValidatePipelineOrder = validateOrder)
            .AddDirectiveType(new DirectiveType(d => d.Name("wrap")
                .Location(DirectiveLocation.FieldDefinition)
                .Use((next, _) => context => next(context))))
            .AddQueryType(d => d.Field("items").Resolve(new[] { new ValidationItem(1) }))
            .AddObjectType<ValidationItem>(ValidationItemType.Initialize);

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(e => new
        {
            e.Code,
            e.Message,
            Extensions = e.Extensions?.OrderBy(t => t.Key, StringComparer.Ordinal)
                .ToDictionary(t => t.Key, t => t.Value)
        }).MatchSnapshot();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Schema_Should_RejectMutationRoot_When_GeneratedObjectIsRegisteredAsMutation(bool validateOrder)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL()
            .ModifyOptions(o => o.ValidatePipelineOrder = validateOrder)
            .AddQueryType(d => d.Field("noop").Resolve("noop"))
            .AddMutationType<ValidationMutation>(ValidationMutationType.Initialize);

        // act
        var exception = await Assert.ThrowsAsync<SchemaException>(async () =>
            await builder.BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        // assert
        exception.Errors.Select(e => new
        {
            e.Code,
            e.Message,
            Extensions = e.Extensions?.OrderBy(t => t.Key, StringComparer.Ordinal)
                .ToDictionary(t => t.Key, t => t.Value)
        }).MatchSnapshot();
    }
}

public sealed record ValidationItem(int Id);

[ObjectType<ValidationItem>]
public static partial class ValidationItemType
{
    [BatchResolver]
    [UseWrap]
    public static IReadOnlyList<string> GetValue([Parent] List<ValidationItem> items)
        => items.Select(t => t.Id.ToString()).ToArray();

    [BatchResolver]
    [UseKeyedWrap]
    public static IReadOnlyList<string> GetKeyed([Parent] List<ValidationItem> items)
        => items.Select(t => t.Id.ToString()).ToArray();

    [BatchResolver]
    [UseDirectiveWrap]
    public static IReadOnlyList<string> GetDirective([Parent] List<ValidationItem> items)
        => items.Select(t => t.Id.ToString()).ToArray();
}

public sealed class ValidationMutation;

[ObjectType<ValidationMutation>]
public static partial class ValidationMutationType
{
    [BatchResolver]
    public static IReadOnlyList<string> GetValue() => ["value"];
}

public sealed class UseWrapAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Use(next => next);
}

public sealed class UseKeyedWrapAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Extend().Configuration.MiddlewareConfigurations.Add(new(next => next, key: "custom"));
}

public sealed class UseDirectiveWrapAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.Directive("wrap");
}
