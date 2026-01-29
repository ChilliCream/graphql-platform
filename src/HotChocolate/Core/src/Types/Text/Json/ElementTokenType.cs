namespace HotChocolate.Text.Json;

internal enum ElementTokenType : byte
{
    None = 0,
    StartObject = 1,
    EndObject = 2,
    StartArray = 3,
    EndArray = 4,
    PropertyName = 5,
    // Retained for compatibility, we do not actually need this.
    Comment = 6,
    String = 7,
    Number = 8,
    True = 9,
    False = 10,
    Null = 11,
    // A reference in case a property or array element point
    // to an array or an object
    Reference = 12
}
