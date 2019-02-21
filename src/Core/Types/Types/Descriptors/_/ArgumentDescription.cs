using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescription
        : FieldDescriptionBase<InputValueDefinitionNode>
        , IHasSyntaxNode
    {
        public IValueNode DefaultValue { get; set; }

        public object NativeDefaultValue { get; set; }

        public override IDescriptionValidationResult Validate()
        {
            throw new NotImplementedException();
        }
    }
}
