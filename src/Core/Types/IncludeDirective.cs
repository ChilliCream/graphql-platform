namespace HotChocolate.Types
{
    public class IncludeDirective
        : Directive
    {
        internal IncludeDirective()
            : base(CreateConfig())
        {
        }

        private static DirectiveConfig CreateConfig()
        {
            return new DirectiveConfig
            {
                Name = "include",
                Description =
                    "Directs the executor to include this field or fragment " +
                    "only when the `if` argument is true.",
                Locations = new[]
                {
                    DirectiveLocation.Field,
                    DirectiveLocation.FragmentSpread,
                    DirectiveLocation.InlineFragment
                },
                Arguments = new[]
                {
                    new InputField(new InputFieldConfig
                    {
                        Name = "if",
                        Description = "Included when true.",
                        Type = t => t.GetType<IInputType>(typeof(BooleanType))
                    })
                }
            };
        }
    }
}
