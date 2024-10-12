using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiCSVMapCash.Models;
using WebApiCSVMapCash.ViewModels;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace WebApiCSVMapCash.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class ProductsController : ControllerBase
    {


        [HttpPost(template: "addgroup")]
        public ActionResult AddGroup(string name, string description)
        {
            try
            {
                using (var context = new ProductsContext())
                {
                    if (context.ProductGroups.Any(g => g.Name == name))
                    {
                        return StatusCode(409);
                    }
                    else
                    {
                        context.ProductGroups.Add(new ProductGroup { Name = name, Description = description });
                        context.SaveChanges();
                    }

                }
                return StatusCode(200);
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
                using (var context = new ProductsContext())
                {
                    if (!context.ProductGroups.Any(x => x.Name == name))
                    {
                        return StatusCode(404);
                    }
                    else
                    {
                        var group = context.ProductGroups.FirstOrDefault(x => x.Name == name);
                        if (group != null)
                        {
                            context.ProductGroups.Remove(group);
                            context.SaveChanges();
                        }
                        else
                        {
                            return StatusCode(500);
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

        [HttpGet(template: "getgroups")]
        public ActionResult<IEnumerable<ProductGroupModel>> GetGroups()
        {
            try
            {
                using (var context = new ProductsContext())
                {
                    var groups = context.ProductGroups
                        .Select(g => new ProductGroupModel
                        {
                            Id = g.Id,
                            Name = g.Name,
                            Description = g.Description
                        })
                        .ToList();
                    return groups;
                }
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost(template: "addproduct")]
        public ActionResult AddProduct(string name, string description, int productGroupId)
        {
            try
            {
                using (var context = new ProductsContext())
                {
                    if (context.Procucts.Any(p => p.Name == name))
                    {
                        return StatusCode(409);
                    }
                    else
                    {
                        context.Procucts.Add(new Product { Name = name, Description = description, ProductGroupId = productGroupId });
                        context.SaveChanges();
                    }
                }
                return StatusCode(200);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete(template: "deleteproduct")]
        public ActionResult DeleteProduct(string productName)
        {
            try
            {

                using (var context = new ProductsContext())
                {
                    if (!context.Procucts.Any(x => x.Name.ToLower() == productName.ToLower()))
                    {
                        return StatusCode(404);
                    }
                    else
                    {
                        var product = context.Procucts.FirstOrDefault(x => x.Name == productName);
                        if (product != null)
                        {
                            context.Remove(product);
                            context.SaveChanges();
                        }
                        else
                        {
                            return StatusCode(500);
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

        [HttpGet(template: "getproducts")]
        public ActionResult<IEnumerable<ProductModel>> GetProducts()
        {
            try
            {
                using (var context = new ProductsContext())
                {
                    var products = context.Procucts.Select(x => new ProductModel
                    {
                        Description = x.Description,
                        Name = x.Name,
                        GroupName = x.ProductGroup.Name,
                        Price = x.Price

                    })
                            .ToList();
                    return products;

                }
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPut(template: "setprice")]
        public async Task<ActionResult> SetPrice(string productName, int price)
        {
            try
            {
                using (var context = new ProductsContext())
                {

                    if (context.Procucts.Any(x => x.Name.ToLower() == productName.ToLower()))
                    {

                        await context.Procucts
                             .Where(x => x.Name.ToLower() == productName.ToLower())
                             .ExecuteUpdateAsync(x => x.SetProperty(x => x.Price, price));
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        return StatusCode(404);
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
