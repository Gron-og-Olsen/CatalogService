using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Models;


namespace CatalogAPI.Controllers;

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

        return Ok(products);
    }

    [HttpGet("{productId}")]
    [Route("GetProductById/{productId}")]
    public async Task<ActionResult<Product>> GetProductById(Guid productId)
    {
        _logger.LogInformation("Method GetProductById called at {DT}", DateTime.UtcNow.ToLongTimeString());

        var product = await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {productId} not found." });
        }

        return Ok(product);
    }
}
