using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class ReviewInput
        : IInput
    {
        public Optional<string> Commentary { get; set; }

        public Optional<int> Stars { get; set; }

        public IReadOnlyList<InputValue> GetChangedProperties()
        {
            var values = new List<InputValue>();

            if (Commentary.HasValue)
            {
                values.Add(new InputValue("commentary", "String", Commentary.Value));
            }

            if (Stars.HasValue)
            {
                values.Add(new InputValue("stars", "Int", Stars.Value));
            }

            return values;
        }
    }

    public class Foo
    {
        public void Bar()
        {
            var x = new ReviewInput
            {
                Commentary = "Bar"
            };
        }
    }
}
