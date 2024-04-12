using HotChocolate.Types;

namespace HotChocolate.Authorization;

internal sealed class ApplyPolicyType : EnumType<ApplyPolicy>
{
    protected override void Configure(
        IEnumTypeDescriptor<ApplyPolicy> descriptor)
    {
        descriptor
            .Name(Names.ApplyPolicy)
            .Description("Defines when a policy shall be executed.")
            .BindValuesExplicitly();

        descriptor
            .Value(ApplyPolicy.BeforeResolver)
            .Name(Names.BeforeResolver)
            .Description("Before the resolver was executed.");

        descriptor
            .Value(ApplyPolicy.AfterResolver)
            .Name(Names.AfterResolver)
            .Description("After the resolver was executed.");

        descriptor
            .Value(ApplyPolicy.Validation)
            .Name(Names.Validation)
            .Description("The policy is applied in the validation step before the execution.");
    }

    public static class Names
    {
        public const string ApplyPolicy = nameof(ApplyPolicy);
        public const string BeforeResolver = "BEFORE_RESOLVER";
        public const string AfterResolver = "AFTER_RESOLVER";
        public const string Validation = "VALIDATION";
    }
}
