using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }

        public string StudentNumber { get; set; }
        public string Department { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public Student Student { get; set; }
        public List<Enrollment> Enrollments { get; set; }
        public List<AssignmentSubmission> RecentSubmissions { get; set; }
        public List<Assignment> UpcomingAssignments { get; set; }
    }

    public class LecturerDashboardViewModel
    {
        public Lecturer Lecturer { get; set; }
        public List<Course> Courses { get; set; }
        public List<AssignmentSubmission> PendingGrading { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public List<Course> PopularCourses { get; set; }
    }

    public class CourseReportViewModel
    {
        public string CourseName { get; set; }
        public string CourseCode { get; set; }
        public int EnrollmentCount { get; set; }
    }

    public class StudentReportViewModel
    {
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public double AverageGrade { get; set; }
        public int TotalSubmissions { get; set; }
    }

    public class LecturerReportViewModel
    {
        public string LecturerName { get; set; }
        public string Department { get; set; }
        public int CourseCount { get; set; }
        public int TotalStudents { get; set; }
    }
}
