using System;
using System.Collections.Generic;
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
                    Session["UserEmail"] = user.Email;
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
                model.StudentNumber = "E-" + (lastNumber + 1).ToString("D4");
            }
            else
            {
                model.StudentNumber = "E-0001";
            }

            // Load active departments for lecturer registration
            try
            {
                ViewBag.Departments = db.Departments.Where(d => d.Status == "Active").OrderBy(d => d.DepartmentName).ToList();
                ViewBag.DepartmentDebug = "Departments loaded successfully";
            }
            catch (Exception ex)
            {
                ViewBag.DepartmentDebug = "Error loading departments: " + ex.Message;
                ViewBag.Departments = new List<dynamic>
                {
                    new { DepartmentName = "Computer Science" },
                    new { DepartmentName = "Mathematics" },
                    new { DepartmentName = "Physics" },
                    new { DepartmentName = "Chemistry" },
                    new { DepartmentName = "Biology" },
                    new { DepartmentName = "Economics" }
                };
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            // Load departments for the view in case we need to return to the form
            try
            {
                ViewBag.Departments = db.Departments.Where(d => d.Status == "Active").OrderBy(d => d.DepartmentName).ToList();
                ViewBag.DepartmentDebug = "Departments loaded successfully";
            }
            catch (Exception ex)
            {
                ViewBag.DepartmentDebug = "Error loading departments: " + ex.Message;
                ViewBag.Departments = new List<dynamic>
                {
                    new { DepartmentName = "Computer Science" },
                    new { DepartmentName = "Mathematics" },
                    new { DepartmentName = "Physics" },
                    new { DepartmentName = "Chemistry" },
                    new { DepartmentName = "Biology" },
                    new { DepartmentName = "Economics" }
                };
            }

            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists");
                    
                    // Re-generate student number if validation fails
                    if (model.Role == "Student")
                    {
                        var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
                        if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
                        {
                            var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                            model.StudentNumber = "E-" + (lastNumber + 1).ToString("D4");
                        }
                        else
                        {
                            model.StudentNumber = "E-0001";
                        }
                    }
                    
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
                    // Generate student number at registration time
                    var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
                    string studentNumber;
                    if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
                    {
                        var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                        studentNumber = "E-" + (lastNumber + 1).ToString("D4");
                    }
                    else
                    {
                        studentNumber = "E-0001";
                    }
                    
                    db.Students.Add(new Student { UserId = user.UserId, StudentNumber = studentNumber });
                    db.SaveChanges();
                }
                else if (model.Role == "Lecturer" && !string.IsNullOrEmpty(model.Department))
                {
                    db.Lecturers.Add(new Lecturer { UserId = user.UserId, Department = model.Department });
                    db.SaveChanges();
                }
                
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            
            // Re-generate student number if model state is invalid
            if (model.Role == "Student")
            {
                var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
                if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
                {
                    var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                    model.StudentNumber = "E-" + (lastNumber + 1).ToString("D4");
                }
                else
                {
                    model.StudentNumber = "E-0001";
                }
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
