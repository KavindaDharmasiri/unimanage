using System.Web.Mvc;

namespace uniManage.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Session["UserId"] != null)
            {
                string role = Session["UserRole"].ToString();
                if (role == "Student")
                    return RedirectToAction("Dashboard", "Student");
                else if (role == "Lecturer")
                    return RedirectToAction("Dashboard", "Lecturer");
                else if (role == "Administrator")
                    return RedirectToAction("Dashboard", "Admin");
            }
            return View();
        }
    }
}
