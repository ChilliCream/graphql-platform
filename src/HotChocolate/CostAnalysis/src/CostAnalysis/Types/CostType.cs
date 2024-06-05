using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-__cost</summary>
internal sealed class CostType : ObjectType<Cost>;
