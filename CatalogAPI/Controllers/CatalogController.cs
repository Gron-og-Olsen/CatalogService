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
            _logger = logger;  // Logger er nu en del af constructoren
        }

        [HttpPost]
        [Route("AddProduct")]
        public async Task<ActionResult<Product>> AddProduct([FromBody] Product newProduct)
        {
            try
            {
                // Inputvalidering: Sørger for at produktet har nødvendige felter (f.eks. navn og vurdering)
                if (newProduct == null || string.IsNullOrEmpty(newProduct.Name) || newProduct.Valuation <= 0)
                {
                    _logger.LogWarning("Invalid product data received: {Product}", newProduct);
                    return BadRequest("Produktet er ikke gyldigt. Sørg for at have et navn og en positiv pris.");
                }

                // Hvis produktet ikke har et ID, opret et nyt
                if (newProduct.Id == Guid.Empty)
                {
                    newProduct.Id = Guid.NewGuid();
                }

                // Indsæt produktet i databasen
                await _productCollection.InsertOneAsync(newProduct);

                // Returner en CreatedAtRouteResult med det oprettede produkt
                _logger.LogInformation("Product added with ID: {ProductId}", newProduct.Id);
                return CreatedAtRoute("GetProductById", new { productId = newProduct.Id }, newProduct);
            }
            catch (Exception ex)
            {
                // Hvis noget går galt, f.eks. i forbindelse med databasen, returner en serverfejl
                _logger.LogError(ex, "Error while adding product");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetAllProducts")]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            _logger.LogInformation("Fetching all products at {DT}", DateTime.UtcNow);

            var products = await _productCollection.Find(_ => true).ToListAsync();

            // Construct full image URLs if any exist
            foreach (var product in products)
            {
                if (product.ImageUrls != null && product.ImageUrls.Length > 0)
                {
                    var imageBaseUrl = "http://localhost:5047/UploadedImages"; // Assuming port 5047
                    product.ImageUrls = product.ImageUrls.Select(imageUrl =>
                        Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(imageUrl))).ToArray();
                }
            }

            return Ok(products);
        }

        [HttpGet("{productId}", Name = "GetProductById")]
        [Route("GetProductById/{productId}")]
        public async Task<ActionResult<Product>> GetProductById(Guid productId)
        {
            _logger.LogInformation("Fetching product with ID {ProductId} at {DT}", productId, DateTime.UtcNow);

            var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", productId);
                return NotFound(new { message = $"Product with ID {productId} not found." });
            }

            // Construct full image URLs if any exist
            if (product.ImageUrls != null && product.ImageUrls.Length > 0)
            {
                var imageBaseUrl = "http://localhost:5047/UploadedImages"; // Assuming port 5047
                product.ImageUrls = product.ImageUrls.Select(imageUrl =>
                    Path.Combine(imageBaseUrl, product.Id.ToString(), Path.GetFileName(imageUrl))).ToArray();
            }

            return Ok(product);
        }

        [HttpPost("UploadImage/{productId}")]
        public async Task<ActionResult> UploadImage(Guid productId, IFormFile image)
        {
            _logger.LogInformation("Uploading image for product with ID {ProductId} at {DT}", productId, DateTime.UtcNow);

            // Check if file is null or empty
            if (image == null || image.Length == 0)
            {
                _logger.LogWarning("No image provided for product with ID {ProductId}", productId);
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
                _logger.LogWarning("Product with ID {ProductId} not found during image upload", productId);
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

            _logger.LogInformation("Image uploaded successfully for product ID {ProductId} at {DT}", productId, DateTime.UtcNow);

            return Ok(new { imageUrls = product.ImageUrls.Select(imageUrl => $"http://localhost:5047/UploadedImages/{productId}/{Path.GetFileName(imageUrl)}") });
        }
    }
}
