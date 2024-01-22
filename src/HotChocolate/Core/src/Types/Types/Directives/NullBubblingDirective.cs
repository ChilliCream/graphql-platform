namespace HotChocolate.Types;

[DirectiveType(
    WellKnownDirectives.NullBubbling,
    DirectiveLocation.Query | 
    DirectiveLocation.Mutation | 
    DirectiveLocation.Subscription)]
public class NullBubblingDirective
{
    public NullBubblingDirective(bool enable = true)
    {
        Enable = enable;
    }

    [DefaultValue(true)]
    [GraphQLName(WellKnownDirectives.Enable)]
    public bool Enable { get; }
}