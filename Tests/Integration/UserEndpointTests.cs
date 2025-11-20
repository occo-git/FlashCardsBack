using Application.DTO.Users;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.UseCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration
{
    public class UserEndpointTests : BaseIntegrationTest<IUserService>
    {
        public UserEndpointTests(IntegrationTestWebAppFactory factory)
            : base(factory)
        { }

        [Fact]
        public async Task Create_ShouldCreateNewUser()
        {
            Console.WriteLine("--> Create_ShouldCreateNewUser");
            // Arrange
            RegisterRequestDto request = new RegisterRequestDto
            {
                Username = "testuser",
                Email = "test@test.com",
                Password = "testpassword"
            };
            User newUser = UserMapper.ToDomain(request);

            // Act
            var createdUser = await Service.CreateNewAsync(newUser, CancellationToken.None);
            var addedUser = await Service.AddAsync(createdUser, CancellationToken.None);

            // Assert
            var count = DbContext.Users.Count();
            var user = DbContext.Users.FirstOrDefault(p => p.Id == addedUser.Id);

            Assert.Equal(1, count);
            Assert.NotNull(user);

            Console.WriteLine("<-- Create_ShouldCreateNewUser");
        }

        [Fact]
        public async Task Create_ShouldCreateNewUser1()
        {

            Console.WriteLine("--> Create_ShouldCreateNewUser1");
            // Arrange
            RegisterRequestDto request = new RegisterRequestDto
            {
                Username = "testuser",
                Email = "test@test.com",
                Password = "testpassword"
            };
            User newUser = UserMapper.ToDomain(request);

            // Act
            var createdUser = await Service.CreateNewAsync(newUser, CancellationToken.None);
            var addedUser = await Service.AddAsync(createdUser, CancellationToken.None);

            // Assert
            var count = DbContext.Users.Count();
            var user = DbContext.Users.FirstOrDefault(p => p.Id == addedUser.Id);

            Assert.Equal(1, count);
            Assert.NotNull(user);

            Console.WriteLine("<-- Create_ShouldCreateNewUser1");
        }
    }
}
