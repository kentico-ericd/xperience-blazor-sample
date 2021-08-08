using System.Collections.Generic;
using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;

namespace BlazorApp.Models
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public string URL { get; set; }
        public string Image { get; set; }
        public string StatusName { get; set; }
        public string IconClass { get; set; }
        public ProductCatalogPrices Prices { get; set; }
        public bool Available { get; set; }
        public IEnumerable<OptionViewModel> AccessoryOptions { get; set; }
        public IEnumerable<OptionViewModel> TextOptions { get; set; }
        public IEnumerable<OptionViewModel> NonVariantAttributeOptions { get; set; }
        public IEnumerable<Product> Variants { get; set; }
    }
}