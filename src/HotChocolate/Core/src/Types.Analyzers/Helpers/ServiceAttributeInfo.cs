using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

public readonly record struct ServiceAttributeInfo(
    bool HasServiceAttribute,
    TypedConstant? ServiceKey,
    object? SourceDerivedServiceKey,
    ITypeSymbol? SourceDerivedServiceKeyType,
    bool IsServiceKeyUndeterminable,
    bool HasFromKeyedServicesAttribute,
    TypedConstant? FromKeyedServicesKey);

public readonly record struct SourceDerivedServiceKey(
    object? Value,
    ITypeSymbol? Type);
