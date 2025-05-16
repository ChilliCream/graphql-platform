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
    public ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> ScalarDirectives { get; set; } =
        ImmutableDictionary<string, IReadOnlyList<DirectiveNode>>.Empty;
    public ImmutableDictionary<Type, RuntimeTypeBinding> RuntimeTypeBindings { get; set; } =
        ImmutableDictionary<Type, RuntimeTypeBinding>.Empty;
    public ImmutableDictionary<Type, RuntimeTypeNameBinding> RuntimeTypeNameBindings { get; set; } =
        ImmutableDictionary<Type, RuntimeTypeNameBinding>.Empty;
}
