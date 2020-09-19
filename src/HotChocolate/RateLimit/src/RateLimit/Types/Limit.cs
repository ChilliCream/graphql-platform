using System;

namespace HotChocolate.RateLimit
{
    public readonly struct Limit
    {
        public static readonly Limit Empty = new Limit(DateTime.MinValue, int.MinValue);

        public static Limit One => new Limit(DateTime.UtcNow, 1);

        private Limit(DateTime timestamp, int requests)
        {
            Timestamp = timestamp;
            Requests = requests;
        }

        public static Limit Create(DateTime timestamp, int requests)
        {
            if (requests < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requests), requests, "Requests cannot be less than 1.");
            }

            return new Limit(timestamp, requests);
        }

        public DateTime Timestamp { get; }
        public int Requests { get; }

        public bool IsValid(LimitPolicy policy)
        {
            return !IsEmpty() && Requests <= policy.Limit;
        }

        public bool IsExpired(LimitPolicy policy)
        {
            return IsEmpty() || Timestamp + policy.Period < DateTime.UtcNow;
        }

        public Limit Increment()
        {
            return new Limit(Timestamp, Requests + 1);
        }

        public bool IsEmpty()
        {
            return Timestamp == DateTime.MinValue && Requests == int.MinValue;
        }
    }
}
