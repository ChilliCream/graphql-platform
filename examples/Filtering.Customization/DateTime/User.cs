using System;

namespace Filtering.Customization
{
    public class User
    {
        public User(string name, string lastName, DateTime signedUp)
        {
            Name = name;
            LastName = lastName;
            SignedUp = signedUp;
        }

        public string Name { get; set; }
        public string LastName { get; set; }
        public DateTime SignedUp { get; set; }
    }
}