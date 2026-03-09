using System;
using System.Linq;
using System.Web.Mvc;
using uniManage.Models;

namespace uniManage.Controllers
{
    public class AdminController : Controller
    {
        private UniManageContext db = new UniManageContext();

        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var viewModel = new AdminDashboardViewModel
            {
                TotalStudents = db.Students.Count(),
                TotalLecturers = db.Lecturers.Count(),
                TotalCourses = db.Courses.Count(),
                TotalEnrollments = db.Enrollments.Count(),
                PopularCourses = db.Courses.Include("Enrollments").OrderByDescending(c => c.Enrollments.Count).Take(5).ToList()
            };

            return View(viewModel);
        }

        public ActionResult Courses()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var courses = db.Courses.Include("Lecturer.User").ToList();
            return View(courses);
        }

        public ActionResult CreateCourse()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            ViewBag.Lecturers = db.Lecturers.Include("User").ToList();
            ViewBag.Courses = db.Courses.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCourse(Course course)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                db.Courses.Add(course);
                db.SaveChanges();
                TempData["Success"] = "Course created successfully";
                return RedirectToAction("Courses");
            }

            ViewBag.Lecturers = db.Lecturers.Include("User").ToList();
            ViewBag.Courses = db.Courses.ToList();
            return View(course);
        }

        public ActionResult EditCourse(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var course = db.Courses.Find(id);
            ViewBag.Lecturers = db.Lecturers.Include("User").ToList();
            ViewBag.Courses = db.Courses.Where(c => c.CourseId != id).ToList();
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCourse(Course course)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                db.Entry(course).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                TempData["Success"] = "Course updated successfully";
                return RedirectToAction("Courses");
            }

            ViewBag.Lecturers = db.Lecturers.Include("User").ToList();
            ViewBag.Courses = db.Courses.Where(c => c.CourseId != course.CourseId).ToList();
            return View(course);
        }

        public ActionResult DeleteCourse(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var course = db.Courses.Find(id);
            return View(course);
        }

        [HttpPost, ActionName("DeleteCourse")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var course = db.Courses.Find(id);
            db.Courses.Remove(course);
            db.SaveChanges();
            TempData["Success"] = "Course deleted successfully";
            return RedirectToAction("Courses");
        }

        public ActionResult Reports()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            return View();
        }

        public ActionResult CoursePopularityReport()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Courses
                .Select(c => new CourseReportViewModel
                {
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ToList();

            return View(report);
        }

        public ActionResult ExportCoursePDF()
        {
            var report = db.Courses
                .Select(c => new CourseReportViewModel
                {
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ToList();

            return File(System.Text.Encoding.UTF8.GetBytes("PDF Export - Install iTextSharp package"), "text/plain", "report.txt");
        }

        public ActionResult ExportCourseExcel()
        {
            var report = db.Courses
                .Select(c => new CourseReportViewModel
                {
                    CourseName = c.CourseName,
                    CourseCode = c.CourseCode,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ToList();

            var csv = "Course Code,Course Name,Enrollments\n";
            foreach (var item in report)
            {
                csv += $"{item.CourseCode},{item.CourseName},{item.EnrollmentCount}\n";
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "CourseReport.csv");
        }

        public ActionResult StudentPerformanceReport()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Students
                .Select(s => new StudentReportViewModel
                {
                    StudentName = s.User.FullName,
                    StudentNumber = s.StudentNumber,
                    AverageGrade = s.Submissions.Average(sub => (double?)sub.Grade) ?? 0,
                    TotalSubmissions = s.Submissions.Count
                })
                .OrderByDescending(s => s.AverageGrade)
                .ToList();

            return View(report);
        }

        public ActionResult LecturerWorkloadReport()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Lecturers
                .Select(l => new LecturerReportViewModel
                {
                    LecturerName = l.User.FullName,
                    Department = l.Department,
                    CourseCount = l.Courses.Count,
                    TotalStudents = l.Courses.Sum(c => c.Enrollments.Count)
                })
                .OrderByDescending(l => l.CourseCount)
                .ToList();

            return View(report);
        }

        public ActionResult Messages()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserId"];
            var messages = db.Messages.Include("Sender").Include("Receiver").Where(m => m.ReceiverId == userId || m.SenderId == userId).OrderByDescending(m => m.SentDate).ToList();
            return View(messages);
        }

        public ActionResult ViewMessage(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserId"];
            var message = db.Messages.Include("Sender").Include("Receiver").FirstOrDefault(m => m.MessageId == id);
            if (message != null && message.ReceiverId == userId && !message.IsRead)
            {
                message.IsRead = true;
                db.SaveChanges();
            }
            return View(message);
        }

        public ActionResult SendMessage(int? receiverId, string subject)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int currentUserId = (int)Session["UserId"];
            ViewBag.Users = db.Users.Where(u => u.UserId != currentUserId).ToList();
            ViewBag.ReceiverId = receiverId;
            ViewBag.Subject = subject;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendMessage(string Subject, string Body, int[] receiverIds)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            
            int senderId = (int)Session["UserId"];
            
            if (receiverIds != null && receiverIds.Length > 0)
            {
                foreach (var receiverId in receiverIds)
                {
                    db.Messages.Add(new Message
                    {
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        Subject = Subject,
                        Body = Body,
                        SentDate = DateTime.Now,
                        IsRead = false
                    });
                }
                db.SaveChanges();
                TempData["Success"] = $"Message sent to {receiverIds.Length} recipient(s) successfully";
            }
            else
            {
                TempData["Error"] = "Please select at least one recipient";
            }
            
            return RedirectToAction("Messages");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
