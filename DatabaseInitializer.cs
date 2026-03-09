using System;
using System.Data.Entity;
using uniManage.Models;

namespace uniManage
{
    public class DatabaseInitializer : DropCreateDatabaseIfModelChanges<UniManageContext>
    {
        protected override void Seed(UniManageContext context)
        {
            var admin = new User
            {
                FullName = "Admin User",
                Email = "admin@unimanage.com",
                Password = "admin123",
                Role = "Administrator",
                CreatedDate = DateTime.Now
            };
            context.Users.Add(admin);

            var lecturer1 = new User
            {
                FullName = "Dr. John Smith",
                Email = "john@unimanage.com",
                Password = "lecturer123",
                Role = "Lecturer",
                CreatedDate = DateTime.Now
            };
            context.Users.Add(lecturer1);

            var student1 = new User
            {
                FullName = "Alice Johnson",
                Email = "alice@unimanage.com",
                Password = "student123",
                Role = "Student",
                CreatedDate = DateTime.Now
            };
            context.Users.Add(student1);

            context.SaveChanges();

            context.Lecturers.Add(new Lecturer { UserId = lecturer1.UserId, Department = "Computer Science" });
            context.Students.Add(new Student { UserId = student1.UserId, StudentNumber = "S001" });

            context.Courses.Add(new Course
            {
                CourseCode = "CS101",
                CourseName = "Introduction to Programming",
                Description = "Learn programming basics",
                Credits = 3,
                MaxEnrollment = 30,
                LecturerId = lecturer1.UserId
            });

            context.SaveChanges();
            base.Seed(context);
        }
    }
}
