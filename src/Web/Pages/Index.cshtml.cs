using System;
using System.Transactions;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.eShopWeb.Web.ViewModels;

namespace Microsoft.eShopWeb.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ICatalogViewModelService _catalogViewModelService;

    public IndexModel(ICatalogViewModelService catalogViewModelService)
    {
        _catalogViewModelService = catalogViewModelService;
    }

    public CatalogIndexViewModel CatalogModel { get; set; } = new CatalogIndexViewModel();

    public async Task OnGet(CatalogIndexViewModel catalogModel, int? pageId)
    {
        // transaction 1
        ITransaction catalogitems = Elastic.Apm.Agent.Tracer.CurrentTransaction;

        await catalogitems.CaptureSpan("Get CatalogItems", ApiConstants.ActionExec, async () =>
       // ITransaction span = Transaction.CaptureSpan("Select FROM customer", ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery
        {
            CatalogModel = await _catalogViewModelService.GetCatalogItems(pageId ?? 0, Constants.ITEMS_PER_PAGE, catalogModel.BrandFilterApplied, catalogModel.TypesFilterApplied);
            
          //  span.End();

        });

    }

}
