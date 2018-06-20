using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace DiffB64
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "Default",
            //    routeTemplate: "{controller}/{action}/{id}",
            //    defaults: new { controller = "Home", action = "Index", id = RouteParameter.Optional }
            //);
            config.Routes.MapHttpRoute(
                name: "DiffB64Api",
                routeTemplate: "v1/{controller}/{id}/{pos}",
                defaults: new { id = RouteParameter.Optional, pos = RouteParameter.Optional }
            );
        }
    }
}
