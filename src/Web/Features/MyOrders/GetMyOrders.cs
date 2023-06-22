using System.Collections.Generic;
using Elastic.Apm.Api;
using MediatR;
using Microsoft.eShopWeb.Web.ViewModels;

namespace Microsoft.eShopWeb.Web.Features.MyOrders;

public class GetMyOrders : IRequest<IEnumerable<OrderViewModel>>
{
    public string UserName { get; set; }
    public ITransaction Transaction { get; set; }

    public GetMyOrders(string userName, ITransaction transaction)
    {
        UserName = userName;
        Transaction = transaction;
    }
}
