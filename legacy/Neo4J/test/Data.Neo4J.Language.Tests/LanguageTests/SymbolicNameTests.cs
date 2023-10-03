using HotChocolate.Data.Neo4J.Language;
using Shouldly;

namespace HotChocolate.Data.LanguageTests;

public class SymbolicNameTests
{
    public class ResolvedSymbolicNames
    {
        [Fact]
        public void EqualsShouldWorkSameValue()
        {
            var name1 = SymbolicName.Of("a");
            var name2 = name1;
            var name3 = SymbolicName.Of("a");

            name1.ShouldBeEquivalentTo(name2);
            name1.ShouldBeEquivalentTo(name3);
        }

        [Fact]
        public void EqualsShouldWorkDifferentValue()
        {
            var name1 = SymbolicName.Of("a");
            var name2 = SymbolicName.Of("b");

            name1.ShouldNotBe(name2);
        }

        [Fact]
        public void ShouldNotEqualUnresolved()
        {
            var name1 = SymbolicName.Of("a");

            name1.ShouldNotBe(SymbolicName.Unresolved());
        }

        [Fact]
        public void DifferentResolvedNamesShouldHaveDifferentHashCodes()
        {
            var name1 = SymbolicName.Of("a");
            var name2 = SymbolicName.Of("b");

            name1.GetHashCode().ShouldNotBe(name2.GetHashCode());
        }
    }
}

public class UnresolvedSymbolicNames
{
    [Fact]
    public void EqualsShouldWorkSameValue()
    {
        var name1 = SymbolicName.Unresolved();
        var name2 = name1;
        var name3 = SymbolicName.Unresolved();

        name1.ShouldBeEquivalentTo(name2);
        name1.ShouldNotBe(name3);
        name2.ShouldNotBe(name3);
    }

    [Fact]
    public void ShouldNotEqualResolved()
    {
        var name1 = SymbolicName.Unresolved();

        name1.ShouldNotBe(SymbolicName.Of("a"));
    }
}
