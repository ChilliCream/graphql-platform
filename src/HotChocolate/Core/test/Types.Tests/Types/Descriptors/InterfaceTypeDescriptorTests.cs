namespace HotChocolate.Types.Descriptors;

public class InterfaceTypeDescriptorTests : DescriptorTestBase
{
    [Fact]
    public void InterfaceType_Issue_6222_CreateDefinition_DoesNotDuplicateFields()
    {
        var descriptor = InterfaceTypeDescriptor.New(Context);
        descriptor.Field("id").Type("ID!");

        var interfaceType = descriptor.CreateDefinition();
        Assert.Single(interfaceType.Fields);

        descriptor.CreateDefinition();
        Assert.Single(interfaceType.Fields);
    }
}
