using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateDirectiveType
        : DirectiveType<DelegateDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DelegateDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Delegate);
            descriptor.Location(Types.DirectiveLocation.FieldDefinition)
                .Location(Types.DirectiveLocation.Field);

            descriptor.Middleware(next => async context =>
            {
                var queryBroker = context.Service<IQueryBroker>();
                IExecutionResult result =
                    await queryBroker.RedirectQueryAsync(context);

                if (result is IQueryExecutionResult qr)
                {
                    foreach (IError error in qr.Errors)
                    {
                        // context.ReportError(error);
                    }

                    // qr.Data
                }








            });
        }
    }
}
