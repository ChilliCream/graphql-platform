using HotChocolate.Types;

namespace HotChocolate.AspNetCore.Authorization
{
    public sealed class ExecuteResolverType
        : EnumType<ExecuteResolver>
    {
        protected override void Configure(
            IEnumTypeDescriptor<ExecuteResolver> descriptor)
        {
            descriptor
                .Name("ExecuteResolver")
                .BindValuesExplicitly();

            descriptor
                .Value(ExecuteResolver.AfterPolicy)
                .Name("AFTER_POLICY");

            descriptor
                .Value(ExecuteResolver.BeforePolicy)
                .Name("BEFORE_POLICY");
        }
    }
}
