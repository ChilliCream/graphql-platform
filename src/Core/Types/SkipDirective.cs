namespace HotChocolate.Types
{
    public class SkipDirective
        : Directive
    {
        internal SkipDirective()
            : base(CreateConfig())
        {
        }

        private static DirectiveConfig CreateConfig()
        {
            return new DirectiveConfig
            {
                Name = "skip",
                Description =
                    "Directs the executor to skip this field or " +
                    "fragment when the `if` argument is true.",
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
                        Description = "Skipped when true.",
                        Type = t => t.GetType<IInputType>(typeof(BooleanType))
                    })
                }
            };
        }
    }
}
