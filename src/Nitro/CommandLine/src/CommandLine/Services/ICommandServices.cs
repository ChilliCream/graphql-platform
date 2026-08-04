namespace ChilliCream.Nitro.CommandLine;

internal interface ICommandServices
{
    T GetRequiredService<T>() where T : notnull;
}
