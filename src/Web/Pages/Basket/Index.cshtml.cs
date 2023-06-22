using Ardalis.GuardClauses;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.ViewModels;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

public class IndexModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IRepository<CatalogItem> _itemRepository;


    public IndexModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        IRepository<CatalogItem> itemRepository)
    {
        _basketService = basketService;
        _basketViewModelService = basketViewModelService;
        _itemRepository = itemRepository;
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();


    public async Task OnGet()
    {
        //var GetBasket = Elastic.Apm.Agent.Tracer.CurrentTransaction;

       // var GetBasket = Elastic.Apm.Agent.Tracer.CurrentSpan;
        //await GetBasket.CaptureSpan("Get Basket for User", ApiConstants.ActionExec, async () => { });

        //ITransaction GetBasket = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        var GetBasket = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        var asyncResult = await GetBasket.CaptureSpan("Get Basket for User", ApiConstants.ActionQuery, async (s) =>
        {

            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(GetOrSetBasketCookieAndUserName());
            //await Task.Delay(500); //sample async code
            return GetBasket;
        });
    }

    public async Task<IActionResult> OnPost(CatalogItemViewModel productDetails)
    {

        var PostBasket = Elastic.Apm.Agent.Tracer.CurrentTransaction;

            if (productDetails?.Id == null)
        {
            return RedirectToPage("/Index");
        }

        //var asyncResult = await PostBasket.CaptureSpan("Get Basket for User", ApiConstants.ActionQuery, async (s) => { });

        var item = await _itemRepository.GetByIdAsync(productDetails.Id);
            if (item == null)
            {
                return RedirectToPage("/Index");
            }

       
        await PostBasket.CaptureSpan("Add Items for User", ApiConstants.ActionQuery, async (s) => {
        var username = GetOrSetBasketCookieAndUserName();
        var basket = await _basketService.AddItemToBasket(username,
            productDetails.Id, item.Price);

        BasketModel = await _basketViewModelService.Map(basket);
        });
        return RedirectToPage();

    }

    public async Task OnPostUpdate(IEnumerable<BasketItemViewModel> items)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        var basketView = await _basketViewModelService.GetOrCreateBasketForUser(GetOrSetBasketCookieAndUserName());
        var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
        var basket = await _basketService.SetQuantities(basketView.Id, updateModel);
        BasketModel = await _basketViewModelService.Map(basket);
    }

    private string GetOrSetBasketCookieAndUserName()
    {
        Guard.Against.Null(Request.HttpContext.User.Identity, nameof(Request.HttpContext.User.Identity));
        string? userName = null;

        if (Request.HttpContext.User.Identity.IsAuthenticated)
        {
            Guard.Against.Null(Request.HttpContext.User.Identity.Name, nameof(Request.HttpContext.User.Identity.Name));
            return Request.HttpContext.User.Identity.Name!;
        }

        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            userName = Request.Cookies[Constants.BASKET_COOKIENAME];

            if (!Request.HttpContext.User.Identity.IsAuthenticated)
            {
                if (!Guid.TryParse(userName, out var _))
                {
                    userName = null;
                }
            }
        }
        if (userName != null) return userName;

        userName = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions { IsEssential = true };
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, userName, cookieOptions);

        return userName;
    }
}
