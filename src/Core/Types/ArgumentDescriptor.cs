using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescriptor
        : IArgumentDescriptor
    {
        public ArgumentDescriptor(string name)
        {

        }

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public IValueNode DefaultValue { get; protected set; }
        public object NativeDefaultValue { get; protected set; }

        public InputField CreateArgument()
        {

        }

        #region IArgumentDescriptor

        IArgumentDescriptor IArgumentDescriptor.Description(string description)
        {
            throw new System.NotImplementedException();
        }

        IArgumentDescriptor IArgumentDescriptor.Type<IInputType>()
        {
            throw new System.NotImplementedException();
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(IValueNode valueNode)
        {
            throw new System.NotImplementedException();
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(object defaultValue)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
