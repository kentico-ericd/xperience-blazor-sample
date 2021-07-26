using System;
using System.Collections.Generic;
using BlazorApp.Models;
using CMS.Membership;
using CMS.Search;

namespace BlazorApp.Services
{
    public class SearchService : ISearchService
    {
        private const string INDEX_NAME = "Products";
        private const int PAGE_SIZE = 5;

        public SearchResultsModel Search(string searchText, int page)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new SearchResultsModel() {
                    Query = searchText,
                    Items = new List<SearchResultItem>()
                };
            }

            // Validate page number (starting from 1)
            page = Math.Max(page, 1);

            var searchParameters = SearchParameters.PrepareForPages(searchText, new[] { INDEX_NAME }, page, PAGE_SIZE, UserInfoProvider.AdministratorUser);
            var result = SearchHelper.Search(searchParameters);
            return new SearchResultsModel() {
                Query = searchText,
                Items = result.Items,
                CurrentPage = page,
                NumberOfPages = (int)Math.Ceiling(result.TotalNumberOfResults / (double)PAGE_SIZE)
            };
        }
    }
}