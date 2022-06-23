using System;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Types;

public class DirectiveArgumentDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void Type_Syntax_Type_Null()
    {
        void Error() => DirectiveArgumentDescriptor.New(Context, "foo").Type((string)null);
        Assert.Throws<ArgumentNullException>(Error);
    }

    [Fact]
    public void Type_Syntax_Descriptor_Null()
    {
        void Error() => default(DirectiveArgumentDescriptor).Type("foo");
        Assert.Throws<ArgumentNullException>(Error);
    }
}
