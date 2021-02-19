using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class ClassBuilderExtensions
    {
        public static MethodBuilder AddMethod(this ClassBuilder builder, string name)
        {
            MethodBuilder methodBuilder = MethodBuilder.New().SetName(name);
            builder.AddMethod(methodBuilder);
            return methodBuilder;
        }

        public static FieldBuilder AddField(this ClassBuilder builder, string name)
        {
            FieldBuilder fieldBuilder = FieldBuilder.New().SetName(name);
            builder.AddField(fieldBuilder);
            return fieldBuilder;
        }

        public static PropertyBuilder AddProperty(this ClassBuilder builder, string name)
        {
            PropertyBuilder propertyBuilder = PropertyBuilder.New().SetName(name);
            builder.AddProperty(propertyBuilder);
            return propertyBuilder;
        }

        public static PropertyBuilder AddProperty(this AbstractTypeBuilder builder, string name)
        {
            PropertyBuilder propertyBuilder = PropertyBuilder.New().SetName(name);
            builder.AddProperty(propertyBuilder);
            return propertyBuilder;
        }

        public static ClassBuilder ForEach<T>(
            this ClassBuilder classBuilder,
            IEnumerable<T> enumerable,
            Action<ClassBuilder, T> configure)
        {
            foreach (T element in enumerable)
            {
                configure(classBuilder, element);
            }

            return classBuilder;
        }
    }
}
