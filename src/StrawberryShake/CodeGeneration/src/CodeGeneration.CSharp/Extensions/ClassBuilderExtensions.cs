using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Generators;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class ClassBuilderExtensions
{
    public static MethodBuilder AddMethod(this ClassBuilder builder, string? name = null)
    {
        var methodBuilder = MethodBuilder.New();

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
        var methodBuilder = MethodBuilder.New().SetName(name);
        configure(methodBuilder);
        builder.AddMethod(methodBuilder);
        return builder;
    }

    public static FieldBuilder AddField(this ClassBuilder builder, string? name = null)
    {
        var fieldBuilder = FieldBuilder.New();

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
        var propertyBuilder = PropertyBuilder.New().SetName(name);
        configure(propertyBuilder);
        builder.AddProperty(propertyBuilder);
        return builder;
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

    public static ConstructorBuilder AddConstructor(this ClassBuilder builder)
    {
        var constructorBuilder = ConstructorBuilder.New();
        builder.AddConstructor(constructorBuilder);
        return constructorBuilder;
    }

    public static T AddEquality<T>(
        this T syntax,
        string typeName,
        IReadOnlyList<PropertyDescriptor> properties,
        bool generateRecordEquality = false)
        where T : TypeDeclarationSyntax
    {
        TypeDeclarationSyntax modified = syntax;

        var builder = new StringBuilder();
        var codeWriter = new CodeWriter(builder);

        if (!generateRecordEquality)
        {
            modified = modified.AddImplements(TypeNames.IEquatable.WithGeneric(typeName));

            BuildObjectEqualsMethod(typeName).Build(codeWriter);
            codeWriter.Flush();
            var overrideMethod = builder.ToString();
            builder.Clear();

            modified = modified.AddMembers(
                CSharpSyntaxTree
                    .ParseText(overrideMethod,
                        new CSharpParseOptions(kind: SourceCodeKind.Script))
                    .GetCompilationUnitRoot()
                    .Members
                    .ToArray());
        }

        BuildEqualsMethod(typeName, properties).Build(codeWriter);
        codeWriter.Flush();
        var equalsMethod = builder.ToString();
        builder.Clear();

        modified = modified.AddMembers(
            CSharpSyntaxTree
                .ParseText(
                    equalsMethod,
                    new CSharpParseOptions(kind: SourceCodeKind.Script))
                .GetCompilationUnitRoot()
                .Members
                .ToArray());

        BuildGetHashCodeMethod(properties).Build(codeWriter);
        codeWriter.Flush();
        var hashCodeMethod = builder.ToString();
        builder.Clear();
        modified = modified.AddMembers(
            CSharpSyntaxTree
                .ParseText(hashCodeMethod, new CSharpParseOptions(kind: SourceCodeKind.Script))
                .GetCompilationUnitRoot()
                .Members
                .ToArray());

        if (modified is T target)
        {
            return target;
        }

        throw new InvalidOperationException();
    }

    private static MethodBuilder BuildObjectEqualsMethod(string typeName)
    {
        const string obj = nameof(obj);

        return MethodBuilder
            .New()
            .SetName(nameof(IEquatable<object>.Equals))
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
    }

    private static MethodBuilder BuildGetHashCodeMethod(
        IReadOnlyList<PropertyDescriptor> properties)
    {
        const string obj = nameof(obj);

        return MethodBuilder
            .New()
            .SetName(nameof(GetHashCode))
            .SetPublic()
            .SetOverride()
            .SetReturnType(TypeNames.Int32)
            .AddCode(HashCodeBuilder
                .New()
                .AddProperties(properties));
    }

    private static MethodBuilder BuildEqualsMethod(
        string typeName,
        IReadOnlyList<PropertyDescriptor> properties)
    {
        const string other = nameof(other);

        var equalCondition =
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
            foreach (var property in properties)
            {
                equalCondition.And(ConditionBuilder
                    .New()
                    .Set(BuildPropertyComparison(property.Type, property.Name)));
            }
        }

        return MethodBuilder
            .New()
            .SetName(nameof(IEquatable<object>.Equals))
            .SetPublic()
            .SetInheritance(Inheritance.Virtual)
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
    }

    public static ClassBuilder AddEquality(
        this ClassBuilder builder,
        string typeName,
        IReadOnlyList<PropertyDescriptor> properties)
    {
        builder.AddImplements(TypeNames.IEquatable.WithGeneric(typeName));
        builder.AddMethod(BuildEqualsMethod(typeName, properties));
        builder.AddMethod(BuildObjectEqualsMethod(typeName));
        builder.AddMethod(BuildGetHashCodeMethod(properties));

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
                ILeafTypeDescriptor { SerializationType.IsValueType: true, } =>
                    CodeInlineBuilder
                        .New()
                        .SetText($"global::System.Object.{nameof(Equals)}({propertyName}, {other}.{propertyName})"),
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
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
