using Xunit.Abstractions;

namespace HotChocolate.Fusion.Composition;

public class ResolveTests(ITestOutputHelper output) : CompositionTestBase(output)
{
    [Fact]
    public async Task Remove_Private_Field()
        => await Succeed(
            """
            type Query {
              foo: String @private
              bar: String
            }
            """);
    
    [Fact]
    public async Task Remove_Private_Field_When_Source()
      => await Succeed(
        """
        type Query {
          data: Data
        }
        
        type Data {
          foo: String @private
          bar: String
        }
        """);
    
    [Fact]
    public async Task Remove_Resolver_On_Private_Field()
      => await Succeed(
        """
        type Query {
          foo: String @private
          bar: String
        }
        """,
        """
        type Query {
          foo: String
          bar: String
        }
        """);
    
    [Fact]
    public async Task Remove_Resolver_On_Private_Field_When_Source()
      => await Succeed(
        """
        type Query {
          data: Data
        }
        
        type Data {
          foo: String @private
          bar: String
        }
        """,
        """
        type Query {
          data: Data
        }
        
        type Data {
          foo: String
          bar: String
        }
        """);

    [Fact]
    public async Task Output_Rewrite_Nullability_For_Output_Types()
        => await Succeed(
            """
            type Query {
              foo(arg: String): string @private
            }
            """,
            """
            type Query {
              userByName(name: String): User
            }

            type User {
              name: String
            }
            """);
}