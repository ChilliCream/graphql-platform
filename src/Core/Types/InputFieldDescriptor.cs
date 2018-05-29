using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldDescriptor
        : IInputFieldDescriptor
    {
        #region IInputFieldDescriptor

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(IValueNode defaultValue)
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(object defaultValue)
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.Description(string description)
        {
            throw new System.NotImplementedException();
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type<TInputType>()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
