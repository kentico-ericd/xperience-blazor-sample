using BlazorApp.Models;
using BlazorApp.Services;
using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;
using CMS.Helpers;
using Kentico.Content.Web.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BlazorApp.Services {
    public class ProductService : IProductService
    {
        private readonly ISiteService siteService;
        private readonly ICalculationService calculationService;
        private readonly IConfiguration config;

        public ProductService(ISiteService siteService, ICalculationService calculationService, IConfiguration config)
        {
            this.siteService = siteService;
            this.calculationService = calculationService;
            this.config = config;
        }
        public ProductViewModel GetViewModel(Product product)
        {
            var dimensions = config.GetValue<int>("AppSettings:CardImageDimensions");
            var defaultImage = config.GetValue<string>("AppSettings:DefaultProductImage");
            var image = string.IsNullOrEmpty(product.SKU.SKUImagePath) ? defaultImage :
                URLHelper.ResolveUrl(
                    new FileUrl(product.SKU.SKUImagePath, true)
                        .WithSizeConstraint(SizeConstraint.Size(dimensions, dimensions))
                        .RelativePath);
            var status = product.SKUProduct.PublicStatus == null ? "" :
                product.SKUProduct.PublicStatus.PublicStatusDisplayName;
            var icon = string.IsNullOrEmpty(product.CardIconClass) ?
                config.GetValue<string>("AppSettings:DefaultCardIcon") : product.CardIconClass;

            return CacheHelper.Cache((cs) => {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"nodeid|${product.NodeID}");

                return new ProductViewModel
                {
                    Product = product,
                    Image = image,
                    StatusName = status,
                    IconClass = icon,
                    Prices = calculationService.CalculatePrice(product.SKU),
                    Available = !product.SKU.SKUSellOnlyAvailable || product.SKU.SKUAvailableItems > 0,
                    AccessoryOptions = GetAccessoryOptions(product.SKU.SKUID),
                    TextOptions = GetTextOptions(product.SKU.SKUID),
                    NonVariantAttributeOptions = GetNonVariantAttributeOptions(product.SKU.SKUID),
                    Variants = GetVariants(product.SKU.SKUID)
                };
            }, new CacheSettings(TimeSpan.FromMinutes(10).TotalMinutes, $"ProductService|{product.NodeID}"));
        }

        public IEnumerable<OptionViewModel> GetTextOptions(int SKUID)
        {
            var skuOptionCategories = SKUOptionCategoryInfo.Provider.Get().WhereEquals("SKUID", SKUID);
            Dictionary<int, string> categoryNames;
            var textQueries = GetQueries(SKUID, skuOptionCategories, OptionCategoryTypeEnum.Text, out categoryNames);
            return OptionViewModel.GetViewModels(textQueries, categoryNames);
        }

        public IEnumerable<OptionViewModel> GetAccessoryOptions(int SKUID)
        {
            //Get all the SKUOptionCategory bindings for the given SKU
            var skuOptionCategories = SKUOptionCategoryInfo.Provider.Get().WhereEquals("SKUID", SKUID);
            Dictionary<int, string> categoryNames;
            var accessoryQueries = GetQueries(SKUID, skuOptionCategories, OptionCategoryTypeEnum.Products, out categoryNames);
            return OptionViewModel.GetViewModels(accessoryQueries, categoryNames);
        }

        public IEnumerable<Product> GetVariants(int SKUID)
        {
            List<Product> variants = new List<Product>();
            ObjectQuery<SKUInfo> query = VariantHelper.GetVariants(SKUID);
            foreach (SKUInfo sku in query)
            {
                Product p = GetProduct(sku.SKUID);
                variants.Add(p);
            }
            return variants;
        }

        public Product GetProduct(int SKUID)
        {
            return ProductProvider.GetProducts()
                .WhereEquals("NodeSKUID", SKUID)
                .TopN(1)
                .FirstOrDefault();
        }

        public IEnumerable<ProductViewModel> GetFeaturedProducts(int count)
        {
            var featuredStatus = PublicStatusInfo.Provider.Get("Featured", siteService.CurrentSite.SiteID);
            var featuredSKUs = SKUInfo.Provider.Get()
                .WhereEquals("SKUPublicStatusID", featuredStatus.PublicStatusID)
                .TopN(count)
                .AsSingleColumn("SKUID");

            return ProductProvider.GetProducts()
                .WhereIn("NodeSKUID", featuredSKUs)
                .TypedResult
                .Select(p => GetViewModel(p));
        }

        public IEnumerable<SKUOptionCategoryInfo> GetOtherSKUOptionCategories(int SKUID)
        {
            //find the IDs of variants associated with the given product
            IDataQuery variantIDs = VariantHelper.GetVariants(SKUID)
                .AsSingleColumn("SKUID");

            //find the IDs of the options associated with those variants
            IDataQuery variantOptionIDs = VariantOptionInfo.Provider.Get()
                .WhereIn("VariantSKUID", variantIDs)
                .AsSingleColumn("OptionSKUID");

            //find the IDs of the categories associated with the variants
            IDataQuery variantCategoryIDs = SKUInfo.Provider.Get()
                .Columns("SKUOptionCategoryID")
                .Distinct(true)
                .WhereIn("SKUID", variantOptionIDs)
                .AsSingleColumn("SKUOptionCategoryID");

            //find the IDs of the categories associated with the parent sku, but not the variants.
            return SKUOptionCategoryInfo.Provider.Get()
                .WhereEquals("SKUID", SKUID)
                .And()
                .WhereNotIn("CategoryID", variantCategoryIDs);
        }

        public IEnumerable<ObjectQuery<SKUInfo>> GetQueries(int SKUID, IEnumerable<SKUOptionCategoryInfo> SKUOptionCategories, OptionCategoryTypeEnum type, out Dictionary<int, string> categoryNames)
        {
            categoryNames = new Dictionary<int, string>();
            List<ObjectQuery<SKUInfo>> queries = new List<ObjectQuery<SKUInfo>>();
            foreach (SKUOptionCategoryInfo soci in SKUOptionCategories)
            {
                //Get the OptionCategory indicated by the SKUOptionCategory binding
                var cat = OptionCategoryInfo.Provider.Get(soci.CategoryID);

                //Check whether it is the correct type
                if (cat.CategoryType == type)
                {
                    //add the display name to the collection for later
                    if (!categoryNames.ContainsKey(cat.CategoryID))
                    {
                        categoryNames.Add(cat.CategoryID, cat.CategoryDisplayName);
                    }

                    if (!soci.AllowAllOptions)
                    {
                        //add allowed options to the list of queries
                        queries.Add(
                        SKUInfo.Provider.Get()
                            .WhereEquals("SKUOptionCategoryID", cat.CategoryID)
                            .And()
                            .WhereIn("SKUID", SKUAllowedOptionInfo.Provider.Get()
                                                .WhereEquals("SKUID", SKUID)
                                                .AsSingleColumn("OptionSKUID"))
                        );
                    }
                    else
                    {
                        //add all accessories to the list of queries
                        queries.Add(
                        SKUInfo.Provider.Get()
                            .WhereEquals("SKUOptionCategoryID", cat.CategoryID)
                        );
                    }
                }

            }
            return queries;
        }

        public IEnumerable<OptionViewModel> GetNonVariantAttributeOptions(int SKUID)
        {
            List<OptionViewModel> ovms = new List<OptionViewModel>();
            //Get variants of the given SKU (VariantHelper)
            var variants = VariantHelper.GetVariants(SKUID);
            //Find the Variant Options(VariantOptionInfo objects) for those variants
            foreach (SKUInfo v in variants)
            {
                IEnumerable<VariantOptionInfo> variantOptionInfos = VariantOptionInfo.Provider.Get()
                    .WhereEquals("VariantSKUID", v.SKUID);
                foreach (VariantOptionInfo voi in variantOptionInfos)
                {
                    //Get the categories associated with those options , by looking at SKUOptionCategoryId of the SKU
                    //referenced by the VariantOptionInfo (OptionSKUID)
                    var category = SKUOptionCategoryInfo.Provider.Get()
                        .WhereIn("SKUCategoryID",
                            SKUInfo.Provider.Get()
                            .WhereEquals("SKUID", voi.OptionSKUID)
                            .AsSingleColumn("SKUOptionCategoryID")
                        ).FirstOrDefault();
                    //Get option categories (SKUOptionCategoryInfo objects associated with SKU) which are not in
                    //those categories (WhereNotIn will come in handy here)
                    var skuOptionCategories = SKUOptionCategoryInfo.Provider.Get()
                        .WhereEquals("SKUID", SKUID)
                        .WhereNotEquals("CategoryID", category.CategoryID);
                    foreach (SKUOptionCategoryInfo optionCategory in skuOptionCategories)
                    {
                        //Get the Option Category object (OptionCategoryInfo) for each one
                        var oci = OptionCategoryInfo.Provider.Get(optionCategory.CategoryID);
                        //Check if it’s an ATTRIBUTE category, and if so…
                        if (oci.CategoryType == OptionCategoryTypeEnum.Attribute)
                        {
                            //Figure out which options from that category are allowed for the SKU
                            //Add either all options, or the allowed options, (or a way to get them) to a list
                            IEnumerable<SKUInfo> allowedSKUs;
                            if (!optionCategory.AllowAllOptions)
                            {
                                allowedSKUs = SKUInfo.Provider.Get()
                                    .WhereEquals("SKUOptionCategoryID", oci.CategoryID)
                                    .And()
                                    .WhereIn("SKUID", SKUAllowedOptionInfo.Provider.Get()
                                                        .WhereEquals("SKUID", SKUID)
                                                        .AsSingleColumn("OptionSKUID")
                                    );
                            }
                            else
                            {
                                allowedSKUs = SKUInfo.Provider.Get()
                                    .WhereEquals("SKUOptionCategoryID", oci.CategoryID);
                            }
                            //Package/use this data somehow
                            foreach (SKUInfo sku in allowedSKUs)
                            {
                                if (ovms.Where(model => model.SKUID == sku.SKUID).Count() == 0)
                                    ovms.Add(new OptionViewModel()
                                    {
                                        SKUID = sku.SKUID,
                                        Adjustment = sku.SKUPrice,
                                        Name = sku.SKUName,
                                        OptionCategoryName = oci.CategoryDisplayName
                                    });
                            }
                        }
                    }
                }
            }
            return ovms;
        }
    }
}