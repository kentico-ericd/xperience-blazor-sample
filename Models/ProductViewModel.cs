using System.Collections.Generic;
using CMS.DocumentEngine.Types.Blazor;

namespace BlazorApp.Models
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public IEnumerable<OptionViewModel> AccessoryOptions { get; set; }
        public IEnumerable<OptionViewModel> TextOptions { get; set; }
        public IEnumerable<OptionViewModel> NonVariantAttributeOptions { get; set; }
        public IEnumerable<Product> Variants { get; set; }
    }
}