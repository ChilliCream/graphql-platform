using System.Text.Json.Serialization.Metadata;

namespace ChilliCream.Nitro.CLI;

internal interface IConfigurationFile
{
    public static abstract string FileName { get; }

    public static abstract object? Default { get; }

    public static abstract JsonTypeInfo TypeInfo { get; }
}
