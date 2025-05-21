using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.Extensions;

public static class TypeExtensions
{
    public static bool IsEntity(this ITypeDefinition typeDefinition)
        => typeDefinition.Features.Get<EntityFeature>() is not null;

    public static SelectionSetNode GetEntityDefinition(this ITypeDefinition typeDefinition)
        => typeDefinition.Features.GetRequired<EntityFeature>().Pattern;

    public static string GetRuntimeType(this ILeafType leafType)
        => leafType.Features.GetRequired<LeafTypeInfo>().RuntimeType;

    public static string GetSerializationType(this ILeafType leafType)
        => leafType.Features.GetRequired<LeafTypeInfo>().SerializationType;
}
