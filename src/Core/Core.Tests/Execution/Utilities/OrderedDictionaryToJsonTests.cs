using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChilliCream.Testing;
using Newtonsoft.Json;
using Xunit;

namespace HotChocolate.Execution
{
    public class OrderedDictionaryToJsonTests
    {
        [Fact]
        static public void BasicObjectToJsonTest()
        {
            // arrange 
            OrderedDictionary queryResult = OrderedDictionaryTestData.BasicQueryResult;

            // act
            string jsonResult;
            using (var memStream = new MemoryStream())
            {
                ObjectToJsonBytes.WriteObjectToStream(queryResult, memStream);
                
                jsonResult = Encoding.UTF8.GetString(memStream.ToArray());
            }

            // assert
            jsonResult.Snapshot();
        }

        [Fact]
        static public void ComplexObjectToJsonTest1()
        {
            // arrange 
            OrderedDictionary queryResult = OrderedDictionaryTestData.ComplexQueryResult1;

            // act
            string jsonResult;
            using (var memStream = new MemoryStream())
            {
                ObjectToJsonBytes.WriteObjectToStream(queryResult, memStream);

                jsonResult = Encoding.UTF8.GetString(memStream.ToArray());
            }

            // assert
            jsonResult.Snapshot();
        }

        [Fact]
        static public void ComplexObjectToJsonTest2()
        {
            // arrange 
            OrderedDictionary queryResult = OrderedDictionaryTestData.ComplexQueryResult2;

            // act
            string jsonResult;
            using (var memStream = new MemoryStream())
            {
                ObjectToJsonBytes.WriteObjectToStream(queryResult, memStream);

                jsonResult = Encoding.UTF8.GetString(memStream.ToArray());
            }

            // assert
            jsonResult.Snapshot();
        }
    }
}
