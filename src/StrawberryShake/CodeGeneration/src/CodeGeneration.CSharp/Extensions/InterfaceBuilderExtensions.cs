using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class InterfaceBuilderExtensions
    {
        public static InterfaceBuilder AddProperty(
            this InterfaceBuilder builder,
            string name,
            Action<PropertyBuilder> configure)
        {
            PropertyBuilder propertyBuilder = PropertyBuilder.New().SetName(name);
            configure(propertyBuilder);
            builder.AddProperty(propertyBuilder);
            return builder;
        }
    }
}
