using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Helpers;

public readonly record struct ServiceAttributeInfo(
    bool HasServiceAttribute,
    TypedConstant? ServiceKey,
    string? SourceDerivedServiceKey,
    bool IsServiceKeyUndeterminable,
    bool HasFromKeyedServicesAttribute,
    TypedConstant? FromKeyedServicesKey);
