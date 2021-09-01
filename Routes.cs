using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;

namespace BlazorApp
{
    public static class Routes
    {
        public const string Home = "/";
        public const string Store = "/Store";
        public const string StoreWithCategory = "/Store/{Category}";
        public const string Product = "/Store/Detail/{Alias}";
        public const string AddItem = "/Store/AddItem";
        public const string Cart = "/Cart";
        public const string CheckoutPrefix = "/Checkout";
        public const string Address = CheckoutPrefix + "/Address";
        public const string Payment = CheckoutPrefix + "/Payment";
        public const string Shipping = CheckoutPrefix + "/Shipping";
        public static readonly string[] CheckoutProcess = { Address, Shipping, Payment };

        public static string GetCategoryURL(DepartmentInfo d)
        {
            return Routes.StoreWithCategory.Replace(
                "{Category}",
                d.DepartmentName
            );
        }

        public static string GetCheckoutStep(int index)
        {
            return Routes.CheckoutProcess[index];
        }

        public static string GetProductURL(Product p)
        {
            return Routes.Product.Replace("{Alias}", p.NodeAlias);
        }
    }
}