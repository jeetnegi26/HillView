using HillView.sln.Admin;
using HillView.sln.Controllers;
using Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Unity;
using Unity.AspNet.Mvc;
using Unity.Injection;

namespace HillView.sln
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            //Code for DI
            var container = new UnityContainer();
            string connection_string = ConfigurationManager.ConnectionStrings["HillView"].ConnectionString;
            container.RegisterType<LogErrorService>(new InjectionConstructor(connection_string));
            container.RegisterType<WebInfoService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<ImageService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<AccountService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<PageService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<SectionTypeService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<SectionService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<SectionItemService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<ButtonService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<PageSectionService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<AdminMenuService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<TestimonialService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<UIPageService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<UIPageService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));
            container.RegisterType<ContactService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>(), new ResolvedParameter<EmailService>()));
            container.RegisterType<ContactUsService>(new InjectionConstructor(connection_string, new ResolvedParameter<LogErrorService>()));


            container.RegisterType<AdminController>();
            container.RegisterType<AccountController>();
            container.RegisterType<HomeController>();
            container.RegisterType<JobsController>();
            container.RegisterType<ServicesController>();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}
