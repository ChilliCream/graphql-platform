using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Types;
using static System.Reflection.BindingFlags;

#nullable enable

namespace HotChocolate.Utilities.Serialization;

internal static class InputObjectConstructorResolver
{
    public static ConstructorInfo? GetCompatibleConstructor<T>(
        Type type,
        FieldCollection<T> fields,
        Dictionary<string, T> fieldMap,
        HashSet<string> required)
        where T : class, IInputField, IHasProperty
    {
        var constructors = type.GetConstructors(NonPublic | Public | Instance);

        if (AllPropertiesCanWrite(fields))
        {
            if (constructors.Length == 0 || type.IsValueType)
            {
                return null;
            }

            var defaultCtor = Array.Find(constructors, t => t.GetParameters().Length == 0);

            if (defaultCtor is not null)
            {
                return defaultCtor;
            }
        }

        CollectReadOnlyProperties(fields, required);
        ConstructorInfo? compatibleCtor = null;

        if (constructors.Length == 1)
        {
            var constructor = constructors[0];
            if (IsCompatibleConstructor(constructor, fieldMap, required))
            {
                compatibleCtor = constructor;
            }
        }
        else if (constructors.Length != 0)
        {
            foreach (var constructor in
                constructors.OrderByDescending(t => t.GetParameters().Length))
            {
                if (IsCompatibleConstructor(constructor, fieldMap, required))
                {
                    compatibleCtor = constructor;
                    break;
                }
            }
        }

        if (compatibleCtor is not null)
        {
            return compatibleCtor;
        }

        throw new InvalidOperationException(
            $"No compatible constructor found for input type type `{type.FullName}`.\r\n" +
            "Either you have to provide a public constructor with settable properties or " +
            "a public constructor that allows to pass in values for read-only properties. " +
            $"There was no way to set the following properties: {string.Join(", ", required)}.");
    }

    private static bool AllPropertiesCanWrite<T>(FieldCollection<T> fields)
        where T : class, IInputField, IHasProperty
    {
        foreach (var field in fields.AsSpan())
        {
            if (!(field.Property?.CanWrite ?? false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCompatibleConstructor<T>(
        ConstructorInfo constructor,
        IReadOnlyDictionary<string, T> fields,
        HashSet<string> required)
        where T : class, IInputField, IHasProperty
    {
        var count = required.Count;

        foreach (var parameter in constructor.GetParameters())
        {
            if (fields.TryGetParameter(parameter, out var field))
            {
                if (parameter.ParameterType == field.Property!.PropertyType)
                {
                    if (required.Contains(field.Name))
                    {
                        count--;
                    }
                }
                else if (
                    parameter.ParameterType.IsValueType &&
                    field.Property!.PropertyType.IsValueType &&
                    parameter.ParameterType.IsGenericType &&
                    typeof(Nullable<>) == parameter.ParameterType.GetGenericTypeDefinition() &&
                    parameter.ParameterType.GetGenericArguments()[0] == field.Property!.PropertyType)
                {
                    if (required.Contains(field.Name))
                    {
                        count--;
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (!parameter.HasDefaultValue)
            {
                return false;
            }
        }

        return count == 0;
    }

    private static void CollectReadOnlyProperties<T>(
        FieldCollection<T> fields,
        ISet<string> required)
        where T : class, IInputField, IHasProperty
    {
        required.Clear();

        foreach (var item in fields.AsSpan())
        {
            if (!(item.Property?.CanWrite ?? false))
            {
                required.Add(item.Name);
            }
        }
    }

    public static bool TryGetParameter<T>(
        this IReadOnlyDictionary<string, T> fields,
        ParameterInfo parameter,
        [NotNullWhen(true)] out T? field)
        where T : class, IInputField, IHasProperty
    {
        var name = parameter.Name!;
        var alterName = GetAlternativeParameterName(parameter.Name!);
        return fields.TryGetValue(alterName, out field) || fields.TryGetValue(name, out field);
    }

    private static string GetAlternativeParameterName(string name)
        => name.Length > 1
            ? string.Concat(name[..1].ToUpperInvariant(), name.AsSpan(1))
            : name.ToUpperInvariant();
}
