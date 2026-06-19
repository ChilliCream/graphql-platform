using BeforeGraphQLDotNet.GraphTypes;
using BeforeGraphQLDotNet.Models;
using BeforeGraphQLDotNet.Services;
using GraphQL;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.Schema;

public sealed class Mutation : ObjectGraphType
{
    public Mutation(BookDataStore store, IBookEventService events)
    {
        Name = "Mutation";

        Field<NonNullGraphType<BookType>, Book>("addBook")
            .Argument<NonNullGraphType<StringGraphType>>("title")
            .Argument<NonNullGraphType<IdGraphType>>("authorId")
            .Argument<NonNullGraphType<BookGenreEnum>>("genre")
            .Argument<NonNullGraphType<IntGraphType>>("publishedYear")
            .Resolve(context =>
            {
                var title = context.GetArgument<string>("title");
                var authorId = context.GetArgument<int>("authorId");
                var genre = context.GetArgument<BookGenre>("genre");
                var publishedYear = context.GetArgument<int>("publishedYear");

                var book = store.AddBook(title, authorId, genre, publishedYear);
                events.PublishBookAdded(book);

                return book;
            });
    }
}
