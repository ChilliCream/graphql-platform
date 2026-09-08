using System.Security.Claims;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

internal static class PolicyInputWriter
{
    public static void WriteDefaultSubject(JsonWriter writer, ClaimsPrincipal user)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("id");
        writer.WriteStringValue(
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name);

        writer.WritePropertyName("roles");
        writer.WriteStartArray();

        foreach (var claim in user.Claims)
        {
            if (IsRoleClaim(claim))
            {
                writer.WriteStringValue(claim.Value);
            }
        }

        writer.WriteEndArray();

        writer.WritePropertyName("claims");
        writer.WriteStartObject();

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in user.Claims)
        {
            if (IsRoleClaim(claim) || !seen.Add(claim.Type))
            {
                continue;
            }

            writer.WritePropertyName(claim.Type);
            writer.WriteStringValue(claim.Value);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static bool IsRoleClaim(Claim claim)
        => string.Equals(
            claim.Type,
            claim.Subject?.RoleClaimType ?? ClaimsIdentity.DefaultRoleClaimType,
            StringComparison.Ordinal);

    /// <summary>
    /// Writes the <c>action</c> part of the input envelope: <c>{ "name": …, "arguments": { … } }</c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="action"/>'s arguments are already fully coerced (no variables, schema defaults
    /// filled in, canonical field order); this only serializes the literal value tree to JSON, using
    /// <paramref name="valueBuffer"/> to stage a numeric literal's raw token, never a stackalloc.
    /// </remarks>
    public static void WriteAction(JsonWriter writer, PooledArrayWriter valueBuffer, PolicyAction action)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("name");
        writer.WriteStringValue(action.Name);
        writer.WritePropertyName("arguments");
        WriteValue(writer, valueBuffer, action.Arguments);
        writer.WriteEndObject();
    }

    private static void WriteValue(JsonWriter writer, PooledArrayWriter valueBuffer, IValueNode value)
    {
        switch (value)
        {
            case NullValueNode:
                writer.WriteNullValue();
                break;

            case StringValueNode stringValue:
                writer.WriteStringValue(stringValue.Value);
                break;

            case EnumValueNode enumValue:
                writer.WriteStringValue(enumValue.Value);
                break;

            case BooleanValueNode booleanValue:
                writer.WriteBooleanValue(booleanValue.Value);
                break;

            case IntValueNode intValue:
                WriteRawNumber(writer, valueBuffer, intValue.Value);
                break;

            case FloatValueNode floatValue:
                WriteRawNumber(writer, valueBuffer, floatValue.Value);
                break;

            case ListValueNode listValue:
                writer.WriteStartArray();
                foreach (var item in listValue.Items)
                {
                    WriteValue(writer, valueBuffer, item);
                }
                writer.WriteEndArray();
                break;

            case ObjectValueNode objectValue:
                writer.WriteStartObject();
                foreach (var field in objectValue.Fields)
                {
                    writer.WritePropertyName(field.Name.Value);
                    WriteValue(writer, valueBuffer, field.Value);
                }
                writer.WriteEndObject();
                break;

            default:
                throw new InvalidOperationException(
                    $"The policy action argument value kind '{value.Kind}' is not supported.");
        }
    }

    private static void WriteRawNumber(JsonWriter writer, PooledArrayWriter valueBuffer, string numericLiteral)
    {
        valueBuffer.Reset();
        var byteCount = Encoding.UTF8.GetByteCount(numericLiteral);
        var span = valueBuffer.GetSpan(byteCount);
        var written = Encoding.UTF8.GetBytes(numericLiteral, span);
        valueBuffer.Advance(written);
        writer.WriteRawValue(valueBuffer.WrittenSpan);
    }
}
