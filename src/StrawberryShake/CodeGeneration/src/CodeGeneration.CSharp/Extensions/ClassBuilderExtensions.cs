using System.Collections;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class ClassBuilderExtensions
    {
        public static MethodBuilder AddMethod(this ClassBuilder builder, string name)
        {
            var methodBuilder = MethodBuilder.New().SetName(name);
            builder.AddMethod(methodBuilder);
            return methodBuilder;
        }

        public static PropertyBuilder AddProperty(this ClassBuilder builder, string name)
        {
            var propertyBuilder = PropertyBuilder.New().SetName(name);
            builder.AddProperty(propertyBuilder);
            return propertyBuilder;
        }

        public static PropertyBuilder AddProperty(this AbstractTypeBuilder builder, string name)
        {
            var propertyBuilder = PropertyBuilder.New().SetName(name);
            builder.AddProperty(propertyBuilder);
            return propertyBuilder;
        }
    }
}
