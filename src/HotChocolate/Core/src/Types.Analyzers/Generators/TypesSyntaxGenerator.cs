using System.Collections.Immutable;
using System.Text;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class TypesSyntaxGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
        => Execute(context, compilation, syntaxInfos);

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var sb = StringBuilderPool.Get();

        WriteTypes(context, syntaxInfos, sb);

        sb.Clear();

        WriteResolvers(context, compilation, syntaxInfos, sb);

        StringBuilderPool.Return(sb);
    }

    private static void WriteTypes(
        SourceProductionContext context,
        ImmutableArray<ISyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var firstNamespace = true;
        foreach (var group in syntaxInfos
                     .OfType<ObjectTypeExtensionInfo>()
                     .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            var generator = new ObjectTypeExtensionSyntaxGenerator(sb, group.Key);

            if (firstNamespace)
            {
                generator.WriteHeader();
                firstNamespace = false;
            }

            generator.WriteBeginNamespace();

            var firstClass = true;
            foreach (var objectTypeExtension in group)
            {
                if (objectTypeExtension.Diagnostics.Length > 0)
                {
                    foreach (var diagnostic in objectTypeExtension.Diagnostics)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }

                    continue;
                }

                if (!firstClass)
                {
                    sb.AppendLine();
                }
                firstClass = false;

                generator.WriteBeginClass(objectTypeExtension.Type.Name);
                generator.WriteInitializeMethod(objectTypeExtension);
                sb.AppendLine();
                generator.WriteConfigureMethod(objectTypeExtension);
                generator.WriteEndClass();
            }

            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.TypesFile, sb.ToString());
    }

    private static void WriteResolvers(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ISyntaxInfo> syntaxInfos,
        StringBuilder sb)
    {
        var generator = new ResolverSyntaxGenerator(sb);
        generator.WriteHeader();

        var firstNamespace = true;
        foreach (var group in syntaxInfos
                     .OfType<ObjectTypeExtensionInfo>()
                     .GroupBy(t => t.Type.ContainingNamespace.ToDisplayString()))
        {
            if(!firstNamespace)
            {
                sb.AppendLine();
            }
            firstNamespace = false;

            generator.WriteBeginNamespace(group.Key);

            var firstClass = true;
            foreach (var objectTypeExtension in group)
            {
                if(!firstClass)
                {
                    sb.AppendLine();
                }
                firstClass = false;

                var resolverInfos = objectTypeExtension.Members.Select(
                    m => CreateResolverInfo(objectTypeExtension, m));

                generator.WriteBeginClass(objectTypeExtension.Type.Name + "Resolvers");

                if (generator.AddResolverDeclarations(resolverInfos))
                {
                    sb.AppendLine();
                }

                generator.AddParameterInitializer(resolverInfos);

                foreach (var member in objectTypeExtension.Members)
                {
                    sb.AppendLine();
                    generator.AddResolver(
                        new ResolverName(objectTypeExtension.Type.Name, member.Name),
                        member,
                        compilation);
                }

                generator.WriteEndClass();
            }

            generator.WriteEndNamespace();
        }

        context.AddSource(WellKnownFileNames.ResolversFile, sb.ToString());
    }

    private static ResolverInfo CreateResolverInfo(
        ObjectTypeExtensionInfo objectTypeExtension,
        ISymbol member)
        => new ResolverInfo(
            new ResolverName(objectTypeExtension.Type.Name, member.Name),
            member as IMethodSymbol);
}
