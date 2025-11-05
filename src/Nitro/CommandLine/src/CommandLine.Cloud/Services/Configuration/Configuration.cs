using System.Text.Json.Serialization.Metadata;

namespace ChilliCream.Nitro.CommandLine.Cloud;

internal interface IConfigurationFile
{
    static abstract string FileName { get; }

    static abstract object? Default { get; }

    static abstract JsonTypeInfo TypeInfo { get; }
}
