//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Xunit;

//namespace GreenDonut
//{
//    public class TaskCompletionBufferTests
//    {
//        [Fact(DisplayName = "Constructor: Should not throw any exception")]
//        public void ConstructorNoException()
//        {
//            // act
//            Action verify = () => new TaskCompletionBuffer<string, string>();

//            // assert
//            Assert.Null(Record.Exception(verify));
//        }

//        [Fact(DisplayName = "IsEmpty: Should return true")]
//        public void IsEmptyTrue()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();

//            // act
//            var result = buffer.IsEmpty;

//            // assert
//            Assert.True(result);
//        }

//        [Fact(DisplayName = "IsEmpty: Should return false")]
//        public void IsEmptyFalse()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();

//            buffer.TryAdd("Foo", new TaskCompletionSource<string>());

//            // act
//            var result = buffer.IsEmpty;

//            // assert
//            Assert.False(result);
//        }

//        [Fact(DisplayName = "GetAndClear: Should not throw any exception")]
//        public void GetAndClearNoException()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();

//            // act
//            Action verify = () => buffer.GetAndClear();

//            // assert
//            Assert.Null(Record.Exception(verify));
//        }

//        [Fact(DisplayName = "GetAndClear: Should clear empty cache")]
//        public void GetAndClearEmptyCache()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();

//            // act
//            IDictionary<string, TaskCompletionSource<string>> result =
//                buffer.GetAndClear();

//            // assert
//            Assert.True(buffer.IsEmpty);
//            Assert.Empty(result);
//        }

//        [Fact(DisplayName = "GetAndClear: Should remove all entries from the cache")]
//        public void GetAndClearAllEntries()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();

//            buffer.TryAdd("Foo", new TaskCompletionSource<string>());
//            buffer.TryAdd("Bar", new TaskCompletionSource<string>());

//            // act
//            IDictionary<string, TaskCompletionSource<string>> result =
//                buffer.GetAndClear();

//            // assert
//            Assert.True(buffer.IsEmpty);
//            Assert.Collection(result,
//                item => Assert.Equal("Foo", item.Key),
//                item => Assert.Equal("Bar", item.Key));
//        }

//        [Fact(DisplayName = "TryAdd: Should throw an argument null exception for key")]
//        public void TryAddKeyNull()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();
//            string key = null;
//            var value = new TaskCompletionSource<string>();

//            // act
//            Action verify = () => buffer.TryAdd(key, value);

//            // assert
//            Assert.Throws<ArgumentNullException>("key", verify);
//        }

//        [Fact(DisplayName = "TryAdd: Should throw an argument null exception for value")]
//        public void TryAddValueNull()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();
//            var key = "Foo";
//            TaskCompletionSource<string> value = null;

//            // act
//            Action verify = () => buffer.TryAdd(key, value);

//            // assert
//            Assert.Throws<ArgumentNullException>("value", verify);
//        }

//        [Fact(DisplayName = "TryAdd: Should not throw any exception")]
//        public void TryAddNoException()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();
//            var key = "Foo";
//            var value = new TaskCompletionSource<string>();

//            // act
//            Action verify = () => buffer.TryAdd(key, value);

//            // assert
//            Assert.Null(Record.Exception(verify));
//        }

//        [Fact(DisplayName = "TryAdd: Should result in a new cache entry")]
//        public void TryAddNewCacheEntry()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();
//            var key = "Foo";
//            var expected = new TaskCompletionSource<string>();

//            // act
//            var added = buffer.TryAdd(key, expected);

//            // assert
//            IDictionary<string, TaskCompletionSource<string>> result =
//                buffer.GetAndClear();

//            Assert.True(added);
//            Assert.Collection(result,
//                item => Assert.Equal("Foo", item.Key));
//        }

//        [Fact(DisplayName = "TryAdd: Should result in 'Bar'")]
//        public void TryAddTwice()
//        {
//            // arrange
//            var buffer = new TaskCompletionBuffer<string, string>();
//            var key = "Foo";
//            var expected = new TaskCompletionSource<string>();
//            var another = new TaskCompletionSource<string>();

//            // act
//            var addedFirst = buffer.TryAdd(key, expected);
//            var addedSecond = buffer.TryAdd(key, another);

//            // assert
//            IDictionary<string, TaskCompletionSource<string>> result =
//                buffer.GetAndClear();

//            Assert.True(addedFirst);
//            Assert.False(addedSecond);
//            Assert.Collection(result,
//                item =>
//                {
//                    Assert.Equal("Foo", item.Key);
//                    Assert.Equal(expected, item.Value);
//                });
//        }
//    }
//}
