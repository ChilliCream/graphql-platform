using System.Text.Json;
using ChilliCream.Regorus;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// An authorization policy that evaluates each entity of an ordered batch with Rego.
/// </summary>
/// <remarks>
/// <para>
/// The policy is a thin view over a compiled policy set that all policies of a schema share. It
/// evaluates one entity at a time against an input envelope of the form
/// <c>{ "subject": { … }, "resource": { … } }</c> and is safe for concurrent evaluation. The subject
/// is always present, and the resource is present only when the policy declares a resource
/// requirement.
/// </para>
/// <para>
/// The subject is derived from the request user once per call. Its <c>id</c> is the name identifier
/// claim, or the identity name when that claim is absent, or <c>null</c> when neither is present. Its
/// <c>roles</c> are the values of the user's role claims. Its <c>claims</c> map carries the first value
/// of every non-role claim keyed by claim type. The resource is the entity projected through the
/// resource requirement and is omitted when the policy reads no resource.
/// </para>
/// </remarks>
public sealed class RegoPolicy : IPolicy
{
    private readonly PolicySetHandle _handle;
    private readonly int _entryPoint;

    /// <summary>
    /// Initializes a new instance of <see cref="RegoPolicy"/>.
    /// </summary>
    /// <param name="name">The Fusion authorization policy name.</param>
    /// <param name="requirements">The parts of the evaluation input the policy reads.</param>
    /// <param name="handle">The handle of the shared compiled policy set backing this policy.</param>
    /// <param name="entryPoint">The entrypoint index of this policy within the shared set.</param>
    internal RegoPolicy(
        string name,
        PolicyRequirements requirements,
        PolicySetHandle handle,
        int entryPoint)
    {
        Name = name;
        Requirements = requirements;
        _handle = handle;
        _entryPoint = entryPoint;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public PolicyRequirements Requirements { get; }

    internal PolicySetHandle Handle => _handle;

    /// <inheritdoc />
    public ValueTask EvaluateAsync(
        IPolicyContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var subjectBuffer = new PooledArrayWriter();
        using var inputBuffer = new PooledArrayWriter();
        using var valueBuffer = new PooledArrayWriter();

        var subjectWriter = new JsonWriter(subjectBuffer, new JsonWriterOptions { Indented = false });
        PolicyInputWriter.WriteDefaultSubject(subjectWriter, context.User);

        var policySet = _handle.PolicySet;
        var inputWriter = new JsonWriter(inputBuffer, new JsonWriterOptions { Indented = false });

        // A null selection is a request-constant evaluation: it produces exactly one decision,
        // regardless of how many entities a resource-bearing evaluation would carry.
        var selection = context.Selection;
        var span = selection is null ? default : selection.Entities.Span;
        var count = selection is null ? 1 : span.Length;

        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            inputBuffer.Reset();
            inputWriter.Reset(inputBuffer);
            var entity = selection is null ? default : span[i];
            WriteInput(inputWriter, valueBuffer, subjectBuffer.WrittenSpan, entity);

            var result = policySet.EvalBooleanWithInput(_entryPoint, inputBuffer.WrittenSpan);

            if (result.IsUndefined)
            {
                context.Deny(i, "The policy did not produce a decision.");
            }
            else if (!result.Allowed)
            {
                context.Deny(i);
            }
        }

        return ValueTask.CompletedTask;
    }

    private void WriteInput(
        JsonWriter writer,
        PooledArrayWriter valueBuffer,
        ReadOnlySpan<byte> subject,
        CompositeResultElement entity)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("subject");
        writer.WriteRawValue(subject);

        if (Requirements.Resource is { } resource)
        {
            writer.WritePropertyName("resource");
            WriteProjectedObject(writer, valueBuffer, entity, resource);
        }

        writer.WriteEndObject();
    }

    private void WriteProjectedObject(
        JsonWriter writer,
        PooledArrayWriter valueBuffer,
        CompositeResultElement entity,
        SelectionSetNode requirements)
    {
        if (entity.ValueKind is not JsonValueKind.Object)
        {
            throw InvalidInput("The policy input entity is not an object.");
        }

        writer.WriteStartObject();

        foreach (var selection in requirements.Selections)
        {
            if (selection is not FieldNode field)
            {
                throw InvalidInput("The policy contains an unsupported requirement selection.");
            }

            var responseName = field.Alias?.Value ?? field.Name.Value;

            if (!entity.TryGetProperty(responseName, out var value))
            {
                throw InvalidInput(
                    $"The required field '{responseName}' was not provided by the operation plan.");
            }

            writer.WritePropertyName(responseName);
            WriteProjectedValue(writer, valueBuffer, value, field.SelectionSet);
        }

        writer.WriteEndObject();
    }

    private void WriteProjectedValue(
        JsonWriter writer,
        PooledArrayWriter valueBuffer,
        CompositeResultElement value,
        SelectionSetNode? requirements)
    {
        if (value.IsNullOrInvalidated)
        {
            writer.WriteNullValue();
            return;
        }

        if (requirements is null)
        {
            valueBuffer.Reset();
            value.WriteTo(valueBuffer);
            writer.WriteRawValue(valueBuffer.WrittenSpan);
            return;
        }

        if (value.ValueKind is JsonValueKind.Array)
        {
            writer.WriteStartArray();

            for (var i = 0; i < value.GetArrayLength(); i++)
            {
                var item = value[i];

                if (item.IsNullOrInvalidated)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    WriteProjectedObject(writer, valueBuffer, item, requirements);
                }
            }

            writer.WriteEndArray();
            return;
        }

        WriteProjectedObject(writer, valueBuffer, value, requirements);
    }

    private InvalidOperationException InvalidInput(string message)
        => new($"Rego authorization policy '{Name}' cannot be evaluated. {message}");
}
