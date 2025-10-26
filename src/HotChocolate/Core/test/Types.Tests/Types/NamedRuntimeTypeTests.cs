using HotChocolate.Internal;

namespace HotChocolate.Types;

public class NamedRuntimeTypeTests
{
    [Fact]
    public void Kind_NotSupportedException()
    {
        // arrange
        var type = new NamedRuntimeType<string>();

        // act
        TypeKind kind;
        void Action() => kind = ((IInputType)type).Kind;

        // assert
        Assert.Throws<NotSupportedException>(Action);
    }

    [Fact]
    public void ClrType_NotSupportedException()
    {
        // arrange
        var type = new NamedRuntimeType<string>();

        // act
        void Action() => type.ToRuntimeType();

        // assert
        Assert.Throws<NotSupportedException>(Action);
    }
}
