using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using HotChocolate.Language;
using Snapshooter.Xunit;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OperationGeneratorTests
    {
       
        [Fact]
        public void Operation_With_MultipleOperations()
        {
            AssertResult(
                @"query TestOperation($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"query TestOperation2($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"query TestOperation3($single: Bar!, $list: [Bar!]!, $nestedList: [[Bar!]]) {
                    foo(single: $single, list: $list, nestedList:$nestedList)
                }",
                @"type Query {
                    foo(single: Bar!, list: [Bar!]!, nestedList: [[Bar]]): String
                }

                input Bar {
                    str: String
                    strNonNullable: String!
                    nested: Bar
                    nestedList: [Bar!]!
                    nestedMatrix: [[Bar]]
                }",
                "extend schema @key(fields: \"id\")");
        }
    }
}
