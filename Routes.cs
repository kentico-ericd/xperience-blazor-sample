using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;

namespace BlazorApp {
    public static class Routes {
        public const string Home = "/";
        public const string Store = "/Store";
        public const string StoreWithCategory = "/Store/{Category}";
        public const string Product = "/Store/Detail/{Alias}";
        public const string AddItem = "/Store/AddItem";
        public const string Cart = "/Cart";

        public static string GetCategoryURL(DepartmentInfo d) {
            return Routes.StoreWithCategory.Replace(
                "{Category}",
                d.DepartmentName
            );
        }

        public static string GetProductURL(Product p) {
            return Routes.Product.Replace("{Alias}", p.NodeAlias);
        }
    }
}