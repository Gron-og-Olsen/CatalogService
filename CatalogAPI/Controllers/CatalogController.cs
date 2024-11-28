using Microsoft.AspNetCore.Mvc;
using Models; 

namespace CatalogAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CatalogController : ControllerBase
{
   

    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ILogger<CatalogController> logger)
    {
        _logger = logger;
    }
     private static List<Product> _products = new List<Product>()
        {
            new()
            {
                Id = new Guid("7125e019-c469-4dbd-93e5-426de6652523"),
                Name = "Kunstværk",
                Description = "Meget flot billede",
                Price = 12.99m,
                Brand = "Autentisk",
                Manufacturer = "Østrigsk maler",
                Model = "Standard",
                ImageUrl = "https://example.com/billede.jpg",
                ProductUrl = "https://example.com/billede",
                ReleaseDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(3)
            }
        };

[HttpGet("{productId}", Name = "GetProductById")]
        public Product Get(Guid productId)
        {
            _logger.LogInformation("Metode GetProduct called at {DT}",
            DateTime.UtcNow.ToLongTimeString());
            return _products.FirstOrDefault(p => p.Id == productId);
        }

        // POST method to receive product object and add a product
        [HttpPost("add-product")]
        public IActionResult AddProduct([FromBody] Product product)
        {
            _logger.LogInformation("Metode add-product called at {DT}",
            DateTime.UtcNow.ToLongTimeString());

            // Tjek om produktet allerede findes baseret på ProductID
            var existingProduct = _products.FirstOrDefault(p => p.Id == product.Id);

            if (existingProduct != null)
            {
                // Log en advarsel hvis produktet allerede findes
                _logger.LogWarning("Attempt to add product with ID {ProductID} which already exists at {DT}", product.Id, DateTime.UtcNow.ToLongTimeString());

                // Returner en HTTP statuskode 409 (Conflict)
                return StatusCode(StatusCodes.Status409Conflict, "Product with this ID already exists.");
            }

            // Tilføj produktet til listen, hvis det ikke findes
            _products.Add(product);

            // Log information om tilføjelse af produktet
            _logger.LogInformation("New product with ID {ProductID} added successfully at {DT}", product.Id, DateTime.UtcNow.ToLongTimeString());

            // Returner en HTTP statuskode 201 (Created)
            return StatusCode(StatusCodes.Status201Created, "Product added successfully.");
        }
   
}
