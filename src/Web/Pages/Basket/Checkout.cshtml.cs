using Ardalis.GuardClauses;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Interfaces;

namespace Microsoft.eShopWeb.Web.Pages.Basket;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private string? _username = null;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IAppLogger<CheckoutModel> _logger;

    public CheckoutModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
        IAppLogger<CheckoutModel> logger)
    {
        _basketService = basketService;
        _signInManager = signInManager;
        _orderService = orderService;
        _basketViewModelService = basketViewModelService;
        _logger = logger;
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        await SetBasketModelAsync();
    }

    public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
    {

        //span1
        ITransaction checkout_post1 = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        //await checkout_post1.CaptureSpan("Checkout#Post", ApiConstants.ActionExec, async () => { });

        try
        {
            await checkout_post1.CaptureSpan("Set basket model", ApiConstants.SubtypeHttp, async () =>
            {
                await SetBasketModelAsync();
            });


            //await checkout_post1.CaptureSpan("ModelState", ApiConstants.ActionExec, async () => { });
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            await checkout_post1.CaptureSpan("Add Order Quanitity", ApiConstants.ActionExec, async () =>
            {
                var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
                //});

                //await checkout_post1.CaptureSpan("Update Order quanity", ApiConstants.ActionExec, async () =>
                //{
                await _basketService.SetQuantities(BasketModel.Id, updateModel);
            });

            await checkout_post1.CaptureSpan("create order", ApiConstants.SubTypeInternal, async () => {
                await _orderService.CreateOrderAsync(BasketModel.Id, new Address("123 Main St.", "Kent", "OH", "United States", "44240"));
            });

            await checkout_post1.CaptureSpan("Complete Purchase & Delete product from Basket ", ApiConstants.ActionExec, async () => {
                await _basketService.DeleteBasketAsync(BasketModel.Id);
            });
        }
        catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
        {
            checkout_post1.CaptureException(emptyBasketOnCheckoutException);

            checkout_post1.CaptureErrorLog(new ErrorLog(emptyBasketOnCheckoutException.Message));
            _logger.LogWarning(emptyBasketOnCheckoutException.Message);

            await checkout_post1.CaptureSpan("Redirect to /Basket/Index", ApiConstants.ActionExec, async () => {

                //checkout_post1.CaptureError("Exception in Checkout : Redirect to /Basket/Index", emptyBasketOnCheckoutException.Message, null);
                //Redirect to Empty Basket page
                return RedirectToPage("/Basket/Index");
            });
        }


        //checkout_post1.End();
        return RedirectToPage("Success");


        //return 42;



    }

    private async Task SetBasketModelAsync()
    {
        //ITransaction checkout_post1 = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        //await checkout_post1.CaptureSpan("Set Basket for User ", ApiConstants.ActionQuery, async () => { 

        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        if (_signInManager.IsSignedIn(HttpContext.User))
        {
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
        }
        else
        {
            GetOrSetBasketCookieAndUserName();
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username!);
        }
        //});
    }

    private void GetOrSetBasketCookieAndUserName()
    {
        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            _username = Request.Cookies[Constants.BASKET_COOKIENAME];
        }
        if (_username != null) return;

        _username = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
    }
}
