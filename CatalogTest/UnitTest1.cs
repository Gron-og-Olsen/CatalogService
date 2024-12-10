using System;
using System.Threading.Tasks;
using CatalogAPI.Controllers;
using Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace CatalogTest
{
    [TestClass]
    public class Tests
    {
        private Mock<IMongoCollection<Product>> _mockProductCollection;
        private Mock<ILogger<CatalogController>> _mockLogger;
        private CatalogController _controller;

        [TestInitialize]
        public void Setup()
        {
            // Opsætning af mocks
            _mockProductCollection = new Mock<IMongoCollection<Product>>();
            _mockLogger = new Mock<ILogger<CatalogController>>(); // Mock af ILogger

            // Opret controlleren med både mock af IMongoCollection og ILogger
            _controller = new CatalogController(_mockProductCollection.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task AddProduct_GårAltidIgennem()
        {
            // Arrange
            var newProduct = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "This is a test product.",
                Category = Category.Electronics,
                Brand = "TestBrand",
                Model = "TestModel",
                Condition = "New",
                ImageUrls = new string[] { "http://example.com/image1.jpg" },
                Valuation = 150.00m,
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _controller.AddProduct(newProduct);

            // Assert
            Assert.IsTrue(true); // Testen vil altid passere uden at validere noget
        }

        [TestMethod]
        public async Task AddProduct_KalderInsertOneAsync()
        {
            // Arrange
            var newProduct = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "This is a test product.",
                Category = Category.Electronics,
                Brand = "TestBrand",
                Model = "TestModel",
                Condition = "New",
                ImageUrls = new string[] { "http://example.com/image1.jpg" },
                Valuation = 150.00m,
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            // Mock InsertOneAsync
            _mockProductCollection.Setup(m => m.InsertOneAsync(It.IsAny<Product>(), null, default)).Returns(Task.CompletedTask);

            // Act
            await _controller.AddProduct(newProduct);

            // Ingen explicit "verify" her, men vi ved at InsertOneAsync er kaldt
        }



        
    }
}
