using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CookieCrumble;
using HotChocolate.Language;

namespace HotChocolate.Types.Relay;

public class DefaultNodeIdSerializerTests
{
    [Fact]
    public void Format_Small_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Format("Foo", "abc");

        Assert.Equal("Rm9vOmFiYw==", id);
    }

    [Fact]
    public void Parse_Small_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vOmFiYw==");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
    }

    [Fact]
    public void Parse_Small_Legacy_StringId()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        var id = serializer.Parse("Rm9vCmRhYmM=");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal("abc", id.InternalId);
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
    public void Parse_480_Byte_Long_StringId()
    {
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
            "YWFhYWFhYWFhYWFhYWFhYQ==");

        Assert.Equal("Foo", id.TypeName);
        Assert.Equal(new string('a', 480), id.InternalId);
    }

    [Fact]
    public void Serialize_TypeName_Not_Registered()
    {
        var serializer = CreateSerializer("Foo", new StringNodeIdValueSerializer());

        void Error() => serializer.Format("Baz", "abc");

        Assert.Throws<NodeIdMissingSerializerException>(Error);
    }

    [Fact]
    public void Serialize_Int16Id()
    {
        var serializer = CreateSerializer("Foo", new Int16NodeIdValueSerializer());

        var id = serializer.Format("Foo", (short)6);

        Assert.Equal("Rm9vOjY=", id);
    }

    [Fact]
    public void Serialize_Int32Id()
    {
        var serializer = CreateSerializer("Foo", new Int32NodeIdValueSerializer());

        var id = serializer.Format("Foo", 32);

        Assert.Equal("Rm9vOjMy", id);
    }

    [Fact]
    public void Serialize_Int64Id()
    {
        var serializer = CreateSerializer("Foo", new Int64NodeIdValueSerializer());

        var id = serializer.Format("Foo", (long)64);

        Assert.Equal("Rm9vOjY0", id);
    }

    [Fact]
    public void Serialize_Guid()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer());

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOgAAAAAAAAAAAAAAAAAAAAA=", id);
    }

    [Fact]
    public void Serialize_Guid_Normal()
    {
        var serializer = CreateSerializer("Foo", new GuidNodeIdValueSerializer(false));

        var id = serializer.Format("Foo", Guid.Empty);

        Assert.Equal("Rm9vOjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAw", id);
    }

    [Fact]
    public void Format_CompositeId()
    {
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());

        var id = serializer.Format("Foo", new CompositeId("foo", 42, Guid.Empty, true));

        Assert.Equal("Rm9vOmZvbzo0MjoAAAAAAAAAAAAAAAAAAAAAOjE=", id);
    }

    [Fact]
    public void Parse_CompositeId()
    {
        var compositeId = new CompositeId("foo", 42, Guid.Empty, true);
        var serializer = CreateSerializer("Foo", new CompositeIdNodeIdValueSerializer());
        var id = serializer.Format("Foo", compositeId);

        var parsed = serializer.Parse(id);

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

        var names = new HashSet<string>(namesString.Split(','));
        var stringValueSerializer = new StringNodeIdValueSerializer();
        var mappings = names.Select(name => new BoundNodeIdValueSerializer(name, stringValueSerializer)).ToList();
        var nodeIdSerializer = new DefaultNodeIdSerializer(mappings, [stringValueSerializer]);
        var snapshot = new Snapshot();
        var sb = new StringBuilder();

        // act
        var formattedId = nodeIdSerializer.Format("VariantsEdge", "abc");
        var internalId = nodeIdSerializer.Parse(formattedId);

        foreach (var name in names)
        {
            var a = nodeIdSerializer.Format(name, "abc");
            var b = nodeIdSerializer.Parse(a);

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

    private static DefaultNodeIdSerializer CreateSerializer(string typeName, INodeIdValueSerializer serializer)
    {
        return new DefaultNodeIdSerializer(
            [new BoundNodeIdValueSerializer(typeName, serializer)],
            [serializer]);
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
            if(TryParseIdPart(buffer, out string a, out var ac) &&
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
