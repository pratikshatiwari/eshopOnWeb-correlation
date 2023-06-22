using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BlazorShared.Models;
using Elastic.Apm.Api;

namespace BlazorShared.Interfaces;

public interface ICatalogItemService
{
    Task<CatalogItem> Create(CreateCatalogItemRequest catalogItem);
    Task<CatalogItem> Edit(CatalogItem catalogItem);
    Task<string> Delete(int id);
    Task<CatalogItem> GetById(int id);
    //Task<List<CatalogItem>> ListPaged(int pageSize, ITransaction transaction);
    Task<List<CatalogItem>> ListPaged(int pageSize);
    Task<List<CatalogItem>> List();
    //Task<List<CatalogItem>> ListPaged(int pageSize, ITransaction transaction);
}
