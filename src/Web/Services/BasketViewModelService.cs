using Elastic.Apm.Api;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.eShopWeb.Web.Interfaces;
using Microsoft.eShopWeb.Web.Pages.Basket;

namespace Microsoft.eShopWeb.Web.Services;

public class BasketViewModelService : IBasketViewModelService
{
    private readonly IRepository<Basket> _basketRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IBasketQueryService _basketQueryService;
    private readonly IRepository<CatalogItem> _itemRepository;

    public BasketViewModelService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IUriComposer uriComposer,
        IBasketQueryService basketQueryService)
    {
        this._basketRepository = basketRepository;
        this._uriComposer = uriComposer;
        this._basketQueryService = basketQueryService;
        this._itemRepository = itemRepository;
    }

    public async Task<BasketViewModel> GetOrCreateBasketForUser(string userName)
    {
        ITransaction GetBasket = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        //await GetBasket.CaptureSpan("Get Basket", ApiConstants.ActionQuery);
        var model = await GetBasket.CaptureSpan("Get BasketWithItemsSpecification", ApiConstants.ActionExec, async () => { 
            var basketSpec = new BasketWithItemsSpecification(userName);
            var basket = (await _basketRepository.FirstOrDefaultAsync(basketSpec));
            
            if (basket == null)
            {
                return await CreateBasketForUser(userName);
            }

            var viewModel = await Map(basket);
            return viewModel;
        });
        return model;
    }

    

    private async Task<BasketViewModel> CreateBasketForUser(string userId)
    {



        var basket = new Basket(userId);
        await _basketRepository.AddAsync(basket);

        return new BasketViewModel()
        {
            BuyerId = basket.BuyerId,
            Id = basket.Id,
        };
    }

    private async Task<List<BasketItemViewModel>> GetBasketItems(IReadOnlyCollection<BasketItem> basketItems)
    {

        //span1
        //ITransaction trans1 = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        //await trans1.CaptureSpan("Get BasketItems", ApiConstants.ActionExec, async () => await Task.Delay(30));

        var catalogItemsSpecification = new CatalogItemsSpecification(basketItems.Select(b => b.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basketItems.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);

            var basketItemViewModel = new BasketItemViewModel
            {
                Id = basketItem.Id,
                UnitPrice = basketItem.UnitPrice,
                Quantity = basketItem.Quantity,
                CatalogItemId = basketItem.CatalogItemId,
                PictureUrl = _uriComposer.ComposePicUri(catalogItem.PictureUri),
                ProductName = catalogItem.Name
            };
            return basketItemViewModel;
        }).ToList();

        //trans1.End();

        return items;
    }

    public async Task<BasketViewModel> Map(Basket basket)
    {
        return new BasketViewModel()
        {
            BuyerId = basket.BuyerId,
            Id = basket.Id,
            Items = await GetBasketItems(basket.Items)
        };
    }

    public async Task<int> CountTotalBasketItems(string username)
    {
        var counter = await _basketQueryService.CountTotalBasketItems(username);

        return counter;
    }
}
