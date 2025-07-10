#nullable enable

using System.Collections.Immutable;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed class TypeSystemFeature
{
    public List<Func<IDescriptorContext, TypeDiscoveryHandler>> TypeDiscoveryHandlers { get; } = [];
    public Dictionary<string, ISchemaDirective> SchemaDirectives { get; } = [];
    public Dictionary<string, Type> ScalarNameOverrides { get; } = [];
    public List<Action<ISchemaTypeDescriptor>> SchemaTypeOptions { get; } = [];
    public List<SchemaDocumentInfo> SchemaDocuments { get; } = [];
#if NET10_0_OR_GREATER
    public ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> ScalarDirectives { get; set; } =
        [];
    public ImmutableDictionary<Type, RuntimeTypeBinding> RuntimeTypeBindings { get; set; } =
        [];
    public ImmutableDictionary<Type, RuntimeTypeNameBinding> RuntimeTypeNameBindings { get; set; } =
        [];
    public ImmutableDictionary<string, RuntimeTypeNameBinding> NameRuntimeTypeBinding { get; set; } =
        [];
#else
    public ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> ScalarDirectives { get; set; } =
        ImmutableDictionary<string, IReadOnlyList<DirectiveNode>>.Empty;
    public ImmutableDictionary<Type, RuntimeTypeBinding> RuntimeTypeBindings { get; set; } =
        ImmutableDictionary<Type, RuntimeTypeBinding>.Empty;
    public ImmutableDictionary<Type, RuntimeTypeNameBinding> RuntimeTypeNameBindings { get; set; } =
        ImmutableDictionary<Type, RuntimeTypeNameBinding>.Empty;
    public ImmutableDictionary<string, RuntimeTypeNameBinding> NameRuntimeTypeBinding { get; set; } =
        ImmutableDictionary<string, RuntimeTypeNameBinding>.Empty;
#endif
}
