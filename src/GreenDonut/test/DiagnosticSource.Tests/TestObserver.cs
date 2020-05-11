using System;
using System.Diagnostics;

namespace GreenDonut
{
    internal class TestObserver
        : IObserver<DiagnosticListener>
    {
        private readonly TestListener _listener;

        public TestObserver(TestListener listener)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == "GreenDonut")
            {
                value.SubscribeWithAdapter(_listener);
            }
        }
    }
}
