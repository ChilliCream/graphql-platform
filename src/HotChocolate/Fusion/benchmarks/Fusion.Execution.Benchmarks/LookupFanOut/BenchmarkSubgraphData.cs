using System.Buffers;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Benchmarks.LookupFanOut;

/// <summary>
/// Answers subgraph operations of the federation benchmark query (accounts, products,
/// reviews and inventory) from the benchmark's static data set.
/// </summary>
internal static class BenchmarkSubgraphData
{
    private static readonly Dictionary<string, (string AuthorId, string ProductUpc, string Body)> s_reviews = new()
    {
        ["1"] = ("1", "1", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."),
        ["2"] = ("1", "1", "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugi"),
        ["3"] = ("1", "1", "sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem."),
        ["4"] = ("1", "1", "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem"),
        ["5"] = ("1", "2", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."),
        ["6"] = ("1", "2", "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugi"),
        ["7"] = ("1", "2", "sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem."),
        ["8"] = ("1", "2", "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem"),
        ["9"] = ("1", "3", "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem"),
        ["10"] = ("1", "4", "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem"),
        ["11"] = ("1", "4", "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet ut et voluptates repudiandae sint et molestiae non recusandae. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat.")
    };

    private static readonly Dictionary<string, (string Username, string Name)> s_users = new()
    {
        ["1"] = ("urigo", "Uri Goldshtein"),
        ["2"] = ("dotansimha", "Dotan Simha"),
        ["3"] = ("kamilkisiela", "Kamil Kisiela"),
        ["4"] = ("ardatan", "Arda Tanrikulu"),
        ["5"] = ("gilgardosh", "Gil Gardosh"),
        ["6"] = ("laurin", "Laurin Quast")
    };

    private static readonly Dictionary<string, (string Name, long Price, long Weight, bool InStock)> s_products = new()
    {
        ["1"] = ("Table", 899, 100, true),
        ["2"] = ("Couch", 1299, 1000, false),
        ["3"] = ("Glass", 15, 20, false),
        ["4"] = ("Chair", 499, 100, false),
        ["5"] = ("TV", 1299, 1000, true)
    };

    // ReviewRepository.GetByUserId of the benchmark subgraph ignores the user id.
    private static readonly string[] s_userReviews = ["1", "2"];

    public static byte[] CreateResponse(DocumentNode document, JsonElement variables)
    {
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            WriteSelectionSet(writer, document, operation.SelectionSet, new Entity("Query", ""), variables);
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteSelectionSet(
        Utf8JsonWriter writer,
        DocumentNode document,
        SelectionSetNode selectionSet,
        Entity parent,
        JsonElement variables)
    {
        writer.WriteStartObject();
        WriteSelections(writer, document, selectionSet, parent, variables);
        writer.WriteEndObject();
    }

    private static void WriteSelections(
        Utf8JsonWriter writer,
        DocumentNode document,
        SelectionSetNode selectionSet,
        Entity parent,
        JsonElement variables)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    writer.WritePropertyName(field.Alias?.Value ?? field.Name.Value);
                    WriteValue(writer, document, field, Resolve(parent, field, variables), variables);
                    break;

                case InlineFragmentNode fragment
                    when fragment.TypeCondition is null
                        || fragment.TypeCondition.Name.Value == parent.Type:
                    WriteSelections(writer, document, fragment.SelectionSet, parent, variables);
                    break;

                case FragmentSpreadNode spread:
                    var definition = document.Definitions
                        .OfType<FragmentDefinitionNode>()
                        .Single(t => t.Name.Value == spread.Name.Value);

                    if (definition.TypeCondition.Name.Value == parent.Type)
                    {
                        WriteSelections(writer, document, definition.SelectionSet, parent, variables);
                    }

                    break;
            }
        }
    }

    private static void WriteValue(
        Utf8JsonWriter writer,
        DocumentNode document,
        FieldNode field,
        object? value,
        JsonElement variables)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case string s:
                writer.WriteStringValue(s);
                break;

            case long l:
                writer.WriteNumberValue(l);
                break;

            case bool b:
                writer.WriteBooleanValue(b);
                break;

            case Entity entity:
                WriteSelectionSet(writer, document, field.SelectionSet!, entity, variables);
                break;

            case IEnumerable<Entity> list:
                writer.WriteStartArray();

                foreach (var entity in list)
                {
                    WriteSelectionSet(writer, document, field.SelectionSet!, entity, variables);
                }

                writer.WriteEndArray();
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

    private static object? Resolve(Entity parent, FieldNode field, JsonElement variables)
    {
        var name = field.Name.Value;

        if (name == "__typename")
        {
            return parent.Type;
        }

        switch (parent.Type)
        {
            case "Query":
                return name switch
                {
                    "topProducts" => s_products.Keys.Select(Product).ToArray(),
                    "users" => s_users.Keys.Select(User).ToArray(),
                    "user" or "userById" => User(GetArgument(field, "id", variables)),
                    "product" or "productByUpc" => Product(GetArgument(field, "upc", variables)),
                    "review" => new Entity("Review", GetArgument(field, "id", variables)),
                    "_entities" => ResolveEntities(field, variables),
                    _ => throw new NotSupportedException(name)
                };

            case "User":
                var user = s_users[parent.Key];
                return name switch
                {
                    "id" => parent.Key,
                    "username" => user.Username,
                    "name" => user.Name,
                    "birthday" => null,
                    "reviews" => s_userReviews.Select(Review).ToArray(),
                    _ => throw new NotSupportedException(name)
                };

            case "Product":
                var product = s_products[parent.Key];
                return name switch
                {
                    "upc" => parent.Key,
                    "name" => product.Name,
                    "price" => product.Price,
                    "weight" => product.Weight,
                    "inStock" => product.InStock,
                    "shippingEstimate" => product.Price > 1000 ? 0L : product.Weight / 2,
                    "reviews" => s_reviews.Where(t => t.Value.ProductUpc == parent.Key)
                        .Select(t => Review(t.Key))
                        .ToArray(),
                    _ => throw new NotSupportedException(name)
                };

            case "Review":
                var review = s_reviews[parent.Key];
                return name switch
                {
                    "id" => parent.Key,
                    "body" => review.Body,
                    "authorId" => review.AuthorId,
                    "productUpc" => review.ProductUpc,
                    "author" => User(review.AuthorId),
                    "product" => Product(review.ProductUpc),
                    _ => throw new NotSupportedException(name)
                };

            default:
                throw new NotSupportedException(parent.Type);
        }
    }

    private static Entity[] ResolveEntities(FieldNode field, JsonElement variables)
    {
        var representations = GetArgumentElement(field, "representations", variables);
        var entities = new List<Entity>();

        foreach (var representation in representations.EnumerateArray())
        {
            var typeName = representation.GetProperty("__typename").GetString()!;
            var key = typeName switch
            {
                "User" => representation.GetProperty("id").GetString()!,
                "Product" => representation.GetProperty("upc").GetString()!,
                "Review" => representation.GetProperty("id").GetString()!,
                _ => throw new NotSupportedException(typeName)
            };

            entities.Add(new Entity(typeName, key));
        }

        return [.. entities];
    }

    private static string GetArgument(FieldNode field, string name, JsonElement variables)
    {
        var argument = field.Arguments.Single(t => t.Name.Value == name);

        return argument.Value switch
        {
            StringValueNode s => s.Value,
            IntValueNode i => i.Value,
            VariableNode v => variables.GetProperty(v.Name.Value).ToString(),
            _ => throw new NotSupportedException(argument.Value.Kind.ToString())
        };
    }

    private static JsonElement GetArgumentElement(FieldNode field, string name, JsonElement variables)
    {
        var argument = field.Arguments.Single(t => t.Name.Value == name);

        return argument.Value is VariableNode v
            ? variables.GetProperty(v.Name.Value)
            : throw new NotSupportedException(argument.Value.Kind.ToString());
    }

    private static Entity User(string id) => new("User", id);

    private static Entity Product(string upc) => new("Product", upc);

    private static Entity Review(string id) => new("Review", id);

    private sealed record Entity(string Type, string Key);
}
