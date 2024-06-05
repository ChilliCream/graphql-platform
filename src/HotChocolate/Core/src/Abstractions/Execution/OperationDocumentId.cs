using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a valid identifier for a GraphQL operation document.
/// </summary>
public readonly struct OperationDocumentId : IEquatable<OperationDocumentId>
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="value">
    /// The GraphQL operation document id.
    /// </param>
    public OperationDocumentId(string value)
    {
        EnsureValidId(value);
        Value = value;
    }
    
    private OperationDocumentId(string value, bool skipValidation)
    {
        if (!skipValidation)
        {
            EnsureValidId(value);
        }
        Value = value;
    }
    
    /// <summary>
    /// Gets a value indicating whether the GraphQL operation document id is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Value);
    
    /// <summary>
    /// Gets the GraphQL operation document id.
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Determines whether the specified <paramref name="other"/>
    /// is equal to the current <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="OperationDocumentId"/> to compare with the current <see cref="OperationDocumentId"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="other"/> is equal to the current <see cref="OperationDocumentId"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(OperationDocumentId other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether the specified <paramref name="obj"/>
    /// is equal to the current <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current <see cref="OperationDocumentId"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="obj"/> is equal to the current <see cref="OperationDocumentId"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is OperationDocumentId other && Equals(other);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="OperationDocumentId"/>.
    /// </returns>
    public override int GetHashCode()
        => Value.GetHashCode();
    
    /// <summary>
    /// Returns a string that represents the current <see cref="OperationDocumentId"/>.
    /// </summary>
    public override string? ToString() => Value;

    /// <summary>
    /// Determines whether the specified <paramref name="left"/> is equal to the specified <paramref name="right"/>.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationDocumentId"/> to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationDocumentId"/> to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="left"/> is equal to the specified <paramref name="right"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool operator ==(OperationDocumentId left, OperationDocumentId right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether the specified <paramref name="left"/> is not equal to the specified <paramref name="right"/>.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationDocumentId"/> to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationDocumentId"/> to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="left"/> is not equal to the specified <paramref name="right"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(OperationDocumentId left, OperationDocumentId right)
        => !left.Equals(right);
    
    /// <summary>
    /// Implicitly converts the specified <paramref name="value"/> to a <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="value">
    /// The GraphQL operation document id string representation.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="OperationDocumentId"/> representing the specified <paramref name="value"/>.
    /// </returns>
    public static implicit operator  OperationDocumentId?(string? value)
        => value is null ? null : new OperationDocumentId(value);

    /// <summary>
    /// Ensures that the specified GraphQL operation document id is valid.
    /// </summary>
    /// <param name="operationId">
    /// The GraphQL operation document id.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The specified GraphQL operation document id is invalid.
    /// </exception>
    public static void EnsureValidId(string operationId)
    {
        if(!IsValidId(operationId))
        {
            throw new ArgumentException(
                OperationDocumentId_InvalidOperationIdFormat, 
                nameof(operationId));
        }
    }
    
    /// <summary>
    /// Determines whether the specified <paramref name="operationId"/>
    /// string is valid input for an <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="operationId">
    /// The GraphQL operation document id.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="operationId"/> is valid;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidId(string operationId)
    {
        if(operationId.Length == 0)
        {
            return false;
        }
        
        var span = operationId.AsSpan();
        ref var start = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref start, span.Length);

        while (Unsafe.IsAddressLessThan(ref start, ref end))
        {
            if (!IsAllowedCharacter((byte)start))
            {
                return false;
            }

            start = ref Unsafe.Add(ref start, 1)!;
        }
        
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAllowedCharacter(byte c)
    {
        switch (c)
        {
            case > 96 and < 123 or > 64 and < 91:
            case > 47 and < 58:
            case 45 or 95:
                return true;

            default:
                return false;
        }
    }
    
    /// <summary>
    /// Determines whether the specified <paramref name="id"/> is <c>null</c> or empty.
    /// </summary>
    /// <param name="id">
    /// The <see cref="OperationDocumentId"/> to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="id"/> is <c>null</c> or empty;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] OperationDocumentId? id)
        => string.IsNullOrEmpty(id?.Value);
    
    /// <summary>
    /// Tries to parse the specified <paramref name="value"/> to a <see cref="OperationDocumentId"/>.
    /// </summary>
    /// <param name="value">
    /// The GraphQL operation document id string representation.
    /// </param>
    /// <param name="id">
    /// The parsed <see cref="OperationDocumentId"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="value"/> could be parsed to a <see cref="OperationDocumentId"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryParse(string? value, out OperationDocumentId id)
    {
        if (string.IsNullOrEmpty(value) || !IsValidId(value))
        {
            id = default;
            return false;
        }

        id = new OperationDocumentId(value, skipValidation: true);
        return true;
    }
}