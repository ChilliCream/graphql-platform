using System.Collections.Generic;

namespace HotChocolate.Types
{
    public class TypeSystemBase
        : INeedsInitialization
    {
        private readonly List<INeedsInitialization> _dependencies =
            new List<INeedsInitialization>();

        private TypeStatus _status = TypeStatus.Created;

        private void RegisterDependencies(
            ITypeInitializationContext context)
        {
            if (_status == TypeStatus.Created)
            {
                _status = TypeStatus.Registering;

                foreach (INeedsInitialization dependency in _dependencies)
                {
                    dependency.RegisterDependencies(context);
                }

                OnRegisterDependencies(context);

                _status = TypeStatus.Registered;
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
            if (_status == TypeStatus.Registered)
            {
                _status = TypeStatus.Completing;

                foreach (INeedsInitialization dependency in _dependencies)
                {
                    dependency.CompleteType(context);
                }

                OnCompleteType(context);

                _status = TypeStatus.Completed;
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

        internal void RegisterForInitialization(
            INeedsInitialization dependency)
        {
            if (_status == TypeStatus.Created)
            {
                _dependencies.Add(dependency);
            }
        }

        private enum TypeStatus
        {
            Created,
            Registering,
            Registered,
            Completing,
            Completed
        }
    }
}
