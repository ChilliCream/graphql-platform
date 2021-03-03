using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class ClassBuilderExtensions
    {
        public static MethodBuilder AddMethod(this ClassBuilder builder, string? name = null)
        {
            MethodBuilder methodBuilder = MethodBuilder.New();

            if (name is not null)
            {
                methodBuilder.SetName(name);
            }

            builder.AddMethod(methodBuilder);
            return methodBuilder;
        }

        public static ClassBuilder AddMethod(
            this ClassBuilder builder,
            string name,
            Action<MethodBuilder> configure)
        {
            MethodBuilder methodBuilder = MethodBuilder.New().SetName(name);
            configure(methodBuilder);
            builder.AddMethod(methodBuilder);
            return builder;
        }

        public static FieldBuilder AddField(this ClassBuilder builder, string? name = null)
        {
            FieldBuilder fieldBuilder = FieldBuilder.New();

            if (name is not null)
            {
                fieldBuilder.SetName(name);
            }

            builder.AddField(fieldBuilder);
            return fieldBuilder;
        }

        public static ClassBuilder AddProperty(
            this ClassBuilder builder,
            string name,
            Action<PropertyBuilder> configure)
        {
            PropertyBuilder propertyBuilder = PropertyBuilder.New().SetName(name);
            configure(propertyBuilder);
            builder.AddProperty(propertyBuilder);
            return builder;
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

        public static ConstructorBuilder AddConstructor(this ClassBuilder builder)
        {
            var constructorBuilder = ConstructorBuilder.New();
            builder.AddConstructor(constructorBuilder);
            return constructorBuilder;
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
