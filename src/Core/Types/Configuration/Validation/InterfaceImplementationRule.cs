using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Validation
{
    internal class InterfaceImplementationRule
        : ISchemaValidationRule
    {
        public IEnumerable<ISchemaError> Validate(
            IReadOnlyList<ITypeSystemObject> typeSystemObjects)
        {
            var errors = new List<ISchemaError>();
            foreach (ObjectType objectType in typeSystemObjects.OfType<ObjectType>())
            {
                ValidateInterfaceImplementation(errors, objectType);
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
                    // TODO : RESOURCES
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                           "The return type of the interface field " +
                            $"{first.Name} from interface " +
                            $"{first.DeclaringType.Name} and " +
                            $"{field.DeclaringType.Name} do not match " +
                            $"and are implemented by object type {objectType.Name}.")
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                    return false;
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    // TODO : RESOURCES
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            $"The arguments of the interface field {first.Name} " +
                            $"from interface {first.DeclaringType.Name} and " +
                            $"{field.DeclaringType.Name} do not match " +
                            $"and are implemented by object type {objectType.Name}.")
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
                    // TODO : RESOURCES
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            "The return type of the interface field " +
                            $"{first.Name} does not match the field declared " +
                            $"by object type {objectType.Name}.")
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                }

                if (!ArgumentsAreEqual(field.Arguments, first.Arguments))
                {
                    // TODO : RESOURCES
                    errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            $"Object type {objectType.Name} does not implement " +
                            $"all arguments of field {first.Name} " +
                            $"from interface {first.DeclaringType.Name}.")
                        .SetTypeSystemObject(objectType)
                        .AddSyntaxNode(objectType.SyntaxNode)
                        .AddSyntaxNode(first.SyntaxNode)
                        .Build());
                }
            }
            else
            {
                // TODO : RESOURCES
                errors.Add(SchemaErrorBuilder.New()
                    .SetMessage(
                        $"Object type {objectType.Name} does not implement the " +
                        $"field {first.Name} " +
                        $"from interface {first.DeclaringType.Name}.")
                    .SetCode(TypeErrorCodes.MissingType)
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
