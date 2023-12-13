using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Pipeline;

internal sealed class ResolveMiddleware : IMergeMiddleware
{
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        
        
       

        if (!context.Log.HasErrors)
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private sealed class Collector : SchemaVisitor<CollectorContext>
    {
        public Collector(CompositionContext context)
        {
            Context = context;
        }

        private CompositionContext Context { get; }

        public override void VisitOutputFields(FieldCollection<OutputField> fields, CollectorContext context)
        {
            context.Clear();
            
            foreach (var field in fields)
            {
                if (!ResolveDirective.ExistsIn(field, Context.FusionTypes))
                {
                    continue;
                }
                
                context.PrepareObjectStates(fields);
                context.PrepareFieldState(field.Arguments);

                foreach (var declare in DeclareDirective.GetAllFrom(field, Context.FusionTypes))
                {
                    
                }
            }
            
            base.VisitOutputFields(fields, context);
        }
    }

    private sealed class CollectorContext
    {
        public Dictionary<string, ResolverState> ObjectStates { get; } = new();
        
        public Dictionary<string, ResolverState> FieldStates { get; } = new();


        public void PrepareObjectStates(FieldCollection<OutputField> fields)
        {
            ObjectStates.Clear();
            
            foreach (var field in fields)
            {
                ObjectStates.Add(field.Name, new ResolverState(field));
            }            
        }

        public void PrepareFieldState(FieldCollection<InputField> arguments)
        {
            FieldStates.Clear();
            
            foreach (var state in ObjectStates)
            {
                FieldStates.Add(state.Key, state.Value);
            }

            foreach (var argument in arguments)
            {
                FieldStates.Add(argument.Name, new ResolverState(argument));
            }
        }

        public void Clear()
        {
            ObjectStates.Clear();
            FieldStates.Clear();
        }
    }

    private readonly struct ResolverState(IField field)
    {
        public IField Field { get; } = field;
    }
}