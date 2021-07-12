using System;

namespace HotChocolate.Analyzers.Configuration.EFCoreSample
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}
