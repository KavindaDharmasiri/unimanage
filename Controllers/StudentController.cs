using System;
using System.Linq;
using System.Web.Mvc;
using uniManage.Models;

namespace uniManage.Controllers
{
    public class StudentController : Controller
    {
        private UniManageContext db = new UniManageContext();

        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Student")
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var student = db.Students.Include("User").FirstOrDefault(s => s.UserId == userId);
            var enrollments = db.Enrollments.Include("Course.Lecturer.User").Where(e => e.StudentId == userId).ToList();
            var recentSubmissions = db.AssignmentSubmissions.Include("Assignment").Where(s => s.StudentId == userId).OrderByDescending(s => s.SubmissionDate).Take(5).ToList();
            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
            var upcomingAssignments = db.Assignments.Include("Course").Where(a => a.DueDate > DateTime.Now && enrolledCourseIds.Contains(a.CourseId)).OrderBy(a => a.DueDate).ToList();

            var viewModel = new StudentDashboardViewModel
            {
                Student = student,
                Enrollments = enrollments,
                RecentSubmissions = recentSubmissions,
                UpcomingAssignments = upcomingAssignments
            };

            return View(viewModel);
        }

        public ActionResult BrowseCourses()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserId"];
            var courses = db.Courses.Include("Lecturer.User").Include("Enrollments").ToList();
            ViewBag.EnrolledCourseIds = db.Enrollments.Where(e => e.StudentId == userId).Select(e => e.CourseId).ToList();
            return View(courses);
        }

        public ActionResult CourseDetails(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var course = db.Courses.Include("Assignments").Include("Materials").Include("Lecturer.User").FirstOrDefault(c => c.CourseId == id);
            return View(course);
        }

        [HttpPost]
        public ActionResult Enroll(int courseId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var course = db.Courses.Include("Enrollments").FirstOrDefault(c => c.CourseId == courseId);

            if (course.Enrollments.Count >= course.MaxEnrollment)
            {
                TempData["Error"] = "Course is full";
                return RedirectToAction("BrowseCourses");
            }

            if (course.PrerequisiteCourseId.HasValue)
            {
                var hasPrerequisite = db.Enrollments.Any(e => e.StudentId == userId && e.CourseId == course.PrerequisiteCourseId.Value && e.Status == "Completed");
                if (!hasPrerequisite)
                {
                    TempData["Error"] = "Prerequisite not met";
                    return RedirectToAction("BrowseCourses");
                }
            }

            if (db.Enrollments.Any(e => e.StudentId == userId && e.CourseId == courseId))
            {
                TempData["Error"] = "Already enrolled";
                return RedirectToAction("BrowseCourses");
            }

            db.Enrollments.Add(new Enrollment
            {
                StudentId = userId,
                CourseId = courseId,
                EnrollmentDate = DateTime.Now,
                Status = "Active"
            });
            db.SaveChanges();

            TempData["Success"] = "Successfully enrolled in " + course.CourseName;
            return RedirectToAction("Dashboard");
        }

        public ActionResult Assignments()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var enrolledCourseIds = db.Enrollments.Where(e => e.StudentId == userId).Select(e => e.CourseId).ToList();
            var assignments = db.Assignments.Include("Course").Where(a => enrolledCourseIds.Contains(a.CourseId)).ToList();

            return View(assignments);
        }

        public ActionResult SubmitAssignment(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var assignment = db.Assignments.Include("Course").FirstOrDefault(a => a.AssignmentId == id);
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitAssignment(int id, string submissionText)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            db.AssignmentSubmissions.Add(new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = userId,
                SubmissionDate = DateTime.Now,
                SubmissionText = submissionText
            });
            db.SaveChanges();

            TempData["Success"] = "Assignment submitted";
            return RedirectToAction("Assignments");
        }

        public ActionResult Messages()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var messages = db.Messages.Include("Sender").Include("Receiver").Where(m => m.ReceiverId == userId || m.SenderId == userId).OrderByDescending(m => m.SentDate).ToList();
            return View(messages);
        }

        public ActionResult SendMessage(int? receiverId, string subject)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int currentUserId = (int)Session["UserId"];
            ViewBag.Lecturers = db.Lecturers.Include("User").ToList();
            ViewBag.Admins = db.Users.Where(u => u.Role == "Administrator" && u.UserId != currentUserId).ToList();
            ViewBag.ReceiverId = receiverId;
            ViewBag.Subject = subject;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendMessage(int receiverId, string subject, string body)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            db.Messages.Add(new Message
            {
                SenderId = userId,
                ReceiverId = receiverId,
                Subject = subject,
                Body = body,
                SentDate = DateTime.Now,
                IsRead = false
            });
            db.SaveChanges();

            TempData["Success"] = "Message sent";
            return RedirectToAction("Messages");
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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
