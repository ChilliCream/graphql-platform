namespace StrawberryShake.Tools;

public interface ICommand
{
    Task<int> OnExecute();
}
