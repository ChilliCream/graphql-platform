using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Authorization
{
    public sealed class ApplyPolicyType
        : EnumType<ApplyPolicy>
    {
        protected override void Configure(
            IEnumTypeDescriptor<ApplyPolicy> descriptor)
        {
            descriptor
                .Name("ApplyPolicy")
                .BindValuesExplicitly();

            descriptor
                .Value(ApplyPolicy.BeforeResolver)
                .Name("BEFORE_RESOLVER");

            descriptor
                .Value(ApplyPolicy.AfterResolver)
                .Name("AFTER_RESOLVER");
        }
    }
}
