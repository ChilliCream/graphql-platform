using System;
using System.Collections.Generic;
using HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;
using HotChocolate.Stitching.Types.Pipeline.ApplyMissingBindings;
using HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;
using HotChocolate.Stitching.Types.Pipeline.PrepareDocuments;

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

    public static MergeSchema CreateDefaultPipeline()
        => new SchemaMergePipelineBuilder()
            .Use(next =>
            {
                var middleware = new PrepareDocumentsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyExtensionsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyRenamingMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyMissingBindingsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new SquashDocumentsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Use(next =>
            {
                var middleware = new ApplyExtensionsMiddleware(next);
                return context => middleware.InvokeAsync(context);
            })
            .Compile();
}
