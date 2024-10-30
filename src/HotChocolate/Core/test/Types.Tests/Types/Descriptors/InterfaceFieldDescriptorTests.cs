using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class InterfaceFieldDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => InterfaceFieldDescriptor.New(Context, "foo").Type((string)null);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(InterfaceFieldDescriptor).Type("foo");
        Assert.Throws<ArgumentNullException>(Error);
    }
}
