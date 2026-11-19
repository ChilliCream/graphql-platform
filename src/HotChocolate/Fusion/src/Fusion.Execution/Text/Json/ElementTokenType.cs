using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

internal enum ElementTokenType : byte
{
    None = JsonTokenType.None,
    StartObject = JsonTokenType.StartObject,
    EndObject = JsonTokenType.EndObject,
    StartArray = JsonTokenType.StartArray,
    EndArray = JsonTokenType.EndArray,
    PropertyName = JsonTokenType.PropertyName,
    // Retained for compatibility, we do not actually need this.
    Comment = JsonTokenType.Comment,
    String = JsonTokenType.String,
    Number = JsonTokenType.Number,
    True = JsonTokenType.True,
    False = JsonTokenType.False,
    Null = JsonTokenType.Null,
    // A reference in case a property or array element point
    // to an array or an object
    Reference = 12
}
