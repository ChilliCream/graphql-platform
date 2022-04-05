using System.Collections.Generic;

namespace HotChocolate.Stitching.SchemaBuilding;

public delegate MergeDirectiveRuleDelegate MergeDirectiveRuleFactory(
    MergeDirectiveRuleDelegate next);

public delegate void MergeDirectiveRuleDelegate(
    ISchemaMergeContext context,
    IReadOnlyList<IDirectiveTypeInfo> types);
