using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DirectiveArgumentDefinition
        : ArgumentDefinition
    {
        public PropertyInfo Property { get; set; }

        protected override void OnValidate(ICollection<IError> errors)
        {
            base.OnValidate(errors);

            if (Ignore && Property == null)
            {
                // TODO : resources
                errors.Add(ErrorBuilder.New()
                    .SetMessage(
                        "In order to ignore a directive argument " +
                        "you have to specify a property.")
                    .Build());
            }

            if (Directives.Count > 0)
            {
                // TODO : resources
                errors.Add(ErrorBuilder.New()
                    .SetMessage(
                        "A directive argument mustn't be annotated with " +
                        "directives.")
                    .Build());
            }
        }
    }
}
