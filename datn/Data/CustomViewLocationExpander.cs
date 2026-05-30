using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;
using System.Linq;

namespace datn.Data
{
    public class CustomViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            var locations = viewLocations.ToList();

            // Landing folder
            locations.Insert(0, "/Views/Landing/{1}/{0}.cshtml");
            locations.Insert(0, "/Views/Landing/Shared/{0}.cshtml");

            // Dashboard sub-folders by roles (Admin, Teacher, Parent)
            string[] roles = new[] { "Parent", "Teacher", "Admin" };
            foreach (var role in roles)
            {
                locations.Insert(0, "/Views/Dashboard/" + role + "/{1}/{0}.cshtml");
                locations.Insert(0, "/Views/Dashboard/" + role + "/Shared/{0}.cshtml");
            }

            // Common folder
            locations.Insert(0, "/Views/Common/{1}/{0}.cshtml");
            locations.Insert(0, "/Views/Common/Shared/{0}.cshtml");

            return locations;
        }
    }
}
