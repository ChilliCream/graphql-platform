using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Descriptors.Definitions.TypeDependencyFulfilled;

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
        DirectiveTypeDefinition definition,
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
                    dependencies.Add(new(
                        argument.Type,
                        GetDefaultValueDependencyKind(argument)));
                }
            }
        }
    }

    internal static void CollectDirectiveDependencies(
        TypeDefinitionBase definition,
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
        FieldDefinitionBase definition,
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
        IReadOnlyList<OutputFieldDefinitionBase> fields,
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
        IReadOnlyList<ArgumentDefinition> fields,
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

    private static TypeDependencyFulfilled GetDefaultValueDependencyKind(
        ArgumentDefinition argumentDefinition)
    {
        var hasDefaultValue =
            argumentDefinition.DefaultValue is not null and not NullValueNode ||
            argumentDefinition.RuntimeDefaultValue is not null;

        return hasDefaultValue
            ? Completed
            : Default;
    }
}
