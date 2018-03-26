using System;

namespace Prometheus.Language
{
    internal class NextTokenThunk
        : Thunk<Token>
    {
        private Token _previous;
        private readonly Func<Token, Token> _thaw;

        public NextTokenThunk(Func<Token, Token> thaw)
        {
            if (thaw == null)
            {
                throw new ArgumentNullException(nameof(thaw));
            }
            _thaw = thaw;
        }

        public void SetPrevious(Token previous)
        {
            if (previous == null)
            {
                _previous = previous;
            }
        }

        protected override void Thaw()
        {
            Thaw(_thaw(_previous));
        }
    }
}