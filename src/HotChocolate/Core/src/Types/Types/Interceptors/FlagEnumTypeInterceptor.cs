using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Interceptors;
#nullable enable

public class FlagsEnumInterceptor : TypeInterceptor
{
    private const string _flagNameAddition = "Flags";

    private readonly Dictionary<Type, string> _outputTypeCache = new();
    private readonly Dictionary<Type, RegisteredInputType> _inputTypeCache = new();
    private INamingConventions _namingConventions = default!;
    private TypeInitializer _typeInitializer = default!;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _namingConventions = context.Naming;
        _typeInitializer = typeInitializer;
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        switch (definition)
        {
            case ObjectTypeDefinition o:
                ProcessOutputFields(o.Fields);

                break;

            case InterfaceTypeDefinition i:
                ProcessOutputFields(i.Fields);

                break;

            case InputObjectTypeDefinition i:
                ProcessInputFields(i.Fields);

                break;

            case DirectiveTypeDefinition i:
                ProcessArguments(i.Arguments);

                break;
        }
    }

    private void ProcessOutputFields(IEnumerable<OutputFieldDefinitionBase> fields)
    {
        foreach (var field in fields)
        {
            ProcessArguments(field.Arguments);

            if (IsFlagsEnum(field.Type, out var fieldType))
            {
                var type = CreateObjectType(fieldType);
                field.Type = CreateTypeReference(field.Type, type);
            }
        }
    }

    private void ProcessArguments(IEnumerable<ArgumentDefinition> argumentDefinitions)
    {
        foreach (var arg in argumentDefinitions)
        {
            if (IsFlagsEnum(arg.Type, out var t))
            {
                var type = CreateInputObjectType(t);
                arg.Type = CreateTypeReference(arg.Type, type.Name);
                arg.Formatters.Add(type.Formatter);
            }
        }
    }

    private void ProcessInputFields(IEnumerable<InputFieldDefinition> fields)
    {
        foreach (var field in fields)
        {
            if (IsFlagsEnum(field.Type, out var t))
            {
                var type = CreateInputObjectType(t);
                field.Type = CreateTypeReference(field.Type, type.Name);
                field.Formatters.Add(type.Formatter);
            }
        }
    }

    private void RegisterType(TypeSystemObjectBase type)
    {
        _typeInitializer.InitializeType(type);
    }

    private string CreateObjectType(Type type)
    {
        if (_outputTypeCache.TryGetValue(type, out var outputType))
        {
            return outputType;
        }

        var typeName = _namingConventions.GetTypeName(type) + _flagNameAddition;
        var desc = _namingConventions.GetTypeDescription(type, TypeKind.Enum);
        var objectTypeDefinition = new ObjectTypeDefinition(typeName, desc)
        {
            RuntimeType = typeof(Dictionary<string, object>),
        };

        foreach (var value in Enum.GetValues(type))
        {
            var valueName = GetFlagFieldName(type, value);
            var description = _namingConventions.GetEnumValueDescription(value);
            var typeReference = TypeReference.Parse("Boolean!");
            PureFieldDelegate resolver = c => c.Parent<Enum>().HasFlag((Enum)value);
            var fieldDefinition =
                new ObjectFieldDefinition(valueName, description, typeReference, null, resolver);
            objectTypeDefinition.Fields.Add(fieldDefinition);
        }

        _outputTypeCache[type] = typeName;
        RegisterType(ObjectType.CreateUnsafe(objectTypeDefinition));

        return typeName;
    }

    private RegisteredInputType CreateInputObjectType(Type type)
    {
        if (_inputTypeCache.TryGetValue(type, out var result))
        {
            return result;
        }

        var typeName = $"{_namingConventions.GetTypeName(type)}{_flagNameAddition}Input";
        var desc = _namingConventions.GetTypeDescription(type, TypeKind.Enum);
        var objectTypeDefinition = new InputObjectTypeDefinition(typeName, desc)
        {
            RuntimeType = typeof(Dictionary<string, object>),
        };

        var metadata = new Dictionary<string, object>();
        foreach (var value in Enum.GetValues(type))
        {
            var valueName = GetFlagFieldName(type, value);
            var description = _namingConventions.GetEnumValueDescription(value);
            var typeReference = TypeReference.Parse("Boolean");
            var fieldDefinition = new InputFieldDefinition(valueName, description, typeReference);
            objectTypeDefinition.Fields.Add(fieldDefinition);
            metadata[valueName] = value;
        }

        var inputType = InputObjectType.CreateUnsafe(objectTypeDefinition);
        RegisterType(inputType);

        var typedFormatter = typeof(FlagsEnumFormatter<>).MakeGenericType(type);
        var formatter =
            (IInputValueFormatter)Activator.CreateInstance(typedFormatter, metadata, inputType)!;

        result = new RegisteredInputType(typeName, formatter);
        _inputTypeCache[type] = result;

        return result;
    }

    private static bool IsFlagsEnum(TypeReference? reference, [NotNullWhen(true)] out Type? type)
    {
        if (reference is not ExtendedTypeReference extReference)
        {
            type = null;

            return false;
        }

        var extendedType = extReference.Type;

        while (extendedType.ElementType is not null)
        {
            extendedType = extendedType.ElementType;
        }

        type = extendedType.Type;

        return extendedType.Type.IsDefined(typeof(FlagsAttribute), false);
    }

    private static string GetFlagFieldName(Type type, object value)
    {
        var valueName = Enum.GetName(type, value);
        if (string.IsNullOrEmpty(valueName))
        {
            throw ThrowHelper.Flags_IllegalFlagEnumName(type, valueName);
        }

        return $"is{char.ToUpper(valueName[0])}{valueName.Substring(1)}";
    }

    private static TypeReference? CreateTypeReference(TypeReference? reference, string typeName)
    {
        if (reference is not ExtendedTypeReference extReference)
        {
            return reference;
        }

        var referenceName = Rewrite(extReference.Type, typeName);

        return TypeReference.Parse(referenceName);

        static string Rewrite(IExtendedType reference, string typeName)
        {
            var nullability = reference.IsNullable ? "" : "!";
            if (reference.ElementType is not null)
            {
                return $"[{Rewrite(reference.ElementType, typeName)}]{nullability}";
            }

            return $"{typeName}{nullability}";
        }
    }

    private readonly struct RegisteredInputType
    {
        public RegisteredInputType(string name, IInputValueFormatter formatter)
        {
            Name = name;
            Formatter = formatter;
        }

        public string Name { get; }

        public IInputValueFormatter Formatter { get; }
    }

    private sealed class FlagsEnumFormatter<T> : IInputValueFormatter where T : struct, Enum
    {
        private readonly Dictionary<string, object> _flags;
        private readonly InputObjectType _inputType;

        public FlagsEnumFormatter(Dictionary<string, object> flags, InputObjectType inputType)
        {
            _flags = flags;
            _inputType = inputType;
        }

        public object Format(object? originalValue)
        {
            if (originalValue is IDictionary<string, object> dict)
            {
                T? value = null;
                foreach (var key in dict)
                {
                    if (key.Value is true)
                    {
                        if (_flags.TryGetValue(key.Key, out var v) && v is T enumValue)
                        {
                            value ??= enumValue;
                            value = SetFlag(value.Value, enumValue);
                        }
                        else
                        {
                            throw ThrowHelper.Flags_Parser_UnknownSelection(key.Key, _inputType);
                        }
                    }
                }

                return value ?? throw ThrowHelper.Flags_Parser_NoSelection(_inputType);
            }

            if (originalValue is IList l)
            {
                var list = new List<object>(l.Count);
                for (var i = 0; i < l.Count; i++)
                {
                    list.Add(Format(l[i]));
                }

                return list;
            }

            return originalValue!;
        }

        /// <summary>
        /// Sets the value of a flag on the enum bitwise
        /// This can be made easier with .NET 7 Generic Math
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T SetFlag(T e, T flag)
        {
            switch (Unsafe.SizeOf<T>())
            {
                // byte, sbyte
                case 1:
                    var b =
                        (byte)(Unsafe.As<T, byte>(ref e) | Unsafe.As<T, byte>(ref flag));

                    return Unsafe.As<byte, T>(ref b);

                //short, ushort
                case 2:
                    var s =
                        (short)(Unsafe.As<T, short>(ref e) | Unsafe.As<T, short>(ref flag));

                    return Unsafe.As<short, T>(ref s);

                //int, uint
                case 4:
                    var i = Unsafe.As<T, uint>(ref e) | Unsafe.As<T, uint>(ref flag);

                    return Unsafe.As<uint, T>(ref i);

                //long , ulong
                case 8:
                    var l = Unsafe.As<T, ulong>(ref e) | Unsafe.As<T, ulong>(ref flag);

                    return Unsafe.As<ulong, T>(ref l);

                default:
                    throw ThrowHelper.Flags_Enum_Shape_Unknown(e.GetType());
            }
        }
    }
}
