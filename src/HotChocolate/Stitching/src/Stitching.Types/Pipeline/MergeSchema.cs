using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types;

public delegate ValueTask MergeSchema(ISchemaMergeContext context);
