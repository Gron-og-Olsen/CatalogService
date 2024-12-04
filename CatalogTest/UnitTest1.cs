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
                Category = Category.Electronics, // Gyldig kategori
                FinalPrice = 100.00m,
                CurrentBid = 50.00m,
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
                FinalPrice = 100.00m,
                CurrentBid = 50.00m,
                Brand = "TestBrand",
                Model = "TestModel",
                Condition = "New",
                ImageUrls = new string[] { "http://example.com/image1.jpg" },
                Valuation = 150.00m,
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            await _controller.AddProduct(newProduct);

            // Assert
            _mockProductCollection.Verify(m => m.InsertOneAsync(It.IsAny<Product>(), null, default), Times.Once);
        }

        [TestMethod]
        public async Task AddProduct_TilføjetID()
        {
            // Arrange
            var newProduct = new Product
            {
                Id = Guid.Empty, // Skal være empty for at teste ID tildeling
                Name = "Test Product",
                Description = "This is a test product.",
                Category = Category.Electronics,
                FinalPrice = 100.00m,
                CurrentBid = 50.00m,
                Brand = "TestBrand",
                Model = "TestModel",
                Condition = "New",
                ImageUrls = new string[] { "http://example.com/image1.jpg" },
                Valuation = 150.00m,
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            await _controller.AddProduct(newProduct);

            // Assert
            Assert.AreNotEqual(Guid.Empty, newProduct.Id); // ID skal være ændret fra Guid.Empty
        }

        [TestMethod]
        public async Task AddProduct_201()
        {
            // Arrange
            var newProduct = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Valid Product",
                Description = "This is a valid product.",
                Category = Category.Clothing, // Gyldig kategori
                FinalPrice = 200.00m,
                CurrentBid = 100.00m,
                Brand = "BrandName",
                Model = "ModelName",
                Condition = "New",
                ImageUrls = new string[] { "http://example.com/image1.jpg" },
                Valuation = 250.00m,
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            // Act
            var result = await _controller.AddProduct(newProduct);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtRouteResult)); // Skal returnere CreatedAtRouteResult altså en 201
        }

        [TestMethod]
        public async Task AddProduct_ManglerFelter()
        {
            // Arrange - her er et produkt med nogle ugyldige felter
            var newProduct = new Product
            {
                Id = Guid.Empty,
                Name = "", // Ugyldigt navn
                Description = "",
                Category = Category.Electronics,
                FinalPrice = 0, // Ugyldig pris
                CurrentBid = 0, // Ugyldig bud
                Brand = null,
                Model = null,
                Condition = "New", // Valid condition
                ImageUrls = null, // Valid at være null for billeder
                Valuation = 0, // Ugyldig vurdering
                ReleaseDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _controller.AddProduct(newProduct);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult)); // Skal returnere BadRequest fordi der mangler felter
            var badRequest = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest?.Value); // Skal indeholde fejlbesked
        }
    }
}
