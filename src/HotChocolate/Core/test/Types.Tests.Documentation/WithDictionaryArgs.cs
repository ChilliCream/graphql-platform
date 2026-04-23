namespace HotChocolate.Types.Descriptors;

public class WithDictionaryArgs
{
#pragma warning disable RCS1228
    /// <summary>
    /// This is a method description
    /// </summary>
    /// <param name="args">Args description</param>
    /// <returns></returns>
#pragma warning restore RCS1228
    public string Method(Dictionary<string, string>? args = null) => string.Empty;
}
