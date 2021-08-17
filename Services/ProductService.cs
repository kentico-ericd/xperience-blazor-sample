using BlazorApp.Models;
using BlazorApp.Services;
using CMS.Base;
using CMS.DataEngine;
using CMS.DocumentEngine.Types.Blazor;
using CMS.Ecommerce;
using CMS.Helpers;
using CMS.Search;
using Kentico.Content.Web.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BlazorApp.Services
{
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

        public ProductViewModel GetViewModel(SearchResultItem searchItem)
        {
            var id = searchItem.SearchDocument.Get("nodeid").ToInteger(0);
            if (id > 0)
            {
                var product = ProductProvider.GetProduct(id, "en-us", siteService.CurrentSite.SiteName);
                return GetViewModel(product);
            }
            else
            {
                return null;
            }
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

            return CacheHelper.Cache((cs) =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency($"nodeid|${product.NodeID}");

                return new ProductViewModel
                {
                    Product = product,
                    URL = Routes.Product.Replace("{Alias}", product.NodeAlias),
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

        public IEnumerable<SKUInfo> GetVariants(int SKUID)
        {
            List<SKUInfo> variants = new List<SKUInfo>();
            ObjectQuery<SKUInfo> query = VariantHelper.GetVariants(SKUID);
            variants.AddRange(query);

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

            //find the categories associated with the parent sku, but not the variants.
            var skuOptionCategories = SKUOptionCategoryInfo.Provider.Get()
                .WhereEquals("SKUID", SKUID)
                .And()
                .WhereNotIn("CategoryID", variantCategoryIDs);

            Dictionary<int, string> categoryNames = new Dictionary<int, string>();
            List<ObjectQuery<SKUInfo>> queries = new List<ObjectQuery<SKUInfo>>();
            foreach (SKUOptionCategoryInfo soci in skuOptionCategories)
            {
                //Get the OptionCategory indicated by the SKUOptionCategory binding
                var cat = OptionCategoryInfo.Provider.Get(soci.CategoryID);

                //Check whether it is the correct type
                if (cat.CategoryType == OptionCategoryTypeEnum.Attribute)
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
                        //add all options to the list of queries
                        queries.Add(
                        SKUInfo.Provider.Get()
                            .WhereEquals("SKUOptionCategoryID", cat.CategoryID)
                        );
                    }
                }
            }
            //create a sku query that is empty (negative IDs can't exist, so this will return empty)
            ObjectQuery<SKUInfo> finalQuery = new ObjectQuery<SKUInfo>().WhereEquals("SKUID", -4206969);
            if (queries.Count() > 0)
            {
                //use it to combine all the other ones
                finalQuery = queries.Aggregate((currentQuery, next) => currentQuery.Union(next));
            }

            List<OptionViewModel> ovms = new List<OptionViewModel>();
            foreach (SKUInfo sku in finalQuery)
            {
                //make option view models based on the results
                ovms.Add(new OptionViewModel {
                    OptionCategoryName = categoryNames[sku.SKUOptionCategoryID],
                    Name = sku.SKUName,
                    Adjustment = sku.SKUPrice,
                    SKUID = sku.SKUID,
                    OptionCategoryID = sku.SKUOptionCategoryID
                });
            }
            return ovms;
        }

        public IEnumerable<OptionCategoryInfo> GetOptionCategoryInfos(IEnumerable<OptionViewModel> options)
        {
            var distinctNames = options
                .GroupBy(o => o.OptionCategoryName)
                .Select(g => g.First().OptionCategoryName)
                .ToList();
            return OptionCategoryInfo.Provider.Get()
                .WhereIn("CategoryName", distinctNames);
        }
    }
}