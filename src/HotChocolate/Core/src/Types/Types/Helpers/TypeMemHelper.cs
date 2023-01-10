#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Helpers;

/// <summary>
/// This internal helper is used centralize rented maps and list during type initialization.
/// This ensures that we can release these helper objects when the schema is created.
/// </summary>
internal static class TypeMemHelper
{
    private static Dictionary<string, ObjectFieldDefinition>? _objectFieldDefinitionMap;
    private static Dictionary<string, InputFieldDefinition>? _inputFieldDefinitionMap;
    private static Dictionary<string, InputField>? _inputFieldMap;
    private static Dictionary<string, DirectiveArgument>? _directiveArgumentMap;
    private static HashSet<MemberInfo>? _memberSet;
    private static HashSet<string>? _nameSet;

    public static Dictionary<string, ObjectFieldDefinition> RentObjectFieldDefinitionMap()
        => Interlocked.Exchange(ref _objectFieldDefinitionMap, null) ??
            new Dictionary<string, ObjectFieldDefinition>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, ObjectFieldDefinition> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref _objectFieldDefinitionMap, map, null);
    }

    public static Dictionary<string, InputFieldDefinition> RentInputFieldDefinitionMap()
        => Interlocked.Exchange(ref _inputFieldDefinitionMap, null) ??
            new Dictionary<string, InputFieldDefinition>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, InputFieldDefinition> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref _inputFieldDefinitionMap, map, null);
    }

    public static Dictionary<string, InputField> RentInputFieldMap()
        => Interlocked.Exchange(ref _inputFieldMap, null) ??
            new Dictionary<string, InputField>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, InputField> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref _inputFieldMap, map, null);
    }

    public static Dictionary<string, DirectiveArgument> RentDirectiveArgumentMap()
        => Interlocked.Exchange(ref _directiveArgumentMap, null) ??
            new Dictionary<string, DirectiveArgument>(StringComparer.Ordinal);

    public static void Return(Dictionary<string, DirectiveArgument> map)
    {
        map.Clear();
        Interlocked.CompareExchange(ref _directiveArgumentMap, map, null);
    }

    public static HashSet<MemberInfo> RentMemberSet()
        => Interlocked.Exchange(ref _memberSet, null) ??
            new HashSet<MemberInfo>();

    public static void Return(HashSet<MemberInfo> set)
    {
        set.Clear();
        Interlocked.CompareExchange(ref _memberSet, set, null);
    }

    public static HashSet<string> RentNameSet()
        => Interlocked.Exchange(ref _nameSet, null) ??
            new HashSet<string>(StringComparer.Ordinal);

    public static void Return(HashSet<string> set)
    {
        set.Clear();
        Interlocked.CompareExchange(ref _nameSet, set, null);
    }

    // We allow the helper to clear all pooled objects so that after
    // building the schema we can release the memory.
    // There is a risk of extra allocation here if we build
    // multiple schemas at the same time.
    public static void Clear()
    {
        Interlocked.Exchange(ref _objectFieldDefinitionMap, null);
        Interlocked.Exchange(ref _inputFieldDefinitionMap, null);
        Interlocked.Exchange(ref _inputFieldMap, null);
        Interlocked.Exchange(ref _directiveArgumentMap, null);
        Interlocked.Exchange(ref _memberSet, null);
        Interlocked.Exchange(ref _nameSet, null);
    }
}
