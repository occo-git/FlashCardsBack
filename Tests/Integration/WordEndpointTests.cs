using Application.UseCases;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration
{
    public class WordEndpointTests : BaseIntegrationTest<IWordService>
    {
        public WordEndpointTests(IntegrationTestWebAppFactory factory)
            : base(factory)
        {
        }

        //[Fact]
        //public async Task Create_ShouldCreateWord()
        //{
        //    // Arrange
        //    var command = new CreateProduct.Command
        //    {
        //        Name = "AMD Ryzen 7 7700X",
        //        Category = "CPU",
        //        Price = 223.99m
        //    };

        //    // Act
        //    var word = await Service..Send(command);

        //    // Assert
        //    var product = DbContext
        //        .Products
        //        .FirstOrDefault(p => p.Id == productId);

        //    Assert.NotNull(product);
        //}
    }
}
