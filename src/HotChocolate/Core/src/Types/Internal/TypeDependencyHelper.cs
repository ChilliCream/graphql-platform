using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Internal;

public static class TypeDependencyHelper
{
    public static void CollectDependencies(
        InterfaceTypeDefinition definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (dependencies is null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        if (definition.HasDependencies)
        {
            foreach (TypeDependency dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        if (definition.HasInterfaces)
        {
            foreach (ITypeReference typeRef in definition.Interfaces)
            {
                dependencies.Add(new(typeRef, TypeDependencyKind.Completed));
            }
        }

        CollectDirectiveDependencies(definition, dependencies);
        CollectFieldDependencies(definition.Fields, dependencies);
    }

    public static void CollectDependencies(
        ObjectTypeDefinition definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (dependencies is null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        if (definition.HasDependencies)
        {
            foreach (TypeDependency dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        if (definition.HasInterfaces)
        {
            foreach (ITypeReference typeRef in definition.Interfaces)
            {
                dependencies.Add(new(typeRef, TypeDependencyKind.Completed));
            }
        }

        CollectDirectiveDependencies(definition, dependencies);
        CollectFieldDependencies(definition.Fields, dependencies);
    }

    public static void CollectDependencies(
        InputObjectTypeDefinition definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (dependencies is null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        if (definition.HasDependencies)
        {
            foreach (TypeDependency dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        foreach (InputFieldDefinition field in definition.Fields)
        {
            if (field.HasDependencies)
            {
                foreach (TypeDependency dependency in field.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (field.Type is not null)
            {
                dependencies.Add(new(field.Type, GetDefaultValueDependencyKind(field)));
            }

            CollectDirectiveDependencies(field, dependencies);
        }

        CollectDirectiveDependencies(definition, dependencies);
    }

    public static void CollectDependencies(
        EnumTypeDefinition definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (dependencies is null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        if (definition.HasDependencies)
        {
            foreach (TypeDependency dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        foreach (EnumValueDefinition value in definition.Values)
        {
            if (value.HasDependencies)
            {
                foreach (TypeDependency dependency in value.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            CollectDirectiveDependencies(value, dependencies);
        }

        CollectDirectiveDependencies(definition, dependencies);
    }

    public static void CollectDependencies(
        DirectiveTypeDefinition definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition.HasArguments)
        {
            foreach (DirectiveArgumentDefinition argument in definition.Arguments)
            {
                if (argument.Type is not null)
                {
                    dependencies.Add(new(
                        argument.Type,
                        GetDefaultValueDependencyKind(argument)));
                }
            }
        }
    }

    internal static void CollectDirectiveDependencies<T>(
        TypeDefinitionBase<T> definition,
        ICollection<TypeDependency> dependencies)
        where T : class, ISyntaxNode
    {
        if (definition.HasDirectives)
        {
            foreach (DirectiveDefinition directive in definition.Directives)
            {
                dependencies.Add(new(directive.TypeReference, TypeDependencyKind.Completed));
            }
        }
    }

    private static void CollectDirectiveDependencies(
        FieldDefinitionBase definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition.HasDirectives)
        {
            foreach (DirectiveDefinition directive in definition.Directives)
            {
                dependencies.Add(new(directive.TypeReference, TypeDependencyKind.Completed));
            }
        }
    }

    private static void CollectFieldDependencies(
        IReadOnlyList<OutputFieldDefinitionBase> fields,
        ICollection<TypeDependency> dependencies)
    {
        foreach (OutputFieldDefinitionBase field in fields)
        {
            if (field.HasDependencies)
            {
                foreach (TypeDependency dependency in field.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (field.Type is not null)
            {
                dependencies.Add(new(field.Type));
            }

            if (field.HasArguments)
            {
                CollectArgumentDependencies(field.GetArguments(), dependencies);
            }

            CollectDirectiveDependencies(field, dependencies);
        }
    }

    private static void CollectArgumentDependencies(
        IReadOnlyList<ArgumentDefinition> fields,
        ICollection<TypeDependency> dependencies)
    {
        foreach (ArgumentDefinition field in fields)
        {
            if (field.HasDependencies)
            {
                foreach (TypeDependency dependency in field.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (field.Type is not null)
            {
                dependencies.Add(new(field.Type, TypeDependencyKind.Completed));
            }

            CollectDirectiveDependencies(field, dependencies);
        }
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        ObjectTypeDefinition definition)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        InterfaceTypeDefinition definition)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        EnumTypeDefinition definition)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        InputObjectTypeDefinition definition)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        CollectDependencies(definition, context.Dependencies);
    }

    private static TypeDependencyKind GetDefaultValueDependencyKind(
        ArgumentDefinition argumentDefinition)
    {
        var hasDefaultValue =
            argumentDefinition.DefaultValue is not null and not NullValueNode ||
            argumentDefinition.RuntimeDefaultValue is not null;

        return hasDefaultValue
            ? TypeDependencyKind.Completed
            : TypeDependencyKind.Default;
    }
}
