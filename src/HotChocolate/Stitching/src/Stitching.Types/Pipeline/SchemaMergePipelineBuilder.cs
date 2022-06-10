using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Pipeline;

public sealed class SchemaMergePipelineBuilder
{
    private readonly List<MergeSchemaMiddleware> _pipeline = new();

    public SchemaMergePipelineBuilder Use(MergeSchemaMiddleware middleware)
    {
        _pipeline.Add(middleware);
        return this;
    }

    public MergeSchema Compile()
    {
        MergeSchema next = _ => default;

        for (var i = _pipeline.Count - 1; i >= 0; i--)
        {
            next = _pipeline[i].Invoke(next);
        }

        return next;
    }
}
