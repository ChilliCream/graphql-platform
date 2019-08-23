using System;
using System.Collections.Generic;

namespace Generators
{
    public class Foo
    {
        public object Deserialize(IReadOnlyDictionary<string, object> data)
        {
            return new GetHero
            {
                Hero = DeserializeHero(GetObjectValue(data, "hero"))
            };
        }

        private static IHero DeserializeHero(IReadOnlyDictionary<string, object> data)
        {
            if (data is null)
            {
                return null;
            }

            string typeName = (string)data["__typename"];

            switch (typeName)
            {
                case "Droid":
                    return new Droid
                    {

                    };

                case "Human":
                    return new Human
                    {

                    };

                default:
                    throw new NotSupportedException();
            }
        }

        protected IReadOnlyDictionary<string, object> GetObjectValue(
            IReadOnlyDictionary<string, object> parent, string field)
        {
            if (parent.TryGetValue(field, out object o)
                && o is IReadOnlyDictionary<string, object> obj)
            {
                return obj;
            }
            return null;
        }
    }

    public interface IGetHero
    {
        IHero Hero { get; }
    }

    public class GetHero
        : IGetHero
    {
        public IHero Hero { get; set; }
    }

    public interface IDroid
        : IHero
    {
    }

    public interface IHero
        : IHasName
        , IHasFriends
    {
    }

    public interface IHasName
    {
        string Name { get; }
    }

    public interface IHasFriends
    {
        IFriend Friends { get; }
    }

    public class Droid
        : IDroid
    {
    }

    public interface IHuman
        : IHero
    {
    }

    public class Human
        : IHuman
    {
    }

    public interface IFriend
    {
        System.Collections.Generic.IReadOnlyList<IHasName> Nodes { get; }
    }

    public class Friend
        : IFriend
    {
        public System.Collections.Generic.List<IHasName> Nodes { get; set; }
        System.Collections.Generic.IReadOnlyList<IHasName> IFriend.Nodes => Nodes;
    }




}
