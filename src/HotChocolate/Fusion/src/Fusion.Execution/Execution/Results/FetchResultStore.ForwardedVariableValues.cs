using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Results;

internal sealed partial class FetchResultStore
{
    private readonly ref struct ForwardedVariableValues
    {
        private readonly IReadOnlyList<ObjectFieldNode>? _fields;
        private readonly ReadOnlySpan<ForwardedVariableValue> _values;

        public ForwardedVariableValues(IReadOnlyList<ObjectFieldNode> requestVariables)
        {
            ArgumentNullException.ThrowIfNull(requestVariables);

            _fields = requestVariables;
            _values = default;
        }

        public ForwardedVariableValues(ReadOnlySpan<ForwardedVariableValue> requestVariables)
        {
            _fields = null;
            _values = requestVariables;
        }

        public bool CanUseRequirementFastPath
            => _fields is null || _fields.Count == 0;

        public bool IsDirect => _fields is null;

        public IReadOnlyList<ObjectFieldNode> Fields => _fields!;

        public ReadOnlySpan<ForwardedVariableValue> Values => _values;
    }
}
