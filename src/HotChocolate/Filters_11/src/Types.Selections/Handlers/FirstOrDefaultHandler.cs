namespace HotChocolate.Types.Selections.Handlers
{
    public class FirstOrDefaultHandler : TakeHandlerBase
    {
        public FirstOrDefaultHandler()
            : base(SelectionOptions.FirstOrDefault, 1)
        {
        }
    }
}
