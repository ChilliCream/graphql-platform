using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting.Expressions;

public class QueryableSortVisitorObjectTests : IClassFixture<SchemaCache>
{
    private static readonly Bar[] s_barEntities =
    [
        new()
        {
            Foo = new Foo
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                //ScalarArray = new[] { "c", "d", "a" },
                ObjectArray =
                [
                    new()
                    {
                        Foo = new Foo
                        {
                            // ScalarArray = new[] { "c", "d", "a" }
                            BarShort = 12, BarString = "a"
                        }
                    }
                ]
            }
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 14,
                BarBool = true,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                //ScalarArray = new[] { "c", "d", "b" },
                ObjectArray =
                [
                    new()
                    {
                        Foo = new Foo
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = 14, BarString = "d"
                        }
                    }
                ]
            }
        },
        new()
        {
            Foo = new Foo
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testctest",
                //ScalarArray = null,
                ObjectArray = null
            }
        }
    ];

    private static readonly BarNullable?[] s_barNullableEntities =
    [
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 12,
                BarBool = true,
                BarEnum = BarEnum.BAR,
                BarString = "testatest",
                //ScalarArray = new[] { "c", "d", "a" },
                ObjectArray =
                [
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "a" }
                            BarShort = 12
                        }
                    }
                ]
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = null,
                BarBool = null,
                BarEnum = BarEnum.BAZ,
                BarString = "testbtest",
                //ScalarArray = new[] { "c", "d", "b" },
                ObjectArray =
                [
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = null
                        }
                    }
                ]
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 14,
                BarBool = false,
                BarEnum = BarEnum.QUX,
                BarString = "testctest",
                //ScalarArray = null,
                ObjectArray =
                [
                    new()
                    {
                        Foo = new FooNullable
                        {
                            //ScalarArray = new[] { "c", "d", "b" }
                            BarShort = 14
                        }
                    }
                ]
            }
        },
        new()
        {
            Foo = new FooNullable
            {
                BarShort = 13,
                BarBool = false,
                BarEnum = BarEnum.FOO,
                BarString = "testdtest",
                //ScalarArray = null,
                ObjectArray = null
            }
        },
        new()
        {
            Foo =null
        },
        null
    ];

    private readonly SchemaCache _cache;

    public QueryableSortVisitorObjectTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_ObjectShort_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barShort: ASC}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barShort: DESC}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableShort_OrderBy()
    {
        // arrange
        var tester =
            _cache.CreateSchema<BarNullable, BarNullableSortType>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barShort: ASC}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barShort: DESC}}) "
                    + "{ foo{ barShort}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectEnum_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: ASC}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: DESC}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableEnum_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableSortType>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: ASC}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barEnum: DESC}}) "
                    + "{ foo{ barEnum}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barString: ASC}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barString: DESC}}) "
                    + "{ foo{ barString}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableString_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableSortType>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barString: ASC}}) "
                    + "{ foo{ barString}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barString: DESC}}) "
                    + "{ foo{ barString}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectBool_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: ASC}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: DESC}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectNullableBool_OrderBy()
    {
        // arrange
        var tester = _cache.CreateSchema<BarNullable, BarNullableSortType>(s_barNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: ASC}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: DESC}}) "
                    + "{ foo{ barBool}}}")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "13")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectString_OrderBy_TwoProperties()
    {
        // arrange
        var tester = _cache.CreateSchema<Bar, BarSortType>(s_barEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: ASC, barShort: ASC }}) "
                    + "{ foo{ barBool barShort}}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                        {
                            root(order: [
                                { foo: { barBool: ASC } },
                                { foo: { barShort: ASC } }]) {
                                foo {
                                    barBool
                                    barShort
                                }
                            }
                        }
                        ")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(order: { foo: { barBool: DESC, barShort: DESC}}) "
                    + "{ foo{ barBool barShort}}}")
                .Build());

        var res4 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root(order: [
                            { foo: { barBool: DESC } },
                            { foo: { barShort: DESC } }]) {
                            foo {
                                barBool
                                barShort
                            }
                        }
                    }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "ASC")
            .Add(res3, "DESC")
            .Add(res4, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_ObjectComplex_OrderBy_Sum()
    {
        // arrange
        var convention = new SortConvention(x =>
        {
            x.AddDefaults().BindRuntimeType<Bar, ComplexBarSortType>();
            x.AddProviderExtension(
                new MockProviderExtension(y =>
                {
                    y.AddFieldHandler<ComplexOrderSumHandler>();
                    y.AddFieldHandler<ComplexOrderSumFieldsHandler>();
                    y.AddFieldHandler<ComplexOrderSumSortHandler>();
                }));
        });
        var tester = _cache.CreateSchema<Bar, ComplexBarSortType>(
            s_barEntities,
            convention: convention);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        root(order: [
                            { foo: { complex_order_sum: {
                                fields: ["barShort" "barBool"]
                                sort: ASC
                            } } }
                            { foo: { barString: DESC } }]) {
                            foo {
                                barShort
                                barBool
                                barString
                            }
                        }
                    }
                    """)
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        root(order: [
                            { foo: { complex_order_sum: {
                                fields: ["barShort" "barBool"]
                                sort: DESC
                            } } }
                            { foo: { barString: ASC } }]) {
                            foo {
                                barShort
                                barBool
                                barString
                            }
                        }
                    }
                    """)
                .Build());

        // assert
        await Snapshot.Create().Add(res1, "ASC").Add(res2, "13").MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public short BarShort { get; set; }

        public string BarString { get; set; } = "";

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        //Not supported in SQL
        //public string[] ScalarArray { get; set; }

        public List<Bar>? ObjectArray { get; set; } = [];
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        //Not supported in SQL
        //public string?[] ScalarArray { get; set; }

        public List<BarNullable>? ObjectArray { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public Foo Foo { get; set; } = null!;
    }

    public class BarNullable
    {
        public int Id { get; set; }

        public FooNullable? Foo { get; set; }
    }

    public class BarSortType : SortInputType<Bar>;

    public class BarNullableSortType : SortInputType<BarNullable>;

    public class ComplexBarSortType : SortInputType<Bar>
    {
        protected override void Configure(ISortInputTypeDescriptor<Bar> descriptor)
        {
            descriptor.Field(x => x.Foo).Type<ComplexFooSortType>();
        }
    }

    public class ComplexFooSortType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor
                .Field("complex_order_sum")
                .Type(
                    new SortInputType(z =>
                    {
                        z.Field("fields").Type<ListType<StringType>>();
                        z.Field("sort").Type<DefaultSortEnumType>();
                    }));
        }
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX
    }

    class ComplexOrderSumHandler(ISortConvention convention, InputParser inputParser)
        : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        private readonly Dictionary<string, PropertyInfo> _fieldMap = typeof(Foo)
            .GetProperties()
            .ToDictionary(convention.GetFieldName);

        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeConfiguration typeConfiguration,
            ISortFieldConfiguration fieldConfiguration)
        {
            return fieldConfiguration.Name == "complex_order_sum";
        }

        public override bool TryHandleEnter(
            QueryableSortContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context.GetInstance() is QueryableFieldSelector fieldSelector
                && field.Type is InputObjectType inputType
                && node.Value is ObjectValueNode objectValueNode)
            {
                var fieldsField = objectValueNode.Fields.First(x => x.Name.Value == "fields");
                if (inputParser.ParseLiteral(
                        fieldsField.Value,
                        inputType.Fields["fields"],
                        typeof(string[]))
                    is string[] fields)
                {
                    var properties = fields
                        .Select(x => _fieldMap[x])
                        .Select(x => Expression.Property(fieldSelector.Selector, x));
                    context.PushInstance(
                        fieldSelector.WithSelector(
                            properties
                                .Select(x => Expression.Convert(x, typeof(int)).Reduce())
                                .Aggregate(Expression.Add)));
                    action = SyntaxVisitor.Continue;
                    return true;
                }
            }
            action = SyntaxVisitor.Skip;
            return false;
        }

        public override bool TryHandleLeave(
            QueryableSortContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            context.PopInstance();
            action = SyntaxVisitor.Continue;
            return true;
        }
    }

    class ComplexOrderSumFieldsHandler
        : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeConfiguration typeConfiguration,
            ISortFieldConfiguration fieldConfiguration)
        {
            return fieldConfiguration.Name == "fields";
        }
    }

    class ComplexOrderSumSortHandler
        : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            ISortInputTypeConfiguration typeConfiguration,
            ISortFieldConfiguration fieldConfiguration)
        {
            return fieldConfiguration.Name == "sort";
        }

        public override bool TryHandleEnter(
            QueryableSortContext context,
            ISortField field,
            ObjectFieldNode node,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = SyntaxVisitor.Continue;
            return true;
        }
    }

    class MockProviderExtension(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : SortProviderExtensions<QueryableSortContext>(configure);
}
