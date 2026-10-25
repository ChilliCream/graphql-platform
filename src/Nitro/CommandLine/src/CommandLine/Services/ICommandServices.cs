namespace ChilliCream.Nitro.CommandLine;

internal interface ICommandServices
{
    T GetRequiredService<T>() where T : notnull;

    T GetRequiredKeyedService<T>(object key) where T : notnull;
}
