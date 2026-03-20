using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using uniManage.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace uniManage.Controllers
{
    public class AdminController : Controller
    {
        private UniManageContext db = new UniManageContext();

        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            // Get basic counts
            var totalUsers = db.Users.Count();
            var totalStudents = db.Students.Count();
            var totalLecturers = db.Lecturers.Count();
            var totalCourses = db.Courses.Count();
            var totalEnrollments = db.Enrollments.Count();
            var activeEnrollments = db.Enrollments.Count(e => e.Status == "Active");
            var pendingEnrollments = db.Enrollments.Count(e => e.Status == "Pending");

            // Get recent user registrations (last 10)
            var recentUsers = db.Users
                .OrderByDescending(u => u.CreatedDate)
                .Take(10)
                .ToList();

            var recentRegistrations = new List<RecentUserRegistration>();
            var colors = new[] { "#f59e0b", "#ef4444", "#06b6d4", "#10b981", "#8b5cf6", "#f97316", "#ec4899", "#84cc16" };
            var colorIndex = 0;

            foreach (var user in recentUsers)
            {
                var department = "N/A";
                var status = "Active";

                // Get department based on role
                if (user.Role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.UserId == user.UserId);
                    department = "General"; // You can enhance this based on your student model
                }
                else if (user.Role == "Lecturer")
                {
                    var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == user.UserId);
                    department = lecturer?.Department ?? "N/A";
                }
                else if (user.Role == "Administrator")
                {
                    department = "Administration";
                }

                // Generate initials
                var names = user.FullName.Split(' ');
                var initials = names.Length >= 2 ? 
                    string.Format("{0}{1}", names[0][0], names[names.Length - 1][0]) : 
                    user.FullName.Length >= 2 ? user.FullName.Substring(0, 2).ToUpper() : user.FullName.ToUpper();

                recentRegistrations.Add(new RecentUserRegistration
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role == "Administrator" ? "Admin" : user.Role,
                    Department = department,
                    Status = status,
                    CreatedDate = user.CreatedDate,
                    Initials = initials,
                    AvatarColor = colors[colorIndex % colors.Length]
                });
                colorIndex++;
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalStudents = totalStudents,
                TotalLecturers = totalLecturers,
                TotalCourses = totalCourses,
                TotalEnrollments = totalEnrollments,
                ActiveEnrollments = activeEnrollments,
                PendingEnrollments = pendingEnrollments,
                PopularCourses = db.Courses.Include("Enrollments").OrderByDescending(c => c.Enrollments.Count).Take(5).ToList(),
                RecentRegistrations = recentRegistrations,
                SystemUptime = 99.9, // You can calculate this based on your system monitoring
                AverageLatency = new Random().Next(100, 200) // Simulate latency, replace with real monitoring data
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
            ViewBag.Departments = db.Departments.Where(d => d.Status == "Active").OrderBy(d => d.DepartmentName).ToList();
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
            ViewBag.Departments = db.Departments.Where(d => d.Status == "Active").OrderBy(d => d.DepartmentName).ToList();
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

            // Generate CSV (more reliable than PDF without all dependencies)
            var csv = "Course Code,Course Name,Total Enrollments\n";
            foreach (var item in report)
            {
                csv += string.Format("\"{0}\",\"{1}\",{2}\n", 
                    item.CourseCode, 
                    item.CourseName, 
                    item.EnrollmentCount);
            }

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csv);
            
            // Set response headers
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=CourseReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            Response.BinaryWrite(fileBytes);
            Response.End();
            
            return new EmptyResult();
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
                csv += string.Format("{0},{1},{2}\n", item.CourseCode, item.CourseName, item.EnrollmentCount);
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

        // DETAILED REPORTS SECTION

        public ActionResult DetailedEnrollmentReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var report = db.Enrollments
                .Include("Student.User")
                .Include("Course")
                .Select(e => new DetailedEnrollmentReportViewModel
                {
                    StudentName = e.Student.User.FullName,
                    StudentNumber = e.Student.StudentNumber,
                    CourseName = e.Course.CourseName,
                    CourseCode = e.Course.CourseCode,
                    EnrollmentDate = e.EnrollmentDate,
                    Status = e.Status,
                    Credits = e.Course.Credits
                })
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            ViewBag.TotalEnrollments = report.Count;
            ViewBag.ActiveEnrollments = report.Count(e => e.Status == "Active");
            ViewBag.CompletedEnrollments = report.Count(e => e.Status == "Completed");
            ViewBag.DroppedEnrollments = report.Count(e => e.Status == "Dropped");

            return View(report);
        }

        public ActionResult DetailedStudentPerformanceReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var report = db.Students
                .Include("User")
                .Include("Submissions")
                .Select(s => new DetailedStudentPerformanceReportViewModel
                {
                    StudentName = s.User.FullName,
                    StudentNumber = s.StudentNumber,
                    TotalSubmissions = s.Submissions.Count,
                    GradedSubmissions = s.Submissions.Count(sub => sub.Grade.HasValue),
                    PendingSubmissions = s.Submissions.Count(sub => !sub.Grade.HasValue),
                    AverageGrade = s.Submissions.Where(sub => sub.Grade.HasValue).Average(sub => (double?)sub.Grade) ?? 0,
                    EnrolledCourses = s.Enrollments.Count,
                    CompletedCourses = s.Enrollments.Count(e => e.Status == "Completed")
                })
                .OrderByDescending(s => s.AverageGrade)
                .ToList();

            ViewBag.TotalStudents = report.Count;
            ViewBag.AveragePerformance = report.Average(s => s.AverageGrade);
            ViewBag.TopPerformer = report.FirstOrDefault()?.StudentName ?? "N/A";

            return View(report);
        }

        public ActionResult DetailedCourseAnalysisReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var report = db.Courses
                .Include("Lecturer.User")
                .Include("Enrollments")
                .Include("Assignments")
                .Select(c => new DetailedCourseAnalysisReportViewModel
                {
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    LecturerName = c.Lecturer != null ? c.Lecturer.User.FullName : "Unassigned",
                    MaxEnrollment = c.MaxEnrollment,
                    CurrentEnrollment = c.Enrollments.Count,
                    TotalAssignments = c.Assignments.Count,
                    AverageGrade = c.Enrollments.Any() ? 
                        c.Enrollments.SelectMany(e => e.Student.Submissions)
                            .Where(s => s.Assignment.CourseId == c.CourseId && s.Grade.HasValue)
                            .Average(s => (double?)s.Grade) ?? 0 : 0
                })
                .OrderByDescending(c => c.CurrentEnrollment)
                .ToList();

            ViewBag.TotalCourses = report.Count;
            ViewBag.TotalEnrollments = report.Sum(c => c.CurrentEnrollment);
            ViewBag.AverageCapacityUsage = report.Average(c => (double)c.CurrentEnrollment / c.MaxEnrollment * 100).ToString("F1") + "%";
            ViewBag.AverageGrade = report.Average(c => c.AverageGrade).ToString("F2");

            return View(report);
        }

        public ActionResult DetailedAssignmentReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var report = db.Assignments
                .Include("Course")
                .Include("Submissions")
                .Select(a => new DetailedAssignmentReportViewModel
                {
                    AssignmentTitle = a.Title,
                    CourseName = a.Course.CourseName,
                    DueDate = a.DueDate,
                    TotalSubmissions = a.Submissions.Count,
                    GradedSubmissions = a.Submissions.Count(s => s.Grade.HasValue),
                    PendingSubmissions = a.Submissions.Count(s => !s.Grade.HasValue),
                    AverageGrade = a.Submissions.Where(s => s.Grade.HasValue).Average(s => (double?)s.Grade) ?? 0
                })
                .OrderByDescending(a => a.DueDate)
                .ToList();

            ViewBag.TotalAssignments = report.Count;
            ViewBag.TotalSubmissions = report.Sum(a => a.TotalSubmissions);
            ViewBag.SubmissionRate = report.Count > 0 ? ((double)report.Sum(a => a.GradedSubmissions) / report.Sum(a => a.TotalSubmissions) * 100).ToString("F1") + "%" : "0%";
            ViewBag.AverageGrade = report.Average(a => a.AverageGrade).ToString("F2");

            return View(report);
        }

        public ActionResult DetailedDepartmentReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            // Get all departments
            var departments = db.Departments.ToList();
            
            // Get all lecturers with their courses
            var lecturersWithCourses = db.Lecturers.Include("Courses.Enrollments").ToList();

            var report = departments
                .Select(d => new DetailedDepartmentReportViewModel
                {
                    DepartmentName = d.DepartmentName,
                    Status = d.Status,
                    TotalLecturers = lecturersWithCourses.Count(l => l.Department == d.DepartmentName),
                    TotalCourses = lecturersWithCourses.Where(l => l.Department == d.DepartmentName).SelectMany(l => l.Courses).Count(),
                    TotalStudents = lecturersWithCourses.Where(l => l.Department == d.DepartmentName).SelectMany(l => l.Courses).SelectMany(c => c.Enrollments).Count(),
                    AverageCoursesPerLecturer = lecturersWithCourses.Count(l => l.Department == d.DepartmentName) > 0 ? 
                        (double)lecturersWithCourses.Where(l => l.Department == d.DepartmentName).SelectMany(l => l.Courses).Count() / lecturersWithCourses.Count(l => l.Department == d.DepartmentName) : 0
                })
                .OrderByDescending(d => d.TotalCourses)
                .ToList();

            ViewBag.TotalDepartments = report.Count;
            ViewBag.TotalLecturers = report.Sum(d => d.TotalLecturers);
            ViewBag.TotalCourses = report.Sum(d => d.TotalCourses);
            ViewBag.TotalStudents = report.Sum(d => d.TotalStudents);

            return View(report);
        }

        public ActionResult DetailedUserActivityReport()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var report = db.Users
                .ToList()
                .Select(u => new DetailedUserActivityReportViewModel
                {
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    DaysSinceCreation = (int)(DateTime.Now - u.CreatedDate).TotalDays,
                    SentMessages = u.SentMessages.Count,
                    ReceivedMessages = u.ReceivedMessages.Count,
                    TotalMessages = u.SentMessages.Count + u.ReceivedMessages.Count
                })
                .OrderByDescending(u => u.TotalMessages)
                .ToList();

            ViewBag.TotalUsers = report.Count;
            ViewBag.StudentCount = report.Count(u => u.Role == "Student");
            ViewBag.LecturerCount = report.Count(u => u.Role == "Lecturer");
            ViewBag.AdminCount = report.Count(u => u.Role == "Administrator");
            ViewBag.TotalMessages = report.Sum(u => u.TotalMessages);
            ViewBag.ActiveUsers = report.Count(u => u.DaysSinceCreation <= 30);
            ViewBag.EngagementRate = report.Count > 0 ? ((double)report.Count(u => u.TotalMessages > 0) / report.Count * 100).ToString("F1") + "%" : "0%";

            return View(report);
        }

        // EXPORT DETAILED REPORTS AS CSV

        public ActionResult ExportDetailedEnrollmentReportCSV()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Enrollments
                .Include("Student.User")
                .Include("Course")
                .Select(e => new
                {
                    StudentName = e.Student.User.FullName,
                    StudentNumber = e.Student.StudentNumber,
                    CourseName = e.Course.CourseName,
                    CourseCode = e.Course.CourseCode,
                    EnrollmentDate = e.EnrollmentDate,
                    Status = e.Status,
                    Credits = e.Course.Credits
                })
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            var csv = "Student Name,Student Number,Course Name,Course Code,Enrollment Date,Status,Credits\n";
            foreach (var item in report)
            {
                csv += string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},\"{5}\",{6}\n",
                    item.StudentName,
                    item.StudentNumber,
                    item.CourseName,
                    item.CourseCode,
                    item.EnrollmentDate.ToString("yyyy-MM-dd"),
                    item.Status,
                    item.Credits);
            }

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(fileBytes, "text/csv", "DetailedEnrollmentReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
        }

        public ActionResult ExportDetailedStudentPerformanceReportCSV()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Students
                .Include("User")
                .Include("Submissions")
                .Select(s => new
                {
                    StudentName = s.User.FullName,
                    StudentNumber = s.StudentNumber,
                    Email = s.User.Email,
                    TotalSubmissions = s.Submissions.Count,
                    GradedSubmissions = s.Submissions.Count(sub => sub.Grade.HasValue),
                    PendingSubmissions = s.Submissions.Count(sub => !sub.Grade.HasValue),
                    AverageGrade = s.Submissions.Where(sub => sub.Grade.HasValue).Average(sub => (double?)sub.Grade) ?? 0,
                    EnrolledCourses = s.Enrollments.Count,
                    CompletedCourses = s.Enrollments.Count(e => e.Status == "Completed")
                })
                .OrderByDescending(s => s.AverageGrade)
                .ToList();

            var csv = "Student Name,Student Number,Email,Total Submissions,Graded,Pending,Average Grade,Enrolled Courses,Completed Courses\n";
            foreach (var item in report)
            {
                csv += string.Format("\"{0}\",\"{1}\",\"{2}\",{3},{4},{5},{6:F2},{7},{8}\n",
                    item.StudentName,
                    item.StudentNumber,
                    item.Email,
                    item.TotalSubmissions,
                    item.GradedSubmissions,
                    item.PendingSubmissions,
                    item.AverageGrade,
                    item.EnrolledCourses,
                    item.CompletedCourses);
            }

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(fileBytes, "text/csv", "DetailedStudentPerformanceReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
        }

        public ActionResult ExportDetailedCourseAnalysisReportCSV()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var report = db.Courses
                .Include("Lecturer.User")
                .Include("Enrollments")
                .Include("Assignments")
                .Select(c => new
                {
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    Department = c.Department,
                    LecturerName = c.Lecturer != null ? c.Lecturer.User.FullName : "Unassigned",
                    Credits = c.Credits,
                    MaxEnrollment = c.MaxEnrollment,
                    CurrentEnrollment = c.Enrollments.Count,
                    AvailableSeats = c.MaxEnrollment - c.Enrollments.Count,
                    EnrollmentPercentage = (c.Enrollments.Count * 100) / c.MaxEnrollment,
                    TotalAssignments = c.Assignments.Count,
                    AverageGrade = c.Enrollments.Any() ? 
                        c.Enrollments.SelectMany(e => e.Student.Submissions)
                            .Where(s => s.Assignment.CourseId == c.CourseId && s.Grade.HasValue)
                            .Average(s => (double?)s.Grade) ?? 0 : 0
                })
                .OrderByDescending(c => c.CurrentEnrollment)
                .ToList();

            var csv = "Course Code,Course Name,Department,Lecturer,Credits,Max Enrollment,Current Enrollment,Available Seats,Enrollment %,Total Assignments,Average Grade\n";
            foreach (var item in report)
            {
                csv += string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},{5},{6},{7},{8}%,{9},{10:F2}\n",
                    item.CourseCode,
                    item.CourseName,
                    item.Department,
                    item.LecturerName,
                    item.Credits,
                    item.MaxEnrollment,
                    item.CurrentEnrollment,
                    item.AvailableSeats,
                    item.EnrollmentPercentage,
                    item.TotalAssignments,
                    item.AverageGrade);
            }

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(fileBytes, "text/csv", "DetailedCourseAnalysisReport_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
        }

        public ActionResult Students()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");
            
            // Get all users with their details (same as Users action)
            var allUsers = db.Users.OrderByDescending(u => u.CreatedDate).ToList();
            var userList = new List<UserManagementViewModel>();
            var colors = new[] { "#4f46e5", "#f59e0b", "#ef4444", "#06b6d4", "#10b981", "#8b5cf6", "#f97316", "#ec4899" };
            var colorIndex = 0;

            foreach (var user in allUsers)
            {
                var department = "N/A";
                var userId = "N/A";

                // Get department and ID based on role
                if (user.Role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.UserId == user.UserId);
                    department = "General";
                    userId = student?.StudentNumber ?? "N/A";
                }
                else if (user.Role == "Lecturer")
                {
                    var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == user.UserId);
                    department = lecturer?.Department ?? "N/A";
                    userId = "LEC-" + user.UserId.ToString("D4");
                }
                else if (user.Role == "Administrator")
                {
                    department = "Administration";
                    userId = "ADM-" + user.UserId.ToString("D4");
                }

                // Generate initials
                var names = user.FullName.Split(' ');
                var initials = names.Length >= 2 ? 
                    string.Format("{0}{1}", names[0][0], names[names.Length - 1][0]) : 
                    user.FullName.Length >= 2 ? user.FullName.Substring(0, 2).ToUpper() : user.FullName.ToUpper();

                userList.Add(new UserManagementViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    Department = department,
                    UserIdentifier = userId,
                    Status = "Active",
                    CreatedDate = user.CreatedDate,
                    Initials = initials,
                    AvatarColor = colors[colorIndex % colors.Length]
                });
                colorIndex++;
            }

            return View(userList);
        }

        public ActionResult Users()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");
            
            // Get all users with their details
            var allUsers = db.Users.OrderByDescending(u => u.CreatedDate).ToList();
            var userList = new List<UserManagementViewModel>();
            var colors = new[] { "#4f46e5", "#f59e0b", "#ef4444", "#06b6d4", "#10b981", "#8b5cf6", "#f97316", "#ec4899" };
            var colorIndex = 0;

            foreach (var user in allUsers)
            {
                var department = "N/A";
                var userId = "N/A";

                // Get department and ID based on role
                if (user.Role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.UserId == user.UserId);
                    department = "General";
                    userId = student != null ? student.StudentNumber : "N/A";
                }
                else if (user.Role == "Lecturer")
                {
                    var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == user.UserId);
                    department = lecturer != null ? lecturer.Department : "N/A";
                    userId = "LEC-" + user.UserId.ToString("D4");
                }
                else if (user.Role == "Administrator")
                {
                    department = "Administration";
                    userId = "ADM-" + user.UserId.ToString("D4");
                }

                // Generate initials
                var names = user.FullName.Split(' ');
                var initials = names.Length >= 2 ? 
                    names[0][0].ToString() + names[names.Length - 1][0].ToString() : 
                    user.FullName.Length >= 2 ? user.FullName.Substring(0, 2).ToUpper() : user.FullName.ToUpper();

                userList.Add(new UserManagementViewModel
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    Department = department,
                    UserIdentifier = userId,
                    Status = "Active",
                    CreatedDate = user.CreatedDate,
                    Initials = initials,
                    AvatarColor = colors[colorIndex % colors.Length]
                });
                colorIndex++;
            }

            // Initialize ViewBag.Departments to prevent null reference errors
            ViewBag.Departments = new List<dynamic>();
            ViewBag.DepartmentDebug = "Initializing departments...";

            // Try to get departments, with detailed error handling
            try
            {
                // Test if we can access the Departments table
                var departments = db.Departments.Where(d => d.Status == "Active").OrderBy(d => d.DepartmentName).ToList();
                ViewBag.Departments = departments;
                ViewBag.DepartmentDebug = "Successfully loaded " + departments.Count + " active departments from database";
            }
            catch (Exception ex)
            {
                // Log the specific error for debugging
                ViewBag.DepartmentDebug = "Error accessing Departments table: " + ex.Message;
                
                // Create a fallback list with anonymous objects that have DepartmentName property
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

            return View(userList);
        }

        [HttpPost]
        public JsonResult UpdateUser(int userId, string fullName, string email, string role, string department)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                var user = db.Users.Find(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Update user details
                user.FullName = fullName;
                user.Email = email;
                user.Role = role;

                // Update department based on role
                if (role == "Lecturer")
                {
                    var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == userId);
                    if (lecturer != null)
                    {
                        lecturer.Department = department;
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                var user = db.Users.Find(userId);
                if (user == null)
                    return Json(new { success = false, message = "User not found" });

                // Don't allow deleting yourself
                if (userId == (int)Session["UserId"])
                    return Json(new { success = false, message = "You cannot delete your own account" });

                // Delete related records first
                if (user.Role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.UserId == userId);
                    if (student != null)
                    {
                        // Delete enrollments
                        var enrollments = db.Enrollments.Where(e => e.StudentId == student.UserId).ToList();
                        db.Enrollments.RemoveRange(enrollments);

                        // Delete submissions
                        var submissions = db.AssignmentSubmissions.Where(s => s.StudentId == student.UserId).ToList();
                        db.AssignmentSubmissions.RemoveRange(submissions);

                        db.Students.Remove(student);
                    }
                }
                else if (user.Role == "Lecturer")
                {
                    var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == userId);
                    if (lecturer != null)
                    {
                        // Check if lecturer has courses
                        var hasCourses = db.Courses.Any(c => c.LecturerId == userId);
                        if (hasCourses)
                            return Json(new { success = false, message = "Cannot delete lecturer with assigned courses" });

                        db.Lecturers.Remove(lecturer);
                    }
                }

                // Delete messages
                var messages = db.Messages.Where(m => m.SenderId == userId || m.ReceiverId == userId).ToList();
                db.Messages.RemoveRange(messages);

                // Delete user
                db.Users.Remove(user);
                db.SaveChanges();

                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult CreateUser(string fullName, string email, string password, string role, string studentNumber, string department)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                if (db.Users.Any(u => u.Email == email))
                    return Json(new { success = false, message = "Email already exists" });

                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    Password = HashPassword(password),
                    Role = role,
                    CreatedDate = DateTime.Now
                };

                db.Users.Add(newUser);
                db.SaveChanges();

                if (role == "Student")
                {
                    // Auto-generate student number using the same logic as registration
                    var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
                    string autoStudentNumber;
                    if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
                    {
                        var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                        autoStudentNumber = string.Format("E-{0:D4}", lastNumber + 1);
                    }
                    else
                    {
                        autoStudentNumber = "E-0001";
                    }

                    var student = new Student
                    {
                        UserId = newUser.UserId,
                        StudentNumber = autoStudentNumber
                    };
                    db.Students.Add(student);
                }
                else if (role == "Lecturer")
                {
                    var lecturer = new Lecturer
                    {
                        UserId = newUser.UserId,
                        Department = !string.IsNullOrEmpty(department) ? department : "General"
                    };
                    db.Lecturers.Add(lecturer);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "User created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating user: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetNextStudentNumber()
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" }, JsonRequestBehavior.AllowGet);

                var lastStudent = db.Students.OrderByDescending(s => s.StudentNumber).FirstOrDefault();
                string nextStudentNumber;
                
                if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.StudentNumber))
                {
                    var lastNumber = int.Parse(lastStudent.StudentNumber.Replace("E-", ""));
                    nextStudentNumber = string.Format("E-{0:D4}", lastNumber + 1);
                }
                else
                {
                    nextStudentNumber = "E-0001";
                }

                return Json(new { success = true, studentNumber = nextStudentNumber }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating student number: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Lecturers()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");
            
            var lecturers = db.Lecturers.Include("User").ToList();
            
            // Get course counts for each lecturer
            var courseCounts = new Dictionary<int, int>();
            foreach (var lecturer in lecturers)
            {
                courseCounts[lecturer.UserId] = db.Courses.Count(c => c.LecturerId == lecturer.UserId);
            }
            ViewBag.CourseCounts = courseCounts;
            
            return View(lecturers);
        }

        public ActionResult Departments()
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");
            
            var departments = db.Departments.OrderByDescending(d => d.CreatedDate).ToList();
            return View(departments);
        }

        [HttpPost]
        public JsonResult CreateDepartment(string departmentName)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                if (string.IsNullOrWhiteSpace(departmentName))
                    return Json(new { success = false, message = "Department name is required" });

                if (db.Departments.Any(d => d.DepartmentName.ToLower() == departmentName.ToLower()))
                    return Json(new { success = false, message = "Department already exists" });

                var department = new Department
                {
                    DepartmentName = departmentName.Trim(),
                    Status = "Active",
                    CreatedDate = DateTime.Now
                };

                db.Departments.Add(department);
                db.SaveChanges();

                return Json(new { success = true, message = "Department created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating department: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateDepartment(int departmentId, string departmentName, string status)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                var department = db.Departments.Find(departmentId);
                if (department == null)
                    return Json(new { success = false, message = "Department not found" });

                if (string.IsNullOrWhiteSpace(departmentName))
                    return Json(new { success = false, message = "Department name is required" });

                // Check if another department with the same name exists
                if (db.Departments.Any(d => d.DepartmentName.ToLower() == departmentName.ToLower() && d.DepartmentId != departmentId))
                    return Json(new { success = false, message = "Department name already exists" });

                department.DepartmentName = departmentName.Trim();
                department.Status = status;
                db.SaveChanges();

                return Json(new { success = true, message = "Department updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating department: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteDepartment(int departmentId)
        {
            try
            {
                if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                    return Json(new { success = false, message = "Unauthorized" });

                var department = db.Departments.Find(departmentId);
                if (department == null)
                    return Json(new { success = false, message = "Department not found" });

                // Check if department has lecturers
                var hasLecturers = db.Lecturers.Any(l => l.Department == department.DepartmentName);
                if (hasLecturers)
                    return Json(new { success = false, message = "Cannot delete department with assigned lecturers" });

                db.Departments.Remove(department);
                db.SaveChanges();

                return Json(new { success = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting department: " + ex.Message });
            }
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
                TempData["Success"] = string.Format("Message sent to {0} recipient(s) successfully", receiverIds.Length);
            }
            else
            {
                TempData["Error"] = "Please select at least one recipient";
            }
            
            return RedirectToAction("Messages");
        }

        public ActionResult ViewUser(int id)
        {
            if (Session["UserId"] == null || Session["UserRole"].ToString() != "Administrator")
                return RedirectToAction("Login", "Account");

            var user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();

            var viewModel = new UserDetailViewModel
            {
                User = user,
                Courses = new List<Course>(),
                Assignments = new List<Assignment>(),
                CompletedAssignments = new List<AssignmentSubmission>(),
                PendingAssignments = new List<Assignment>(),
                StudentCount = 0,
                Department = "N/A"
            };

            if (user.Role == "Student")
            {
                var student = db.Students.FirstOrDefault(s => s.UserId == id);
                if (student != null)
                {
                    // Get enrolled courses
                    var enrollments = db.Enrollments.Include("Course.Lecturer.User").Where(e => e.StudentId == id).ToList();
                    viewModel.Courses = enrollments.Select(e => e.Course).ToList();

                    // Get assignments for enrolled courses
                    var courseIds = viewModel.Courses.Select(c => c.CourseId).ToList();
                    viewModel.Assignments = db.Assignments.Where(a => courseIds.Contains(a.CourseId)).ToList();

                    // Get completed assignments
                    viewModel.CompletedAssignments = db.AssignmentSubmissions.Include("Assignment")
                        .Where(s => s.StudentId == id && s.Grade != null).ToList();

                    // Get pending assignments
                    var completedAssignmentIds = viewModel.CompletedAssignments.Select(s => s.AssignmentId).ToList();
                    viewModel.PendingAssignments = viewModel.Assignments
                        .Where(a => !completedAssignmentIds.Contains(a.AssignmentId)).ToList();

                    viewModel.Department = "General";
                }
            }
            else if (user.Role == "Lecturer")
            {
                var lecturer = db.Lecturers.FirstOrDefault(l => l.UserId == id);
                if (lecturer != null)
                {
                    // Get assigned courses
                    viewModel.Courses = db.Courses.Include("Enrollments").Where(c => c.LecturerId == id).ToList();

                    // Get total student count across all courses
                    viewModel.StudentCount = viewModel.Courses.Sum(c => c.Enrollments.Count);

                    // Get assignments created by this lecturer
                    var courseIds = viewModel.Courses.Select(c => c.CourseId).ToList();
                    viewModel.Assignments = db.Assignments.Where(a => courseIds.Contains(a.CourseId)).ToList();

                    viewModel.Department = lecturer.Department != null ? lecturer.Department : "N/A";
                }
            }
            else if (user.Role == "Administrator")
            {
                viewModel.Department = "Administration";
            }

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private string HashPassword(string password)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
