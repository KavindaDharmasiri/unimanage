using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Web.Security;
using uniManage.Models;

namespace uniManage.Controllers
{
    public class AccountController : Controller
    {
        private UniManageContext db = new UniManageContext();

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == hashedPassword);
                if (user != null)
                {
                    Session["UserId"] = user.UserId;
                    Session["UserName"] = user.FullName;
                    Session["UserRole"] = user.Role;

                    if (user.Role == "Student")
                        return RedirectToAction("Dashboard", "Student");
                    else if (user.Role == "Lecturer")
                        return RedirectToAction("Dashboard", "Lecturer");
                    else if (user.Role == "Administrator")
                        return RedirectToAction("Dashboard", "Admin");
                }
                ModelState.AddModelError("", "Invalid email or password");
            }
            return View(model);
        }

        public ActionResult Register()
        {
            var model = new RegisterViewModel();
            
            // Auto-generate student number from Student table
            var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
            
            if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
            {
                var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                model.StudentNumber = $"E-{(lastNumber + 1):D4}";
            }
            else
            {
                model.StudentNumber = "E-0000";
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = HashPassword(model.Password),
                    Role = model.Role,
                    CreatedDate = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                if (model.Role == "Student")
                {
                    db.Students.Add(new Student { UserId = user.UserId, StudentNumber = model.StudentNumber });
                    db.SaveChanges();
                }
                else if (model.Role == "Lecturer" && !string.IsNullOrEmpty(model.Department))
                {
                    db.Lecturers.Add(new Lecturer { UserId = user.UserId, Department = model.Department });
                    db.SaveChanges();
                }
                
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Register");
            }
            return View(model);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
