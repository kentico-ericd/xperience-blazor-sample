using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Kentico.Web.Mvc;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using BlazorApp.Services;

namespace BlazorApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor()
                .AddCircuitOptions(options => { options.DetailedErrors = true; });
            services.AddAuthentication();
            services.AddSingleton<ICalculationService, CalculationService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<ISearchService, SearchService>();
            services.AddTransient<ICartService, CartService>();

            services.AddKentico(features =>
                features.UsePageBuilder()
            )
            .SetAdminCookiesSameSiteNone();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
           
            app.UseStaticFiles();
            app.UseSession();
            /*app.Use(async (ctx, next) => {
                var pageRetriever = Service.Resolve<IPageRetriever>();
                var pageDataContextInitializer = Service.Resolve<IPageDataContextInitializer>();
                var page = pageRetriever.Retrieve<TreeNode>(query => query
                            .Path("/", PathTypeEnum.Single))
                            .FirstOrDefault();
                pageDataContextInitializer.Initialize(page);
                
                await next();
            });*/
            app.UseKentico();
            app.UseCookiePolicy();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Kentico().MapRoutes();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/Root/Root");
            });
        }
    }
}
