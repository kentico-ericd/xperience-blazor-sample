using System;
using BlazorApp.Models;
using CMS.Membership;
using CMS.Search;
using Microsoft.Extensions.Configuration;

namespace BlazorApp.Services
{
    public class SearchService : ISearchService
    {
        private string mIndex;
        private int mPageSize;
        private int mMaxResults = 100;
        private int mGroupSize = 5;

        public SearchService(IConfiguration config) {
            mIndex = config.GetValue<string>("AppSettings:SearchIndexCodeName");
            mPageSize = config.GetValue<int>("AppSettings:SearchResultsPageSize");
        }

        public SearchResultsModel Search(string searchText, int page, string extraCondition)
        {
            if(string.IsNullOrEmpty(searchText)) {
                searchText = "";
            }
            searchText = searchText.Trim();

            // Get positions and ranges for search method
            page = Math.Max(page, 1);
            int startPosition = 0;
            int numberOfProceeded = 100;
            int displayResults = 100;
            if (mPageSize != 0 && mGroupSize != 0)
            {
                startPosition = (page - 1) * mPageSize;
                numberOfProceeded = (((page / mGroupSize) + 1) * mPageSize * mGroupSize) + mPageSize;
                displayResults = mPageSize;
            }

            if ((mMaxResults > 0) && (numberOfProceeded > mMaxResults))
            {
                numberOfProceeded = mMaxResults;
            }

            // Prepare search text
            var docCondition = new DocumentSearchCondition("", "en-US", "en-US", false);
            var condition = new SearchCondition(extraCondition, SearchModeEnum.AllWords, SearchOptionsEnum.FullSearch, docCondition, false);
            var searchFor = SearchSyntaxHelper.CombineSearchCondition(searchText, condition);
            SearchParameters parameters = new SearchParameters
            {
                SearchFor = searchFor,
                SearchSort = "##SCORE##",
                Path = "/%",
                ClassNames = "",
                CurrentCulture = "en-US",
                DefaultCulture = "en-US",
                User = MembershipContext.AuthenticatedUser,
                SearchIndexes = mIndex,
                StartingPosition = startPosition,
                DisplayResults = displayResults,
                NumberOfProcessedResults = numberOfProceeded,
                NumberOfResults = 0,
            };

            var result = SearchHelper.Search(parameters);
            return new SearchResultsModel() {
                Query = searchText,
                Items = result.Items,
                CurrentPage = page,
                NumberOfPages = (int)Math.Ceiling(result.TotalNumberOfResults / (double)mPageSize)
            };
        }
    }
}