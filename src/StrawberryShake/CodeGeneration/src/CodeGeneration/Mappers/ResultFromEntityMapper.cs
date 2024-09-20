using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class ResultFromEntityMapper
{
    public static void Map(IMapperContext context)
    {
        foreach (var objectType in
             context.Types.OfType<ObjectTypeDescriptor>()
                 .Where(t => t.Kind is TypeKind.Entity))
        {
            var result = new ResultFromEntityDescriptor(
                objectType.Name,
                objectType.RuntimeType,
                objectType.Implements,
                objectType.Deferred,
                objectType.Description);
            result.CompleteProperties(objectType.Properties);
            context.Register(result);

            foreach (var fragmentDescriptor in objectType.Deferred)
            {
                var fragmentResult = new ResultFromEntityDescriptor(
                    objectType.Name,
                    fragmentDescriptor.Class.RuntimeType,
                    fragmentDescriptor.Class.Implements,
                    fragmentDescriptor.Class.Deferred,
                    fragmentDescriptor.Class.Description);
                fragmentResult.CompleteProperties(fragmentDescriptor.Class.Properties);
                context.Register(fragmentResult);
            }
        }
    }
}
