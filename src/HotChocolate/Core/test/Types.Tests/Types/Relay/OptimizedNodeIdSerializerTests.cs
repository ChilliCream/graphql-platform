using System.Text;
using HotChocolate.Buffers.Text;
using Moq;

namespace HotChocolate.Types.Relay;

public class OptimizedNodeIdSerializerTests
{
    [Fact]
    public void Format_Empty_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "");

        Assert.Equal("Rm9vOg==", id);
    }

    [Fact]
    public void Format_Small_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vOmFiYw==", id);
    }

    [Fact]
    public void Format_Small_StringId_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vCmRhYmM=", id);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", new string('a', 480));

        Assert.Equal(
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYQ==",
            id);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", new string('a', 480));

        Assert.Equal(
            "Rm9vCmRhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWE=",
            id);
    }

    [Fact]
    public void Format_TypeName_Not_Registered()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        void Error() => serializer.Format("Baz", "abc");

        Assert.Throws<NodeIdMissingSerializerException>(Error);
    }

    [Fact]
    public void Format_Int16Id()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Format_Int16Id_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vCnM2", id);
    }

    [Fact]
    public void Format_Int32Id()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vOjMy", id);
    }

    [Fact]
    public void Format_Int32Id_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vCmkzMg==", id);
    }

    [Fact]
    public void Format_Int64Id()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vOjY0", id);
    }

    [Fact]
    public void Format_Int64Id_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vCmw2NA==", id);
    }

    [Fact]
    public void Format_DecimalId()
    {
        var serializer = CreateSerializer("Foo", new DecimalNodeIdValueSerializer());

        var id = serializer.Format("Foo", (decimal)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Format_FloatId()
    {
        var serializer = CreateSerializer("Foo", new SingleNodeIdValueSerializer());

        var id = serializer.Format("Foo", (float)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Format_DoubleId()
    {
        var serializer = CreateSerializer("Foo", new DoubleNodeIdValueSerializer());

        var id = serializer.Format("Foo", (double)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Format_Empty_Guid()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer());

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOgAAAAAAAAAAAAAAAAAAAAA=", id);
    }

    [Fact]
    public void Format_Normal_Guid()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer(false));

        var internalId = new Guid("1ae27b14-8cf6-440d-9a46-09090a4af6f3");
        var id = serializer.Format("Foo", internalId);

        Assert.Equal("Rm9vOjFhZTI3YjE0OGNmNjQ0MGQ5YTQ2MDkwOTBhNGFmNmYz", id);
    }

    [Fact]
    public void Format_Normal_Guid_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer(false), outputNewIdFormat: false);

        var internalId = new Guid("1ae27b14-8cf6-440d-9a46-09090a4af6f3");
        var id = serializer.Format("Foo", internalId);

        Assert.Equal("Rm9vCmcxYWUyN2IxNDhjZjY0NDBkOWE0NjA5MDkwYTRhZjZmMw==", id);
    }

    [Fact]
    public void Format_Normal_Guid_Compressed()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer());

        var id = serializer.Format("Foo", new Guid("1ae27b14-8cf6-440d-9a46-09090a4af6f3"));

        Assert.Equal("Rm9vOhR74hr2jA1EmkYJCQpK9vM=", id);
    }

    [Fact]
    public void Format_CompositeId()
    {
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());

        var id = serializer.Format("Foo", new CompositeId("foo", 42, Guid.Empty, true));

        Assert.Equal("Rm9vOmZvbzo0MjoAAAAAAAAAAAAAAAAAAAAAOjE=", id);
    }

    [Fact]
    public void Format_CompositeId_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", new CompositeId("foo", 42, Guid.Empty, true));

        Assert.Equal("Rm9vCmRmb286NDI6AAAAAAAAAAAAAAAAAAAAADox", id);
    }

    [Fact]
    public void Parse_Small_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOmFiYw==", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_Empty_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOg==", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Parse_Empty_StringId2()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOg==", typeof(string));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Parse_Small_Legacy_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCmRhYmM=", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_480_Byte_Long_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse(
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh"
            + "YWFhYWFhYWFhYWFhYWFhYQ==",
            lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(new string('a', 480), id.InternalId);
    }

    [Fact]
    public void Parse_Int16Id()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(short));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((short)123, id.InternalId);
    }

    [Fact]
    public void Parse_Legacy_Int16Id()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCnMxMjM=", typeof(short));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((short)123, id.InternalId);
    }

    [Fact]
    public void Parse_Int32Id()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(int));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(123, id.InternalId);
    }

    [Fact]
    public void Parse_Legacy_Int32Id()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCmkxMjM=", typeof(int));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(123, id.InternalId);
    }

    [Fact]
    public void Parse_Int64Id()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(long));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((long)123, id.InternalId);
    }

    [Fact]
    public void Parse_Legacy_Int64Id()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCmwxMjM=", typeof(long));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((long)123, id.InternalId);
    }

    [Fact]
    public void Parse_DecimalId()
    {
        var serializer = CreateSerializer("Foo", new DecimalNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(decimal));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((decimal)123, id.InternalId);
    }

    [Fact]
    public void Parse_SingleId()
    {
        var serializer = CreateSerializer("Foo", new SingleNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(float));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((float)123, id.InternalId);
    }

    [Fact]
    public void Parse_DoublelId()
    {
        var serializer = CreateSerializer("Foo", new DoubleNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOjEyMw==", typeof(double));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((double)123, id.InternalId);
    }

    [Fact]
    public void Parse_CompositeId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var compositeId = new CompositeId("foo", 42, Guid.Empty, true);
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());
        var id = serializer.Format("Foo", compositeId);

        var parsed = serializer.Parse(id, lookup.Object);

        Assert.Equal(compositeId, parsed.InternalId);
    }

    [Fact]
    public void Parse_Throws_NodeIdInvalidFormatException_On_InvalidBase64Input()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        Assert.Throws<NodeIdInvalidFormatException>(() => serializer.Parse("!", typeof(string)));
    }

    [Fact]
    public void ParseOnRuntimeLookup_Throws_NodeIdInvalidFormatException_On_InvalidBase64Input()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        Assert.Throws<NodeIdInvalidFormatException>(() => serializer.Parse("!", lookup.Object));
    }

    [Theory]
    [InlineData("RW50aXR5OjE")] // No padding (length: 11).
    [InlineData("RW50aXR5OjE=")] // Correct padding (length: 12).
    [InlineData("RW50aXR5OjE==")] // Excess padding (length: 13).
    [InlineData("RW50aXR5OjE===")] // Excess padding (length: 14).
    [InlineData("RW50aXR5OjE====")] // Excess padding (length: 15).
    [InlineData("RW50aXR5OjE=====")] // Excess padding (length: 16).
    public void Parse_Ensures_Correct_Padding(string id)
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));
        var serializer = CreateSerializer("Entity", new Int32NodeIdValueSerializer());

        void Act1() => serializer.Parse(id, typeof(int));
        void Act2() => serializer.Parse(id, lookup.Object);

        Assert.Null(Record.Exception(Act1));
        Assert.Null(Record.Exception(Act2));
    }

    [Fact]
    public void Ensure_Lookup_Works_With_HashCollision()
    {
        // arrange
        const string namesString =
            "Error,Node,Attribute,AttributeNotFoundError,AttributeProduct,AttributeProductValue,"
            + "AttributeValue,AttributesConnection,AttributesEdge,CategoriesConnection,CategoriesEdge,"
            + "Category,CategoryNotFoundError,Channel,ChannelNotFoundError,ChannelsConnection,ChannelsEdge,"
            + "Collection,CreateAttributePayload,CreateCategoryPayload,CreateChannelPayload,CreateProductPayload,"
            + "CreateVariantPayload,CreateVariantPricePayload,Currency,CurrencyChannel,DeleteAttributePayload,"
            + "DeleteCategoryPayload,DeleteChannelPayload,DeleteProductPayload,DeleteVariantPayload,"
            + "DeleteVariantPricePayload,EntitySaveError,InventoryEntry,Media,MediasConnection,"
            + "MediasEdge,MetadataBooleanValue,MetadataCollection,MetadataCollectionsConnection,"
            + "MetadataCollectionsEdge,MetadataDateValue,MetadataDefinition,MetadataNumberValue,"
            + "MetadataTextValue,MetadataValue,Mutation,PageInfo,Product,ProductCategorySortOrder,"
            + "ProductChannel,ProductCollection,ProductNotFoundError,ProductType,ProductTypesConnection,"
            + "ProductTypesEdge,ProductVendor,ProductVendorsConnection,ProductVendorsEdge,ProductsConnection,"
            + "ProductsEdge,Query,StorageProviderPayload,SubCategoriesConnection,SubCategoriesEdge,Tag,"
            + "TagsConnection,TagsEdge,UpdateAttributePayload,UpdateCategoryPayload,UpdateChannelPayload,"
            + "UpdateProductChannelAvailabilityPayload,UpdateProductPayload,UpdateVariantChannelAvailabilityPayload,"
            + "UpdateVariantPayload,UpdateVariantPricePayload,UploadMediaPayload,Variant,VariantChannel,VariantMedia,"
            + "VariantPrice,VariantsConnection,VariantsEdge,Warehouse,WarehouseChannel,CreateAttributeError,"
            + "CreateCategoryError,CreateChannelError,CreateProductError,CreateVariantError,CreateVariantPriceError,"
            + "DeleteAttributeError,DeleteCategoryError,DeleteChannelError,DeleteProductError,DeleteVariantError,"
            + "DeleteVariantPriceError,MetadataTypedValue,StorageProviderError,UpdateAttributeError,"
            + "UpdateCategoryError,UpdateChannelError,UpdateProductChannelAvailabilityError,UpdateProductError,"
            + "UpdateVariantChannelAvailabilityError,UpdateVariantError,UpdateVariantPriceError,UploadMediaError,"
            + "AttributeFilterInput,AttributeProductInput,AttributeProductValueUpdateInput,AttributeSortInput,"
            + "AttributeValueFilterInput,BooleanOperationFilterInput,CategoryFilterInput,CategorySortInput,"
            + "ChannelFilterInput,ChannelSortInput,CollectionFilterInput,CreateAttributeInput,CreateCategoryInput,"
            + "CreateChannelInput,CreateProductInput,CreateVariantInput,CreateVariantPriceInput,"
            + "CurrencyChannelFilterInput,CurrencyFilterInput,DateTimeOperationFilterInput,DeleteAttributeInput,"
            + "DeleteCategoryInput,DeleteChannelInput,DeleteProductInput,DeleteVariantInput,DeleteVariantPriceInput,"
            + "GeneralMetadataInput,IMetadataTypedValueFilterInput,IdOperationFilterInput,IntOperationFilterInput,"
            + "InventoryEntryFilterInput,ListAttributeFilterInputWithSearchFilterInput,"
            + "ListFilterInputTypeOfAttributeValueFilterInput,ListFilterInputTypeOfCurrencyChannelFilterInput,"
            + "ListFilterInputTypeOfInventoryEntryFilterInput,ListFilterInputTypeOfMetadataDefinitionFilterInput,"
            + "ListFilterInputTypeOfMetadataValueFilterInput,ListFilterInputTypeOfProductCategorySortOrderFilterInput,"
            + "ListFilterInputTypeOfProductChannelFilterInput,ListFilterInputTypeOfProductCollectionFilterInput,"
            + "ListFilterInputTypeOfVariantChannelFilterInput,ListFilterInputTypeOfVariantMediaFilterInput,"
            + "ListFilterInputTypeOfVariantPriceFilterInput,ListFilterInputTypeOfWarehouseChannelFilterInput,"
            + "ListProductFilterInputWithSearchFilterInput,ListTagFilterInputWithSearchFilterInput,"
            + "ListVariantFilterInputWithSearchFilterInput,LongOperationFilterInput,MediaFilterInput,"
            + "MediaSortInput,MetadataCollectionFilterInput,MetadataCollectionSortInput,MetadataDefinitionFilterInput,"
            + "MetadataTypeOperationFilterInput,MetadataValueFilterInput,ProductCategorySortOrderFilterInput,"
            + "ProductChannelAvailabilityUpdateInput,ProductChannelFilterInput,ProductCollectionFilterInput,"
            + "ProductFilterInput,ProductSortInput,ProductTypeFilterInput,ProductTypeSortInput,"
            + "ProductVendorFilterInput,ProductVendorSortInput,StorageProviderInput,StringOperationFilterInput,"
            + "TagFilterInput,TagSortInput,UpdateAttributeInput,UpdateCategoryInput,UpdateChannelInput,"
            + "UpdateProductChannelAvailabilityInput,UpdateProductInput,UpdateVariantChannelAvailabilityInput,"
            + "UpdateVariantInput,UpdateVariantPriceInput,UploadMediaInput,UuidOperationFilterInput,"
            + "VariantChannelAvailabilityUpdateInput,VariantChannelFilterInput,VariantFilterInput,"
            + "VariantMediaFilterInput,VariantPriceFilterInput,VariantSortInput,WarehouseChannelFilterInput,"
            + "WarehouseFilterInput,ApplyPolicy,MediaStorageProvider,MetadataType,SortEnumType,DateTime,Long,"
            + "UUID,Upload";

        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var names = new HashSet<string>(namesString.Split(','));
        var stringValueSerializer = new StringNodeIdValueSerializer();
        var mappings = names.Select(name => new BoundNodeIdValueSerializer(name, stringValueSerializer)).ToList();
        var nodeIdSerializer = new OptimizedNodeIdSerializer(mappings, [stringValueSerializer]);
        var snapshot = new Snapshot();
        var sb = new StringBuilder();

        // act
        var formattedId = nodeIdSerializer.Format("VariantsEdge", "abc");
        var internalId = nodeIdSerializer.Parse(formattedId, lookup.Object);

        foreach (var name in names)
        {
            var a = nodeIdSerializer.Format(name, "abc");
            var b = nodeIdSerializer.Parse(a, lookup.Object);

            sb.Clear();
            sb.AppendLine(a);
            sb.Append($"{b.TypeName}:{b.InternalId}");
            snapshot.Add(sb.ToString(), name);
        }

        // assert
        Assert.Equal("VariantsEdge", internalId.TypeName);
        Assert.Equal("abc", internalId.InternalId);
        Assert.Equal("VmFyaWFudHNFZGdlOmFiYw==", formattedId);

        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public void Format_Base36_Empty_StringId()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "");

        // "Foo:" in Base36
        Assert.Equal("JJK3SQ", id);

        // round trip
        var nodeId = serializer.Parse(id, typeof(string));
        Assert.Empty(Assert.IsType<string>(nodeId.InternalId));
    }

    [Fact]
    public void Format_Base36_Small_StringId()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("5F7NDV7UA8Z", id); // "Foo:abc" in Base36
    }

    [Fact]
    public void Format_Base36_Small_StringId_Legacy_Format()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", "abc");

        // Legacy format includes type indicator byte
        var expected = Base36.Encode("Foo\ndabc"u8);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void Format_Base36_Long_StringId()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var longString = new string('a', 100);
        var id = serializer.Format("Foo", longString);

        // Should encode "Foo:" + 100 'a' characters
        var expectedBytes = Encoding.UTF8.GetBytes($"Foo:{longString}");
        var expectedBase36 = Base36.Encode(expectedBytes);
        Assert.Equal(expectedBase36, id);
    }

    [Fact]
    public void Format_Base36_Int16Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Format("Foo", (short)6);

        var expectedBytes = "Foo:6"u8.ToArray();
        var expectedBase36 = Base36.Encode(expectedBytes);
        Assert.Equal(expectedBase36, id);
    }

    [Fact]
    public void Format_Base36_Int32Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Format("Foo", 32);

        var expectedBytes = "Foo:32"u8.ToArray();
        var expectedBase36 = Base36.Encode(expectedBytes);
        Assert.Equal(expectedBase36, id);
    }

    [Fact]
    public void Format_Base36_Int64Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Format("Foo", (long)64);

        var expectedBytes = "Foo:64"u8.ToArray();
        var expectedBase36 = Base36.Encode(expectedBytes);
        Assert.Equal(expectedBase36, id);
    }

    [Fact]
    public void Format_Base36_Empty_Guid()
    {
        var serializer = CreateBase36Serializer("Foo", new GuidNodeIdValueSerializer());

        var id = serializer.Format("Foo", Guid.Empty);

        // Should encode "Foo:" + 16 zero bytes
        const string expectedResult = "887073HCMXIKMYVDGFXRCN6JFJPPVCW";
        Assert.Equal(expectedResult, id);
    }

    [Fact]
    public void Format_Base36_Normal_Guid()
    {
        var serializer = CreateBase36Serializer("Foo", new GuidNodeIdValueSerializer(false));

        var internalId = new Guid("1ae27b14-8cf6-440d-9a46-09090a4af6f3");
        var id = serializer.Format("Foo", internalId);

        // Verify it's valid Base36 and round-trips correctly
        Assert.True(id.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));
        Assert.NotEmpty(id);
    }

    [Fact]
    public void Format_Base36_CompositeId()
    {
        var serializer = CreateBase36Serializer("Foo", new CompositeIdNodeIdValueSerializer());

        var id = serializer.Format("Foo", new CompositeId("foo", 42, Guid.Empty, true));

        Assert.True(id.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));
        Assert.NotEmpty(id);
    }

    [Fact]
    public void Parse_Base36_Small_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("5F7NDV7UA8Z", lookup.Object); // "Foo:abc"

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Empty_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(typeof(string));

        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("JJK3SQ", lookup.Object); // "Foo:"

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Empty_StringId_WithRuntimeType()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("JJK3SQ", typeof(string)); // "Foo:"

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("", id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Int16Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int16NodeIdValueSerializer());

        // Create Base36 encoding of "Foo:123"
        var testBytes = "Foo:123"u8.ToArray();
        var base36Id = Base36.Encode(testBytes);

        var id = serializer.Parse(base36Id, typeof(short));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((short)123, id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Int32Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int32NodeIdValueSerializer());

        var testBytes = "Foo:123"u8.ToArray();
        var base36Id = Base36.Encode(testBytes);

        var id = serializer.Parse(base36Id, typeof(int));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(123, id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Int64Id()
    {
        var serializer = CreateBase36Serializer("Foo", new Int64NodeIdValueSerializer());

        var testBytes = "Foo:123"u8.ToArray();
        var base36Id = Base36.Encode(testBytes);

        var id = serializer.Parse(base36Id, typeof(long));

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal((long)123, id.InternalId);
    }

    [Fact]
    public void Parse_Base36_Case_Insensitive()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        // Test both upper and lower case versions
        var upperCaseId = serializer.Parse("5F7NDV7UA8Z", lookup.Object);
        var lowerCaseId = serializer.Parse("5f7ndv7ua8z", lookup.Object);

        Assert.Equal("Foo", upperCaseId.TypeName);
        Assert.Equal("abc", upperCaseId.InternalId);
        Assert.Equal("Foo", lowerCaseId.TypeName);
        Assert.Equal("abc", lowerCaseId.InternalId);
        Assert.Equal(upperCaseId.InternalId, lowerCaseId.InternalId);
    }

    [Fact]
    public void Parse_Base36_CompositeId_RoundTrip()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var compositeId = new CompositeId("test", 42, Guid.Empty, true);
        var serializer = CreateBase36Serializer("Foo", new CompositeIdNodeIdValueSerializer());

        var formatted = serializer.Format("Foo", compositeId);
        var parsed = serializer.Parse(formatted, lookup.Object);

        Assert.Equal("Foo", parsed.TypeName);
        Assert.Equal(compositeId, parsed.InternalId);
    }

    [Fact]
    public void Parse_Base36_Throws_NodeIdInvalidFormatException_On_InvalidInput()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        // Invalid Base36 character
        Assert.Throws<NodeIdInvalidFormatException>(() => serializer.Parse("5F7NDV7U@", typeof(string)));
    }

    [Fact]
    public void Parse_Base36_OnRuntimeLookup_Throws_NodeIdInvalidFormatException_On_InvalidInput()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(It.IsAny<string>())).Returns(default(Type));

        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        // Invalid Base36 character
        Assert.Throws<NodeIdInvalidFormatException>(() => serializer.Parse("5F7NDV7U@", lookup.Object));
    }

    [Fact]
    public void Format_Base36_TrailingZeros_Preserved()
    {
        var serializer = CreateBase36Serializer("Foo", new StringNodeIdValueSerializer());

        // Create string with null characters (trailing zeros)
        const string testString = "test\0\0\0";
        var id = serializer.Format("Foo", testString);

        // Verify it's valid Base36
        Assert.True(id.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));

        // Verify round trip preserves the zeros
        var parsed = serializer.Parse(id, typeof(string));
        Assert.Equal("Foo", parsed.TypeName);
        Assert.Equal(testString, parsed.InternalId);
    }

    [Fact]
    public void Base36_Format_Performance_Large_Data()
    {
        var serializer = CreateBase36Serializer(
            "LongTypeName",
            new StringNodeIdValueSerializer(),
            maxIdLength: 1568);

        // Test with larger data to ensure no performance regression
        var largeString = new string('x', 1000);
        var id = serializer.Format("LongTypeName", largeString);

        Assert.True(id.All(c => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(c)));

        // Verify round trip works
        var parsed = serializer.Parse(id, typeof(string));
        Assert.Equal("LongTypeName", parsed.TypeName);
        Assert.Equal(largeString, parsed.InternalId);
    }

    [Fact]
    public void Base36_Lookup_Works_With_Multiple_Types()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType("Foo")).Returns(typeof(string));
        lookup.Setup(t => t.GetNodeIdRuntimeType("Bar")).Returns(typeof(int));

        var stringSerializer = new StringNodeIdValueSerializer();
        var intSerializer = new Int32NodeIdValueSerializer();

        var serializer = new OptimizedNodeIdSerializer(
            [
                new BoundNodeIdValueSerializer("Foo", stringSerializer),
                new BoundNodeIdValueSerializer("Bar", intSerializer)
            ],
            [stringSerializer, intSerializer],
            format: NodeIdSerializerFormat.Base36);

        // Test string type
        var stringId = serializer.Format("Foo", "hello");
        var parsedString = serializer.Parse(stringId, lookup.Object);
        Assert.Equal("Foo", parsedString.TypeName);
        Assert.Equal("hello", parsedString.InternalId);

        // Test int type
        var intId = serializer.Format("Bar", 42);
        var parsedInt = serializer.Parse(intId, lookup.Object);
        Assert.Equal("Bar", parsedInt.TypeName);
        Assert.Equal(42, parsedInt.InternalId);
    }

    private static OptimizedNodeIdSerializer CreateBase36Serializer(
        string typeName,
        INodeIdValueSerializer serializer,
        bool outputNewIdFormat = true,
        int maxIdLength = 1024)
    {
        return new OptimizedNodeIdSerializer(
            [new BoundNodeIdValueSerializer(typeName, serializer)],
            [serializer],
            maxIdLength: maxIdLength,
            outputNewIdFormat: outputNewIdFormat,
            format: NodeIdSerializerFormat.Base36);
    }

    private static OptimizedNodeIdSerializer CreateSerializer(
        string typeName,
        INodeIdValueSerializer serializer,
        bool outputNewIdFormat = true)
    {
        return new OptimizedNodeIdSerializer(
            [new BoundNodeIdValueSerializer(typeName, serializer)],
            [serializer],
            outputNewIdFormat: outputNewIdFormat);
    }

    private sealed class CompositeIdNodeIdValueSerializer : CompositeNodeIdValueSerializer<CompositeId>
    {
        protected override NodeIdFormatterResult Format(Span<byte> buffer, CompositeId value, out int written)
        {
            if (TryFormatIdPart(buffer, value.A, out var a)
                && TryFormatIdPart(buffer[a..], value.B, out var b)
                && TryFormatIdPart(buffer[(a + b)..], value.C, out var c)
                && TryFormatIdPart(buffer[(a + b + c)..], value.D, out var d))
            {
                written = a + b + c + d;
                return NodeIdFormatterResult.Success;
            }

            written = 0;
            return NodeIdFormatterResult.BufferTooSmall;
        }

        protected override bool TryParse(ReadOnlySpan<byte> buffer, out CompositeId value)
        {
            if (TryParseIdPart(buffer, out string? a, out var ac)
                && TryParseIdPart(buffer[ac..], out int b, out var bc)
                && TryParseIdPart(buffer[(ac + bc)..], out Guid c, out var cc)
                && TryParseIdPart(buffer[(ac + bc + cc)..], out bool d, out _))
            {
                value = new CompositeId(a, b, c, d);
                return true;
            }

            value = default;
            return false;
        }
    }

    private readonly record struct CompositeId(string A, int B, Guid C, bool D);
}
