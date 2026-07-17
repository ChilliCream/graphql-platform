namespace HotChocolate.Types.Composite;

internal sealed class PolicyDenialBehaviorType : EnumType<PolicyDenialBehavior>
{
    protected override void Configure(IEnumTypeDescriptor<PolicyDenialBehavior> descriptor)
    {
        descriptor.Name("PolicyDenialBehavior");
        descriptor.BindValuesExplicitly();
        descriptor.Value(PolicyDenialBehavior.Null).Name("NULL");
        descriptor.Value(PolicyDenialBehavior.Error).Name("ERROR");
        descriptor.Value(PolicyDenialBehavior.Abort).Name("ABORT");
    }
}
