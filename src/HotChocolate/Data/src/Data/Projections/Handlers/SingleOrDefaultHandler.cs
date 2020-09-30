namespace HotChocolate.Data.Projections.Handlers
{
    public class SingleOrDefaultHandler : TakeHandlerBase
    {
        public SingleOrDefaultHandler()
            : base(SelectionOptions.SingleOrDefault, 2)
        {
        }
    }
}
