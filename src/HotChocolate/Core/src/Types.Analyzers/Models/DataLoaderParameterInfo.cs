using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class DataLoaderParameterInfo
{
    public DataLoaderParameterInfo(
        string variableName,
        IParameterSymbol parameter,
        DataLoaderParameterKind kind,
        string? stateKey = null,
        string? key = null)
    {
        VariableName = variableName;
        StateKey = stateKey;
        Key = key;
        Parameter = parameter;
        Kind = kind;
        IsNullable = !parameter.IsNonNullable();
    }

    public string VariableName { get; }

    public string? StateKey { get; }

    public string? Key { get; }

    public bool IsNullable { get; }

    public ITypeSymbol Type => Parameter.Type;

    public IParameterSymbol Parameter { get; }

    public DataLoaderParameterKind Kind { get; }
}
