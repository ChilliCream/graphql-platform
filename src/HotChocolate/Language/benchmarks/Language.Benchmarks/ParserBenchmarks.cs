using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using HotChocolate.Language;

namespace HotChocolate.Language.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class ParserBenchmarks
{
    private byte[] _kitchenSinkQuery = null!;
    private byte[] _schemaKitchenSink = null!;
    private byte[] _simpleQuery = null!;

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
    }

    [Benchmark]
    public DocumentNode ParseKitchenSinkQuery()
        => Utf8GraphQLParser.Parse(_kitchenSinkQuery);

    [Benchmark]
    public DocumentNode ParseSchemaKitchenSink()
        => Utf8GraphQLParser.Parse(_schemaKitchenSink);

    [Benchmark]
    public DocumentNode ParseSimpleQuery()
        => Utf8GraphQLParser.Parse(_simpleQuery);
}
