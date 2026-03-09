using System.Data.Entity;
using MySql.Data.Entity;

namespace uniManage.Models
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class UniManageContext : DbContext
    {
        public UniManageContext() : base("UniManageDB")
        {
            Database.SetInitializer<UniManageContext>(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<CourseMaterial> CourseMaterials { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOptional(u => u.Student)
                .WithRequired(s => s.User);

            modelBuilder.Entity<User>()
                .HasOptional(u => u.Lecturer)
                .WithRequired(l => l.User);

            modelBuilder.Entity<Course>()
                .HasOptional(c => c.Lecturer)
                .WithMany(l => l.Courses)
                .HasForeignKey(c => c.LecturerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Course>()
                .HasOptional(c => c.PrerequisiteCourse)
                .WithMany()
                .HasForeignKey(c => c.PrerequisiteCourseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Enrollment>()
                .HasRequired(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Enrollment>()
                .HasRequired(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<AssignmentSubmission>()
                .HasRequired(a => a.Student)
                .WithMany(s => s.Submissions)
                .HasForeignKey(a => a.StudentId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Message>()
                .HasRequired(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
