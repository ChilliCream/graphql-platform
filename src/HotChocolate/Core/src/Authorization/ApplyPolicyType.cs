using HotChocolate.Types;

namespace HotChocolate.Authorization;

internal sealed class ApplyPolicyType : EnumType<ApplyPolicy>
{
    protected override void Configure(
        IEnumTypeDescriptor<ApplyPolicy> descriptor)
    {
        descriptor
            .Name(Names.ApplyPolicy)
            .BindValuesExplicitly();

        descriptor
            .Value(ApplyPolicy.BeforeResolver)
            .Name(Names.BeforeResolver);

        descriptor
            .Value(ApplyPolicy.AfterResolver)
            .Name(Names.AfterResolver);

        descriptor
            .Value(ApplyPolicy.Validation)
            .Name(Names.Validation);
    }

    public static class Names
    {
        public const string ApplyPolicy = nameof(ApplyPolicy);
        public const string BeforeResolver = "BEFORE_RESOLVER";
        public const string AfterResolver = "AFTER_RESOLVER";
        public const string Validation = "VALIDATION";
    }
}
