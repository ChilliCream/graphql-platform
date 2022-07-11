using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities.Serialization;

internal static class InputObjectConstructorResolver
{
    public static ConstructorInfo? GetCompatibleConstructor(
        Type type,
        IReadOnlyDictionary<string, InputField> fields)
    {
        var constructors = type.GetConstructors(
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        var defaultConstructor = constructors.FirstOrDefault(
            t => t.GetParameters().Length == 0);

        if (fields.Values.All(t => t.Property!.CanWrite))
        {
            if (defaultConstructor is not null)
            {
                return defaultConstructor;
            }

            if (constructors.Length == 0)
            {
                return null;
            }
        }

        var required = new HashSet<string>();
        CollectReadOnlyProperties(fields, required);

        if (constructors.Length > 0)
        {
            foreach (var constructor in
                constructors.OrderByDescending(t => t.GetParameters().Length))
            {
                if (IsCompatibleConstructor(constructor, fields, required))
                {
                    return constructor;
                }
            }
        }

        throw new InvalidOperationException(
            $"No compatible constructor found for input type type `{type.FullName}`.\r\n" +
            "Either you have to provide a public constructor with settable properties or " +
            "a public constructor that allows to pass in values for read-only properties." +
            $"There was no way to set the following properties: {string.Join(", ", required)}.");
    }

    private static bool IsCompatibleConstructor(
        ConstructorInfo constructor,
        IReadOnlyDictionary<string, InputField> fields,
        ISet<string> required)
    {
        return IsCompatibleConstructor(
            constructor.GetParameters(),
            fields,
            required);
    }

    private static bool IsCompatibleConstructor(
        ParameterInfo[] parameters,
        IReadOnlyDictionary<string, InputField> fields,
        ISet<string> required)
    {
        foreach (var parameter in parameters)
        {
            if (fields.TryGetParameter(parameter, out var field) &&
                parameter.ParameterType == field.Property!.PropertyType)
            {
                required.Remove(field.Name);
            }
            else
            {
                return false;
            }
        }

        return required.Count == 0;
    }

    private static void CollectReadOnlyProperties(
        IReadOnlyDictionary<string, InputField> fields,
        ISet<string> required)
    {
        required.Clear();

        foreach (var item in fields)
        {
            if (!item.Value.Property!.CanWrite)
            {
                required.Add(item.Value.Name);
            }
        }
    }

    public static bool TryGetParameter(
        this IReadOnlyDictionary<string, InputField> fields,
        ParameterInfo parameter,
        [NotNullWhen(true)] out InputField? field)
    {
        var name = parameter.Name!;
        var alternativeName = GetAlternativeParameterName(parameter.Name!);

        return (fields.TryGetValue(alternativeName, out field) ||
            fields.TryGetValue(name, out field));
    }

    private static string GetAlternativeParameterName(string name)
        => name.Length > 1
            ? name.Substring(0, 1).ToUpperInvariant() + name.Substring(1)
            : name.ToUpperInvariant();
}
