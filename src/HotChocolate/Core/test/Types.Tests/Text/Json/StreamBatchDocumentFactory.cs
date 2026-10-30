using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Text.Json;

internal static class StreamBatchDocumentFactory
{
    public static Operation CreateOperation()
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      films: [Film!]
                    }

                    type Film {
                      title: String
                      characters: [String]
                    }
                    """)
                .Use(_ => _)
                .Create();

        return OperationCompiler.Compile(
            "abc",
            Utf8GraphQLParser.Parse(
                """
                {
                  films {
                    title
                    characters
                  }
                }
                """),
            schema);
    }

    public static ResultDocument Create(Operation operation, int startIndex, int itemCount, int characterCount)
    {
        var selection = operation.RootSelectionSet.Selections[0];
        var itemSelectionSet = operation.GetSelectionSet(selection);
        var document = new ResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            selection,
            Path.Root,
            startIndex,
            new ConditionFlags(0));

        var list = document.Data.GetProperty("films");
        list.SetArrayValue(itemCount);

        foreach (var item in list.EnumerateArray())
        {
            item.SetObjectValue(itemSelectionSet);
            item.GetProperty("title").SetStringValue("film"u8);

            var characters = item.GetProperty("characters");
            characters.SetArrayValue(characterCount);

            foreach (var character in characters.EnumerateArray())
            {
                character.SetStringValue("character"u8);
            }
        }

        return document;
    }
}
