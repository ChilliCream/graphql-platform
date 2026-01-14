using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace StrawberryShake.Razor;

public class UseFragment<TFragment> : ComponentBase where TFragment : class
{
    [Parameter] public TFragment? Data { get; set; }

    [Parameter] public RenderFragment<TFragment> ChildContent { get; set; } = null!;

    [Parameter] public RenderFragment? LoadingContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Data is not null)
        {
            builder.AddContent(0, ChildContent, Data);
        }
        else
        {
            builder.AddContent(0, LoadingContent);
        }

        base.BuildRenderTree(builder);
    }
}
