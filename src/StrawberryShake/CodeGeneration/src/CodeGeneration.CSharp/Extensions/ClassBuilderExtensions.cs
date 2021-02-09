using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HotChocolate;
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

    internal static class PropertyBuilderExtensions
    {
        public static PropertyBuilder SetPublic(this PropertyBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Public);
        }

        public static PropertyBuilder SetPrivate(this PropertyBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Private);
        }

        public static PropertyBuilder SetInternal(this PropertyBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Internal);
        }

        public static PropertyBuilder SetProtected(this PropertyBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Protected);
        }
    }

    internal static class MethodBuilderExtensions
    {
        public static MethodBuilder SetPublic(this MethodBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Public);
        }

        public static MethodBuilder SetPrivate(this MethodBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Private);
        }

        public static MethodBuilder SetInternal(this MethodBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Internal);
        }

        public static MethodBuilder SetProtected(this MethodBuilder builder)
        {
            return builder.SetAccessModifier(AccessModifier.Protected);
        }

        public static MethodBuilder AddParameter(
            this MethodBuilder builder,
            string name,
            Action<ParameterBuilder> configure)
        {
            ParameterBuilder? parameterBuilder = ParameterBuilder.New().SetName(name);
            configure(parameterBuilder);
            builder.AddParameter(parameterBuilder);
            return builder;
        }
    }

    internal static class StringExtensions
    {
        public static string AsStringToken(this NameString str)
        {
            return "\"" + str + "\"";
        }

        public static string WithGeneric(this string str, params string[] generics)
        {
            return str + "<" + string.Join(", ", generics) + ">";
        }
    }

    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendLineForEach<T>(
            this StringBuilder builder,
            IEnumerable<T> collection,
            Func<T, string> factory)
        {
            foreach (var item in collection)
            {
                builder.AppendLine(factory(item));
            }

            return builder;
        }
    }
}
