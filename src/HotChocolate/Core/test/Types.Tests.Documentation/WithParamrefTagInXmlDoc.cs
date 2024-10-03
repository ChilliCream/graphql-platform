namespace HotChocolate.Types.Descriptors;

public class WithParamrefTagInXmlDoc
{
    /// <summary>
    /// This is a parameter reference to <paramref name="id"/>.
    /// </summary>
    public int Foo(int id) => id;
}
