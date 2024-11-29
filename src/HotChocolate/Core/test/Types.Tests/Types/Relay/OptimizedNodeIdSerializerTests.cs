using System.Text;
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
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYQ==",
            id);
    }

    [Fact]
    public void Format_480_Byte_Long_StringId_Legacy_Format()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer(), outputNewIdFormat: false);

        var id = serializer.Format("Foo", new string('a', 480));

        Assert.Equal(
            "Rm9vCmRhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWE=",
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
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOmFiYw==", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_Empty_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

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
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCmRhYmM=", lookup.Object);

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_480_Byte_Long_StringId()
    {
        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse(
            "Rm9vOmFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFhYWFh" +
            "YWFhYWFhYWFhYWFhYWFhYQ==",
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
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

        var compositeId = new CompositeId("foo", 42, Guid.Empty, true);
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());
        var id = serializer.Format("Foo", compositeId);

        var parsed = serializer.Parse(id, lookup.Object);

        Assert.Equal(compositeId, parsed.InternalId);
    }

    [Fact]
    public void Ensure_Lookup_Works_With_HashCollision()
    {
        // arrange
        const string namesString =
            "Error,Node,Attribute,AttributeNotFoundError,AttributeProduct,AttributeProductValue," +
            "AttributeValue,AttributesConnection,AttributesEdge,CategoriesConnection,CategoriesEdge," +
            "Category,CategoryNotFoundError,Channel,ChannelNotFoundError,ChannelsConnection,ChannelsEdge," +
            "Collection,CreateAttributePayload,CreateCategoryPayload,CreateChannelPayload,CreateProductPayload," +
            "CreateVariantPayload,CreateVariantPricePayload,Currency,CurrencyChannel,DeleteAttributePayload," +
            "DeleteCategoryPayload,DeleteChannelPayload,DeleteProductPayload,DeleteVariantPayload," +
            "DeleteVariantPricePayload,EntitySaveError,InventoryEntry,Media,MediasConnection," +
            "MediasEdge,MetadataBooleanValue,MetadataCollection,MetadataCollectionsConnection," +
            "MetadataCollectionsEdge,MetadataDateValue,MetadataDefinition,MetadataNumberValue," +
            "MetadataTextValue,MetadataValue,Mutation,PageInfo,Product,ProductCategorySortOrder," +
            "ProductChannel,ProductCollection,ProductNotFoundError,ProductType,ProductTypesConnection," +
            "ProductTypesEdge,ProductVendor,ProductVendorsConnection,ProductVendorsEdge,ProductsConnection," +
            "ProductsEdge,Query,StorageProviderPayload,SubCategoriesConnection,SubCategoriesEdge,Tag," +
            "TagsConnection,TagsEdge,UpdateAttributePayload,UpdateCategoryPayload,UpdateChannelPayload," +
            "UpdateProductChannelAvailabilityPayload,UpdateProductPayload,UpdateVariantChannelAvailabilityPayload," +
            "UpdateVariantPayload,UpdateVariantPricePayload,UploadMediaPayload,Variant,VariantChannel,VariantMedia," +
            "VariantPrice,VariantsConnection,VariantsEdge,Warehouse,WarehouseChannel,CreateAttributeError," +
            "CreateCategoryError,CreateChannelError,CreateProductError,CreateVariantError,CreateVariantPriceError," +
            "DeleteAttributeError,DeleteCategoryError,DeleteChannelError,DeleteProductError,DeleteVariantError," +
            "DeleteVariantPriceError,MetadataTypedValue,StorageProviderError,UpdateAttributeError," +
            "UpdateCategoryError,UpdateChannelError,UpdateProductChannelAvailabilityError,UpdateProductError," +
            "UpdateVariantChannelAvailabilityError,UpdateVariantError,UpdateVariantPriceError,UploadMediaError," +
            "AttributeFilterInput,AttributeProductInput,AttributeProductValueUpdateInput,AttributeSortInput," +
            "AttributeValueFilterInput,BooleanOperationFilterInput,CategoryFilterInput,CategorySortInput," +
            "ChannelFilterInput,ChannelSortInput,CollectionFilterInput,CreateAttributeInput,CreateCategoryInput," +
            "CreateChannelInput,CreateProductInput,CreateVariantInput,CreateVariantPriceInput," +
            "CurrencyChannelFilterInput,CurrencyFilterInput,DateTimeOperationFilterInput,DeleteAttributeInput," +
            "DeleteCategoryInput,DeleteChannelInput,DeleteProductInput,DeleteVariantInput,DeleteVariantPriceInput," +
            "GeneralMetadataInput,IMetadataTypedValueFilterInput,IdOperationFilterInput,IntOperationFilterInput," +
            "InventoryEntryFilterInput,ListAttributeFilterInputWithSearchFilterInput," +
            "ListFilterInputTypeOfAttributeValueFilterInput,ListFilterInputTypeOfCurrencyChannelFilterInput," +
            "ListFilterInputTypeOfInventoryEntryFilterInput,ListFilterInputTypeOfMetadataDefinitionFilterInput," +
            "ListFilterInputTypeOfMetadataValueFilterInput,ListFilterInputTypeOfProductCategorySortOrderFilterInput," +
            "ListFilterInputTypeOfProductChannelFilterInput,ListFilterInputTypeOfProductCollectionFilterInput," +
            "ListFilterInputTypeOfVariantChannelFilterInput,ListFilterInputTypeOfVariantMediaFilterInput," +
            "ListFilterInputTypeOfVariantPriceFilterInput,ListFilterInputTypeOfWarehouseChannelFilterInput," +
            "ListProductFilterInputWithSearchFilterInput,ListTagFilterInputWithSearchFilterInput," +
            "ListVariantFilterInputWithSearchFilterInput,LongOperationFilterInput,MediaFilterInput," +
            "MediaSortInput,MetadataCollectionFilterInput,MetadataCollectionSortInput,MetadataDefinitionFilterInput," +
            "MetadataTypeOperationFilterInput,MetadataValueFilterInput,ProductCategorySortOrderFilterInput," +
            "ProductChannelAvailabilityUpdateInput,ProductChannelFilterInput,ProductCollectionFilterInput," +
            "ProductFilterInput,ProductSortInput,ProductTypeFilterInput,ProductTypeSortInput," +
            "ProductVendorFilterInput,ProductVendorSortInput,StorageProviderInput,StringOperationFilterInput," +
            "TagFilterInput,TagSortInput,UpdateAttributeInput,UpdateCategoryInput,UpdateChannelInput," +
            "UpdateProductChannelAvailabilityInput,UpdateProductInput,UpdateVariantChannelAvailabilityInput," +
            "UpdateVariantInput,UpdateVariantPriceInput,UploadMediaInput,UuidOperationFilterInput," +
            "VariantChannelAvailabilityUpdateInput,VariantChannelFilterInput,VariantFilterInput," +
            "VariantMediaFilterInput,VariantPriceFilterInput,VariantSortInput,WarehouseChannelFilterInput," +
            "WarehouseFilterInput,ApplyPolicy,MediaStorageProvider,MetadataType,SortEnumType,DateTime,Long," +
            "UUID,Upload";

        var lookup = new Mock<INodeIdRuntimeTypeLookup>();
        lookup.Setup(t => t.GetNodeIdRuntimeType(default)).Returns(default(Type));

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
            if (TryFormatIdPart(buffer, value.A, out var a) &&
                TryFormatIdPart(buffer.Slice(a), value.B, out var b) &&
                TryFormatIdPart(buffer.Slice(a + b), value.C, out var c) &&
                TryFormatIdPart(buffer.Slice(a + b + c), value.D, out var d))
            {
                written = a + b + c + d;
                return NodeIdFormatterResult.Success;
            }

            written = 0;
            return NodeIdFormatterResult.BufferTooSmall;
        }

        protected override bool TryParse(ReadOnlySpan<byte> buffer, out CompositeId value)
        {
            if (TryParseIdPart(buffer, out string a, out var ac) &&
                TryParseIdPart(buffer.Slice(ac), out int b, out var bc) &&
                TryParseIdPart(buffer.Slice(ac + bc), out Guid c, out var cc) &&
                TryParseIdPart(buffer.Slice(ac + bc + cc), out bool d, out _))
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
