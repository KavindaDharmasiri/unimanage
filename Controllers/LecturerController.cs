using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using uniManage.Models;

namespace uniManage.Controllers
{
    public class LecturerController : Controller
    {
        private UniManageContext db = new UniManageContext();
        
        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Lecturer")
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var lecturer = db.Lecturers.Include("User").Include("Courses").FirstOrDefault(l => l.UserId == userId);

            var courseIds = lecturer.Courses.Select(c => c.CourseId).ToList();
            var pendingGrading = db.AssignmentSubmissions
                .Where(s => s.Grade == null && s.Assignment.Course.LecturerId == userId)
                .ToList();

            var viewModel = new LecturerDashboardViewModel
            {
                Lecturer = lecturer,
                Courses = lecturer.Courses.ToList(),
                PendingGrading = pendingGrading
            };

            return View(viewModel);
        }
        
        public ActionResult MyCourses()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var courses = db.Courses.Include("Enrollments").Where(c => c.LecturerId == userId).ToList();
            return View(courses);
        }
        
        public ActionResult CourseDetails(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var course = db.Courses.Include("Assignments.Submissions").Include("Materials").FirstOrDefault(c => c.CourseId == id);
            var enrollments = db.Enrollments.Include("Student.User").Where(e => e.CourseId == id).ToList();
            course.Enrollments = enrollments;
            return View(course);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateEnrollmentStatus(int enrollmentId, string status)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Lecturer")
                    return Json(new { success = false, message = "Unauthorized" });

                var enrollment = db.Enrollments.Find(enrollmentId);
                if (enrollment == null)
                    return Json(new { success = false, message = "Enrollment not found" });

                // Verify lecturer owns this course
                var course = db.Courses.Find(enrollment.CourseId);
                if (course.LecturerId != (int)Session["UserId"])
                    return Json(new { success = false, message = "You can only update enrollments for your own courses" });

                enrollment.Status = status;
                db.SaveChanges();

                return Json(new { success = true, message = "Enrollment status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating enrollment: " + ex.Message });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult BulkUpdateEnrollmentStatus(int courseId, string status)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Lecturer")
                    return Json(new { success = false, message = "Unauthorized" });

                // Verify lecturer owns this course
                var course = db.Courses.Find(courseId);
                if (course == null || course.LecturerId != (int)Session["UserId"])
                    return Json(new { success = false, message = "You can only update enrollments for your own courses" });

                // Update all active enrollments to the new status
                var enrollments = db.Enrollments.Where(e => e.CourseId == courseId && e.Status == "Active").ToList();
                foreach (var enrollment in enrollments)
                {
                    enrollment.Status = status;
                }
                db.SaveChanges();

                return Json(new { success = true, message = $"Updated {enrollments.Count} enrollments to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating enrollments: " + ex.Message });
            }
        }
        
        public ActionResult CreateAssignment(int? courseId)
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Lecturer")
                return RedirectToAction("Login", "Account");
            
            int lecturerId = (int)Session["UserId"];
            ViewBag.Courses = db.Courses.Where(c => c.LecturerId == lecturerId).ToList();
            ViewBag.CourseId = courseId;
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAssignment(Assignment assignment, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                if (file != null && file.ContentLength > 0)
                {
                    var uploadPath = Server.MapPath("~/Uploads/Assignments/");
                    if (!System.IO.Directory.Exists(uploadPath))
                        System.IO.Directory.CreateDirectory(uploadPath);

                    var fileName = System.IO.Path.GetFileName(file.FileName);
                    var filePath = System.IO.Path.Combine(uploadPath, fileName);
                    file.SaveAs(filePath);
                    assignment.FilePath = "/Uploads/Assignments/" + fileName;
                }

                db.Assignments.Add(assignment);
                db.SaveChanges();
                TempData["Success"] = "Assignment created successfully";
                return RedirectToAction("CourseDetails", new { id = assignment.CourseId });
            }
            
            int lecturerId = (int)Session["UserId"];
            ViewBag.Courses = db.Courses.Where(c => c.LecturerId == lecturerId).ToList();
            ViewBag.CourseId = assignment.CourseId;
            return View(assignment);
        }
        
        public ActionResult ViewSubmissions(int assignmentId)
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Lecturer")
                return RedirectToAction("Login", "Account");
            
            var submissions = db.AssignmentSubmissions.Where(s => s.AssignmentId == assignmentId).ToList();
            ViewBag.Assignment = db.Assignments.FirstOrDefault(a => a.AssignmentId == assignmentId);
            
            return View(submissions);
        }
        
        public ActionResult GradeSubmission(int id)
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Lecturer")
                return RedirectToAction("Login", "Account");
            
            var submission = db.AssignmentSubmissions.Include("Student.User").Include("Assignment").FirstOrDefault(s => s.SubmissionId == id);
            return View(submission);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GradeSubmission(int id, int grade, string feedback)
        {
            var submission = db.AssignmentSubmissions.FirstOrDefault(s => s.SubmissionId == id);
            if (submission != null)
            {
                submission.Grade = grade;
                submission.Feedback = feedback;
                db.SaveChanges();
            }
            
            return RedirectToAction("ViewSubmissions", new { assignmentId = submission.AssignmentId });
        }
        
        public ActionResult Messages()
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Lecturer")
                return RedirectToAction("Login", "Account");
            
            int userId = (int)Session["UserId"];
            var messages = db.Messages.Where(m => m.ReceiverId == userId || m.SenderId == userId).ToList();
            
            return View(messages);
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
        public ActionResult SendMessage(Message message)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            message.SenderId = (int)Session["UserId"];
            message.SentDate = DateTime.Now;
            message.IsRead = false;

            db.Messages.Add(message);
            db.SaveChanges();

            TempData["Success"] = "Message sent successfully";
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

        public ActionResult UploadMaterial(int? courseId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            ViewBag.CourseId = courseId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterial(int CourseId, string Title, string Description, IEnumerable<HttpPostedFileBase> files)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            if (files != null && files.Any())
            {
                var uploadPath = Server.MapPath("~/Uploads/Materials/");
                if (!System.IO.Directory.Exists(uploadPath))
                    System.IO.Directory.CreateDirectory(uploadPath);

                foreach (var file in files)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = System.IO.Path.GetFileName(file.FileName);
                        var filePath = System.IO.Path.Combine(uploadPath, fileName);
                        file.SaveAs(filePath);

                        db.CourseMaterials.Add(new CourseMaterial
                        {
                            CourseId = CourseId,
                            Title = Title,
                            Description = Description,
                            FilePath = "/Uploads/Materials/" + fileName,
                            UploadDate = DateTime.Now
                        });
                    }
                }
                db.SaveChanges();
                TempData["Success"] = "Materials uploaded successfully";
            }
            else
            {
                TempData["Error"] = "Please select at least one file";
            }

            return RedirectToAction("CourseDetails", new { id = CourseId });
        }

        public ActionResult DeleteMaterial(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var material = db.CourseMaterials.Find(id);
            if (material != null)
            {
                var courseId = material.CourseId;
                db.CourseMaterials.Remove(material);
                db.SaveChanges();
                TempData["Success"] = "Material deleted successfully";
                return RedirectToAction("CourseDetails", new { id = courseId });
            }

            return RedirectToAction("Dashboard");
        }

        public ActionResult DeleteAssignment(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var assignment = db.Assignments.Find(id);
            if (assignment != null)
            {
                var courseId = assignment.CourseId;
                db.Assignments.Remove(assignment);
                db.SaveChanges();
                TempData["Success"] = "Assignment deleted successfully";
                return RedirectToAction("CourseDetails", new { id = courseId });
            }

            return RedirectToAction("Dashboard");
        }
    }
}
