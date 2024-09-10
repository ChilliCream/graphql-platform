using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public readonly struct DataLoaderParameterInfo(
    string variableName,
    IParameterSymbol parameter,
    DataLoaderParameterKind kind,
    string? stateKey = null)
{
    public string VariableName { get; } = variableName;

    public string? StateKey { get; } = stateKey;

    public int Index => Parameter.Ordinal;

    public ITypeSymbol Type => Parameter.Type;

    public IParameterSymbol Parameter { get; } = parameter;

    public DataLoaderParameterKind Kind { get; } = kind;
}
