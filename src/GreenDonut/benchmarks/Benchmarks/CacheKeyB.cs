using System;

namespace GreenDonut.Benchmarks
{
    public struct CacheKeyB<TKey>
        : IEquatable<CacheKeyB<TKey>>
    {
        private CacheKeyType _keyType;
        private string _stringKey;
        private TKey _originKey;
        private int _integerKey;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return ReferenceEquals(null, this);
            }

            return Equals((CacheKeyB<TKey>)obj);
        }

        public bool Equals(CacheKeyB<TKey> other)
        {
            if (_keyType == other._keyType)
            {
                if (_keyType == CacheKeyType.IntegerKey)
                {
                    return _integerKey.Equals(other._integerKey);
                }

                if (_keyType == CacheKeyType.StringKey)
                {
                    return _stringKey.Equals(other._stringKey);
                }

                return _originKey.Equals(other._originKey);
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            switch (_keyType)
            {
                case CacheKeyType.OriginKey:
                    return _originKey.GetHashCode();
                case CacheKeyType.StringKey:
                    return _stringKey.GetHashCode();
                case CacheKeyType.IntegerKey:
                    return _integerKey.GetHashCode();
                default:
                    return base.GetHashCode();
            }
        }

        public static bool operator ==(
            CacheKeyB<TKey> left,
            CacheKeyB<TKey> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CacheKeyB<TKey> left,
            CacheKeyB<TKey> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator CacheKeyB<TKey>(TKey key)
        {
            return new CacheKeyB<TKey>
            {
                _keyType = CacheKeyType.OriginKey,
                _originKey = key
            };
        }

        public static implicit operator CacheKeyB<TKey>(string key)
        {
            var cacheKey = new CacheKeyB<TKey>
            {
                _keyType = CacheKeyType.StringKey,
                _stringKey = key
            };

            return cacheKey;
        }

        public static implicit operator CacheKeyB<TKey>(int key)
        {
            var cacheKey = new CacheKeyB<TKey>
            {
                _keyType = CacheKeyType.IntegerKey,
                _integerKey = key
            };

            return cacheKey;
        }

        private enum CacheKeyType
            : byte
        {
            OriginKey = 0,
            StringKey = 1,
            IntegerKey = 2
        }
    }
}
