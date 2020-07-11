namespace HotChocolate.Types.Selections.Handlers
{
    public class SingleOrDefaultHandler : TakeHandlerBase
    {
        public SingleOrDefaultHandler()
            : base(SelectionOptions.SingleOrDefault, 2)
        {
        }
    }
}
