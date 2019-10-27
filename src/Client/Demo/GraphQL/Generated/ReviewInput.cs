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
        private string? _value_commentary;
        private bool _changed_commentary;

        private int? _value_stars;
        private bool _changed_stars;

        public string Commentary
        {
            get
            {
                if (_value_commentary is null)
                {
                    throw new InvalidOperationException(
                        "The field has to be set before it can be used.");
                }
                return _value_commentary;
            }
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _value_commentary = value;
                _changed_commentary = true;
            }
        }

        public int Stars
        {
            get
            {
                if (!_value_stars.HasValue)
                {
                    throw new InvalidOperationException(
                        "The field has to be set before it can be used.");
                }
                return _value_stars.Value;
            }
            set
            {
                _value_stars = value;
                _changed_commentary = true;
            }
        }

        public IReadOnlyList<InputValue> GetChangedProperties()
        {
            var values = new List<InputValue>();

            if (_changed_commentary)
            {
                values.Add(new InputValue("commentary", "String", _value_commentary));
            }

            if (_changed_stars)
            {
                values.Add(new InputValue("stars", "Int", _value_stars));
            }

            return values;
        }
    }
}
