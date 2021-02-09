using System;
using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.Properties;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public abstract class AbstractTypeBuilder: ITypeBuilder
    {
        protected List<PropertyBuilder> Properties {get;} = new();
        protected NameString? Name {get; private set; }
        protected List<string> Implements { get; } = new();

        public abstract void Build(CodeWriter writer);

        protected void SetName(NameString name)
        {
            Name = name;
        }

        public void AddProperty(PropertyBuilder property)
        {
            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Properties.Add(property);
        }

        public void AddImplements(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    Resources.ClassBuilder_AddImplements_TypeNameCannotBeNull,
                    nameof(value));
            }

            Implements.Add(value);
        }
    }
}
