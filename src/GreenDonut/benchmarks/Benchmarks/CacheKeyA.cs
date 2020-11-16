using System;
using System.Linq;

namespace GreenDonut.Benchmarks
{
    public struct CacheKeyA<TKey>
        : IEquatable<CacheKeyA<TKey>>
    {
        private readonly CacheKeyType _keyType;
        private readonly object _objectKey;
        private readonly TKey _originKey;
        private readonly byte[] _primitiveKey;

        public CacheKeyA(object key)
        {
            _keyType = CacheKeyType.ObjectKey;
            _objectKey = key;
            _originKey = default;
            _primitiveKey = null;
        }

        public CacheKeyA(TKey key)
        {
            _keyType = CacheKeyType.OriginKey;
            _objectKey = null;
            _originKey = key;
            _primitiveKey = null;
        }

        public CacheKeyA(byte[] key)
        {
            _keyType = CacheKeyType.PrimitiveKey;
            _objectKey = null;
            _originKey = default;
            _primitiveKey = key;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return ReferenceEquals(null, this);
            }

            return Equals((CacheKeyA<TKey>)obj);
        }

        public bool Equals(CacheKeyA<TKey> other)
        {
            if (_keyType == other._keyType)
            {
                if (_keyType == CacheKeyType.PrimitiveKey)
                {
                    return _primitiveKey.SequenceEqual(other._primitiveKey);
                }

                if (_keyType == CacheKeyType.ObjectKey)
                {
                    return _objectKey.Equals(other._objectKey);
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
                case CacheKeyType.ObjectKey:
                    return _objectKey.GetHashCode();
                case CacheKeyType.PrimitiveKey:
                    return _primitiveKey.GetHashCode();
                default:
                    return base.GetHashCode();
            }
        }

        public static bool operator ==(
            CacheKeyA<TKey> left,
            CacheKeyA<TKey> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            CacheKeyA<TKey> left,
            CacheKeyA<TKey> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator CacheKeyA<TKey>(TKey key)
        {
            return new CacheKeyA<TKey>(key);
        }

        public static implicit operator CacheKeyA<TKey>(int key)
        {
            return new CacheKeyA<TKey>(BitConverter.GetBytes(key));
        }

        private enum CacheKeyType
            : byte
        {
            OriginKey = 0,
            ObjectKey = 1,
            PrimitiveKey = 2
        }
    }
}
