using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiCSVMapCash.Models;
using WebApiCSVMapCash.ViewModels;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;

namespace WebApiCSVMapCash.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemoryCacheProductsController : ControllerBase
    {
        private readonly ProductsContext _dbContext;
        private IMemoryCache _memoryCache;

        public MemoryCacheProductsController(ProductsContext dbContext, IMemoryCache memoryCache)
        { _dbContext = dbContext; _memoryCache = memoryCache; }



        private string GetCsv(IEnumerable<ProductModel> products)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var product in products)
            {
                sb.AppendLine(product.Name + ";" + product.Description + ";" + product.GroupName + ";" + product.Price + "\n");
            }
            return sb.ToString();
        }
        [HttpGet(template: "GetProductsAsCsv")]
        public FileContentResult GetProductsAsCsv()
        {
            var content = "";

            using (_dbContext)
            {
                var products = _dbContext.Procucts.Select(b => new ProductModel { Name = b.Name, Description = b.Description, GroupName = b.ProductGroup.Name, Price = b.Price }).ToList();

                content = GetCsv(products);

            }

            return File(Encoding.UTF8.GetBytes(content), "text/csv", "products.csv");
        }

        [HttpGet(template: "GetProductsCsvUrl")]
        public ActionResult<string> GetProductsCsvUrl()
        {
            var content = "";


            using (_dbContext)
            {
                var products = _dbContext.Procucts.Select(b => new ProductModel { Description = b.Description, Name = b.Name, GroupName = b.ProductGroup.Name, Price = b.Price }).ToList();

                content = GetCsv(products);
            }

            string? fileName = null;
            fileName = "products" + DateTime.Now.ToBinary().ToString() + ".csv";
            System.IO.File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", fileName), content);

            return "https://" + Request.Host.ToString() + "/static/" + fileName;
        }


        [HttpPost(template: "AddProduct")]
        public ActionResult AddProduct(string name, string description, int productGroupId, int price)
        {
            using (_dbContext)
            {
                if (_dbContext.Procucts.FirstOrDefault(a => a.Name == name) != null) return StatusCode(409);
                else if (_dbContext.ProductGroups.FirstOrDefault(a => a.Id == productGroupId) == null)
                    return StatusCode(404, "Group not found , pleaase add product with group.");
                else
                {
                    _dbContext.Procucts.Add(new Product { Name = name, Description = description, ProductGroupId = productGroupId, Price = price });
                    _dbContext.SaveChanges();
                    _memoryCache.Remove("products");
                    return Ok();
                }
            }
        }

        [HttpGet(template: "GetProducts")]
        public ActionResult<IEnumerable<ProductModel>> GetProducts()
        {

            if (_memoryCache.TryGetValue("books", out var books))
                return Ok(books);

            else
            {
                using (_dbContext)
                {
                    var products = _dbContext.Procucts.Select(a => new ProductModel { Description = a.Description, Name = a.Name, GroupName = a.ProductGroup.Name, Price = a.Price }).ToList();

                    _memoryCache.Set("products", products, TimeSpan.FromMinutes(60));
                    return Ok(products);

                }
            }

        }
        [HttpPost(template: "AddGroup")]
        public ActionResult AddGroup(string name, string description)
        {
            using (_dbContext)
            {
                try
                {
                    if (_dbContext.ProductGroups.FirstOrDefault(b => b.Name == name) != null)
                        return StatusCode(409);
                    else
                    {
                        var productGroup = new ProductGroup() { Description = description, Name = name };
                        _dbContext.ProductGroups.Add(productGroup);
                        _dbContext.SaveChanges();


                        _memoryCache.Remove("groups");
                        return Ok();
                    }
                }
                catch
                {
                    return StatusCode(500);
                }


            }

        }

        [HttpGet(template: "GetGroups")]
        public ActionResult<IEnumerable<ProductGroupModel>> GetGroups()
        {
            if (_memoryCache.TryGetValue("groups", out List<ProductGroupModel>? groups))
                return Ok(groups);
            else
            {
                using (_dbContext)
                {

                    groups = _dbContext.ProductGroups.Select(b => new ProductGroupModel { Id = b.Id, Name = b.Name, Description = b.Description }).ToList();
                    _memoryCache.Set("groups", groups);
                    return Ok(groups);
                }
            }
        }
        [HttpPost(template: "AddProductWithGroup")]
        public ActionResult AddProductWithGroup(string description, string name, string groupName, int price)
        {
            ProductModel productModel = new ProductModel() { Description = description, Name = name, GroupName = groupName, Price = price };
            try
            {
                using (_dbContext)
                {


                    var productGroupId = _dbContext.ProductGroups.FirstOrDefault(a => a.Name == productModel.GroupName)?.Id ?? -1;
                    if (productGroupId < 0)
                    {
                        var productGroup = new ProductGroup() { Name = productModel.GroupName, Description = productModel.Description };
                        _dbContext.ProductGroups.Add(productGroup);
                        _dbContext.SaveChanges();
                        productGroupId = productGroup.Id;
                        _memoryCache.Remove("groups");
                    }
                    if (_dbContext.Procucts.FirstOrDefault(a => a.Name == name && a.ProductGroupId == productGroupId) != null)
                        return StatusCode(409);
                    else
                    {
                        _dbContext.Procucts.Add(new Product { Name = name, Description = productModel.Description, ProductGroupId = productGroupId, Price = productModel.Price });
                        _dbContext.SaveChanges();
                        _memoryCache.Remove("products");
                        return Ok();

                    }
                }
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpDelete(template: "deletegroup")]
        public ActionResult DeleteGroup(string name)
        {
            try
            {
                using (_dbContext)
                {
                    if (!_dbContext.ProductGroups.Any(x => x.Name == name))
                    {
                        return StatusCode(404);
                    }
                    else
                    {
                        var group = _dbContext.ProductGroups.FirstOrDefault(x => x.Name == name);
                        if (group != null)
                        {
                            _dbContext.ProductGroups.Remove(group);
                            _dbContext.SaveChanges();
                            _memoryCache.Remove("groups");
                        }
                        else
                        {
                            return StatusCode(500, "There is no group with this name");
                        }
                    }
                }
                return StatusCode(200);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpDelete(template: "deleteproduct")]
        public ActionResult DeleteProduct(string productName)
        {
            try
            {

                using (_dbContext)
                {
                    if (!_dbContext.Procucts.Any(x => x.Name.ToLower() == productName.ToLower()))
                    {
                        return StatusCode(404);
                    }
                    else
                    {
                        var product = _dbContext.Procucts.FirstOrDefault(x => x.Name == productName);
                        if (product != null)
                        {
                            _dbContext.Remove(product);
                            _dbContext.SaveChanges();
                            _memoryCache.Remove("products");
                        }
                        else
                        {
                            return StatusCode(500, "There is no product with this name");
                        }

                    }
                }
                return StatusCode(200);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
