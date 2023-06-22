using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Elastic.Apm.Api;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        ITransaction checkout_post1 = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        //await checkout_post1.CaptureSpan("Update Basket Item Specification", ApiConstants.ActionExec, async () => { });

        var basketSpec = new BasketWithItemsSpecification(basketId);

        //await checkout_post1.CaptureSpan("Update Basket Spec", ApiConstants.ActionExec, async () => { });
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);


        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());

        //await checkout_post1.CaptureSpan("Update CatalogItemsSpecification", ApiConstants.ActionExec, async () => { });
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);


        await checkout_post1.CaptureSpan("Complete Payment", ApiConstants.ActionExec, async () => {
        var items = basket.Items.Select(basketItem =>
        {

            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;

        }).ToList();



       // await checkout_post1.CaptureSpan("Update new Order", ApiConstants.ActionExec, async () => { 
        var order = new Order(basket.BuyerId, shippingAddress, items);


        await _orderRepository.AddAsync(order);
        });
    }
}
