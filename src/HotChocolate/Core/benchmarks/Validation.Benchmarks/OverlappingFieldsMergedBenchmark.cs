using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation;
using HotChocolate.Validation.Rules;

namespace HotChocolate.Validation.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class OverlappingFieldsMergedBenchmark
{
    private ISchemaDefinition _schema = null!;
    private DocumentNode _simpleQuery = null!;
    private DocumentNode _fragmentHeavyQuery = null!;
    private OverlappingFieldsCanBeMergedRule _rule = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schema = BuildSchema();
        _rule = new OverlappingFieldsCanBeMergedRule();

        // Simple query: few inline fragments with overlapping fields
        _simpleQuery = Utf8GraphQLParser.Parse("""
            {
                hero {
                    name
                    friends { name }
                    ... on Human {
                        name
                        height
                        friends { name }
                    }
                    ... on Droid {
                        name
                        primaryFunction
                        friends { name }
                    }
                }
            }
            """);

        // Complex query: 10 fragments, deep nesting, many overlapping selections
        _fragmentHeavyQuery = Utf8GraphQLParser.Parse("""
            query FragmentHeavy {
                hero {
                    ...F1
                    ...F2
                    ...F3
                    ...F4
                    ...F5
                    ...F6
                    ...F7
                    ...F8
                    ...F9
                    ...F10
                }
                search {
                    ...F3
                    ...F5
                }
            }

            fragment F1 on Character {
                name
                friends { name }
            }

            fragment F2 on Character {
                name
                friends { name }
            }

            fragment F3 on Human {
                name
                height
                homePlanet
                friends { name height }
            }

            fragment F4 on Human {
                name
                height
                friends { name }
            }

            fragment F5 on Droid {
                name
                primaryFunction
                friends { name primaryFunction }
            }

            fragment F6 on Droid {
                name
                primaryFunction
                friends { name }
            }

            fragment F7 on Character {
                name
                friends {
                    name
                    friends { name }
                }
            }

            fragment F8 on Human {
                name
                height
                friends {
                    name
                    height
                    friends { name }
                }
            }

            fragment F9 on Droid {
                name
                primaryFunction
                friends {
                    name
                    primaryFunction
                    friends { name }
                }
            }

            fragment F10 on Character {
                name
                friends { name }
            }
            """);
    }

    [Benchmark]
    public void SimpleQuery()
    {
        var context = CreateContext(_simpleQuery);
        _rule.Validate(context, _simpleQuery);
    }

    [Benchmark]
    public void FragmentHeavyQuery()
    {
        var context = CreateContext(_fragmentHeavyQuery);
        _rule.Validate(context, _fragmentHeavyQuery);
    }

    private DocumentValidatorContext CreateContext(DocumentNode document)
    {
        var context = new DocumentValidatorContext();
        context.Initialize(_schema, default, document, 100, 100, null);
        return context;
    }

    private static ISchemaDefinition BuildSchema()
    {
        return SchemaBuilder.New()
            .AddQueryType<BenchQueryType>()
            .AddType<BenchCharacterType>()
            .AddType<BenchHumanType>()
            .AddType<BenchDroidType>()
            .AddType<BenchSearchResultType>()
            .Create();
    }
}

internal class BenchCharacterType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Character");
        descriptor.Field("name").Type<NonNullType<StringType>>();
        descriptor.Field("friends").Type<ListType<BenchCharacterType>>();
    }
}

internal class BenchHumanType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Human");
        descriptor.Implements<BenchCharacterType>();
        descriptor.Field("name").Type<NonNullType<StringType>>().Resolve("Luke");
        descriptor.Field("friends").Type<ListType<BenchCharacterType>>().Resolve(Array.Empty<object>());
        descriptor.Field("height").Type<FloatType>().Resolve(1.72);
        descriptor.Field("homePlanet").Type<StringType>().Resolve("Tatooine");
    }
}

internal class BenchDroidType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Droid");
        descriptor.Implements<BenchCharacterType>();
        descriptor.Field("name").Type<NonNullType<StringType>>().Resolve("R2-D2");
        descriptor.Field("friends").Type<ListType<BenchCharacterType>>().Resolve(Array.Empty<object>());
        descriptor.Field("primaryFunction").Type<StringType>().Resolve("Astromech");
    }
}

internal class BenchSearchResultType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("SearchResult");
        descriptor.Type<BenchHumanType>();
        descriptor.Type<BenchDroidType>();
    }
}

internal class BenchQueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");
        descriptor.Field("hero").Type<BenchCharacterType>().Resolve(() => null!);
        descriptor.Field("search").Type<ListType<BenchSearchResultType>>().Resolve(() => null!);
    }
}
