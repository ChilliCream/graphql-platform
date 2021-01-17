using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;

namespace StrawberryShake.CodeGeneration.CSharp.Mappers
{
    public static class EnumAnalyzerToDescriptorMapper
    {
        public static EnumDescriptor Map(string @namespace, EnumTypeModel enumTypeModel)
        {
            return new (
                enumTypeModel.Name,
                @namespace,
                enumTypeModel.Values.Select(enumValue => new EnumElementDescriptor(enumValue.Name)).ToList()
            );
        }
    }
}
