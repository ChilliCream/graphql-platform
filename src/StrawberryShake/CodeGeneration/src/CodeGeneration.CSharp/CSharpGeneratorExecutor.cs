using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Mappers;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutor
    {
        private const string DefaultNamespace = "StrawberryShake.Generated";

        public void Generate(ClientModel clientModel)
        {
            if (clientModel == null)
            {
                throw new ArgumentNullException(nameof(clientModel));
            }

            var types = new Dictionary<NameString, ICodeDescriptor>();
            // GenerateEnums(types, )
        }

        public void GenerateEnums(
            IDictionary<NameString, ICodeDescriptor> types,
            ClientModel clientModel)
        {
            foreach (LeafTypeModel leafTypeModel in clientModel.LeafTypes)
            {
                if (leafTypeModel is not EnumTypeModel enumTypeModel)
                {
                    continue;
                }

                // EnumDescriptor enumDescriptor =
                    // EnumAnalyzerToDescriptorMapper.Map(DefaultNamespace, enumTypeModel);
                // types.Add(enumDescriptor.Name, enumDescriptor);
            }
        }
    }
}
