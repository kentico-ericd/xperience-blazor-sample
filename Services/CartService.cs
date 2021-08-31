using System;
using System.Collections.Generic;
using BlazorApp.Models;
using CMS.Ecommerce;
using CMS.Helpers;
using CMS.SiteProvider;

namespace BlazorApp.Services
{
    public class CartService : ICartService
    {
        private readonly IShoppingService shoppingService;
        private CustomerInfo mCustomer;

        public CartService(IShoppingService shoppingService)
        {
            this.shoppingService = shoppingService;
        }

        public void AddToCart(int skuid, IEnumerable<OptionViewModel> options)
        {
            List<ShoppingCartItemParameters> parameters = new List<ShoppingCartItemParameters>();

            foreach (OptionViewModel option in options)
            {
                switch (option.CategoryType)
                {
                    case OptionCategoryTypeEnum.Text:
                        parameters.Add(new ShoppingCartItemParameters { SKUID = option.SKUID, Text = ValidationHelper.GetString(option.Value, "") });
                        break;
                    case OptionCategoryTypeEnum.Products:
                        parameters.Add(new ShoppingCartItemParameters { SKUID = option.SKUID, Quantity = 1 });
                        break;
                    case OptionCategoryTypeEnum.Attribute:
                        parameters.Add(new ShoppingCartItemParameters { SKUID = option.SKUID });
                        break;
                }
            }

            shoppingService.AddItemToCart(new ShoppingCartItemParameters(skuid, 1, parameters));
        }

        public string FormatPriceForSelector(decimal price, CurrencyInfo currency, bool isVariant)
        {
            if (price > 0)
            {
                return $"({(isVariant ? "" : "+")}{FormatPrice(price, currency)})";
            }

            return "";
        }

        public string FormatPrice(decimal price, CurrencyInfo currency)
        {
            return String.Format(currency.CurrencyFormatString, price);
        }

        public CustomerInfo GetCurrentCustomer()
        {
            if (mCustomer == null)
            {
                // Try get from cart
                mCustomer = shoppingService.GetCurrentCustomer();
                if (mCustomer == null)
                {
                    // Create new customer
                    CustomerInfo newCustomer = new CustomerInfo
                    {
                        CustomerFirstName = "",
                        CustomerLastName = "",
                        CustomerEmail = "",
                        CustomerSiteID = SiteContext.CurrentSiteID
                    };


                    shoppingService.SetCustomer(newCustomer);
                    mCustomer = newCustomer;
                }
            }

            return mCustomer;
        }
    }
}