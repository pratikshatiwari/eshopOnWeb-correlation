using Ardalis.GuardClauses;
using Elastic.Apm;
using Elastic.Apm.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.eShopWeb.Web.Features.MyOrders;
using Microsoft.eShopWeb.Web.Features.OrderDetails;


namespace Microsoft.eShopWeb.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Authorize] // Controllers that mainly require Authorization still use Controller/View; other pages use Pages
[Route("[controller]/[action]")]
public class OrderController : Controller
{
    private readonly IMediator _mediator;
   

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        //transaction1
        //span1
        ITransaction trans1 = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        await trans1.CaptureSpan("Get MyOrder", ApiConstants.ActionExec, async () => await Task.Delay(30));

        //var span = Elastic.Apm.Agent.Tracer.CurrentSpan;
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        var viewModel = await _mediator.Send(new GetMyOrders(User.Identity.Name, trans1));

        //endtrans1
        trans1.End();
        return View(viewModel);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> Detail(int orderId)
    {


        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
                  
        var viewModel = await _mediator.Send(new GetOrderDetails(User.Identity.Name, orderId));

        if (viewModel == null)
        {
            return BadRequest("No such order found for this user.");
        }

        return View(viewModel);



        }
}
