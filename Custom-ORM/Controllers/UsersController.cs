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
            //var users = _context.Query<User>(sql).ToList();
            var users = _context.Users.GetAll();
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
                _context.Users.Add(user); 
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }



        public IActionResult Update(int id)
        {
            var user = _context.Users.GetAll().FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Update/{id}
        [HttpPost]
        public IActionResult Update(int id, User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var user = _context.Users.GetAll().FirstOrDefault(u => u.Id == id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update properties of the user
                user.Name = updatedUser.Name;
                user.DateOfBirth = updatedUser.DateOfBirth;

                // Update the user in the database
                _context.Users.Update(user);

                return RedirectToAction(nameof(Index));
            }
            return View(updatedUser);
        }

        // POST: Users/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.GetAll().FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(); // If the user is not found, return 404
            }

            _context.Users.Delete(id); // Call the delete method to remove the user from the database
            return RedirectToAction("Index"); // Redirect back to the Index action
        }



    }
}
