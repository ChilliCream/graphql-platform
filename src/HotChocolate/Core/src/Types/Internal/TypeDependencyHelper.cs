using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.Configurations.TypeDependencyFulfilled;

namespace HotChocolate.Internal;

public static class TypeDependencyHelper
{
    public static void CollectDependencies(
        InterfaceTypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (definition.HasDependencies)
        {
            foreach (var dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        if (definition.HasInterfaces)
        {
            foreach (var typeRef in definition.Interfaces)
            {
                dependencies.Add(new TypeDependency(typeRef, Completed));
            }
        }

        CollectDirectiveDependencies(definition, dependencies);
        CollectFieldDependencies(definition.Fields, dependencies);
    }

    public static void CollectDependencies(
        ObjectTypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (definition.HasDependencies)
        {
            foreach (var dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        if (definition.HasInterfaces)
        {
            foreach (var typeRef in definition.Interfaces)
            {
                dependencies.Add(new(typeRef, Completed));
            }
        }

        CollectDirectiveDependencies(definition, dependencies);
        CollectFieldDependencies(definition.Fields, dependencies);
    }

    public static void CollectDependencies(
        InputObjectTypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (definition.HasDependencies)
        {
            foreach (var dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        foreach (var field in definition.Fields)
        {
            if (field.HasDependencies)
            {
                foreach (var dependency in field.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (field.Type is not null)
            {
                dependencies.Add(new(field.Type));
            }

            CollectDirectiveDependencies(field, dependencies);
        }

        CollectDirectiveDependencies(definition, dependencies);
    }

    public static void CollectDependencies(
        EnumTypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (definition.HasDependencies)
        {
            foreach (var dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        foreach (var value in definition.Values)
        {
            if (value.HasDependencies)
            {
                foreach (var dependency in value.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            CollectDirectiveDependencies(value, dependencies);
        }

        CollectDirectiveDependencies(definition, dependencies);
    }

    public static void CollectDependencies(
        DirectiveTypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition.HasDependencies)
        {
            foreach (var dependency in definition.Dependencies)
            {
                dependencies.Add(dependency);
            }
        }

        if (definition.HasArguments)
        {
            foreach (var argument in definition.Arguments)
            {
                if (argument.HasDependencies)
                {
                    foreach (var dependency in argument.Dependencies)
                    {
                        dependencies.Add(dependency);
                    }
                }

                if (argument.Type is not null)
                {
                    dependencies.Add(new(argument.Type));
                }
            }
        }
    }

    internal static void CollectDirectiveDependencies(
        TypeConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition.HasDirectives)
        {
            foreach (var directive in definition.Directives)
            {
                dependencies.Add(new TypeDependency(directive.Type, Completed));
            }
        }
    }

    private static void CollectDirectiveDependencies(
        FieldConfiguration definition,
        ICollection<TypeDependency> dependencies)
    {
        if (definition.HasDirectives)
        {
            foreach (var directive in definition.Directives)
            {
                dependencies.Add(new TypeDependency(directive.Type, Completed));
            }
        }
    }

    private static void CollectFieldDependencies(
        IReadOnlyList<OutputFieldConfiguration> fields,
        ICollection<TypeDependency> dependencies)
    {
        foreach (var field in fields)
        {
            if (field.HasDependencies)
            {
                foreach (var dependency in field.Dependencies)
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
        IReadOnlyList<ArgumentConfiguration> fields,
        ICollection<TypeDependency> dependencies)
    {
        foreach (var field in fields)
        {
            if (field.HasDependencies)
            {
                foreach (var dependency in field.Dependencies)
                {
                    dependencies.Add(dependency);
                }
            }

            if (field.Type is not null)
            {
                dependencies.Add(new(field.Type, Completed));
            }

            CollectDirectiveDependencies(field, dependencies);
        }
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        ObjectTypeConfiguration definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        InterfaceTypeConfiguration definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        EnumTypeConfiguration definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        CollectDependencies(definition, context.Dependencies);
    }

    public static void RegisterDependencies(
        this ITypeDiscoveryContext context,
        InputObjectTypeConfiguration definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);

        CollectDependencies(definition, context.Dependencies);
    }
}
