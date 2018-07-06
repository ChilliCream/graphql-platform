namespace HotChocolate.Types
{
    public class TypeSystemBase
        : INeedsInitialization
    {
        private bool _completed;

        private void RegisterDependencies(
            ITypeInitializationContext context)
        {
            if (!_completed)
            {
                OnRegisterDependencies(context);
            }
        }

        void INeedsInitialization.RegisterDependencies(
            ITypeInitializationContext context)
        {
            RegisterDependencies(context);
        }

        protected virtual void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
        }

        private void CompleteType(
            ITypeInitializationContext context)
        {
            if (!_completed)
            {
                OnCompleteType(context);
                _completed = true;
            }
        }

        void INeedsInitialization.CompleteType(
            ITypeInitializationContext context)
        {
            CompleteType(context);
        }

        protected virtual void OnCompleteType(
            ITypeInitializationContext context)
        {

        }
    }
}
