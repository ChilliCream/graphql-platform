using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class EnumModelGenerator
    {
        public ICodeDescriptor Generate(IModelGeneratorContext context, EnumType enumType)
        {
            var values = new List<EnumValueDescriptor>();

            foreach (EnumValue value in enumType.Values)
            {
                IDirective directive = value.Directives.FirstOrDefault(t =>
                    t.Name.Equals(GeneratorDirectives.Name)
                    && t.GetArgument<string>(GeneratorDirectives.ValueArgument) != null);

                string name = directive is null
                    ? GetPropertyName(value.Name.ToLowerInvariant())
                    : directive.GetArgument<string>(GeneratorDirectives.ValueArgument);

                values.Add(new EnumValueDescriptor(name, value.Name));
            }

            NameString typeName = context.GetOrCreateName(
                enumType.SyntaxNode,
                GetClassName(enumType.Name));

            var descriptor = new EnumDescriptor(
                typeName,
                context.Namespace,
                values);

            context.Register(descriptor);

            return descriptor;
        }
    }
}
