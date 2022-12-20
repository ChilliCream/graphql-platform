using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Helpers;
using static System.Reflection.BindingFlags;

#nullable enable

namespace HotChocolate.Utilities.Serialization;

internal static class InputObjectConstructorResolver
{
    public static ConstructorInfo? GetCompatibleConstructor(
        Type type,
        InputObjectType inputObjectType,
        IReadOnlyDictionary<string, InputField> fields)
    {
        var constructors = type.GetConstructors(NonPublic | Public | Instance);

        if (AllPropertiesCanWrite(inputObjectType))
        {
            if (constructors.Length == 0)
            {
                return null;
            }

            var defaultCtor = Array.Find(constructors, t => t.GetParameters().Length == 0);

            if (defaultCtor is not null)
            {
                return defaultCtor;
            }
        }

        var required = TypeMemHelper.RentNameSet();
        CollectReadOnlyProperties(inputObjectType, required);
        ConstructorInfo? compatibleCtor = null;

        if (constructors.Length == 1)
        {
            var constructor = constructors[0];
            if (IsCompatibleConstructor(constructor, fields, required))
            {
                compatibleCtor = constructor;
            }
        }
        else if (constructors.Length != 0)
        {
            foreach (var constructor in
                constructors.OrderByDescending(t => t.GetParameters().Length))
            {
                if (IsCompatibleConstructor(constructor, fields, required))
                {
                    compatibleCtor = constructor;
                }
            }
        }

        TypeMemHelper.Return(required);

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

    private static bool AllPropertiesCanWrite(InputObjectType type)
    {
        foreach (var field in type.Fields.AsSpan())
        {
            if (!(field.Property?.CanWrite ?? false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCompatibleConstructor(
        ConstructorInfo constructor,
        IReadOnlyDictionary<string, InputField> fields,
        ISet<string> required)
    {
        var count = required.Count;

        foreach (var parameter in constructor.GetParameters())
        {
            if (fields.TryGetParameter(parameter, out var field) &&
                parameter.ParameterType == field.Property!.PropertyType)
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

        return count == 0;
    }

    private static void CollectReadOnlyProperties(
        InputObjectType type,
        ISet<string> required)
    {
        foreach (var item in type.Fields.AsSpan())
        {
            if (!(item.Property?.CanWrite ?? false))
            {
                required.Add(item.Name);
            }
        }
    }

    public static bool TryGetParameter(
        this IReadOnlyDictionary<string, InputField> fields,
        ParameterInfo parameter,
        [NotNullWhen(true)] out InputField? field)
    {
        string name = parameter.Name!;
        return fields.TryGetValue(name, out field);
    }
}
