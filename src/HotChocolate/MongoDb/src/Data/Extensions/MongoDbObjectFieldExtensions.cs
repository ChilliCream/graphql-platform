using System.Linq;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace HotChocolate.Data.MongoDb;

internal static class MongoDbObjectFieldExtensions
{
    public static string GetName(this ObjectField field)
        => field.Member?.Name ?? field.Name;

    public static string GetProjectionName(this ObjectField field)
    {
        if (field.Member is null)
        {
            return field.Name;
        }

        try
        {
            var classMap = BsonClassMap.LookupClassMap(field.Member.DeclaringType!);
            var memberMap = classMap.AllMemberMaps.FirstOrDefault(x => x.MemberName == field.Member.Name);

            if (memberMap?.ElementName is { Length: > 0 } elementName)
            {
                return elementName;
            }
        }
        catch
        {
            // If there is no class map for the member's declaring type we fallback to the member name.
        }

        return field.Member.Name;
    }

    public static bool TryGetDiscriminatorElementName(
        this ObjectField field,
        [NotNullWhen(true)]
        out string? elementName)
    {
        elementName = null;

        if (!field.Type.NamedType().IsAbstractType())
        {
            return false;
        }

        var runtimeType = field.Type.UnwrapRuntimeType();
        if (!runtimeType.IsAbstract && !runtimeType.IsInterface)
        {
            return false;
        }

        IDiscriminatorConvention? convention = null;

        try
        {
            convention = BsonSerializer.LookupSerializer(runtimeType).GetDiscriminatorConvention();
        }
        catch
        {
            // ignored
        }

        if (convention is null)
        {
            try
            {
                convention = BsonSerializer.LookupDiscriminatorConvention(runtimeType);
            }
            catch
            {
                return false;
            }
        }

        if (string.IsNullOrEmpty(convention.ElementName))
        {
            return false;
        }

        elementName = convention.ElementName;
        return true;
    }
}
