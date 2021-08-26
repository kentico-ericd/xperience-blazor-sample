using System.Collections.Generic;
using BlazorApp.Models;
using CMS.Ecommerce;

namespace BlazorApp.Services
{
    public interface ICartService
    {
        public abstract void AddToCart(int skuid, IEnumerable<OptionViewModel> options);
        public abstract string FormatPrice(decimal price, CurrencyInfo currency);
        public abstract string FormatPriceForSelector(decimal price, CurrencyInfo currency, bool isVariant = false);
    }
}