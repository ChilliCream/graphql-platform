using HotChocolate.Types;

namespace HotChocolate.Execution.Batching
{
    public sealed class ExportDirectiveType
        : DirectiveType<ExportDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<ExportDirective> descriptor)
        {
            descriptor.Name(ExportDirectiveHelper.Name);
            descriptor.Location(DirectiveLocation.Field);

            descriptor.Argument(t => t.As).Type<StringType>();

            descriptor.Use(next => async context =>
            {
                await next(context).ConfigureAwait(false);
                context.ExportValueAsVariable();
            });
        }
    }
}
