using System.Collections.Generic;
using System.Linq;
using CMS.DataEngine;
using CMS.Ecommerce;

namespace BlazorApp.Models
{
    public class OptionViewModel
    {
        public int SKUID { get; set; }
        public decimal Adjustment { get; set; }
        public string Name { get; set; }
        public string OptionCategoryName { get; set; }

        public static IEnumerable<OptionViewModel> GetViewModels(IEnumerable<ObjectQuery<SKUInfo>> queries, Dictionary<int, string> categoryNames)
        {
            ObjectQuery<SKUInfo> attributeQ = new ObjectQuery<SKUInfo>().WhereEquals("SKUName", "ThisProductDoesNotExist");
            if (queries.Count() > 0)
            {
                attributeQ = queries.Aggregate((currentQuery, next) => currentQuery.Union(next));
            }
            List<OptionViewModel> ovms = new List<OptionViewModel>();
            foreach (SKUInfo sku in attributeQ)
            {
                ovms.Add(new OptionViewModel { OptionCategoryName = categoryNames[sku.SKUOptionCategoryID], Name = sku.SKUName, Adjustment = sku.SKUPrice, SKUID = sku.SKUID });
            }
            return ovms;
        }
    }
}