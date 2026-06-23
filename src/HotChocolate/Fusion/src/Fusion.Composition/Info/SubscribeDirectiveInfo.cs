using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Info;

internal readonly record struct SubscribeDirectiveInfo(
    ImmutableArray<string> Topics,
    string? Broker,
    SelectionSetNode Message);
