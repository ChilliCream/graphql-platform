using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Language.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class ReaderBenchmarks
{
    private byte[] _kitchenSinkQuery = null!;
    private byte[] _schemaKitchenSink = null!;
    private byte[] _simpleQuery = null!;
    private byte[] _nameHeavy = null!;
    private byte[] _punctuatorHeavy = null!;
    private byte[] _numberHeavy = null!;
    private byte[] _stringHeavy = null!;

    [GlobalSetup]
    public void Setup()
    {
        _kitchenSinkQuery = Encoding.UTF8.GetBytes("""
            "Query description"
            query queryName("$foo description" $foo: ComplexType, "$site description" $site: Site = MOBILE) {
              whoever123is: node(id: [123, 456]) {
                id ,
                ... on User @defer {
                  field2 {
                    id ,
                    alias: field1(first:10, after:$foo,) @include(if: $foo) {
                      id,
                      ...frag
                    }
                  }
                }
                ... @skip(unless: $foo) {
                  id
                }
                ... {
                  id
                }
              }
            }

            "Mutation description"
            mutation likeStory {
              like(story: 123) @defer {
                story {
                  id
                }
              }
            }

            "Subscription description"
            subscription StoryLikeSubscription("$input description" $input: StoryLikeSubscribeInput) {
              storyLikeSubscribe(input: $input) {
                story {
                  likers {
                    count
                  }
                  likeSentence {
                    text
                  }
                }
              }
            }

            "Fragment description"
            fragment frag on Friend {
              foo(size: $size, bar: $b, obj: {key: "value"})
            }

            {
              unnamed(truthy: true, falsey: false, nullish: null),
              query
            }
            """);

        _schemaKitchenSink = Encoding.UTF8.GetBytes("""
            schema {
              query: QueryType
              mutation: MutationType
            }

            type Foo implements Bar & Baz {
              one: Type
              two(argument: InputType!): Type
              three(argument: InputType, other: String): Int
              four(argument: String = "string"): String
              five(argument: [String] = ["string", "string"]): String
              six(argument: InputType = {key: "value"}): Type
              seven(argument: Int = null): Type
            }

            type AnnotatedObject @onObject(arg: "value") {
              annotatedField(arg: Type = "default" @onArg): Type @onField
            }

            interface Bar {
              one: Type
              four(argument: String = "string"): String
            }

            union Feed = Story | Article | Advert

            scalar CustomScalar

            enum Site {
              DESKTOP
              MOBILE
            }

            input InputType {
              key: String!
              answer: Int = 42
            }

            directive @skip(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT

            extend type Foo {
              seven(argument: [String]): Type
            }

            extend interface Bar {
              two(argument: InputType!): Type
            }
            """);

        _simpleQuery = Encoding.UTF8.GetBytes("""
            {
              hero {
                name
                friends {
                  name
                }
              }
            }
            """);

        // Name-heavy: lots of name tokens
        var nameBuilder = new StringBuilder("query {\n");
        for (var i = 0; i < 200; i++)
        {
            nameBuilder.Append($"  field{i}: someVeryLongFieldNameThatExercisesNameParsing{i}\n");
        }
        nameBuilder.Append('}');
        _nameHeavy = Encoding.UTF8.GetBytes(nameBuilder.ToString());

        // Punctuator-heavy: deeply nested structures
        var punctBuilder = new StringBuilder();
        for (var i = 0; i < 50; i++)
        {
            punctBuilder.Append("{ f(a: [");
        }
        punctBuilder.Append('1');
        for (var i = 0; i < 50; i++)
        {
            punctBuilder.Append("]) }");
        }
        _punctuatorHeavy = Encoding.UTF8.GetBytes(punctBuilder.ToString());

        // Number-heavy: lots of integer and float literals
        var numBuilder = new StringBuilder("query { f(a: [");
        for (var i = 0; i < 200; i++)
        {
            numBuilder.Append(i % 3 == 0 ? $"{i}.{i}e{i % 10}, " : $"{i * 100}, ");
        }
        numBuilder.Append("0]) }");
        _numberHeavy = Encoding.UTF8.GetBytes(numBuilder.ToString());

        // String-heavy: lots of string literals
        var strBuilder = new StringBuilder("query { f(");
        for (var i = 0; i < 100; i++)
        {
            strBuilder.Append($"a{i}: \"some string value number {i} with escape \\n end\", ");
        }
        strBuilder.Append("last: \"done\") }");
        _stringHeavy = Encoding.UTF8.GetBytes(strBuilder.ToString());
    }

    [Benchmark]
    public int KitchenSinkQuery()
    {
        var reader = new Utf8GraphQLReader(_kitchenSinkQuery);
        return reader.Count();
    }

    [Benchmark]
    public int SchemaKitchenSink()
    {
        var reader = new Utf8GraphQLReader(_schemaKitchenSink);
        return reader.Count();
    }

    [Benchmark]
    public int SimpleQuery()
    {
        var reader = new Utf8GraphQLReader(_simpleQuery);
        return reader.Count();
    }

    [Benchmark]
    public int NameHeavy()
    {
        var reader = new Utf8GraphQLReader(_nameHeavy);
        return reader.Count();
    }

    [Benchmark]
    public int PunctuatorHeavy()
    {
        var reader = new Utf8GraphQLReader(_punctuatorHeavy);
        return reader.Count();
    }

    [Benchmark]
    public int NumberHeavy()
    {
        var reader = new Utf8GraphQLReader(_numberHeavy);
        return reader.Count();
    }

    [Benchmark]
    public int StringHeavy()
    {
        var reader = new Utf8GraphQLReader(_stringHeavy);
        return reader.Count();
    }
}
