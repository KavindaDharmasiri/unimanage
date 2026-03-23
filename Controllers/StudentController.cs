using System;
using System.Collections.Generic;
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
            
            // Get all courses with related data
            var allCourses = db.Courses.Include("Lecturer.User").Include("Enrollments").Include("PrerequisiteCourse").Include("Assignments").ToList();
            
            // Get student's enrollment data
            var enrolledCourseIds = db.Enrollments.Where(e => e.StudentId == userId).Select(e => e.CourseId).ToList();
            var studentEnrollments = db.Enrollments.Where(e => e.StudentId == userId).ToList();
            
            // Get student's submissions
            var submissions = db.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .GroupBy(s => s.AssignmentId)
                .Select(g => g.OrderByDescending(s => s.SubmissionDate).FirstOrDefault())
                .ToList();
            var submissionDict = submissions.ToDictionary(s => s.AssignmentId, s => s);
            
            // Sort courses: Active enrolled courses first, then available courses, then others
            var sortedCourses = allCourses.OrderBy(c => {
                var isEnrolled = enrolledCourseIds.Contains(c.CourseId);
                var enrollmentStatus = studentEnrollments.FirstOrDefault(e => e.CourseId == c.CourseId)?.Status;
                
                if (isEnrolled && enrollmentStatus == "Active") return 0; // Active courses first
                if (!isEnrolled && c.Enrollments.Count < c.MaxEnrollment) return 1; // Available courses second
                if (isEnrolled && enrollmentStatus == "Completed") return 2; // Completed courses third
                return 3; // Other courses last
            }).ThenBy(c => c.CourseName).ToList();
            
            // Pass data to view
            ViewBag.EnrolledCourseIds = enrolledCourseIds;
            ViewBag.StudentCompletedCourses = studentEnrollments.Where(e => e.Status == "Completed").Select(e => e.CourseId).ToList();
            ViewBag.StudentActiveCourses = studentEnrollments.Where(e => e.Status == "Active").Select(e => e.CourseId).ToList();
            ViewBag.StudentEnrollments = studentEnrollments.ToDictionary(e => e.CourseId, e => e.Status);
            ViewBag.Submissions = submissionDict;
            
            return View(sortedCourses);
        }

        public ActionResult CourseDetails(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserId"];
            var course = db.Courses.Include("Assignments").Include("Materials").Include("Lecturer.User").Include("PrerequisiteCourse").FirstOrDefault(c => c.CourseId == id);
            
            if (course == null)
            {
                TempData["Error"] = "Course not found";
                return RedirectToAction("BrowseCourses");
            }
            
            // Get student's submissions for this course
            var courseAssignmentIds = course.Assignments.Select(a => a.AssignmentId).ToList();
            var submissions = db.AssignmentSubmissions
                .Where(s => s.StudentId == userId && courseAssignmentIds.Contains(s.AssignmentId))
                .GroupBy(s => s.AssignmentId)
                .Select(g => g.OrderByDescending(s => s.SubmissionDate).FirstOrDefault())
                .ToList();
            var submissionDict = submissions.ToDictionary(s => s.AssignmentId, s => s);
            
            // Calculate assignment statistics
            var totalAssignments = course.Assignments.Count;
            var submittedAssignments = submissions.Count;
            var pendingAssignments = totalAssignments - submittedAssignments;
            var overdueAssignments = course.Assignments.Count(a => a.DueDate < DateTime.Now && !submissionDict.ContainsKey(a.AssignmentId));
            
            // Check if student meets prerequisites
            ViewBag.CanEnroll = true;
            ViewBag.PrerequisiteMessage = "";
            
            if (course.PrerequisiteCourseId.HasValue)
            {
                var hasPrerequisite = db.Enrollments.Any(e => 
                    e.StudentId == userId && 
                    e.CourseId == course.PrerequisiteCourseId.Value && 
                    (e.Status == "Completed" || e.Status == "Active")
                );
                
                if (!hasPrerequisite)
                {
                    ViewBag.CanEnroll = false;
                    ViewBag.PrerequisiteMessage = $"You must first enroll in '{course.PrerequisiteCourse.CourseName}' before you can enroll in this course.";
                }
                else
                {
                    var hasCompletedPrerequisite = db.Enrollments.Any(e => 
                        e.StudentId == userId && 
                        e.CourseId == course.PrerequisiteCourseId.Value && 
                        e.Status == "Completed"
                    );
                    
                    if (!hasCompletedPrerequisite)
                    {
                        ViewBag.PrerequisiteMessage = $"Note: You are currently enrolled in the prerequisite course '{course.PrerequisiteCourse.CourseName}'. Complete it successfully to fully meet the requirements.";
                    }
                }
            }
            
            // Pass assignment data to view
            ViewBag.Submissions = submissionDict;
            ViewBag.TotalAssignments = totalAssignments;
            ViewBag.SubmittedAssignments = submittedAssignments;
            ViewBag.PendingAssignments = pendingAssignments;
            ViewBag.OverdueAssignments = overdueAssignments;
            ViewBag.IsEnrolled = db.Enrollments.Any(e => e.StudentId == userId && e.CourseId == id);
            
            return View(course);
        }

        [HttpPost]
        public ActionResult Enroll(int courseId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            var course = db.Courses.Include("Enrollments").Include("PrerequisiteCourse").FirstOrDefault(c => c.CourseId == courseId);

            if (course == null)
            {
                TempData["Error"] = "Course not found";
                return RedirectToAction("BrowseCourses");
            }

            // Check if course is full
            if (course.Enrollments.Count >= course.MaxEnrollment)
            {
                TempData["Error"] = "Course is full";
                return RedirectToAction("BrowseCourses");
            }

            // Check if already enrolled
            if (db.Enrollments.Any(e => e.StudentId == userId && e.CourseId == courseId))
            {
                TempData["Error"] = "Already enrolled in this course";
                return RedirectToAction("BrowseCourses");
            }

            // Enhanced prerequisite checking
            if (course.PrerequisiteCourseId.HasValue)
            {
                var prerequisiteCourse = course.PrerequisiteCourse;
                var prerequisiteCourseName = prerequisiteCourse != null ? prerequisiteCourse.CourseName : "Unknown Course";
                
                // Check if student has completed the prerequisite course
                // Accept both "Completed" and "Active" status for more flexibility
                var hasPrerequisite = db.Enrollments.Any(e => 
                    e.StudentId == userId && 
                    e.CourseId == course.PrerequisiteCourseId.Value && 
                    (e.Status == "Completed" || e.Status == "Active")
                );
                
                if (!hasPrerequisite)
                {
                    TempData["Error"] = $"Prerequisite not met. You must first enroll in and complete '{prerequisiteCourseName}' before enrolling in this course.";
                    return RedirectToAction("BrowseCourses");
                }
                
                // Additional check: If prerequisite is only "Active", warn but allow enrollment
                var hasCompletedPrerequisite = db.Enrollments.Any(e => 
                    e.StudentId == userId && 
                    e.CourseId == course.PrerequisiteCourseId.Value && 
                    e.Status == "Completed"
                );
                
                if (!hasCompletedPrerequisite)
                {
                    // Student is enrolled but hasn't completed prerequisite - allow with warning
                    TempData["Warning"] = $"Note: You are currently enrolled in the prerequisite course '{prerequisiteCourseName}'. Make sure to complete it successfully.";
                }
            }

            // Create enrollment
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
            
            // Get submission status for each assignment (get the latest submission for each assignment)
            var submissions = db.AssignmentSubmissions
                .Where(s => s.StudentId == userId)
                .GroupBy(s => s.AssignmentId)
                .Select(g => g.OrderByDescending(s => s.SubmissionDate).FirstOrDefault())
                .ToList();
            
            var submissionDict = submissions.ToDictionary(s => s.AssignmentId, s => s);
            
            // Pass submission data to view
            ViewBag.Submissions = submissionDict;

            return View(assignments);
        }

        public ActionResult SubmitAssignment(int id)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserId"];
            var assignment = db.Assignments.Include("Course").FirstOrDefault(a => a.AssignmentId == id);
            
            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found";
                return RedirectToAction("Assignments");
            }
            
            // Check if already submitted
            var existingSubmission = db.AssignmentSubmissions.FirstOrDefault(s => s.AssignmentId == id && s.StudentId == userId);
            ViewBag.AlreadySubmitted = existingSubmission != null;
            ViewBag.ExistingSubmission = existingSubmission;
            
            // Check if assignment is overdue
            var isOverdue = DateTime.Now > assignment.DueDate;
            var daysLate = isOverdue ? (int)(DateTime.Now - assignment.DueDate).TotalDays : 0;
            
            ViewBag.IsOverdue = isOverdue;
            ViewBag.DaysLate = daysLate;
            ViewBag.TimeRemaining = isOverdue ? "" : GetTimeRemaining(assignment.DueDate);
            
            return View(assignment);
        }
        
        private string GetTimeRemaining(DateTime dueDate)
        {
            var timeSpan = dueDate - DateTime.Now;
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days remaining";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hours remaining";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes remaining";
            else
                return "Due very soon!";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitAssignment(int id, string submissionText)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            
            // Get assignment details
            var assignment = db.Assignments.FirstOrDefault(a => a.AssignmentId == id);
            if (assignment == null)
            {
                TempData["Error"] = "Assignment not found";
                return RedirectToAction("Assignments");
            }
            
            // Check if already submitted
            if (db.AssignmentSubmissions.Any(s => s.AssignmentId == id && s.StudentId == userId))
            {
                TempData["Error"] = "You have already submitted this assignment";
                return RedirectToAction("Assignments");
            }
            
            // Calculate if submission is late
            var submissionDate = DateTime.Now;
            var isLate = submissionDate > assignment.DueDate;
            var daysLate = isLate ? (int)(submissionDate - assignment.DueDate).TotalDays : 0;
            
            // Create submission record
            var submission = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = userId,
                SubmissionDate = submissionDate,
                SubmissionText = submissionText,
                IsLateSubmission = isLate,
                DaysLate = isLate ? daysLate : (int?)null
            };
            
            db.AssignmentSubmissions.Add(submission);
            db.SaveChanges();

            // Set appropriate success message
            if (isLate)
            {
                TempData["Warning"] = $"Assignment submitted successfully, but it was {daysLate} day(s) late. Late submissions may receive reduced marks.";
            }
            else
            {
                TempData["Success"] = "Assignment submitted successfully and on time!";
            }
            
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
        
        // Test method to verify prerequisite logic
        public ActionResult TestPrerequisites()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            
            int userId = (int)Session["UserId"];
            var testResults = new List<string>();
            
            // Get all courses with prerequisites
            var coursesWithPrereqs = db.Courses.Include("PrerequisiteCourse").Where(c => c.PrerequisiteCourseId.HasValue).ToList();
            
            foreach (var course in coursesWithPrereqs)
            {
                var hasPrerequisite = db.Enrollments.Any(e => 
                    e.StudentId == userId && 
                    e.CourseId == course.PrerequisiteCourseId.Value && 
                    (e.Status == "Completed" || e.Status == "Active")
                );
                
                var status = hasPrerequisite ? "✓ Can Enroll" : "✗ Cannot Enroll";
                testResults.Add($"{course.CourseName} (requires {course.PrerequisiteCourse.CourseName}): {status}");
            }
            
            ViewBag.TestResults = testResults;
            return View();
        }
    }
}
