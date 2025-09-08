namespace HotChocolate.Types.Descriptors;

public class InterfaceTypeDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void InterfaceType_Issue_6222_CreateConfiguration_DoesNotDuplicateFields()
    {
        var descriptor = InterfaceTypeDescriptor.New(Context);
        descriptor.Field("id").Type("ID!");

        var interfaceType = descriptor.CreateConfiguration();
        Assert.Single(interfaceType.Fields);

        descriptor.CreateConfiguration();
        Assert.Single(interfaceType.Fields);
    }
}
