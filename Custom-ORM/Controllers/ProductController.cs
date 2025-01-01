using Custom_ORM.Data;
using Custom_ORM.Models;
using Microsoft.AspNetCore.Mvc;

namespace Custom_ORM.Controllers
{
    public class ProductController : Controller
    {
        private readonly MyCustomDbContext _context;

        public ProductController(MyCustomDbContext context)
        {
            _context = context;

        }
        public IActionResult Index()
        {
            //var sql = "SELECT * FROM Products";
            var Products = _context.Products.GetAll();

            return View(Products);
        }

        public IActionResult Create()
        {
            var model = new Product();
            return View(model);
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Set<Product>().Add(product);
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }


    }
}
