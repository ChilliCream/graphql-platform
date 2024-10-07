namespace HotChocolate.Types.Descriptors;

public class WithExceptionsXmlDoc
{
    /// <summary>
    /// Query and manages users.
    ///
    /// You need one of the following role: Owner,
    /// Editor, use XYZ to manage permissions.
    /// </summary>
    /// <returns>Bar</returns>
    /// <exception cref="Exception" code="FOO_ERROR">Foo Error</exception>
    /// <exception cref="Exception" code="BAR_ERROR">Bar Error</exception>
    public void Foo() { }

    /// <summary>
    /// Query and manages users.
    ///
    /// You need one of the following role: Owner,
    /// Editor, use XYZ to manage permissions.
    /// </summary>
    /// <returns>Bar</returns>
    /// <exception cref="Exception">Foo Error</exception>
    /// <exception cref="Exception" code="FOO_ERROR">Foo Error</exception>
    /// <exception cref="Exception" code="BAR_ERROR">Bar Error</exception>
    public void Bar() { }

    /// <summary>
    /// Query and manages users.
    ///
    /// You need one of the following role: Owner,
    /// Editor, use XYZ to manage permissions.
    /// </summary>
    /// <returns>Bar</returns>
    /// <exception cref="Exception">Foo Error</exception>
    public void Baz() { }
}
