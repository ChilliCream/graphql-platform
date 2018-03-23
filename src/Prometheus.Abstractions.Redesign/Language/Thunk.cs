using System;

namespace Prometheus.Language
{
    public class Thunk<T>
    {
        private bool _thawed;
        private Func<T> _thaw;
        private T _value;

        public Thunk()
        {
        }

        public Thunk(T value)
        {
            _thawed = true;
            _value = value;
        }

        public Thunk(Func<T> thaw)
        {
            if (thaw == null)
            {
                throw new ArgumentNullException(nameof(thaw));
            }

            _thaw = thaw;
        }

        public void Thaw(T value)
        {
            _thawed = true;
            _value = value;
        }

        public T Value
        {
            get
            {
                if (!_thawed)
                {
                    if (_thaw == null)
                    {
                        throw new InvalidOperationException();
                    }
                    _value = _thaw();
                    _thawed = true;
                }
                return _value;
            }
        }
    }
}