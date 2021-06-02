namespace HotChocolate.Analyzers.Configuration
{
    public static class FileContents
    {
        public const string SchemaExtensionFileContent = @"scalar _KeyFieldSet

directive @key(fields: _KeyFieldSet!) on SCHEMA | OBJECT

directive @serializationType(name: String!) on SCALAR

directive @runtimeType(name: String!) on SCALAR

directive @enumValue(value: String!) on ENUM_VALUE

directive @rename(name: String!) on INPUT_FIELD_DEFINITION | INPUT_OBJECT | ENUM | ENUM_VALUE

extend schema @key(fields: ""id"")";
    }
}
