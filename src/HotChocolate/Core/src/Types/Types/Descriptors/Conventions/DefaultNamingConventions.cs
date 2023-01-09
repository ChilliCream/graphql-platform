using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class DefaultNamingConventions
    : Convention
        , INamingConventions
{
    private const string _inputPostfix = "Input";
    private const string _inputTypePostfix = "InputType";
    private const string _directivePostfix = "Directive";
    private const string _directiveTypePostfix = "DirectiveType";

    private readonly IDocumentationProvider _documentation;

    public DefaultNamingConventions()
    {
        _documentation = new NoopDocumentationProvider();
    }

    public DefaultNamingConventions(IDocumentationProvider documentation)
    {
        _documentation = documentation ??
            throw new ArgumentNullException(nameof(documentation));
    }

    protected IDocumentationProvider DocumentationProvider => _documentation;

    /// <inheritdoc />
    public virtual string GetTypeName(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type == typeof(Schema))
        {
            return Schema.DefaultName;
        }

        return type.GetGraphQLName();
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type type, TypeKind kind)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var name = type.GetGraphQLName();

        if (kind == TypeKind.InputObject)
        {
            var isInputObjectType = typeof(InputObjectType).IsAssignableFrom(type);
            var isEndingInput = name.EndsWith(_inputPostfix, StringComparison.Ordinal);
            var isEndingInputType = name.EndsWith(_inputTypePostfix, StringComparison.Ordinal);

            if (isInputObjectType && isEndingInputType)
            {
                return name.Substring(0, name.Length - 4);
            }

            if (isInputObjectType && !isEndingInput && !isEndingInputType)
            {
                return name + _inputPostfix;
            }

            if (!isInputObjectType && !isEndingInput)
            {
                return name + _inputPostfix;
            }
        }

        if (kind is TypeKind.Directive)
        {
            if (name.Length > _directivePostfix.Length &&
                name.EndsWith(_directivePostfix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - _directivePostfix.Length);
            }
            else if (name.Length > _directiveTypePostfix.Length &&
                name.EndsWith(_directiveTypePostfix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - _directiveTypePostfix.Length);
            }

            name = AttributeExtensions.NormalizeFieldName(name);
        }

        return name;
    }

    /// <inheritdoc />
    public virtual string? GetTypeDescription(Type type, TypeKind kind)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var description = type.GetGraphQLDescription();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = _documentation.GetDescription(type);
        }

        return description;
    }

    /// <inheritdoc />
    public virtual string GetMemberName(
        MemberInfo member,
        MemberKind kind)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return member.GetGraphQLName();
    }

    /// <inheritdoc />
    public virtual string? GetMemberDescription(
        MemberInfo member,
        MemberKind kind)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        var description = member.GetGraphQLDescription();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = _documentation.GetDescription(member);
        }

        return description;
    }

    /// <inheritdoc />
    public virtual string GetArgumentName(ParameterInfo parameter)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }
        return parameter.GetGraphQLName();
    }

    /// <inheritdoc />
    public virtual string? GetArgumentDescription(ParameterInfo parameter)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        var description = parameter.GetGraphQLDescription();

        if (string.IsNullOrWhiteSpace(description))
        {
            description = _documentation.GetDescription(parameter);
        }

        return description;
    }

    /// <inheritdoc />
    public virtual unsafe string GetEnumValueName(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var enumType = value.GetType();

        if (enumType.IsEnum)
        {
            var enumMember = enumType
                .GetMember(value.ToString()!)
                .FirstOrDefault();

            if (enumMember is not null &&
                enumMember.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return enumMember.GetCustomAttribute<GraphQLNameAttribute>()!.Name;
            }
        }

        var underscores = 0;
        var name = value.ToString().AsSpan();

        if (name.Length == 1)
        {
            return char.ToUpper(name[0]).ToString();
        }

        var allUpper = true;
        var lengthMinusOne = name.Length - 1;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i > 0 &&
                char.IsUpper(c) &&
                (!char.IsUpper(name[i - 1]) ||
                    (i < lengthMinusOne && char.IsLower(name[i + 1]))))
            {
                underscores++;
            }

            if (char.IsLetter(c) && char.IsLower(c))
            {
                allUpper = false;
            }
        }

        if (allUpper)
        {
            return value.ToString()!;
        }

        if (underscores == name.Length - 1 && char.IsUpper(name[0]))
        {
            fixed (char* charPtr = name)
            {
                return new string(charPtr);
            }
        }

        var size = underscores + name.Length;
        char[]? rented = null;
        var buffer = size <= 128
            ? stackalloc char[size]
            : rented = ArrayPool<char>.Shared.Rent(size);

        try
        {
            var p = 0;
            buffer[p++] = char.ToUpper(name[0]);

            var lastWasUnderline = false;

            for (var i = 1; i < name.Length; i++)
            {
                if (!lastWasUnderline &&
                    char.IsUpper(name[i]) &&
                    (!char.IsUpper(name[i - 1]) ||
                        (i < lengthMinusOne && char.IsLower(name[i + 1]))))
                {
                    buffer[p++] = '_';
                }
                buffer[p++] = char.ToUpper(name[i]);
                lastWasUnderline = name[i] == '_';
            }

            fixed (char* charPtr = buffer)
            {
                return new string(charPtr, 0, p);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    /// <inheritdoc />
    public virtual string? GetEnumValueDescription(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var enumType = value.GetType();

        if (enumType.IsEnum)
        {
            var enumMember = enumType
                .GetMember(value.ToString()!)
                .FirstOrDefault();

            if (enumMember != null)
            {
                var description = enumMember.GetGraphQLDescription();
                return string.IsNullOrEmpty(description)
                    ? _documentation.GetDescription(enumMember)
                    : description;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public virtual bool IsDeprecated(MemberInfo member, out string? reason)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        return member.IsDeprecated(out reason);
    }

    /// <inheritdoc />
    public virtual bool IsDeprecated(object value, out string? reason)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var enumType = value.GetType();

        if (enumType.IsEnum)
        {
            var enumMember = enumType
                .GetMember(value.ToString()!)
                .FirstOrDefault();

            if (enumMember != null)
            {
                return enumMember.IsDeprecated(out reason);
            }
        }

        if (value is ICustomAttributeProvider provider)
        {
            return provider.IsDeprecated(out reason);
        }

        reason = null;
        return false;
    }

    /// <inheritdoc />
    public string FormatFieldName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            throw new ArgumentException(
                TypeResources.DefaultNamingConventions_FormatFieldName_EmptyOrNull,
                nameof(fieldName));
        }

        return fieldName.Length > 1
            ? fieldName.Substring(0, 1).ToLowerInvariant() + fieldName.Substring(1)
            : fieldName.ToLowerInvariant();
    }
}
