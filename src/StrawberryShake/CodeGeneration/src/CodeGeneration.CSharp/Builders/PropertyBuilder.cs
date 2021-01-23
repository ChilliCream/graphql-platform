using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class PropertyBuilder : ICodeBuilder
    {
        private AccessModifier _accessModifier;
        private bool _isAutoProperty = true;
        private bool _isReadOnly = true;
        private string? _lambdaResolver;
        private TypeReferenceBuilder? _type;
        private string? _name;
        private string? _value;
        private bool _isStatic;

        public static PropertyBuilder New() => new();

        public PropertyBuilder SetAccessModifier(AccessModifier value)
        {
            _accessModifier = value;
            return this;
        }

        public PropertyBuilder SetStatic()
        {
            _isStatic = true;
            return this;
        }

        public PropertyBuilder AsLambda(string resolveCode)
        {
            _lambdaResolver = resolveCode;
            return this;
        }

        public PropertyBuilder SetType(string value)
        {
            _type = TypeReferenceBuilder.New().SetName(value);
            return this;
        }

        public PropertyBuilder SetType(TypeReferenceBuilder value)
        {
            _type = value;
            return this;
        }

        public PropertyBuilder SetName(string value)
        {
            _name = value;
            return this;
        }

        public PropertyBuilder SetValue(string value)
        {
            _value = value;
            return this;
        }

        public PropertyBuilder MakeSettable()
        {
            _isReadOnly = false;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_type is null)
            {
                throw new ArgumentNullException(nameof(_type));
            }

            string modifier = _accessModifier.ToString().ToLowerInvariant();

            writer.WriteIndent();
            writer.Write(modifier);
            writer.WriteSpace();
            if (_isStatic)
            {
                writer.Write("static");
                writer.WriteSpace();
            }
            _type.Build(writer);
            writer.Write(_name);

            if (_lambdaResolver is not null)
            {
                writer.Write(" => ");
                writer.Write(_lambdaResolver);
                writer.Write(";");
                writer.WriteLine();
                return;
            }

            writer.Write(" {");
            writer.Write(" get;");
            if (!_isReadOnly)
            {
                writer.Write(" set;");
            }

            writer.Write(" }");

            if (_value is not null)
            {
                writer.Write(" = ");
                writer.Write(_value);
                writer.Write(";");
            }

            writer.WriteLine();
        }


    }
}
