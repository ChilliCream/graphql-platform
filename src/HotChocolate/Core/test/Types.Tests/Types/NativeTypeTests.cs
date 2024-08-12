namespace HotChocolate.Types;

public class NativeTypeTests
{
    [Fact]
    public void Kind_NotSupportedException()
    {
        // arrange
        var type = new NativeType<string>();

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
        var type = new NativeType<string>();

        // act
        Type clrType;
        void Action() => clrType = ((IInputType)type).RuntimeType;

        // assert
        Assert.Throws<NotSupportedException>(Action);
    }
}
