using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public class User
        : IUser
    {
        public User(
            string? name, 
            string? company, 
            System.DateTimeOffset createdAt, 
            IFollowerConnection followers)
        {
            Name = name;
            Company = company;
            CreatedAt = createdAt;
            Followers = followers;
        }

        public string? Name { get; }

        public string? Company { get; }

        public System.DateTimeOffset CreatedAt { get; }

        public IFollowerConnection Followers { get; }
    }
}
