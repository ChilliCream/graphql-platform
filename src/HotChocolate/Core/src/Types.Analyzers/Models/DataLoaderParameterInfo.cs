using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderParameterInfo
{
    public DataLoaderParameterInfo(string variableName,
        IParameterSymbol parameter,
        DataLoaderParameterKind kind,
        string? stateKey = null)
    {
        VariableName = variableName;
        StateKey = stateKey;
        Parameter = parameter;
        Kind = kind;
    }

    public string VariableName { get; }

    public string? StateKey { get; }

    public int Index => Parameter.Ordinal;

    public ITypeSymbol Type => Parameter.Type;

    public IParameterSymbol Parameter { get; }

    public DataLoaderParameterKind Kind { get; }
}
