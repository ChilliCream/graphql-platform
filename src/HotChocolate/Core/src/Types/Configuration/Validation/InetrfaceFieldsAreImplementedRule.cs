using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal class InetrfaceFieldsAreImplementedRule
        : ISchemaValidationRule
    {
        public IEnumerable<ISchemaError> Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects,
            IReadOnlySchemaOptions options)
        {
            var errors = new List<ISchemaError>();

            if (options.StrictValidation)
            {
                foreach (ObjectType objectType in typeSystemObjects.OfType<ObjectType>())
                {
                    ValidateInterfaceImplementation(errors, objectType);
                }
            }

            return errors;
        }

        private static void ValidateInterfaceImplementation(
            ICollection<ISchemaError> errors,
            ObjectType objectType)
        {
            if (objectType.Interfaces.Count > 0)
            {
                foreach (IGrouping<NameString, InterfaceField> fieldGroup in
                    objectType.Interfaces.Values
                        .SelectMany(t => t.Fields)
                        .GroupBy(t => t.Name))
                {
                    ValidateField(errors, objectType, fieldGroup);
                }
            }
        }

        private static void ValidateField(
            ICollection<ISchemaError> errors,
           ObjectType objectType,
           IGrouping<NameString, InterfaceField> interfaceField)
        {
            InterfaceField first = interfaceField.First();
            if (ValidateInterfaceFieldGroup(errors, objectType, first, interfaceField))
            {
                ValidateObjectField(errors, objectType, first);
            }
        }

        private static bool ValidateInterfaceFieldGroup(
            ICollection<ISchemaError> errors,
            ObjectType objectType,
            InterfaceField first,
            IGrouping<NameString, InterfaceField> interfaceField)
        {
            if (interfaceField.Count() == 1)
            {
                return true;
            }

            foreach (InterfaceField field in interfaceField)
            {
                if (!field.Type.IsEqualTo(first.Type))
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.InterfaceImplRule_FieldTypeInvalid,
                            first.Name,
                            first.DeclaringType.Name,
                            field.DeclaringType.Name,
                            objectType.Name))
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                    return false;
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.InterfaceImplRule_ArgumentsDontMatch,
                            first.Name,
                            first.DeclaringType.Name,
                            field.DeclaringType.Name,
                            objectType.Name))
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                    return false;
                }
            }

            return true;
        }

        private static void ValidateObjectField(
            ICollection<ISchemaError> errors,
            ObjectType objectType,
            InterfaceField first)
        {
            if (objectType.Fields.TryGetField(first.Name, out ObjectField field))
            {
                if (!field.Type.IsEqualTo(first.Type))
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.InterfaceImplRule_ReturnTypeInvalid,
                            first.Name,
                            objectType.Name))
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(string.Format(
                            CultureInfo.InvariantCulture,
                            TypeResources.InterfaceImplRule_ArgumentsNotImpl,
                            objectType.Name,
                            first.Name,
                            first.DeclaringType.Name))
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                }
            }
            else
            {
                errors.Add(SchemaErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        TypeResources.InterfaceImplRule_FieldNotImpl,
                        objectType.Name,
                        first.Name,
                        first.DeclaringType.Name))
                    .SetCode(ErrorCodes.Schema.MissingType)
                    .SetTypeSystemObject(objectType)
                    .AddSyntaxNode(objectType.SyntaxNode)
                    .AddSyntaxNode(first.SyntaxNode)
                    .Build());
            }
        }

        private static bool ArgumentsAreEqual(
            FieldCollection<Argument> x,
            FieldCollection<Argument> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (Argument xfield in x)
            {
                if (!y.TryGetField(xfield.Name, out Argument yfield)
                    || !xfield.Type.IsEqualTo(yfield.Type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
