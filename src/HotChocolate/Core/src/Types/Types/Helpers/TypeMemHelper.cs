using System.Reflection;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Helpers;

/// <summary>
/// This internal helper is used to centralize rented maps and list during type initialization.
/// This ensures that we can release these helper objects when the schema is created.
/// </summary>
internal static class TypeMemHelper
{
    private static Dictionary<string, ObjectFieldConfiguration>? s_objectFieldConfigurationMap;
    private static Dictionary<string, InterfaceFieldConfiguration>? s_interfaceFieldConfigurationMap;
    private static Dictionary<string, InputFieldConfiguration>? s_inputFieldConfigurationMap;
    private static Dictionary<string, InputField>? s_inputFieldMap;
    private static Dictionary<string, InputField>? s_inputFieldMapOrdinalIgnoreCase;
    private static Dictionary<string, DirectiveArgument>? s_directiveArgumentMap;
    private static Dictionary<string, DirectiveArgument>? s_directiveArgumentMapOrdinalIgnoreCase;
    private static Dictionary<ParameterInfo, string>? s_argumentNameMap;
    private static HashSet<MemberInfo>? s_memberSet;
    private static HashSet<string>? s_nameSet;
    private static HashSet<string>? s_nameSetOrdinalIgnoreCase;

    public static Dictionary<string, ObjectFieldConfiguration> RentObjectFieldConfigurationMap()
        => Interlocked.Exchange(ref s_objectFieldConfigurationMap, null) ??
            new Dictionary<string, ObjectFieldConfiguration>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, ObjectFieldConfiguration> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref s_objectFieldConfigurationMap, map, null);
    }

    public static Dictionary<string, InterfaceFieldConfiguration> RentInterfaceFieldConfigurationMap()
        => Interlocked.Exchange(ref s_interfaceFieldConfigurationMap, null) ??
            new Dictionary<string, InterfaceFieldConfiguration>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, InterfaceFieldConfiguration> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref s_interfaceFieldConfigurationMap, map, null);
    }

    public static Dictionary<string, InputFieldConfiguration> RentInputFieldConfigurationMap()
        => Interlocked.Exchange(ref s_inputFieldConfigurationMap, null) ??
            new Dictionary<string, InputFieldConfiguration>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, InputFieldConfiguration> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref s_inputFieldConfigurationMap, map, null);
    }

    public static Dictionary<string, InputField> RentInputFieldMap()
        => Interlocked.Exchange(ref s_inputFieldMap, null) ??
            new Dictionary<string, InputField>(StringComparer.Ordinal);

    public static Dictionary<string, InputField> RentInputFieldMapOrdinalIgnoreCase()
        => Interlocked.Exchange(ref s_inputFieldMapOrdinalIgnoreCase, null) ??
            new Dictionary<string, InputField>(StringComparer.OrdinalIgnoreCase);

    public static void Return(Dictionary<string, InputField> map)
    {
        map.Clear();

        if (map.Comparer.Equals(StringComparer.Ordinal))
        {
            Interlocked.CompareExchange(ref s_inputFieldMap, map, null);
        }

        if (map.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
        {
            Interlocked.CompareExchange(ref s_inputFieldMapOrdinalIgnoreCase, map, null);
        }
    }

    public static Dictionary<string, DirectiveArgument> RentDirectiveArgumentMap()
        => Interlocked.Exchange(ref s_directiveArgumentMap, null) ??
            new Dictionary<string, DirectiveArgument>(StringComparer.Ordinal);

    public static Dictionary<string, DirectiveArgument> RentDirectiveArgumentMapOrdinalIgnoreCase()
        => Interlocked.Exchange(ref s_directiveArgumentMapOrdinalIgnoreCase, null) ??
            new Dictionary<string, DirectiveArgument>(StringComparer.OrdinalIgnoreCase);

    public static void Return(Dictionary<string, DirectiveArgument> map)
    {
        map.Clear();

        if (map.Comparer.Equals(StringComparer.Ordinal))
        {
            Interlocked.CompareExchange(ref s_directiveArgumentMap, map, null);
        }

        if (map.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
        {
            Interlocked.CompareExchange(ref s_directiveArgumentMapOrdinalIgnoreCase, map, null);
        }
    }

    public static HashSet<MemberInfo> RentMemberSet()
        => Interlocked.Exchange(ref s_memberSet, null) ??
            [];

    public static void Return(HashSet<MemberInfo> set)
    {
        set.Clear();
        Interlocked.CompareExchange(ref s_memberSet, set, null);
    }

    public static HashSet<string> RentNameSet()
        => Interlocked.Exchange(ref s_nameSet, null) ??
            new HashSet<string>(StringComparer.Ordinal);

    public static HashSet<string> RentNameSetOrdinalIgnoreCase()
        => Interlocked.Exchange(ref s_nameSetOrdinalIgnoreCase, null) ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static void Return(HashSet<string> set)
    {
        set.Clear();

        if (set.Comparer.Equals(StringComparer.Ordinal))
        {
            Interlocked.CompareExchange(ref s_nameSet, set, null);
        }

        if (set.Comparer.Equals(StringComparer.OrdinalIgnoreCase))
        {
            Interlocked.CompareExchange(ref s_nameSetOrdinalIgnoreCase, set, null);
        }
    }

    public static Dictionary<ParameterInfo, string> RentArgumentNameMap()
        => Interlocked.Exchange(ref s_argumentNameMap, null) ?? [];

    public static void Return(Dictionary<ParameterInfo, string> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref s_argumentNameMap, map, null);
    }

    // We allow the helper to clear all pooled objects so that after
    // building the schema, we can release the memory.
    // There is a risk of extra allocation here if we build
    // multiple schemas at the same time.
    public static void Clear()
    {
        Interlocked.Exchange(ref s_objectFieldConfigurationMap, null);
        Interlocked.Exchange(ref s_interfaceFieldConfigurationMap, null);
        Interlocked.Exchange(ref s_inputFieldConfigurationMap, null);
        Interlocked.Exchange(ref s_inputFieldMap, null);
        Interlocked.Exchange(ref s_inputFieldMapOrdinalIgnoreCase, null);
        Interlocked.Exchange(ref s_directiveArgumentMap, null);
        Interlocked.Exchange(ref s_directiveArgumentMapOrdinalIgnoreCase, null);
        Interlocked.Exchange(ref s_argumentNameMap, null);
        Interlocked.Exchange(ref s_memberSet, null);
        Interlocked.Exchange(ref s_nameSet, null);
        Interlocked.Exchange(ref s_nameSetOrdinalIgnoreCase, null);
    }
}
