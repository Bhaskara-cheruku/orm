using Custom_ORM.Data;
using Custom_ORM.Models;
using Microsoft.AspNetCore.Mvc;

namespace Custom_ORM.Controllers
{
    public class UsersController : Controller
    {
        private readonly MyCustomDbContext _context;

        public UsersController(MyCustomDbContext context)
        {
            _context = context;
            
        }

        // GET: Users
        public IActionResult Index()
        {
            var sql = "SELECT * FROM Users";
            var users = _context.Query<User>(sql).ToList();

            return View(users);
        }

        public IActionResult Create()
        {
            var model = new User();
            return View(model);
        }

        [HttpPost]
        public IActionResult Create(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Set<User>().Add(user); 
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
    }
}
