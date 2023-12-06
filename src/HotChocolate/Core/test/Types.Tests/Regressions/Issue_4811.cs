#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Regressions;

public class Issue_4811
{
    [Fact]
    public async Task Compatible_Constructor_Can_Be_Found_For_Inputs()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
              mutation: Mutation
            }
            
            type ADDBookResponse {
              title: String!
            }
            
            type Book {
              title: String!
            }
            
            type Mutation {
              addBook(input: CreateCnaeInput!): ADDBookResponse!
            }
            
            type Query {
              book: Book!
            }
            
            input CNAEMutationInput {
              title: String!
            }
            
            input CreateCnaeInput {
              cnae: CNAEMutationInput!
            }
            
            "The @tag directive is used to apply arbitrary string\nmetadata to a schema location. Custom tooling can use\nthis metadata during any step of the schema delivery flow,\nincluding composition, static analysis, and documentation.\n            \n\ninterface Book {\n  id: ID! @tag(name: \"your-value\")\n  title: String!\n  author: String!\n}"
            directive @tag("The name of the tag." name: String!) repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION
            """);
    }

    public class Book
    {
        public string Title { get; set; } = default!;
    }

    public class Query
    {
        public Book GetBook() =>
            new Book
            {
                Title = "C# in depth."
            };
    }

    public class Mutation
    {
        public ADDBookResponse AddBook(CreateCnaeInput input)
        {
            return new ADDBookResponse(input.CNAE.Title);
        }
    }


    public class CreateCnaeInput
    {
        public CreateCnaeInput(CNAEMutationInput cnae)
        {
            CNAE = cnae;
        }

        [Required]
        public CNAEMutationInput CNAE { get; }
    }

    public record CNAEMutationInput(string Title);

    public record ADDBookResponse(string Title);
}
