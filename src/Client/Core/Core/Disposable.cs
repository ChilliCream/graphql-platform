using System;

namespace HotChocolate.Client.Core
{
    internal class Disposable : IDisposable
    {
        private Action action;

        private Disposable(Action action)
        {
            this.action = action;
        }

        public static IDisposable Create(Action action) => new Disposable(action);

        public void Dispose()
        {
            action();
        }
    }
}
