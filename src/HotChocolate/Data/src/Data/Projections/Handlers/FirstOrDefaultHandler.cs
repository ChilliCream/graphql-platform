namespace HotChocolate.Data.Projections.Handlers
{
    public class FirstOrDefaultHandler : TakeHandlerBase
    {
        public FirstOrDefaultHandler()
            : base(SelectionOptions.FirstOrDefault, 1)
        {
        }
    }
}
