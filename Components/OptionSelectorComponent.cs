using BlazorApp.Models;
using CMS.Ecommerce;
using CMS.Helpers;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;

namespace BlazorApp.Components
{
    public abstract class OptionSelectorComponent : ComponentBase
    {
        [Parameter]
        public IEnumerable<OptionViewModel> Options { get; set; }
        [Parameter]
        public OptionCategoryInfo Category { get; set; }
        [Parameter]
        public CurrencyInfo Currency { get; set; }
        [Parameter]
        public bool IsVertical { get; set; } = true;
        [Parameter]
        public EventCallback<(OptionViewModel, object, OptionCategoryInfo)> OnOptionSelected { get; set; }

        protected IEnumerable<int> DefaultOptions
        {
            get
            {
                return Category.CategoryDefaultOptions.Split(",").Select(o => ValidationHelper.GetInteger(o, 0));
            }
        }

        // Contains a list of options that were selected by default, to fire at the first render
        protected List<OptionViewModel> EventsToFire = new List<OptionViewModel>();

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                var list = new OptionViewModel[EventsToFire.Count];
                EventsToFire.CopyTo(list);
                foreach (var option in list)
                {
                    OnOptionSelected.InvokeAsync((option, option.Value, Category));
                }
            }
        }
    }
}
