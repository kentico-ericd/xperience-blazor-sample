using BlazorApp.Models;

namespace BlazorApp.Services {
    public interface ISearchService {
        public SearchResultsModel Search(string searchText, int pageNumber = 1);
    }
}