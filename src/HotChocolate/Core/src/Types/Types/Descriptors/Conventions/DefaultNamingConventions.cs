using System.Buffers;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public class DefaultNamingConventions
    : Convention
    , INamingConventions
{
    private const string InputPostfix = "Input";
    private const string InputTypePostfix = "InputType";
    private const string DirectivePostfix = "Directive";
    private const string DirectiveTypePostfix = "DirectiveType";

    private readonly IDocumentationProvider _documentation;
    private bool _formatInterfaceName;

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

    protected internal override void Initialize(IConventionContext context)
    {
        base.Initialize(context);
        _formatInterfaceName = context.DescriptorContext.Options.StripLeadingIFromInterface;
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type == typeof(Schema))
        {
            return ISchemaDefinition.DefaultName;
        }

        return type.GetGraphQLName();
    }

    /// <inheritdoc />
    public virtual string GetTypeName(Type type, TypeKind kind)
    {
        ArgumentNullException.ThrowIfNull(type);

        var name = type.GetGraphQLName();

        if (_formatInterfaceName
            && kind == TypeKind.Interface
            && type.IsInterface
            && name.Length > 1
            && char.IsUpper(name[0])
            && char.IsUpper(name[1])
            && name[0] == 'I')
        {
            return name[1..];
        }

        if (kind == TypeKind.InputObject)
        {
            var isInputObjectType = typeof(InputObjectType).IsAssignableFrom(type);
            var isEndingInput = name.EndsWith(InputPostfix, StringComparison.Ordinal);
            var isEndingInputType = name.EndsWith(InputTypePostfix, StringComparison.Ordinal);

            if (isInputObjectType && isEndingInputType)
            {
                return name[..^4];
            }

            if (isInputObjectType && !isEndingInput && !isEndingInputType)
            {
                return name + InputPostfix;
            }

            if (!isInputObjectType && !isEndingInput)
            {
                return name + InputPostfix;
            }
        }

        if (kind is TypeKind.Directive)
        {
            if (name.Length > DirectivePostfix.Length
                && name.EndsWith(DirectivePostfix, StringComparison.Ordinal))
            {
                name = name[..^DirectivePostfix.Length];
            }
            else if (name.Length > DirectiveTypePostfix.Length
                && name.EndsWith(DirectiveTypePostfix, StringComparison.Ordinal))
            {
                name = name[..^DirectiveTypePostfix.Length];
            }

            name = NameFormattingHelpers.FormatFieldName(name);
        }

        return name;
    }

    public virtual string GetTypeName(string originalTypeName, TypeKind kind)
    {
        var name = NameUtils.MakeValidGraphQLName(originalTypeName);
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(originalTypeName));

        if (_formatInterfaceName
            && kind == TypeKind.Interface
            && name.Length > 1
            && char.IsUpper(name[0])
            && char.IsUpper(name[1])
            && name[0] == 'I')
        {
            return name[1..];
        }

        if (kind == TypeKind.InputObject)
        {
            var isEndingInput = name.EndsWith(InputPostfix, StringComparison.Ordinal);

            if (!isEndingInput)
            {
                return name + InputPostfix;
            }
        }

        if (kind is TypeKind.Directive)
        {
            if (name.Length > DirectivePostfix.Length
                && name.EndsWith(DirectivePostfix, StringComparison.Ordinal))
            {
                name = name[..^DirectivePostfix.Length];
            }
            else if (name.Length > DirectiveTypePostfix.Length
                && name.EndsWith(DirectiveTypePostfix, StringComparison.Ordinal))
            {
                name = name[..^DirectiveTypePostfix.Length];
            }

            name = NameFormattingHelpers.FormatFieldName(name);
        }

        return name;
    }

    /// <inheritdoc />
    public virtual string? GetTypeDescription(Type type, TypeKind kind)
    {
        ArgumentNullException.ThrowIfNull(type);

        // we do not want the description of our internal schema types.
        if (ExtendedType.Tools.IsNonGenericBaseType(type)
            || ExtendedType.Tools.IsGenericBaseType(type))
        {
            return null;
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
        ArgumentNullException.ThrowIfNull(member);

        return member.GetGraphQLName();
    }

    public virtual string GetMemberName(string originalMemberName, MemberKind kind)
    {
        var name = NameUtils.MakeValidGraphQLName(originalMemberName);
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(originalMemberName));
        return NameFormattingHelpers.FormatFieldName(name);
    }

    /// <inheritdoc />
    public virtual string? GetMemberDescription(
        MemberInfo member,
        MemberKind kind)
    {
        ArgumentNullException.ThrowIfNull(member);

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
        ArgumentNullException.ThrowIfNull(parameter);

        return parameter.GetGraphQLName();
    }

    /// <inheritdoc />
    public virtual string? GetArgumentDescription(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

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
        ArgumentNullException.ThrowIfNull(value);

        var enumType = value.GetType();

        if (enumType.IsEnum)
        {
            var enumMember = enumType
                .GetMember(value.ToString()!)
                .FirstOrDefault();

            if (enumMember?.IsDefined(typeof(GraphQLNameAttribute)) == true)
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

            if (i > 0
                && char.IsUpper(c)
                && (!char.IsUpper(name[i - 1])
                || (i < lengthMinusOne && char.IsLower(name[i + 1]))))
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
                if (!lastWasUnderline
                    && char.IsUpper(name[i])
                    && (!char.IsUpper(name[i - 1])
                    || (i < lengthMinusOne && char.IsLower(name[i + 1]))))
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
        ArgumentNullException.ThrowIfNull(value);

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
        ArgumentNullException.ThrowIfNull(member);

        return member.IsDeprecated(out reason);
    }

    /// <inheritdoc />
    public virtual bool IsDeprecated(object value, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(value);

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
        => NameFormattingHelpers.FormatFieldName(fieldName);
}
