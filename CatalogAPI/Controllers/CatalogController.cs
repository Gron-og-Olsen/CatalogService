using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;
using System.IO;

namespace CatalogAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly IMongoCollection<Product> _productCollection;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(IMongoCollection<Product> productCollection, ILogger<CatalogController> logger)
        {
            _productCollection = productCollection;
            _logger = logger;
        }

        [HttpPost]
        [Route("AddProduct")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product newProduct)
        {
            _logger.LogInformation("Method AddProduct called at {DT}", DateTime.UtcNow.ToLongTimeString());

            if (newProduct.Id == Guid.Empty)
            {
                newProduct.Id = Guid.NewGuid();
            }

            await _productCollection.InsertOneAsync(newProduct);

            _logger.LogInformation("New product with ID {ID} added at {DT}", newProduct.Id, DateTime.UtcNow.ToLongTimeString());

            return CreatedAtRoute("GetProductById", new { productId = newProduct.Id }, newProduct);
        }

        [HttpGet]
        [Route("GetAllProducts")]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            _logger.LogInformation("Method GetAllProducts called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var products = await _productCollection.Find(_ => true).ToListAsync();

            // Construct full image URL if exists
            foreach (var product in products)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imageBaseUrl = "http://localhost:5047/UploadedImages"; // Assuming port 5047
                    var imageFilePath = Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(product.ImageUrl));
                    product.ImageUrl = imageFilePath;
                }
            }

            return Ok(products);
        }

        [HttpGet("{productId}", Name = "GetProductById")]
        [Route("GetProductById/{productId}")]
        public async Task<ActionResult<Product>> GetProductById(Guid productId)
        {
            _logger.LogInformation("Method GetProductById called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            // Construct full image URL if exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var imageBaseUrl = "http://localhost:5047/UploadedImages"; // Assuming port 5047
                var imageFilePath = Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(product.ImageUrl));
                product.ImageUrl = imageFilePath;
            }

            return Ok(product);
        }

        [HttpPost("UploadImage/{productId}")]
        public async Task<ActionResult> UploadImage(Guid productId, IFormFile image)
        {
            _logger.LogInformation("Uploading image for product with ID {ID} at {DT}", productId, DateTime.UtcNow.ToLongTimeString());

            // Check if file is null or empty
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image provided.");
            }

            // Define the path where images will be stored (inside the Docker volume)
            var imageFolderPath = Path.Combine("/srv/images", productId.ToString()); // Docker volume mount
            Directory.CreateDirectory(imageFolderPath);  // Create directory if it doesn't exist

            var filePath = Path.Combine(imageFolderPath, image.FileName);

            // Save the image to the server
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Store the image path in the product record (you can adjust this part)
            var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            product.ImageUrl = Path.Combine(productId.ToString(), image.FileName);  // Store relative path
            await _productCollection.ReplaceOneAsync(p => p.Id == productId, product);

            _logger.LogInformation("Image uploaded successfully for product ID {ID} at {DT}", productId, DateTime.UtcNow.ToLongTimeString());

            return Ok(new { imageUrl = $"http://localhost:5047/UploadedImages/{productId}/{image.FileName}" });
        }
    }
}
