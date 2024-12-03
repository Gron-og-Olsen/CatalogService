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
        //private readonly ILogger<CatalogController> _logger;  // Kommenteret logger
        private IMongoCollection<Product> @object;

        /*public CatalogController(IMongoCollection<Product> @object)
        {
            this.@object = @object;
        }
*/
        public CatalogController(IMongoCollection<Product> productCollection)  // Loggeren er fjernet fra constructoren
        {
            _productCollection = productCollection;
            // _logger = logger;  // Kommenteret logger
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
                return CreatedAtRoute("GetProductById", new { productId = newProduct.Id }, newProduct);
            }
            catch (Exception ex)
            {
                // Hvis noget går galt, f.eks. i forbindelse med databasen, returner en serverfejl
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetAllProducts")]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            // _logger.LogInformation("Method GetAllProducts called at {DT}", DateTime.UtcNow.ToLongTimeString());  // Kommenteret log

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
            // _logger.LogInformation("Method GetProductById called at {DT}", DateTime.UtcNow.ToLongTimeString());  // Kommenteret log

            var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
            {
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
            // _logger.LogInformation("Uploading image for product with ID {ID} at {DT}", productId, DateTime.UtcNow.ToLongTimeString());  // Kommenteret log

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

            // _logger.LogInformation("Image uploaded successfully for product ID {ID} at {DT}", productId, DateTime.UtcNow.ToLongTimeString());  // Kommenteret log

            return Ok(new { imageUrls = product.ImageUrls.Select(imageUrl => $"http://localhost:5047/UploadedImages/{productId}/{Path.GetFileName(imageUrl)}") });
        }
    }
}
