using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.CSharp.Mappers;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutor
    {
        private const string DefaultNamespace = "StrawberryShake.Generated";

        public void GenerateCSharpClient(ClientModel clientModel)
        {

        }

        public void GenerateEnums(
            IDictionary<string, ICodeDescriptor> types,
            ClientModel clientModel)
        {
            foreach (LeafTypeModel leafTypeModel in clientModel.LeafTypes)
            {
                if (leafTypeModel is not EnumTypeModel enumTypeModel)
                {
                    continue;
                }

                EnumDescriptor enumDescriptor =
                    EnumAnalyzerToDescriptorMapper.Map(DefaultNamespace, enumTypeModel);
                types.Add(enumDescriptor.Name, enumDescriptor);
            }
        }
    }
}
