using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Zeus.Resolvers
{
    public class MemberResolverTests
    {
        [Fact]
        public async Task ResolveFromAsyncMethod()
        {
            // arrange
            string description = Guid.NewGuid().ToString("N");
            CompanyGraphMock company = new CompanyGraphMock
            {
                Name = Guid.NewGuid().ToString("N"),
                Description = () => description,
                AsyncAddress = Guid.NewGuid().ToString("N")
            };

            MemberInfo memberInfo = company.GetType().GetMember("GetAddressAsync").First();

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<object>()).Returns(company);

            // act
            MemberResolver resolver = new MemberResolver(memberInfo);
            object result = await resolver.ResolveAsync(context.Object, CancellationToken.None);

            // assert
            Assert.Equal(company.AsyncAddress, result);
        }

        [Fact]
        public async Task ResolveFromSyncMethod()
        {
            // arrange
            string description = Guid.NewGuid().ToString("N");
            CompanyGraphMock company = new CompanyGraphMock
            {
                Name = Guid.NewGuid().ToString("N"),
                Description = () => description,
                AsyncAddress = Guid.NewGuid().ToString("N")
            };

            MemberInfo memberInfo = company.GetType().GetMember("GetAddressSync").First();

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<object>()).Returns(company);            

            // act
            MemberResolver resolver = new MemberResolver(memberInfo);
            object result = await resolver.ResolveAsync(context.Object, CancellationToken.None);

            // assert
            Assert.Equal(company.AsyncAddress, result);
        }

        [Fact]
        public async Task ResolveFromFuncMethod()
        {
            // arrange
            string description = Guid.NewGuid().ToString("N");
            CompanyGraphMock company = new CompanyGraphMock
            {
                Name = Guid.NewGuid().ToString("N"),
                Description = () => description,
                AsyncAddress = Guid.NewGuid().ToString("N")
            };

            MemberInfo memberInfo = company.GetType().GetMember("GetAddressFunc").First();

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<object>()).Returns(company);

            // act
            MemberResolver resolver = new MemberResolver(memberInfo);
            object result = await resolver.ResolveAsync(context.Object, CancellationToken.None);

            // assert
            Assert.IsType<Func<string>>(result);
            Assert.Equal(company.AsyncAddress, ((Func<string>)result)());
        }

        [Fact]
        public async Task ResolveFromProperty()
        {
            // arrange
            string description = Guid.NewGuid().ToString("N");
            CompanyGraphMock company = new CompanyGraphMock
            {
                Name = Guid.NewGuid().ToString("N"),
                Description = () => description,
                AsyncAddress = Guid.NewGuid().ToString("N")
            };

            MemberInfo memberInfo = company.GetType().GetMember("Name").First();

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<object>()).Returns(company);

            // act
            MemberResolver resolver = new MemberResolver(memberInfo);
            object result = await resolver.ResolveAsync(context.Object, CancellationToken.None);

            // assert
            Assert.Equal(company.Name, result);
        }

        [Fact]
        public async Task ResolveFromFuncProperty()
        {
            // arrange
            string description = Guid.NewGuid().ToString("N");
            CompanyGraphMock company = new CompanyGraphMock
            {
                Name = Guid.NewGuid().ToString("N"),
                Description = () => description,
                AsyncAddress = Guid.NewGuid().ToString("N")
            };

            MemberInfo memberInfo = company.GetType().GetMember("Description").First();

            Mock<IResolverContext> context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Parent<object>()).Returns(company);

            // act
            MemberResolver resolver = new MemberResolver(memberInfo);
            object result = await resolver.ResolveAsync(context.Object, CancellationToken.None);

            // assert
            Assert.IsType<Func<string>>(result);
            Assert.Equal(description, ((Func<string>)result)());
        }

    }

    public class CompanyGraphMock
    {
        public string Name { get; set; }

        public Func<string> Description { get; set; }

        public string AsyncAddress { get; set; }

        public Task<string> GetAddressAsync()
        {
            return Task.FromResult(AsyncAddress);
        }

        public string GetAddressSync()
        {
            return AsyncAddress;
        }

        public Func<string> GetAddressFunc()
        {
            return () => AsyncAddress;
        }
    }
}