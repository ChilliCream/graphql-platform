using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language.Properties;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Security.Cryptography;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestReader
    {
        private const byte _o = (byte)'o';
        private const byte _n = (byte)'n';
        private const byte _q = (byte)'q';
        private const byte _v = (byte)'v';
        private const byte _e = (byte)'e';

        private static readonly byte[] _operationName = new[]
        {
            (byte)'o',
            (byte)'p',
            (byte)'e',
            (byte)'r',
            (byte)'a',
            (byte)'t',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'N',
            (byte)'a',
            (byte)'m',
            (byte)'e'
        };

        private static readonly byte[] _queryName = new[]
        {
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
            (byte)'d',
            (byte)'Q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static readonly byte[] _query = new[]
        {
            (byte)'q',
            (byte)'u',
            (byte)'e',
            (byte)'r',
            (byte)'y'
        };

        private static readonly byte[] _variables = new[]
        {
            (byte)'v',
            (byte)'a',
            (byte)'r',
            (byte)'i',
            (byte)'a',
            (byte)'b',
            (byte)'l',
            (byte)'e',
            (byte)'s'
        };

        private static readonly byte[] _extension = new[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'s',
            (byte)'i',
            (byte)'o',
            (byte)'n'
        };

        private readonly IDocumentHashProvider _hashProvider;
        private readonly IDocumentCache _cache;
        private Utf8GraphQLReader _reader;
        private ParserOptions _options;


        public Utf8GraphQLRequestReader(
            ReadOnlySpan<byte> requestData,
            ParserOptions options,
            IDocumentCache cache,
            IDocumentHashProvider hashProvider)
        {
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _cache = cache
                ?? throw new ArgumentNullException(nameof(cache));
            _hashProvider = hashProvider
                ?? throw new ArgumentNullException(nameof(hashProvider));

            _reader = new Utf8GraphQLReader(requestData);
        }

        public IReadOnlyList<GraphQLRequest> Parse()
        {
            _reader.MoveNext();

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                GraphQLRequest singleRequest = ParseRequest();
                return new[] { singleRequest };
            }

            if (_reader.Kind == TokenKind.LeftBracket)
            {
                return ParseBatchRequest();
            }

            // TODO : resources
            throw new SyntaxException(_reader, "Unexpected request structure.");
        }

        private IReadOnlyList<GraphQLRequest> ParseBatchRequest()
        {
            throw new NotImplementedException();
        }

        private GraphQLRequest ParseRequest()
        {
            var request = new Request();

            _reader.Expect(TokenKind.LeftBrace);


            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseProperty(ref request);
                _reader.MoveNext();
            }

            if (request.Query.Length == 0)
            {
                throw new SyntaxException(_reader, "RESOURCES");
            }

            if (request.NamedQuery is null)
            {
                request.NamedQuery =
                    _hashProvider.ComputeHash(request.Query);
            }

            if (!_cache.TryGet(request.NamedQuery, out DocumentNode document))
            {
                document = ParseQuery(in request);
            }

            return new GraphQLRequest
            (
                request.OperationName,
                request.NamedQuery,
                document,
                request.Variables,
                request.Extensions
            );
        }

        private void ParseProperty(ref Request request)
        {
            ReadOnlySpan<byte> fieldName = _reader.Expect(TokenKind.String);
            _reader.Expect(TokenKind.Colon);

            if (_reader.Kind == TokenKind.String)
            {
                switch (fieldName[0])
                {
                    case _o:
                        if (fieldName.SequenceEqual(_operationName))
                        {
                            request.OperationName = _reader.GetString();
                            return;
                        }
                        break;

                    case _n:
                        if (fieldName.SequenceEqual(_queryName))
                        {
                            request.NamedQuery = _reader.GetString();
                            return;
                        }
                        break;

                    case _q:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Query = _reader.Value;
                            return;
                        }
                        break;
                }
                throw new SyntaxException(_reader, "RESOURCES");
            }

            if (_reader.Kind == TokenKind.LeftBrace)
            {
                switch (fieldName[0])
                {
                    case _v:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Variables = ParseObject();
                            return;
                        }
                        break;

                    case _e:
                        if (fieldName.SequenceEqual(_query))
                        {
                            request.Extensions = ParseObject();
                            return;
                        }
                        break;
                }
            }

            throw new SyntaxException(_reader, "RESOURCES");
        }

        private object ParseValue()
        {
            switch (_reader.Kind)
            {
                case TokenKind.LeftBracket:
                    return ParseList();

                case TokenKind.LeftBrace:
                    return ParseObject();

                case TokenKind.Name:
                case TokenKind.String:
                case TokenKind.Integer:
                case TokenKind.Float:
                    return ParseScalarValue();

                default:
                    throw new SyntaxException(_reader, "RESOURCES");
            }
        }

        private IReadOnlyDictionary<string, object> ParseObject()
        {
            if (_reader.Kind != TokenKind.LeftBrace)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBrace,
                        TokenVisualizer.Visualize(in _reader)));
            }

            _reader.Expect(TokenKind.LeftBrace);

            var obj = new Dictionary<string, object>();

            while (_reader.Kind != TokenKind.RightBrace)
            {
                ParseObjectField(obj);
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBrace);

            return obj;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseObjectField(IDictionary<string, object> obj)
        {
            if (_reader.Kind != TokenKind.String)
            {
                throw new SyntaxException(_reader, "RESOURCES");
            }

            string name = _reader.GetString();
            _reader.Expect(TokenKind.Colon);
            object value = ParseValue();
            obj.Add(name, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IReadOnlyList<object> ParseList()
        {
            if (_reader.Kind != TokenKind.LeftBracket)
            {
                throw new SyntaxException(_reader,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        LangResources.ParseMany_InvalidOpenToken,
                        TokenKind.LeftBracket,
                        TokenVisualizer.Visualize(in _reader)));
            }

            var list = new List<object>();

            // skip opening token
            _reader.MoveNext();

            while (_reader.Kind != TokenKind.RightBracket)
            {
                list.Add(ParseValue());
            }

            // skip closing token
            _reader.Expect(TokenKind.RightBracket);

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object ParseScalarValue()
        {
            switch (_reader.Kind)
            {
                case TokenKind.String:
                    return _reader.GetString();

                case TokenKind.Integer:
                    return long.Parse(_reader.GetScalarValue());

                case TokenKind.Float:
                    return decimal.Parse(_reader.GetScalarValue());

                case TokenKind.Name:
                    if (_reader.Value.SequenceEqual(GraphQLKeywords.True))
                    {
                        return true;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.False))
                    {
                        return false;
                    }

                    if (_reader.Value.SequenceEqual(GraphQLKeywords.Null))
                    {
                        return null;
                    }
                    break;
            }

            throw new SyntaxException(_reader, "RESOURCES");
        }

        private DocumentNode ParseQuery(in Request request)
        {
            int length = checked(request.Query.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[] unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                Utf8Helper.Unescape(
                    request.Query,
                    ref unescapedSpan,
                    false);

                return Utf8GraphQLParser.Parse(unescapedSpan, _options);
            }
            finally
            {
                if (unescapedArray != null)
                {
                    unescapedSpan.Clear();
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        private ref struct Request
        {
            public string OperationName { get; set; }

            public string NamedQuery { get; set; }

            public ReadOnlySpan<byte> Query { get; set; }

            public IReadOnlyDictionary<string, object> Variables { get; set; }

            public IReadOnlyDictionary<string, object> Extensions { get; set; }
        }
    }

    public class GraphQLRequest
    {
        public GraphQLRequest(
            string operationName,
            string namedQuery,
            DocumentNode query,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> extensions)
        {
            if (string.IsNullOrEmpty(namedQuery))
            {
                throw new ArgumentException("message", nameof(namedQuery));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }
        }

        public string OperationName { get; set; }

        public string NamedQuery { get; set; }

        public DocumentNode Query { get; set; }

        public object Variables { get; set; }
    }

    public interface IDocumentCache
    {
        bool TryGet(string key, out DocumentNode document);
    }

    public interface IDocumentHashProvider
    {
        string ComputeHash(ReadOnlySpan<byte> document);
    }

    public class Sha1DocumentHashProvider
        : IDocumentHashProvider
    {
        private ThreadLocal<SHA1> _sha =
            new ThreadLocal<SHA1>(() => SHA1.Create());
        public string ComputeHash(ReadOnlySpan<byte> document)
        {
            HashAlgorithm s;
            s.Try
        }
    }

    /// <summary>
    /// Defines a configuration for a <see cref="IxxHash"/> implementation.
    /// </summary>
    public class xxHashConfig
    {
        /// <summary>
        /// Gets the desired hash size, in bits.
        /// </summary>
        /// <value>
        /// The desired hash size, in bits.
        /// </value>
        public int HashSizeInBits { get; set; } = 32;

        /// <summary>
        /// Gets the seed.
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public UInt64 Seed { get; set; } = 0UL;

        /// <summary>
        /// Makes a deep clone of current instance.
        /// </summary>
        /// <returns>A deep clone of the current instance.</returns>
        public xxHashConfig Clone() =>
            new xxHashConfig()
            {
                HashSizeInBits = HashSizeInBits,
                Seed = Seed
            };
    }

    /// <summary>
    /// Implements xxHash as specified at https://github.com/Cyan4973/xxHash/blob/dev/xxhash.c and
    ///   https://github.com/Cyan4973/xxHash.
    /// </summary>
    internal ref struct xxHash_Implementation
    {
        /// <summary>
        /// Gets the seed.
        /// </summary>
        /// <value>
        /// The seed.
        /// </value>
        public UInt64 Seed { get; set; } = 0UL;

        public override int HashSizeInBits => _config.HashSizeInBits;



        private readonly IxxHashConfig _config;


        private static readonly IReadOnlyList<UInt32> _primes32 =
            new[] {
                2654435761U,
                2246822519U,
                3266489917U,
                 668265263U,
                 374761393U
            };

        private static readonly IReadOnlyList<UInt64> _primes64 =
            new[] {
                11400714785074694791UL,
                14029467366897019727UL,
                 1609587929392839161UL,
                 9650029242287828579UL,
                 2870177450012600261UL
            };


        /// <summary>
        /// Initializes a new instance of the <see cref="xxHash_Implementation" /> class.
        /// </summary>
        /// <param name="config">The configuration to use for this instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="config"/>.<see cref="IxxHashConfig.HashSizeInBits"/>;<paramref name="config"/>.<see cref="IxxHashConfig.HashSizeInBits"/> must be contained within xxHash.ValidHashSizes</exception>
        public xxHash_Implementation(IxxHashConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _config = config.Clone();


            if (!_validHashSizes.Contains(_config.HashSizeInBits))
                throw new ArgumentOutOfRangeException($"{nameof(config)}.{nameof(config.HashSizeInBits)}", _config.HashSizeInBits, $"{nameof(config)}.{nameof(config.HashSizeInBits)} must be contained within xxHash.ValidHashSizes");
        }



        /// <exception cref="System.InvalidOperationException">HashSize set to an invalid value.</exception>
        /// <inheritdoc />
        public void ComputeHash(ReadOnlySpan<byte> data, Span<byte> hash)
        {
            var h = Seed + _primes64[4];

            ulong dataCount = 0;
            byte[] remainder = null;

            var initValues = new[]
            {
                Seed + _primes64[0] + _primes64[1],
                Seed + _primes64[1],
                Seed,
                Seed - _primes64[0]
            };


            ForEachGroup(32,
                (dataGroup, position, length) =>
                {
                    for (var x = position; x < position + length; x += 32)
                    {
                        for (var y = 0; y < 4; ++y)
                        {
                            initValues[y] += BitConverter.ToUInt64(dataGroup, x + (y * 8)) * _primes64[1];
                            initValues[y] = RotateLeft(initValues[y], 31);
                            initValues[y] *= _primes64[0];
                        }
                    }

                    dataCount += (ulong)length;
                },

                (remainderData, position, length) =>
                {
                    remainder = new byte[length];
                    Array.Copy(remainderData, position, remainder, 0, length);

                    dataCount += (ulong)length;
                },
                cancellationToken);


            PostProcess(ref h, initValues, dataCount, remainder);

            hash = BitConverter.GetBytes(h);


            return hash;
        }

        public void ForEachGroup(
            int groupSize,
            Action<byte[], int, int> action,
            Action<byte[], int, int> remainderAction)
        {
            if (groupSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupSize), $"{nameof(groupSize)} must be greater than 0.");

            if (action == null)
                throw new ArgumentNullException(nameof(action));




            var remainderLength = _data.Length % groupSize;

            if (_data.Length - remainderLength > 0)
                action(_data, 0, _data.Length - remainderLength);


            if (remainderAction != null && remainderLength > 0)
            {
                remainderAction(_data, _data.Length - remainderLength, remainderLength);
            }
        }

        private void ComputeGroup(ReadOnlySpan<byte> data, ulong[] initValues)
        {
            for (int x = 0; x < data.Length; x += 32)
            {
                for (var y = 0; y < 4; ++y)
                {
                    initValues[y] += BitConverter.ToUInt64(data, x + (y * 8)) * _primes64[1];
                    initValues[y] = RotateLeft(initValues[y], 31);
                    initValues[y] *= _primes64[0];
                }
            }

            dataCount += (ulong)length;
        }

        private void ComputeGroupRemainder()
        {

        }

        private void PostProcess(ref UInt32 h, UInt32[] initValues, ulong dataCount, byte[] remainder)
        {
            if (dataCount >= 16)
            {
                h = RotateLeft(initValues[0], 1) +
                    RotateLeft(initValues[1], 7) +
                    RotateLeft(initValues[2], 12) +
                    RotateLeft(initValues[3], 18);
            }


            h += (UInt32)dataCount;

            if (remainder != null)
            {
                // In 4-byte chunks, process all process all full chunks
                for (int x = 0; x < remainder.Length / 4; ++x)
                {
                    h += BitConverter.ToUInt32(remainder, x * 4) * _primes32[2];
                    h = RotateLeft(h, 17) * _primes32[3];
                }


                // Process last 4 bytes in 1-byte chunks (only runs if data.Length % 4 != 0)
                for (int x = remainder.Length - (remainder.Length % 4); x < remainder.Length; ++x)
                {
                    h += (UInt32)remainder[x] * _primes32[4];
                    h = RotateLeft(h, 11) * _primes32[0];
                }
            }

            h ^= h >> 15;
            h *= _primes32[1];
            h ^= h >> 13;
            h *= _primes32[2];
            h ^= h >> 16;
        }

        private void PostProcess(ref UInt64 h, UInt64[] initValues, ulong dataCount, byte[] remainder)
        {
            if (dataCount >= 32)
            {
                h = RotateLeft(initValues[0], 1) +
                    RotateLeft(initValues[1], 7) +
                    RotateLeft(initValues[2], 12) +
                    RotateLeft(initValues[3], 18);


                for (var x = 0; x < initValues.Length; ++x)
                {
                    initValues[x] *= _primes64[1];
                    initValues[x] = RotateLeft(initValues[x], 31);
                    initValues[x] *= _primes64[0];

                    h ^= initValues[x];
                    h = (h * _primes64[0]) + _primes64[3];
                }
            }

            h += (UInt64)dataCount;

            if (remainder != null)
            {
                // In 8-byte chunks, process all full chunks
                for (int x = 0; x < remainder.Length / 8; ++x)
                {
                    h ^= RotateLeft(BitConverter.ToUInt64(remainder, x * 8) * _primes64[1], 31) * _primes64[0];
                    h = (RotateLeft(h, 27) * _primes64[0]) + _primes64[3];
                }


                // Process a 4-byte chunk if it exists
                if ((remainder.Length % 8) >= 4)
                {
                    h ^= ((UInt64)BitConverter.ToUInt32(remainder, remainder.Length - (remainder.Length % 8))) * _primes64[0];
                    h = (RotateLeft(h, 23) * _primes64[1]) + _primes64[2];
                }

                // Process last 4 bytes in 1-byte chunks (only runs if data.Length % 4 != 0)
                for (int x = remainder.Length - (remainder.Length % 4); x < remainder.Length; ++x)
                {
                    h ^= (UInt64)remainder[x] * _primes64[4];
                    h = RotateLeft(h, 11) * _primes64[0];
                }
            }


            h ^= h >> 33;
            h *= _primes64[1];
            h ^= h >> 29;
            h *= _primes64[2];
            h ^= h >> 32;
        }

        private static UInt32 RotateLeft(UInt32 operand, int shiftCount)
        {
            shiftCount &= 0x1f;

            return
                (operand << shiftCount) |
                (operand >> (32 - shiftCount));
        }

        private static UInt64 RotateLeft(UInt64 operand, int shiftCount)
        {
            shiftCount &= 0x3f;

            return
                (operand << shiftCount) |
                (operand >> (64 - shiftCount));
        }
    }
}
