using System.Data.Entity;
using System.Web.Mvc;
using System.Web.Routing;
using uniManage.Models;

namespace uniManage
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new DatabaseInitializer());
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
