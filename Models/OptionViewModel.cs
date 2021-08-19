using System.Collections.Generic;
using System.Linq;
using CMS.DataEngine;
using CMS.Ecommerce;
using Newtonsoft.Json;

namespace BlazorApp.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OptionViewModel
    {
        [JsonProperty]
        public int SKUID { get; set; }
        public string Name { get; set; }
        public decimal Adjustment { get; set; }
        [JsonProperty]
        public object Value { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDisplayName { get; set; }
        public int CategoryID { get; set; }
        [JsonProperty]
        public OptionCategoryTypeEnum CategoryType { get; set; }

        public static OptionViewModel GetOptionViewModel(SKUInfo sku) {
            return new OptionViewModel() {
                Adjustment = sku.SKUPrice,
                SKUID = sku.SKUID,
                Name = sku.SKUName,
                CategoryName = sku.SKUOptionCategory.CategoryName,
                CategoryDisplayName = sku.SKUOptionCategory.CategoryLiveSiteDisplayName,
                CategoryID = sku.SKUOptionCategoryID,
                CategoryType = sku.SKUOptionCategory.CategoryType
            };
        }

        public static IEnumerable<OptionViewModel> GetOptionViewModels(IEnumerable<ObjectQuery<SKUInfo>> queries, Dictionary<int, string> categoryNames)
        {
            ObjectQuery<SKUInfo> attributeQ = new ObjectQuery<SKUInfo>().WhereEquals("SKUName", "ThisProductDoesNotExist");
            if (queries.Count() > 0)
            {
                attributeQ = queries.Aggregate((currentQuery, next) => currentQuery.Union(next));
            }
            List<OptionViewModel> ovms = new List<OptionViewModel>();
            foreach (SKUInfo sku in attributeQ)
            {
                ovms.Add(GetOptionViewModel(sku));
            }
            return ovms;
        }
    }
}