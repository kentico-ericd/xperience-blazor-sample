using System.Collections.Generic;
using BlazorApp.Models;
using CMS.DataEngine;
using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;

namespace BlazorApp.Services {
    public interface IProductService {
        public abstract ProductViewModel GetViewModel(Product product);
        public abstract IEnumerable<OptionViewModel> GetTextOptions(int SKUID);
        public abstract IEnumerable<OptionViewModel> GetAccessoryOptions(int SKUID);
        public abstract IEnumerable<Product> GetVariants(int SKUID);
        public abstract Product GetProduct(int SKUID);
        public abstract IEnumerable<ProductViewModel> GetFeaturedProducts(int count);
        public abstract IEnumerable<SKUOptionCategoryInfo> GetOtherSKUOptionCategories(int SKUID);
        public abstract IEnumerable<ObjectQuery<SKUInfo>> GetQueries(int SKUID, IEnumerable<SKUOptionCategoryInfo> SKUOptionCategories, OptionCategoryTypeEnum type, out Dictionary<int, string> categoryNames);
        public abstract IEnumerable<OptionViewModel> GetNonVariantAttributeOptions(int SKUID);
    }
}