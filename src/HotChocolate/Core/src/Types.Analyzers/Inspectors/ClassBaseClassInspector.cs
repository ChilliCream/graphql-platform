using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ClassBaseClassInspector : ISyntaxInspector
{
    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ISyntaxInfo? syntaxInfo)
    {
        if (context.Node is ClassDeclarationSyntax { BaseList.Types.Count: > 0, TypeParameterList: null } possibleType)
        {
            var model = context.SemanticModel.GetDeclaredSymbol(possibleType);
            if (model is { IsAbstract: false } type)
            {
                var typeDisplayString = type.ToDisplayString();
                var processing = new Queue<INamedTypeSymbol>();
                processing.Enqueue(type);

                var current = type.BaseType;

                while (current is not null)
                {
                    processing.Enqueue(current);

                    var displayString = current.ToDisplayString();

                    if (displayString.Equals(WellKnownTypes.SystemObject, StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (WellKnownTypes.TypeClass.Contains(displayString))
                    {
                        syntaxInfo = new TypeInfo(typeDisplayString);
                        return true;
                    }

                    if (WellKnownTypes.TypeExtensionClass.Contains(displayString))
                    {
                        syntaxInfo = new TypeExtensionInfo(typeDisplayString, false);
                        return true;
                    }

                    current = current.BaseType;
                }

                while (processing.Count > 0)
                {
                    current = processing.Dequeue();

                    var displayString = current.ToDisplayString();

                    if (displayString.Equals(WellKnownTypes.DataLoader, StringComparison.Ordinal))
                    {
                        syntaxInfo =  new RegisterDataLoaderInfo(typeDisplayString);
                        return true;
                    }

                    foreach (var interfaceType in current.Interfaces)
                    {
                        processing.Enqueue(interfaceType);
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
