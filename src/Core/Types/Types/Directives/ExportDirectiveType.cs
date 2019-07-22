using System.Collections.Generic;

namespace HotChocolate.Types
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

                if (ExportDirectiveHelper.TryGetExportedVariables(
                    context.ContextData,
                    out ICollection<ExportedVariable> exported))
                {
                    IDirective directive = context.Field.Directives
                        .GetFirst(ExportDirectiveHelper.Name);
                    string name = directive.ToObject<ExportDirective>().As;
                    exported.Add(new ExportedVariable(
                        name, context.Field.Type, context.Result));
                }
            });
        }
    }
}
