using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.ExecutionGroups;

public class ExecutionGroupConfigurationTests
{
    [Fact]
    public void Create_Should_ExposeTheExecutionGroup_When_FieldHasAnExecutionGroup()
    {
        // arrange
        // act
        var schema = CreateSchema(
            new ExecutionGroupEntry("nodes", 1, isStreamable: true),
            new ExecutionGroupEntry("pageInfo.hasNextPage", 0, isStreamable: false));

        // assert
        var field = schema.QueryType.Fields["connection"];
        field.ExecutionGroup!.Entries
            .Select(e => $"{e.Path} {e.Priority} {e.IsStreamable}")
            .MatchInlineSnapshots(
            [
                "nodes 1 True",
                "pageInfo.hasNextPage 0 False"
            ]);
    }

    [Fact]
    public void Create_Should_Throw_When_GroupHasNoEntries()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(() => CreateSchema());

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: it must declare at least one entry."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathReferencesAMissingField()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(new ExecutionGroupEntry("pageInfo.missing", 0, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `pageInfo.missing` "
            + "references the field `missing` that does not exist on type `PageInfo`."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathPassesThroughAListField()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(new ExecutionGroupEntry("nodes.name", 0, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `nodes.name` "
            + "passes through the list field `Connection.nodes`."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathPassesThroughALeafField()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(new ExecutionGroupEntry("probe.length", 0, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `probe.length` "
            + "passes through the field `Connection.probe` that is not of an object type."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathPassesThroughAnotherGroupRoot()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(new ExecutionGroupEntry("child.nodes", 0, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `child.nodes` "
            + "passes through the field `Connection.child` that is an execution group root."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathIsDeclaredTwice()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(
                new ExecutionGroupEntry("nodes", 0, isStreamable: false),
                new ExecutionGroupEntry("nodes", 1, isStreamable: true)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `nodes` "
            + "is declared more than once."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathIsNestedBelowAnotherEntry()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(
                new ExecutionGroupEntry("pageInfo.hasNextPage", 0, isStreamable: false),
                new ExecutionGroupEntry("pageInfo", 1, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `pageInfo.hasNextPage` "
            + "is nested below the entry `pageInfo`."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_MemberIsSerial()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .ModifyOptions(o => o.DefaultResolverStrategy = ExecutionStrategy.Serial)
            .AddQueryType(descriptor =>
            {
                descriptor.Name("Query");
                descriptor
                    .Field("connection")
                    .Type("Connection")
                    .Resolve(_ => new object())
                    .ExecutionGroup(
                        new ExecutionGroupEntry("pageInfo.hasNextPage", 0, isStreamable: false),
                        new ExecutionGroupEntry("nodes", 1, isStreamable: false));
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("Connection");
                descriptor.Field("nodes").Type("[Item!]!").Resolve(_ => Array.Empty<object>());
                descriptor.Field("pageInfo").Type("PageInfo").Resolve(_ => new object());
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("PageInfo");
                descriptor.Field("hasNextPage").Type("Boolean!").Resolve(_ => true);
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("Item");
                descriptor.Field("name").Type("String").Resolve(_ => "name");
            });

        // act
        var exception = Assert.Throws<SchemaException>(builder.Create);

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `pageInfo.hasNextPage` "
            + "references the field `Connection.pageInfo` that is not parallel executable.",
            "The execution group of field `Query.connection` is invalid: the entry `nodes` "
            + "references the field `Connection.nodes` that is not parallel executable."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_PathContainsAnInvalidFieldName()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateSchema(new ExecutionGroupEntry("nodes..name", 0, isStreamable: false)));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.connection` is invalid: the entry `nodes..name` "
            + "contains the invalid field name ``."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_RootFieldIsNotOfACompositeType()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => SchemaBuilder.New()
                .AddQueryType(descriptor =>
                {
                    descriptor.Name("Query");
                    descriptor
                        .Field("label")
                        .Type<StringType>()
                        .Resolve("label")
                        .ExecutionGroup(new ExecutionGroupEntry("length", 0, isStreamable: false));
                })
                .Create());

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.label` is invalid: the entry `length` cannot be resolved "
            + "because the type of the group root must be an object type or interface type."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_ImplementingTypeHasANestedGroupRootOnThePath()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateInterfaceSchema(
                new ExecutionGroupEntry("pageInfo.hasNextPage", 0, isStreamable: false),
                nodes => { },
                pageInfo => pageInfo.ExecutionGroup(
                    new ExecutionGroupEntry("hasNextPage", 0, isStreamable: false))));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.conn` is invalid: the entry `pageInfo.hasNextPage` "
            + "passes through the field `ConnB.pageInfo` that is an execution group root."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_ImplementingTypeHasASerialMember()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateInterfaceSchema(
                new ExecutionGroupEntry("nodes", 0, isStreamable: false),
                nodes => nodes.Serial(),
                pageInfo => { }));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.conn` is invalid: the entry `nodes` "
            + "references the field `ConnB.nodes` that is not parallel executable."
        ]);
    }

    [Fact]
    public void Create_Should_Throw_When_ImplementingTypeHasABatchMember()
    {
        // arrange
        // act
        var exception = Assert.Throws<SchemaException>(
            () => CreateInterfaceSchema(
                new ExecutionGroupEntry("nodes", 0, isStreamable: false),
                nodes => nodes.ResolveBatch(
                    contexts => new ValueTask<IReadOnlyList<ResolverResult>>(
                        contexts.Select(_ => ResolverResult.Ok(Array.Empty<object>())).ToArray())),
                pageInfo => { }));

        // assert
        GetMessages(exception).MatchInlineSnapshots(
        [
            "The execution group of field `Query.conn` is invalid: the entry `nodes` "
            + "references the field `ConnB.nodes` that uses a batch resolver."
        ]);
    }

    private static string[] GetMessages(SchemaException exception)
        => [.. exception.Errors.Select(e => e.Message)];

    private static Schema CreateSchema(params ExecutionGroupEntry[] entries)
        => SchemaBuilder.New()
            .AddQueryType(descriptor =>
            {
                descriptor.Name("Query");
                descriptor
                    .Field("connection")
                    .Type("Connection")
                    .Resolve(_ => new object())
                    .ExecutionGroup(entries);
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("Connection");
                descriptor.Field("nodes").Type("[Item!]!").Resolve(_ => Array.Empty<object>());
                descriptor.Field("pageInfo").Type("PageInfo").Resolve(_ => new object());
                descriptor.Field("probe").Type("String").Resolve(_ => "probe");
                descriptor
                    .Field("child")
                    .Type("Connection")
                    .Resolve(_ => new object())
                    .ExecutionGroup(new ExecutionGroupEntry("nodes", 0, isStreamable: false));
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("PageInfo");
                descriptor.Field("hasNextPage").Type("Boolean!").Resolve(_ => true);
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("Item");
                descriptor.Field("name").Type("String").Resolve(_ => "name");
            })
            .Create();

    private static Schema CreateInterfaceSchema(
        ExecutionGroupEntry entry,
        Action<IObjectFieldDescriptor> configureNodes,
        Action<IObjectFieldDescriptor> configurePageInfo)
        => SchemaBuilder.New()
            .AddQueryType(descriptor =>
            {
                descriptor.Name("Query");
                descriptor
                    .Field("conn")
                    .Type("Conn")
                    .Resolve(_ => new object())
                    .ExecutionGroup(entry);
            })
            .AddInterfaceType(descriptor =>
            {
                descriptor.Name("Conn");
                descriptor.Field("nodes").Type("[Item!]!");
                descriptor.Field("pageInfo").Type("PageInfo");
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("ConnA").Implements("Conn");
                descriptor.Field("nodes").Type("[Item!]!").Resolve(_ => Array.Empty<object>());
                descriptor.Field("pageInfo").Type("PageInfo").Resolve(_ => new object());
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("ConnB").Implements("Conn");
                configureNodes(descriptor.Field("nodes").Type("[Item!]!").Resolve(_ => Array.Empty<object>()));
                configurePageInfo(descriptor.Field("pageInfo").Type("PageInfo").Resolve(_ => new object()));
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("PageInfo");
                descriptor.Field("hasNextPage").Type("Boolean!").Resolve(_ => true);
            })
            .AddObjectType(descriptor =>
            {
                descriptor.Name("Item");
                descriptor.Field("name").Type("String").Resolve(_ => "name");
            })
            .Create();
}
