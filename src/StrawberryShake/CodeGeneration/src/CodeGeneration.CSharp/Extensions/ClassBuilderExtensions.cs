using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Generators;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

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

        public static ClassBuilder AddEquality(
            this ClassBuilder builder,
            string typeName,
            IReadOnlyList<PropertyDescriptor> properties)
        {
            const string obj = nameof(obj);
            const string other = nameof(other);

            builder.AddImplements(TypeNames.IEquatable.WithGeneric(typeName));

            builder
                .AddMethod(nameof(IEquatable<object>.Equals))
                .SetPublic()
                .SetOverride()
                .SetReturnType(TypeNames.Boolean)
                .AddParameter(obj, x => x.SetType(TypeNames.Object.MakeNullable()))
                .AddCode(CodeBlockBuilder
                    .New()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition(MethodCallBuilder
                            .Inline()
                            .SetMethodName(nameof(ReferenceEquals))
                            .AddArgument("null")
                            .AddArgument(obj))
                        .AddCode("return false;"))
                    .AddEmptyLine()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition(MethodCallBuilder
                            .Inline()
                            .SetMethodName(nameof(ReferenceEquals))
                            .AddArgument("this")
                            .AddArgument(obj))
                        .AddCode("return true;"))
                    .AddEmptyLine()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition($"{obj}.GetType() != GetType()")
                        .AddCode("return false;"))
                    .AddEmptyLine()
                    .AddLine($"return Equals(({typeName}){obj});"));

            ConditionBuilder equalCondition =
                ConditionBuilder
                    .New()
                    .SetReturn()
                    .SetDetermineStatement();

            if (properties.Count == 0)
            {
                equalCondition.And("true");
            }
            else
            {
                foreach (PropertyDescriptor property in properties)
                {
                    equalCondition.And(ConditionBuilder
                        .New()
                        .Set(BuildPropertyComparison(property.Type, property.Name)));
                }
            }

            builder
                .AddMethod(nameof(IEquatable<object>.Equals))
                .SetPublic()
                .SetReturnType(TypeNames.Boolean)
                .AddParameter(other, x => x.SetType(typeName.MakeNullable()))
                .AddCode(CodeBlockBuilder
                    .New()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition(MethodCallBuilder
                            .Inline()
                            .SetMethodName(nameof(ReferenceEquals))
                            .AddArgument("null")
                            .AddArgument(other))
                        .AddCode("return false;"))
                    .AddEmptyLine()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition(MethodCallBuilder
                            .Inline()
                            .SetMethodName(nameof(ReferenceEquals))
                            .AddArgument("this")
                            .AddArgument(other))
                        .AddCode("return true;"))
                    .AddEmptyLine()
                    .AddCode(IfBuilder
                        .New()
                        .SetCondition($"{other}.GetType() != GetType()")
                        .AddCode("return false;"))
                    .AddEmptyLine()
                    .AddCode(equalCondition));

            builder
                .AddMethod(nameof(GetHashCode))
                .SetPublic()
                .SetOverride()
                .SetReturnType(TypeNames.Int32)
                .AddCode(HashCodeBuilder
                    .New()
                    .AddProperties(properties));

            return builder;
        }

        private static ICode BuildPropertyComparison(
            ITypeDescriptor type,
            string propertyName)
        {
            const string other = nameof(other);

            return BuildPropertyInternal(type, true);

            ICode BuildPropertyInternal(
                ITypeDescriptor currentType,
                bool isNullable)
            {
                return currentType switch
                {
                    NonNullTypeDescriptor d =>
                        BuildPropertyInternal(d.InnerType, false),
                    ILeafTypeDescriptor d when d.SerializationType.IsValueType =>
                        CodeInlineBuilder
                            .New()
                            .SetText($"{propertyName} == {other}.{propertyName}"),
                    INamedTypeDescriptor when isNullable =>
                        ConditionBuilder
                            .New()
                            .Set($"({propertyName} is null && {other}.{propertyName} is null) ||" +
                                $"{propertyName} != null && {propertyName}.{nameof(Equals)}(" +
                                $"{other}.{propertyName})"),
                    INamedTypeDescriptor =>
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(propertyName, nameof(Equals))
                            .AddArgument($"{other}.{propertyName}"),
                    ListTypeDescriptor =>
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(TypeNames.SequenceEqual)
                            .AddArgument(propertyName)
                            .AddArgument($"{other}.{propertyName}"),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}
