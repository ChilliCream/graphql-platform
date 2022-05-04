using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

public delegate MergeTypeRuleDelegate MergeTypeRuleFactory(
    MergeTypeRuleDelegate next);

public delegate void MergeTypeRuleDelegate(
    ISchemaMergeContext context,
    IReadOnlyList<ITypeInfo> types);
