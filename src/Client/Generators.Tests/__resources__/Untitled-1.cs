using System.Runtime.Serialization.Formatters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Generated
{
    public interface IQueryResult<T> { }

    public interface IStarWarsClient
    {
        Task<IQueryResult<IHero>> GetHeroAsync();
    }

    public interface IHero
    {
        string Name { get; }
        IReadOnlyList<IFriend> Friends { get; }
    }

    public interface IFriend
    {
        string Name { get; }
    }

    public class Hero
        : IHero
    {
        public string Name { get; set; }
        public List<IFriend> Friends { get; } = new List<IFriend>();
        IReadOnlyList<IFriend> IHero.Friends => Friends;
    }

    public class Friend
        : IFriend
    {
        public string Name { get; set; }
    }
}
