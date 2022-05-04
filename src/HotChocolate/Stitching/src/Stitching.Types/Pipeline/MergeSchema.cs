using System.Threading.Tasks;

namespace HotChocolate.Stitching.Types.Pipeline;

public delegate ValueTask MergeSchema(ISchemaMergeContext context);
