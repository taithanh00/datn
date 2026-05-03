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
            // Các thư mục con mới của chúng ta
            string[] customFolders = new[] { "Landing", "Dashboard", "Common" };

            var locations = viewLocations.ToList();
            foreach (var folder in customFolders)
            {
                // Thêm đường dẫn tìm kiếm cho từng thư mục con
                locations.Insert(0, "/Views/" + folder + "/{1}/{0}.cshtml");
                locations.Insert(0, "/Views/" + folder + "/Shared/{0}.cshtml");
            }

            return locations;
        }
    }
}
