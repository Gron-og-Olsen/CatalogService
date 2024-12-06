using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;
using System.IO;
using Microsoft.AspNetCore.Authorization;

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

        [Authorize]
        [HttpPost]
        [Route("AddProduct")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product newProduct)
        {
            _logger.LogInformation("Method AddProduct called at {DT}", DateTime.UtcNow.ToLongTimeString());

            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized("Thou shall not pass, mortal!");
            }
            
            if (newProduct.Id == Guid.Empty)
            {
                newProduct.Id = Guid.NewGuid();
            }

            await _productCollection.InsertOneAsync(newProduct);

            _logger.LogInformation("New product with ID {ID} added at {DT}", newProduct.Id, DateTime.UtcNow.ToLongTimeString());

            return CreatedAtRoute("GetProductById", new { productId = newProduct.Id }, newProduct);
        }

        [Authorize]
        [HttpGet]
        [Route("GetAllProducts")]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            _logger.LogInformation("Method GetAllProducts called at {DT}", DateTime.UtcNow.ToLongTimeString());

            var products = await _productCollection.Find(_ => true).ToListAsync();

            // Construct full image URLs if any exist
            foreach (var product in products)
            {
                if (product.ImageUrls != null && product.ImageUrls.Length > 0)
                {
                    var imageBaseUrl = "http://catalogservice:5001/UploadedImages"; // Assuming port 5047
                    product.ImageUrls = product.ImageUrls.Select(imageUrl => 
                        Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(imageUrl))).ToArray();
                }
            }

            return Ok(products);
        }
        [Authorize]
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

            // Construct full image URLs if any exist
            if (product.ImageUrls != null && product.ImageUrls.Length > 0)
            {
                var imageBaseUrl = "http://catalogservice:5001/UploadedImages"; // Assuming port 5047
                product.ImageUrls = product.ImageUrls.Select(imageUrl =>
                    Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(imageUrl))).ToArray();
            }

            return Ok(product);
        }
        [Authorize]
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

            // Retrieve the product
            var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            // If the ImageUrls array is null, initialize it
            if (product.ImageUrls == null)
            {
                product.ImageUrls = new string[] { };
            }

            // Add the new image URL to the ImageUrls array
            var newImageUrl = Path.Combine(productId.ToString(), image.FileName);
            product.ImageUrls = product.ImageUrls.Append(newImageUrl).ToArray();

            // Update the product in the database
            await _productCollection.ReplaceOneAsync(p => p.Id == productId, product);

            _logger.LogInformation("Image uploaded successfully for product ID {ID} at {DT}", productId, DateTime.UtcNow.ToLongTimeString());

            return Ok(new { imageUrls = product.ImageUrls.Select(imageUrl => $"http://catalogservice:5001/UploadedImages/{productId}/{Path.GetFileName(imageUrl)}") });
        }
    }
}
