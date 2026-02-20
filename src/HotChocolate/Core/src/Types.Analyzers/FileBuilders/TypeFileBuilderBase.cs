using System.Collections.Immutable;
using System.Text;
using HotChocolate.Types.Analyzers.Generators;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HotChocolate.Types.Analyzers.FileBuilders;

public abstract class TypeFileBuilderBase(StringBuilder sb)
{
    public CodeWriter Writer { get; } = new(sb);

    protected abstract string OutputFieldDescriptorType { get; }

    public void WriteHeader()
    {
        Writer.WriteFileHeader();
        Writer.WriteIndentedLine("using Microsoft.Extensions.DependencyInjection;");
        Writer.WriteIndentedLine("using HotChocolate.Internal;");
        Writer.WriteLine();
    }

    public void WriteBeginNamespace(IOutputTypeInfo type)
    {
        Writer.WriteIndentedLine("namespace {0}", type.Namespace);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    public void WriteEndNamespace()
    {
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    public virtual void WriteBeginClass(IOutputTypeInfo type)
    {
        Writer.WriteIndentedLine(
            "{0} static partial class {1}",
            type.IsPublic ? "public" : "internal",
            type.Name);
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    public void WriteEndClass()
    {
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
    }

    public abstract void WriteInitializeMethod(IOutputTypeInfo type, ILocalTypeLookup typeLookup);

    protected void WriteInitializationBase(
        string schemaFullTypeName,
        bool hasResolvers,
        bool requiresParameterBindings,
        ImmutableArray<AttributeData> attributes,
        DirectiveScope inaccessible)
    {
        if (schemaFullTypeName.StartsWith("global::", StringComparison.Ordinal))
        {
            schemaFullTypeName = schemaFullTypeName.Substring(8);
        }

        if (hasResolvers || attributes.Length > 0)
        {
            Writer.WriteIndentedLine("var extension = descriptor.Extend();");
            Writer.WriteIndentedLine("var configuration = extension.Configuration;");
        }

        if (hasResolvers)
        {
            Writer.WriteIndentedLine("var thisType = typeof(global::{0});", schemaFullTypeName);
            Writer.WriteIndentedLine("var bindingResolver = extension.Context.ParameterBindingResolver;");

            Writer.WriteIndentedLine(
                requiresParameterBindings
                    ? "var resolvers = new __Resolvers(bindingResolver);"
                    : "var resolvers = new __Resolvers();");
        }

        if (attributes.Length > 0)
        {
            Writer.WriteLine();
            Writer.WriteIndentedLine(
                "{0}.ApplyConfiguration(",
                WellKnownTypes.ConfigurationHelper);
            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine("extension.Context,");
                Writer.WriteIndentedLine("descriptor,");
                Writer.WriteIndentedLine("null,");

                var first = true;
                foreach (var attribute in attributes)
                {
                    if (!first)
                    {
                        Writer.WriteLine(',');
                    }

                    Writer.WriteIndent();
                    Writer.Write(GenerateAttributeInstantiation(attribute));
                    first = false;
                }

                Writer.WriteLine([')', ';']);
            }

            Writer.WriteIndentedLine("configuration.ConfigurationsAreApplied = true;");
        }

        if (inaccessible is DirectiveScope.Type)
        {
            Writer.WriteLine();
            Writer.WriteIndentedLine("descriptor.Directive(global::{0}.Instance);", WellKnownTypes.Inaccessible);
        }
    }

    protected virtual void WriteResolverBindings(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        if (type.Resolvers.Length == 0)
        {
            return;
        }

        Writer.WriteLine();
        Writer.WriteIndentedLine("var naming = descriptor.Extend().Context.Naming;");

        if (type.Resolvers.Any(t => t.Bindings.Length > 0))
        {
            Writer.WriteIndentedLine("var ignoredFields = new global::System.Collections.Generic.HashSet<string>();");

            foreach (var binding in type.Resolvers.SelectMany(t => t.Bindings))
            {
                if (binding.Kind is MemberBindingKind.Field)
                {
                    Writer.WriteIndentedLine(
                        "ignoredFields.Add(\"{0}\");",
                        binding.Name);
                }
                else if (binding.Kind is MemberBindingKind.Property)
                {
                    Writer.WriteIndentedLine(
                        "ignoredFields.Add(naming.GetMemberName(\"{0}\", global::{1}.ObjectField));",
                        binding.Name,
                        "HotChocolate.Types.MemberKind");
                }
            }

            Writer.WriteLine();
            Writer.WriteIndentedLine("foreach(string fieldName in ignoredFields)");
            Writer.WriteIndentedLine("{");
            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine("descriptor.Field(fieldName).Ignore();");
            }

            Writer.WriteIndentedLine("}");
        }

        foreach (var resolver in type.Resolvers)
        {
            var fieldName = resolver.FieldName;

            if (resolver.Member is IMethodSymbol)
            {
                if (fieldName.StartsWith("Get"))
                {
                    fieldName = fieldName.Substring(3);
                }

                if (resolver.ResultKind is ResolverResultKind.Task or
                    ResolverResultKind.TaskAsyncEnumerable or
                    ResolverResultKind.AsyncEnumerable
                    && fieldName.EndsWith("Async"))
                {
                    fieldName = fieldName.Substring(0, fieldName.Length - "Async".Length);
                }
            }

            Writer.WriteLine();
            Writer.WriteIndentedLine("descriptor");

            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine(
                    ".Field(naming.GetMemberName(\"{0}\", global::HotChocolate.Types.MemberKind.ObjectField))",
                    fieldName);

                if (resolver.Kind is ResolverKind.ConnectionResolver)
                {
                    Writer.WriteIndentedLine(
                        ".AddPagingArguments()");
                }

                if (type.Inaccessible is DirectiveScope.Field || resolver.Inaccessible is not DirectiveScope.None)
                {
                    Writer.WriteIndentedLine(".Directive(global::{0}.Instance)", WellKnownTypes.Inaccessible);
                }

                if (type.Shareable is DirectiveScope.Field || resolver.Shareable is not DirectiveScope.None)
                {
                    Writer.WriteIndentedLine(".Directive(global::{0}.Instance)", WellKnownTypes.Shareable);
                }

                Writer.WriteIndentedLine(".ExtendWith(static (field, context) =>");
                Writer.WriteIndentedLine("{");

                using (Writer.IncreaseIndent())
                {
                    WriteResolverBindingExtendsWith(type, typeLookup, resolver);
                }

                Writer.WriteIndentedLine("},");
                Writer.WriteIndentedLine("(Resolvers: resolvers, ThisType: thisType));");
            }
        }
    }

    protected virtual void WriteResolverBindingDescriptor(
        IOutputTypeInfo type,
        Resolver resolver)
    {
        WriteAssignTypeRef(
            resolver.SchemaTypeRef,
            "configuration.Type",
            "Output",
            ";");
    }

    private void WriteResolverBindingExtendsWith(
        IOutputTypeInfo type,
        ILocalTypeLookup typeLookup,
        Resolver resolver)
    {
        Writer.WriteIndentedLine("var configuration = field.Configuration;");
        Writer.WriteIndentedLine("var typeInspector = field.Context.TypeInspector;");
        Writer.WriteIndentedLine("var bindingResolver = field.Context.ParameterBindingResolver;");
        Writer.WriteIndentedLine("var naming = field.Context.Naming;");
        Writer.WriteLine();

        var description = resolver.Description;
        if (!string.IsNullOrEmpty(description))
        {
            Writer.WriteIndentedLine("configuration.Description = \"{0}\";", GeneratorUtils.EscapeForStringLiteral(description));
        }

        var deprecationReason = resolver.DeprecationReason;
        if (!string.IsNullOrEmpty(deprecationReason))
        {
            Writer.WriteIndentedLine("configuration.DeprecationReason = \"{0}\";", GeneratorUtils.EscapeForStringLiteral(deprecationReason));
        }

        WriteResolverBindingDescriptor(type, resolver);

        Writer.WriteIndentedLine(
            "configuration.ResultType = typeof({0});",
            resolver.ReturnType.ToClassNonNullableFullyQualifiedWithNullRefQualifier());

        WriteFieldFlags(resolver);

        if (resolver.Kind is ResolverKind.ConnectionResolver)
        {
            Writer.WriteIndentedLine(
                "var pagingOptions = global::{0}.GetPagingOptions(field.Context, null);",
                WellKnownTypes.PagingHelper);
            Writer.WriteIndentedLine("configuration.Features.Set(pagingOptions);");
        }

        if (!resolver.Parameters.IsEmpty)
        {
            var parentInfo = resolver.Parameters.GetParentInfo();
            if (parentInfo.HasValue)
            {
                Writer.WriteIndentedLine(
                    "configuration.SetFieldRequirements({0}, typeof({1}));",
                    SymbolDisplay.FormatLiteral(parentInfo.Value.Requirements ?? "", quote: true),
                    parentInfo.Value.Type);
            }
        }

        if (resolver.Parameters.Any(p => p.Kind is ResolverParameterKind.Argument or ResolverParameterKind.Unknown))
        {
            var resolverMethod = (IMethodSymbol)resolver.Member;
            var firstParameter = true;
            foreach (var parameter in resolver.Parameters)
            {
                if (parameter.Type.TypeKind is TypeKind.Error
                    || parameter.Kind is not (ResolverParameterKind.Argument or ResolverParameterKind.Unknown))
                {
                    continue;
                }

                Writer.WriteLine();

                if (firstParameter)
                {
                    Writer.WriteIndentedLine(
                        "var bindingInfo = field.Context.ParameterBindingResolver;");
                    Writer.WriteIndentedLine(
                        "var parameter = context.Resolvers.CreateParameterDescriptor_{0}_{1}();",
                        resolver.Member.Name,
                        parameter.Name);
                    Writer.WriteIndentedLine(
                        "var parameterInfo = bindingInfo.GetBindingInfo(parameter);");
                    firstParameter = false;
                }
                else
                {
                    Writer.WriteIndentedLine(
                        "parameter = context.Resolvers.CreateParameterDescriptor_{0}_{1}();",
                        resolver.Member.Name,
                        parameter.Name);
                    Writer.WriteIndentedLine(
                        "parameterInfo = bindingInfo.GetBindingInfo(parameter);");
                }

                Writer.WriteLine();

                using (Writer.WriteIfClause(
                    "parameterInfo.Kind is global::HotChocolate.Internal.ArgumentKind.Argument"))
                {
                    var parameterTypeString = ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup);

                    Writer.WriteIndentedLine(
                        "var argumentConfiguration = new global::{0}",
                        WellKnownTypes.ArgumentConfiguration);
                    Writer.WriteIndentedLine("{");

                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine(
                            "Name = naming.GetMemberName(\"{0}\", global::HotChocolate.Types.MemberKind.Argument),",
                            parameter.Name);
                        description = parameter.Description;
                        if (!string.IsNullOrEmpty(description))
                        {
                            Writer.WriteIndentedLine(
                                "Description = \"{0}\",",
                                GeneratorUtils.EscapeForStringLiteral(description));
                        }

                        deprecationReason = parameter.DeprecationReason;
                        if (!string.IsNullOrEmpty(deprecationReason))
                        {
                            Writer.WriteIndentedLine("DeprecationReason = \"{0}\",", GeneratorUtils.EscapeForStringLiteral(deprecationReason));
                        }

                        if (parameter.Parameter.HasExplicitDefaultValue)
                        {
                            var defaultValueString = GeneratorUtils.ConvertDefaultValueToString(
                                parameter.Parameter.ExplicitDefaultValue,
                                parameter.Type);
                            Writer.WriteIndentedLine("RuntimeDefaultValue = {0},", defaultValueString);
                        }

                        WriteAssignTypeRef(
                            parameter.SchemaTypeRef,
                            "Type",
                            "Input",
                            ",");

                        Writer.WriteIndentedLine("RuntimeType = typeof({0})", parameterTypeString);
                    }

                    Writer.WriteIndentedLine("};");

                    if (parameter.DescriptorAttributes.Length > 0)
                    {
                        Writer.WriteLine();
                        Writer.WriteIndentedLine(
                            "var argumentDescriptor = global::{0}.From(field.Context, argumentConfiguration);",
                            WellKnownTypes.ArgumentDescriptor);
                        Writer.WriteIndentedLine(
                            "{0}.ApplyConfiguration(",
                            WellKnownTypes.ConfigurationHelper);
                        using (Writer.IncreaseIndent())
                        {
                            Writer.WriteIndentedLine("field.Context,");
                            Writer.WriteIndentedLine("argumentDescriptor,");
                            Writer.WriteIndentedLine("null,");

                            var first = true;
                            foreach (var attribute in parameter.DescriptorAttributes)
                            {
                                if (!first)
                                {
                                    Writer.WriteLine(',');
                                }

                                Writer.WriteIndent();
                                Writer.Write(GenerateAttributeInstantiation(attribute));
                                first = false;
                            }

                            Writer.WriteLine([')', ';']);
                        }

                        Writer.WriteIndentedLine("argumentConfiguration.ConfigurationsAreApplied = true;");
                        Writer.WriteIndentedLine("argumentDescriptor.CreateConfiguration();");
                    }

                    Writer.WriteLine();
                    Writer.WriteIndentedLine("configuration.Arguments.Add(argumentConfiguration);");
                }
            }
        }

        if (resolver.DescriptorAttributes.Length > 0 || resolver.IsNodeResolver)
        {
            Writer.WriteLine();
            Writer.WriteIndentedLine("configuration.Member = context.ThisType.GetMethod(");
            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine(
                    "\"{0}\",",
                    resolver.Member.Name);
                Writer.WriteIndentedLine(
                    "global::{0},",
                    resolver.IsStatic
                        ? WellKnownTypes.StaticMemberFlags
                        : WellKnownTypes.InstanceMemberFlags);
                if (resolver.Parameters.Length == 0)
                {
                    Writer.WriteIndentedLine("global::System.Array.Empty<global::System.Type>());");
                }
                else
                {
                    var resolverMethods = (IMethodSymbol)resolver.Member;

                    Writer.WriteIndentedLine("new global::System.Type[]");
                    Writer.WriteIndentedLine("{");
                    using (Writer.IncreaseIndent())
                    {
                        for (var i = 0; i < resolver.Parameters.Length; i++)
                        {
                            var parameter = resolver.Parameters[i];

                            if (i > 0)
                            {
                                Writer.Write(',');
                                Writer.WriteLine();
                            }

                            Writer.WriteIndented(
                                "typeof({0})",
                                ToFullyQualifiedString(parameter.Type, resolverMethods, typeLookup));
                        }
                    }

                    Writer.WriteLine();
                    Writer.WriteIndentedLine("})!;");
                }
            }
        }

        if (resolver.DescriptorAttributes.Length > 0)
        {
            Writer.WriteLine();
            Writer.WriteIndentedLine(
                "var fieldDescriptor = global::{0}.From(field.Context, configuration);",
                OutputFieldDescriptorType);
            Writer.WriteIndentedLine(
                "{0}.ApplyConfiguration(",
                WellKnownTypes.ConfigurationHelper);
            using (Writer.IncreaseIndent())
            {
                Writer.WriteIndentedLine("field.Context,");
                Writer.WriteIndentedLine("fieldDescriptor,");
                Writer.WriteIndentedLine("configuration.Member,");

                var first = true;
                foreach (var attribute in resolver.DescriptorAttributes)
                {
                    if (!first)
                    {
                        Writer.WriteLine(',');
                    }

                    Writer.WriteIndent();
                    Writer.Write(GenerateAttributeInstantiation(attribute));
                    first = false;
                }

                Writer.WriteLine([')', ';']);
            }

            Writer.WriteIndentedLine("configuration.ConfigurationsAreApplied = true;");
            Writer.WriteIndentedLine("fieldDescriptor.CreateConfiguration();");
        }

        Writer.WriteLine();
        Writer.WriteIndentedLine("configuration.Resolvers = context.Resolvers.{0}();", resolver.Member.Name);

        if (resolver.ResultKind is not ResolverResultKind.Pure
            && !resolver.Member.HasPostProcessorAttribute()
            && resolver.Member.IsListType(out var elementType))
        {
            Writer.WriteIndentedLine(
                "configuration.ResultPostProcessor = global::{0}<{1}>.Default;",
                WellKnownTypes.ListPostProcessor,
                elementType);
        }
    }

    protected void WriteFieldFlags(Resolver resolver)
    {
        Writer.WriteLine();
        Writer.WriteIndentedLine("configuration.SetSourceGeneratorFlags();");

        if (resolver.Kind is ResolverKind.ConnectionResolver)
        {
            Writer.WriteIndentedLine("configuration.SetConnectionFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionEdgesField) == FieldFlags.ConnectionEdgesField)
        {
            Writer.WriteIndentedLine("configuration.SetConnectionEdgesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.ConnectionNodesField) == FieldFlags.ConnectionNodesField)
        {
            Writer.WriteIndentedLine("configuration.SetConnectionNodesFieldFlags();");
        }

        if ((resolver.Flags & FieldFlags.TotalCount) == FieldFlags.TotalCount)
        {
            Writer.WriteIndentedLine("configuration.SetConnectionTotalCountFieldFlags();");
        }
    }

    public virtual void WriteConfigureMethod(IOutputTypeInfo type)
    {
        if (type.RuntimeType is null)
        {
            Writer.WriteIndentedLine(
                "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor descriptor);");
        }
        else
        {
            Writer.WriteIndentedLine(
                "static partial void Configure(global::HotChocolate.Types.IObjectTypeDescriptor<{0}> descriptor);",
                type.RuntimeType.ToFullyQualified());
        }

        Writer.WriteLine();
    }

    public void WriteBeginResolverClass()
    {
        Writer.WriteIndentedLine("private sealed class __Resolvers");
        Writer.WriteIndentedLine("{");
        Writer.IncreaseIndent();
    }

    public void WriteEndResolverClass()
    {
        Writer.DecreaseIndent();
        Writer.WriteIndentedLine("}");
    }

    public virtual void WriteResolverFields(IOutputTypeInfo type)
    {
        foreach (var resolver in type.Resolvers)
        {
            if (resolver.RequiresParameterBindings)
            {
                WriteResolverField(resolver);
            }
        }
    }

    protected void WriteResolverField(Resolver resolver)
    {
        if (resolver.RequiresParameterBindings)
        {
            foreach (var parameter in resolver.Parameters)
            {
                if (parameter.Kind is ResolverParameterKind.Unknown)
                {
                    Writer.WriteIndentedLine(
                        "private readonly global::{0} _binding_{1}_{2};",
                        WellKnownTypes.ParameterBinding,
                        resolver.Member.Name,
                        parameter.Name);
                }
            }
        }
    }

    public virtual void WriteResolverConstructor(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        var resolverType =
            type.SchemaSchemaType ??
            type.RuntimeType ?? throw new InvalidOperationException("Schema type and runtime type are null.");

        WriteResolverConstructor(
            type,
            typeLookup,
            resolverType.ToFullyQualified(),
            type.Resolvers.Any(t => t.RequiresParameterBindings));
    }

    protected void WriteResolverConstructor(
        IOutputTypeInfo type,
        ILocalTypeLookup typeLookup,
        string resolverTypeName,
        bool requiresParameterBindings)
    {
        if (!requiresParameterBindings)
        {
            return;
        }

        Writer.WriteLine();
        Writer.WriteIndentedLine(
            "public __Resolvers(global::{0} bindingResolver)",
            WellKnownTypes.ParameterBindingResolver);
        Writer.WriteIndentedLine("{");

        using (Writer.IncreaseIndent())
        {
            WriteResolversBindingInitialization(type, typeLookup);
        }

        Writer.WriteIndentedLine("}");
        Writer.WriteLine();
    }

    protected virtual void WriteResolversBindingInitialization(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        foreach (var resolver in type.Resolvers)
        {
            WriteResolverBindingInitialization(resolver, typeLookup);
        }
    }

    protected void WriteResolverBindingInitialization(Resolver resolver, ILocalTypeLookup typeLookup)
    {
        if (resolver.Member is not IMethodSymbol resolverMethod)
        {
            return;
        }

        if (!resolver.RequiresParameterBindings)
        {
            return;
        }

        foreach (var parameter in resolver.Parameters)
        {
            if (parameter.Kind is ResolverParameterKind.Unknown)
            {
                Writer.WriteIndentedLine(
                    "_binding_{0}_{1} = bindingResolver.GetBinding(CreateParameterDescriptor_{0}_{1}());",
                    resolverMethod.Name,
                    parameter.Name);
            }
        }
    }

    public virtual void WriteResolverMethods(IOutputTypeInfo type, ILocalTypeLookup typeLookup)
    {
        var first = true;
        foreach (var resolver in type.Resolvers)
        {
            if (!first)
            {
                Writer.WriteLine();
            }

            first = false;

            WriteResolver(resolver, typeLookup);
        }
    }

    protected void WriteResolver(Resolver resolver, ILocalTypeLookup typeLookup)
    {
        if (resolver.RequiresParameterBindings)
        {
            foreach (var parameter in resolver.Parameters)
            {
                if (parameter.Kind is ResolverParameterKind.Unknown)
                {
                    Writer.WriteIndentedLine(
                        "public global::{0} CreateParameterDescriptor_{1}_{2}()",
                        WellKnownTypes.ParameterDescriptor,
                        resolver.Member.Name,
                        parameter.Name);

                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine("=> new {0}(", WellKnownTypes.ParameterDescriptor);
                        using (Writer.IncreaseIndent())
                        {
                            Writer.WriteIndentedLine("\"{0}\",", parameter.Name);
                            Writer.WriteIndentedLine(
                                "typeof({0}),",
                                ToFullyQualifiedString(parameter.Type, (IMethodSymbol)resolver.Member, typeLookup));
                            Writer.WriteIndentedLine(
                                parameter.Type.IsNullableType()
                                    ? "isNullable: true,"
                                    : "isNullable: false,");

                            if (parameter.Attributes.Length == 0)
                            {
                                Writer.WriteIndentedLine("[]);");
                            }
                            else
                            {
                                Writer.WriteIndentedLine("[");

                                using (Writer.IncreaseIndent())
                                {
                                    var first = true;
                                    foreach (var attribute in parameter.Attributes)
                                    {
                                        if (!first)
                                        {
                                            Writer.Write(',');
                                            Writer.WriteLine();
                                        }

                                        Writer.WriteIndented(GenerateAttributeInstantiation(attribute));
                                        first = false;
                                    }

                                    Writer.WriteLine();
                                }

                                Writer.WriteIndentedLine("]);");
                            }
                        }
                    }
                }
            }

            Writer.WriteLine();
        }

        switch (resolver.Member)
        {
            case IMethodSymbol resolverMethod
                when resolver.ResultKind is ResolverResultKind.Pure:
                WritePureResolver(resolver, resolverMethod, typeLookup);
                break;

            case IMethodSymbol resolverMethod
                when resolver.ResultKind is ResolverResultKind.Task or ResolverResultKind.TaskAsyncEnumerable:
                WriteResolver(resolver, true, resolverMethod, typeLookup);
                break;

            case IMethodSymbol resolverMethod
                when resolver.ResultKind is ResolverResultKind.Executable or
                    ResolverResultKind.Queryable or
                    ResolverResultKind.AsyncEnumerable:
                WriteResolver(resolver, false, resolverMethod, typeLookup);
                break;

            case IPropertySymbol:
                WritePropertyResolver(resolver);
                break;
        }
    }

    private void WriteResolver(
        Resolver resolver,
        bool async,
        IMethodSymbol resolverMethod,
        ILocalTypeLookup typeLookup)
    {
        Writer.WriteMethod(
            "public",
            returnType: WellKnownTypes.FieldResolverDelegates,
            methodName: $"{resolver.Member.Name}",
            [],
            string.Format(
                "new global::{0}(resolver: {1})",
                WellKnownTypes.FieldResolverDelegates,
                resolver.Member.Name));

        Writer.WriteLine();

        Writer.WriteIndented("private ");

        if (async)
        {
            Writer.Write("async ");
        }

        Writer.WriteLine(
            "global::{0}<global::{1}?> {2}(global::{3} context)",
            WellKnownTypes.ValueTask,
            WellKnownTypes.Object,
            resolver.Member.Name,
            WellKnownTypes.ResolverContext);
        Writer.WriteIndentedLine("{");
        using (Writer.IncreaseIndent())
        {
            WriteResolverArguments(resolver, resolverMethod, typeLookup);

            if (async)
            {
                Writer.WriteIndentedLine(
                    resolver.IsStatic
                        ? "var result = await {0}.{1}({2});"
                        : "var result = await context.Parent<{0}>().{1}({2});",
                    resolver.Member.ContainingType.ToFullyQualified(),
                    resolver.Member.Name,
                    GetResolverArgumentAssignments(resolver.Parameters.Length));

                Writer.WriteIndentedLine("return result;");
            }
            else
            {
                Writer.WriteIndentedLine(
                    resolver.IsStatic
                        ? "var result = {0}.{1}({2});"
                        : "var result = context.Parent<{0}>().{1}({2});",
                    resolver.Member.ContainingType.ToFullyQualified(),
                    resolver.Member.Name,
                    GetResolverArgumentAssignments(resolver.Parameters.Length));

                Writer.WriteIndentedLine(
                    "return new global::{0}<global::{1}?>(result);",
                    WellKnownTypes.ValueTask,
                    WellKnownTypes.Object);
            }
        }

        Writer.WriteIndentedLine("}");
    }

    private void WritePureResolver(Resolver resolver, IMethodSymbol resolverMethod, ILocalTypeLookup typeLookup)
    {
        using (Writer.WriteMethod(
            "public",
            returnType: WellKnownTypes.FieldResolverDelegates,
            methodName: $"{resolver.Member.Name}"))
        {
            if (resolver.RequiresParameterBindings)
            {
                var firstParam = true;
                Writer.WriteIndented("var isPureResolver = ");

                foreach (var parameter in resolver.Parameters)
                {
                    if (!parameter.RequiresBinding)
                    {
                        continue;
                    }

                    if (!firstParam)
                    {
                        Writer.Write(" && ");
                    }

                    firstParam = false;

                    Writer.Write(
                        "_binding_{0}_{1}.IsPure",
                        resolver.Member.Name,
                        parameter.Name);
                }

                Writer.WriteLine(";");

                Writer.WriteLine();

                Writer.WriteIndentedLine("return isPureResolver");
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(
                        "? new global::{0}(pureResolver: {1})",
                        WellKnownTypes.FieldResolverDelegates,
                        resolver.Member.Name);
                    Writer.WriteIndentedLine(
                        ": new global::{0}(resolver: c => new({1}(c)));",
                        WellKnownTypes.FieldResolverDelegates,
                        resolver.Member.Name);
                }
            }
            else
            {
                Writer.WriteIndentedLine(
                    "return new global::{0}(pureResolver: {1});",
                    WellKnownTypes.FieldResolverDelegates,
                    resolver.Member.Name);
            }
        }

        Writer.WriteLine();

        Writer.WriteIndentedLine(
            "private global::{0}? {1}(global::{2} context)",
            WellKnownTypes.Object,
            resolver.Member.Name,
            WellKnownTypes.ResolverContext);
        Writer.WriteIndentedLine("{");
        using (Writer.IncreaseIndent())
        {
            WriteResolverArguments(resolver, resolverMethod, typeLookup);

            Writer.WriteIndentedLine(
                resolver.IsStatic
                    ? "var result = {0}.{1}({2});"
                    : "var result = context.Parent<{0}>().{1}({2});",
                resolver.Member.ContainingType.ToFullyQualified(),
                resolver.Member.Name,
                GetResolverArgumentAssignments(resolver.Parameters.Length));

            Writer.WriteIndentedLine("return result;");
        }

        Writer.WriteIndentedLine("}");
    }

    private void WritePropertyResolver(Resolver resolver)
    {
        Writer.WriteMethod(
            "public",
            returnType: WellKnownTypes.FieldResolverDelegates,
            methodName: $"{resolver.Member.Name}",
            [],
            string.Format(
                "new global::{0}(pureResolver: {1})",
                WellKnownTypes.FieldResolverDelegates,
                resolver.Member.Name));

        Writer.WriteLine();

        Writer.WriteIndentedLine(
            "private global::{0}? {1}(global::{2} context)",
            WellKnownTypes.Object,
            resolver.Member.Name,
            WellKnownTypes.ResolverContext);
        Writer.WriteIndentedLine("{");
        using (Writer.IncreaseIndent())
        {
            Writer.WriteIndentedLine(
                resolver.IsStatic
                    ? "var result = {0}.{1};"
                    : "var result = context.Parent<{0}>().{1};",
                resolver.Member.ContainingType.ToFullyQualified(),
                resolver.Member.Name);

            Writer.WriteIndentedLine("return result;");
        }

        Writer.WriteIndentedLine("}");
    }

    private void WriteResolverArguments(Resolver resolver, IMethodSymbol resolverMethod, ILocalTypeLookup typeLookup)
    {
        if (resolver.Parameters.Length == 0)
        {
            return;
        }

        for (var i = 0; i < resolver.Parameters.Length; i++)
        {
            var parameter = resolver.Parameters[i];

            if (resolver.Kind is ResolverKind.NodeResolver
                && parameter.Kind is ResolverParameterKind.Argument or ResolverParameterKind.Unknown
                && (parameter.Name == "id" || parameter.Key == "id"))
            {
                Writer.WriteIndentedLine(
                    "var args{0} = context.GetLocalState<{1}>("
                    + "global::HotChocolate.WellKnownContextData.InternalId);",
                    i,
                    parameter.Type.ToFullyQualified());
                continue;
            }

            switch (parameter.Kind)
            {
                case ResolverParameterKind.Parent:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.Parent<{1}>();",
                        i,
                        resolver.Parameters[i].Type.ToFullyQualified());
                    break;

                case ResolverParameterKind.CancellationToken:
                    Writer.WriteIndentedLine("var args{0} = context.RequestAborted;", i);
                    break;

                case ResolverParameterKind.ClaimsPrincipal:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(\"ClaimsPrincipal\");",
                        i,
                        WellKnownTypes.ClaimsPrincipal);
                    break;

                case ResolverParameterKind.DocumentNode:
                    Writer.WriteIndentedLine("var args{0} = context.Operation.Document;", i);
                    break;

                case ResolverParameterKind.EventMessage:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedState<{1}>("
                        + "global::HotChocolate.WellKnownContextData.EventMessage);",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;

                case ResolverParameterKind.FieldNode:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.Selection.SyntaxNode",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;

                case ResolverParameterKind.OutputField:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.Selection.Field",
                        i,
                        parameter.Type.ToFullyQualified());
                    break;

                case ResolverParameterKind.HttpContext:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;

                case ResolverParameterKind.HttpRequest:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Request!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;

                case ResolverParameterKind.HttpResponse:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<global::{1}>(nameof(global::{1}))?.Response!;",
                        i,
                        WellKnownTypes.HttpContext);
                    break;

                case ResolverParameterKind.GetGlobalState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = GeneratorUtils.ConvertDefaultValueToString(defaultValue, parameter.Type);

                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalStateOrDefault<{1}{2}>(\"{3}\", {4});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Type.IsNullableRefType() ? "?" : string.Empty,
                        parameter.Key,
                        defaultValueString);
                    break;
                }

                case ResolverParameterKind.GetGlobalState when !parameter.IsNullable:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.GetGlobalState:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetGlobalStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.SetGlobalState:
                    Writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>("
                        + "value => context.SetGlobalState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.GetScopedState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = GeneratorUtils.ConvertDefaultValueToString(defaultValue, parameter.Type);

                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedStateOrDefault<{1}{2}>(\"{3}\", {4});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Type.IsNullableRefType() ? "?" : string.Empty,
                        parameter.Key,
                        defaultValueString);
                    break;
                }

                case ResolverParameterKind.GetScopedState when !parameter.IsNullable:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.GetScopedState:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetScopedStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.SetScopedState:
                    Writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>("
                        + "value => context.SetScopedState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.GetLocalState when parameter.Parameter.HasExplicitDefaultValue:
                {
                    var defaultValue = parameter.Parameter.ExplicitDefaultValue;
                    var defaultValueString = GeneratorUtils.ConvertDefaultValueToString(defaultValue, parameter.Type);

                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalStateOrDefault<{1}{2}>(\"{3}\", {4});",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Type.IsNullableRefType() ? "?" : string.Empty,
                        parameter.Key,
                        defaultValueString);
                    break;
                }

                case ResolverParameterKind.GetLocalState when !parameter.IsNullable:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalState<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.GetLocalState:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.GetLocalStateOrDefault<{1}>(\"{2}\");",
                        i,
                        parameter.Type.ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.SetLocalState:
                    Writer.WriteIndentedLine(
                        "var args{0} = new HotChocolate.SetState<{1}>("
                        + "value => context.SetLocalState(\"{2}\", value));",
                        i,
                        ((INamedTypeSymbol)parameter.Type).TypeArguments[0].ToFullyQualified(),
                        parameter.Key);
                    break;

                case ResolverParameterKind.Service:
                    if (parameter.Key is null)
                    {
                        Writer.WriteIndentedLine(
                            "var args{0} = context.Service<{1}>();",
                            i,
                            ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup));
                    }
                    else
                    {
                        Writer.WriteIndentedLine(
                            "var args{0} = context.Service<{1}>(\"{2}\");",
                            i,
                            ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup),
                            parameter.Key);
                    }

                    break;

                case ResolverParameterKind.Argument:
                    Writer.WriteIndentedLine(
                        "var args{0} = context.ArgumentValue<{1}>(\"{2}\");",
                        i,
                        ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup),
                        parameter.Key ?? parameter.Name);
                    break;

                case ResolverParameterKind.QueryContext:
                    var entityType = parameter.TypeParameters[0].ToFullyQualified();
                    Writer.WriteIndentedLine("var args{0}_selection = context.Selection;", i);
                    Writer.WriteIndentedLine("var args{0}_filter = global::{1}.GetFilterContext(context);",
                        i,
                        WellKnownTypes.FilterContextResolverContextExtensions);
                    Writer.WriteIndentedLine("var args{0}_sorting = global::{1}.GetSortingContext(context);",
                        i,
                        WellKnownTypes.SortingContextResolverContextExtensions);
                    Writer.WriteIndentedLine(
                        "var args{0} = new global::{1}<{2}>(",
                        i,
                        WellKnownTypes.QueryContext,
                        entityType);
                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine(
                            "global::{0}.AsSelector<{1}>(args{2}_selection, context.IncludeFlags),",
                            WellKnownTypes.HotChocolateExecutionSelectionExtensions,
                            entityType,
                            i);
                        Writer.WriteIndentedLine("args{0}_filter?.AsPredicate<{1}>(),", i, entityType);
                        Writer.WriteIndentedLine("args{0}_sorting?.AsSortDefinition<{1}>());", i, entityType);
                    }

                    break;

                case ResolverParameterKind.PagingArguments:
                    Writer.WriteIndentedLine(
                        "var args{0}_options = global::{1}.GetPagingOptions(context.Schema, context.Selection.Field);",
                        i,
                        WellKnownTypes.PagingHelper);
                    Writer.WriteIndentedLine(
                        "var args{0}_flags = global::{1}.GetConnectionFlags(context);",
                        i,
                        WellKnownTypes.ConnectionFlagsHelper);
                    Writer.WriteIndentedLine("var args{0}_first = context.ArgumentValue<int?>(\"first\");", i);
                    Writer.WriteIndentedLine("var args{0}_after = context.ArgumentValue<string?>(\"after\");", i);
                    Writer.WriteIndentedLine("int? args{0}_last = null;", i);
                    Writer.WriteIndentedLine("string? args{0}_before = null;", i);
                    Writer.WriteIndentedLine("bool args{0}_includeTotalCount = false;", i);
                    Writer.WriteLine();
                    Writer.WriteIndentedLine(
                        "if(args{0}_options.AllowBackwardPagination ?? global::{1}.AllowBackwardPagination)",
                        i,
                        WellKnownTypes.PagingDefaults);
                    Writer.WriteIndentedLine("{");
                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine("args{0}_last = context.ArgumentValue<int?>(\"last\");", i);
                        Writer.WriteIndentedLine("args{0}_before = context.ArgumentValue<string?>(\"before\");", i);
                    }

                    Writer.WriteIndentedLine("}");

                    Writer.WriteLine();
                    Writer.WriteIndentedLine("if(args{0}_first is null && args{0}_last is null)", i);
                    Writer.WriteIndentedLine("{");
                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine(
                            "args{0}_first = args{0}_options.DefaultPageSize ?? global::{1}.DefaultPageSize;",
                            i,
                            WellKnownTypes.PagingDefaults);
                    }

                    Writer.WriteIndentedLine("}");

                    Writer.WriteLine();
                    Writer.WriteIndentedLine(
                        "if(args{0}_options.IncludeTotalCount ?? global::{1}.IncludeTotalCount)",
                        i,
                        WellKnownTypes.PagingDefaults);
                    Writer.WriteIndentedLine("{");
                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine(
                            "args{0}_includeTotalCount = args{0}_flags.HasFlag(global::{1}.TotalCount);",
                            i,
                            WellKnownTypes.ConnectionFlags);
                    }

                    Writer.WriteIndentedLine("}");
                    Writer.WriteLine();
                    Writer.WriteIndentedLine(
                        "var args{0} = new global::{1}(",
                        i,
                        WellKnownTypes.PagingArguments);
                    using (Writer.IncreaseIndent())
                    {
                        Writer.WriteIndentedLine("args{0}_first,", i);
                        Writer.WriteIndentedLine("args{0}_after,", i);
                        Writer.WriteIndentedLine("args{0}_last,", i);
                        Writer.WriteIndentedLine("args{0}_before,", i);
                        Writer.WriteIndentedLine("args{0}_includeTotalCount)", i);
                        Writer.WriteIndentedLine("{");
                        using (Writer.IncreaseIndent())
                        {
                            Writer.WriteIndentedLine(
                                "EnableRelativeCursors = args{0}_flags.HasFlag(global::{1}.RelativeCursor)",
                                i,
                                WellKnownTypes.ConnectionFlags);
                        }

                        Writer.WriteIndentedLine("};");
                    }

                    break;

                case ResolverParameterKind.ConnectionFlags:
                    Writer.WriteIndentedLine(
                        "var args{0} = global::{1}.GetConnectionFlags(context);",
                        i,
                        WellKnownTypes.ConnectionFlagsHelper);
                    break;

                case ResolverParameterKind.Unknown:
                    Writer.WriteIndentedLine(
                        "var args{0} = _binding_{1}_{2}.Execute<{3}>(context);",
                        i,
                        resolver.Member.Name,
                        parameter.Name,
                        ToFullyQualifiedString(parameter.Type, resolverMethod, typeLookup));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void WriteAssignTypeRef(
        SchemaTypeReference typeReference,
        string propertyName,
        string context,
        string lineEnd)
    {
        switch (typeReference.Kind)
        {
            case SchemaTypeReferenceKind.ExtendedTypeReference:
                Writer.WriteIndentedLine(
                    "{0} = typeInspector.GetTypeRef(typeof({1}), {2}.{3}){4}",
                    propertyName,
                    typeReference.TypeString,
                    WellKnownTypes.TypeContext,
                    context,
                    lineEnd);
                break;

            case SchemaTypeReferenceKind.SyntaxTypeReference:
                Writer.WriteIndentedLine(
                    "{0} = global::{1}.Create(\"{2}\", {3}.{4})){5}",
                    propertyName,
                    WellKnownTypes.TypeReference,
                    typeReference.TypeString,
                    WellKnownTypes.TypeContext,
                    context,
                    lineEnd);
                break;

            case SchemaTypeReferenceKind.FactoryTypeReference:
                Writer.WriteIndentedLine(
                    "{0} = global::{1}.Create(",
                    propertyName,
                    WellKnownTypes.TypeReference);
                using (Writer.IncreaseIndent())
                {
                    Writer.WriteIndentedLine(
                        "typeInspector.GetTypeRef(typeof({0}), {1}.{2}),",
                        typeReference.TypeString,
                        WellKnownTypes.TypeContext,
                        context);
                    Writer.WriteIndentedLine(
                        "{0}){1}",
                        typeReference.TypeStructure,
                        lineEnd);
                }
                break;

            default:
                throw new NotSupportedException();
        }
    }

    private static string GetResolverArgumentAssignments(int parameterCount)
    {
        if (parameterCount == 0)
        {
            return string.Empty;
        }

        var arguments = new StringBuilder();

        for (var i = 0; i < parameterCount; i++)
        {
            if (i > 0)
            {
                arguments.Append(", ");
            }

            arguments.Append($"args{i}");
        }

        return arguments.ToString();
    }

    protected static string GenerateAttributeInstantiation(AttributeData attribute)
    {
        if (attribute.AttributeClass == null)
        {
            return string.Empty;
        }

        var className = attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var sb = new StringBuilder();
        sb.Append("new ");
        sb.Append(className);
        sb.Append('(');

        // Add constructor arguments
        var first = true;
        foreach (var arg in attribute.ConstructorArguments)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append(FormatTypedConstant(arg));
            first = false;
        }

        sb.Append(')');

        // Add named arguments (property initializers)
        if (attribute.NamedArguments.Length > 0)
        {
            sb.Append(" { ");

            first = true;
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(namedArg.Key);
                sb.Append(" = ");
                sb.Append(FormatTypedConstant(namedArg.Value));
                first = false;
            }

            sb.Append(" }");
        }

        return sb.ToString();
    }

    private static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return FormatPrimitive(constant.Value);

            case TypedConstantKind.Enum:
                var enumType = constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"{enumType}.{constant.Value}";

            case TypedConstantKind.Type:
                var typeArg = ((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"typeof({typeArg})";

            case TypedConstantKind.Array:
                var elements = constant.Values;
                if (elements.IsDefaultOrEmpty)
                {
                    var elementType = ((IArrayTypeSymbol?)constant.Type)?.ElementType
                        .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return $"new {elementType}[] {{ }}";
                }

                var elementStrings = string.Join(", ", elements.Select(FormatTypedConstant));
                return $"new[] {{ {elementStrings} }}";

            default:
                return constant.Value?.ToString() ?? "null";
        }
    }

    private static string FormatPrimitive(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        return value switch
        {
            string s => $"\"{EscapeString(s)}\"",
            char c => $"'{EscapeChar(c)}'",
            bool b => b ? "true" : "false",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            _ => value.ToString() ?? "null"
        };
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }

    public void Flush() => Writer.Flush();

    private static string ToFullyQualifiedString(
        ITypeSymbol type,
        IMethodSymbol resolverMethod,
        ILocalTypeLookup typeLookup)
    {
        if (type.TypeKind is TypeKind.Error && typeLookup.TryGetTypeName(type, resolverMethod, out var typeDisplayName))
        {
            return typeDisplayName;
        }

        return type.ToFullyQualified();
    }
}

file static class Extensions
{
    public static (string Requirements, string Type)? GetParentInfo(this ImmutableArray<ResolverParameter> parameters)
    {
        var parameter = parameters.FirstOrDefault(t => t.Kind is ResolverParameterKind.Parent);

        if (!string.IsNullOrEmpty(parameter?.Requirements))
        {
            return (parameter!.Requirements!, parameter.Type.ToFullyQualified());
        }

        return null;
    }
}
