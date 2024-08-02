using System.Globalization;
using System.Text;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Utilities;

public static class NameUtils
{
    public static string GetInterfaceName(string typeName)
    {
        return 'I' + GetClassName(typeName);
    }

    public static string GetClassName(params string[] s)
    {
        return GetClassName(string.Join(string.Empty, s));
    }

    public static string GetClassName(string typeName)
    {
        return GetPropertyName(typeName);
    }

    public static string GetPathName(Path path)
    {
        var builder = new StringBuilder();
        var current = path;

        while (current is not null or { IsRoot: false, })
        {
            if (current is NamePathSegment nameSegment)
            {
                builder.Insert(0, GetPropertyName(nameSegment.Name));
            }

            current = current.Parent;
        }

        return builder.ToString();
    }

    public static string GetMethodName(string fieldName) => GetPropertyName(fieldName);

    public static string GetPropertyName(string fieldName)
    {
        var value = new StringBuilder();

        for (var i = 0; i < fieldName.Length; i++)
        {
            if (i == 0 && char.IsLetter(fieldName[i]))
            {
                value.Append(char.ToUpper(fieldName[i], CultureInfo.InvariantCulture));
            }
            else
            {
                value.Append(fieldName[i]);
            }
        }

        return value.ToString();
    }

    public static string GetEnumValue(string enumValue)
    {
        var value = new StringBuilder();
        var upper = true;

        for (var i = 0; i < enumValue.Length; i++)
        {
            if (enumValue[i] == '_')
            {
                upper = true;

                if (i == 0)
                {
                    value.Append('_');
                }
            }
            else if (upper)
            {
                upper = false;
                value.Append(char.ToUpperInvariant(enumValue[i]));
            }
            else
            {
                value.Append(char.ToLowerInvariant(enumValue[i]));
            }
        }

        return value.ToString();
    }

    public static string GetFieldName(params string[] s)
    {
        return GetFieldName(string.Join(string.Empty, s));
    }

    public static string GetFieldName(string fieldName)
    {
        return "_" + GetParamNameUnsafe(fieldName);
    }

    public static string GetLeftPropertyAssignment(string property)
    {
        if (property is { Length: >0, } && property[0] == '_')
        {
            return $"this.{property}";
        }

        return property;
    }

    public static string GetParameterName(string parameterName)
    {
        return Keywords.ToSafeName(GetParamNameUnsafe(parameterName));
    }

    public static string GetParamNameUnsafe(string parameterName)
    {
        if (parameterName.Length > 0 && parameterName[0] == '_')
        {
            return parameterName;
        }

        var value = new StringBuilder();
        var first = true;

        for (var i = 0; i < parameterName.Length; i++)
        {
            if (i == 0 && char.IsLetter(parameterName[i]))
            {
                value.Append(char.ToLower(parameterName[i], CultureInfo.InvariantCulture));
                first = false;
            }
            else if (parameterName[i] == '_')
            {
                value.Append(char.ToLower(parameterName[i], CultureInfo.InvariantCulture));

                if (i + 1 < parameterName.Length &&
                    char.IsLetter(parameterName[i + 1]))
                {
                    value.Append(first
                        ? char.ToLower(parameterName[++i], CultureInfo.InvariantCulture)
                        : char.ToUpper(parameterName[++i], CultureInfo.InvariantCulture));
                    first = false;
                }
            }
            else
            {
                value.Append(parameterName[i]);
            }
        }

        return value.ToString();
    }
}
