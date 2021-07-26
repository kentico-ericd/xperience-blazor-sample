using System.Collections.Generic;
using CMS.Search;

namespace BlazorApp.Models {
    public class SearchResultsModel {
        public string Query { get; set; }
        public List<SearchResultItem> Items { get; set; }
        public int CurrentPage { get; set; }
        public int NumberOfPages { get; set; }
    }
}