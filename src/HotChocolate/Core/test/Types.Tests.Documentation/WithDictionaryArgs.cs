namespace HotChocolate.Types.Descriptors;

public class WithDictionaryArgs
{
    /// <summary>
    /// This is a method description
    /// </summary>
    /// <param name="args">Args description</param>
    /// <returns></returns>
    public string Method(Dictionary<string, string>? args = null) => string.Empty;
}
