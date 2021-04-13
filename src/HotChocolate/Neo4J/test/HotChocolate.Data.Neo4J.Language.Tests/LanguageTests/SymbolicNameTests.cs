using Shouldly;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class SymbolicNameTests
    {
        public class ResolvedSymbolicNames
        {
            [Fact]
            public void EqualsShouldWorkSameValue()
            {
                SymbolicName name1 = SymbolicName.Of("a");
                SymbolicName name2 = name1;
                SymbolicName name3 = SymbolicName.Of("a");

                name1.ShouldBeEquivalentTo(name2);
                name1.ShouldBeEquivalentTo(name3);
            }

            [Fact]
            public void EqualsShouldWorkDifferentValue()
            {
                SymbolicName name1 = SymbolicName.Of("a");
                SymbolicName name2 = SymbolicName.Of("b");

                name1.ShouldNotBe(name2);
            }

            [Fact]
            public void ShouldNotEqualUnresolved()
            {
                SymbolicName name1 = SymbolicName.Of("a");

                name1.ShouldNotBe(SymbolicName.Unresolved());
            }

            [Fact]
            public void SameResolvedNamesShouldHaveSameHashCodes()
            {
                SymbolicName name1 = SymbolicName.Of("a");
                SymbolicName name2 = SymbolicName.Of("a");

                name1.GetHashCode().ShouldBeEquivalentTo(name2.GetHashCode());
            }

            [Fact]
            public void DifferentResolvedNamesShouldHaveDifferentHashCodes()
            {
                SymbolicName name1 = SymbolicName.Of("a");
                SymbolicName name2 = SymbolicName.Of("b");

                name1.GetHashCode().ShouldNotBe(name2.GetHashCode());
            }
        }
    }

    public class UnresolvedSymbolicNames
    {
        [Fact]
        public void EqualsShouldWorkSameValue()
        {
            SymbolicName name1 = SymbolicName.Unresolved();
            SymbolicName name2 = name1;
            SymbolicName name3 = SymbolicName.Unresolved();

            name1.ShouldBeEquivalentTo(name2);
            name1.ShouldNotBe(name3);
            name2.ShouldNotBe(name3);
        }

        [Fact]
        public void ShouldNotEqualResolved()
        {
            SymbolicName name1 = SymbolicName.Unresolved();

            name1.ShouldNotBe(SymbolicName.Of("a"));
        }

        // [Fact]
        // public void DifferentUnresolvedNamesShouldHaveDifferentHashCodes()
        // {
        //     SymbolicName name1 = SymbolicName.Unresolved();
        //     SymbolicName name2 = SymbolicName.Unresolved();
        //
        //     name1.GetHashCode().ShouldNotBe(name2.GetHashCode());
        // }
    }
}
