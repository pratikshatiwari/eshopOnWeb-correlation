using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlazorShared.Authorization;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;

namespace Microsoft.eShopWeb.Web.Controllers;

[Route("[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ITokenClaimsService _tokenClaimsService;

    public UserController(ITokenClaimsService tokenClaimsService)
    {
        _tokenClaimsService = tokenClaimsService;
    }

    [HttpGet]
    [Authorize]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrentUser() =>
        Ok(await CreateUserInfo(User));
    public ITransaction Transaction { get; set; }

    private async Task<UserInfo> CreateUserInfo(ClaimsPrincipal claimsPrincipal)
    {
        // transaction 1
       // var trans1 = Agent.Tracer.StartTransaction("Dist Trans 2", ApiConstants.TypeRequest);
        var tracingData = Elastic.Apm.Agent.Tracer.CurrentTransaction?.OutgoingDistributedTracingData.SerializeToString();

        Elastic.Apm.Agent.Tracer.CaptureTransaction(tracingData, Elastic.Apm.Api.ApiConstants.TypeRequest, transaction =>
        { });


            if (claimsPrincipal.Identity == null || claimsPrincipal.Identity.Name == null || !claimsPrincipal.Identity.IsAuthenticated)
            {
                return UserInfo.Anonymous;
            }


        var userInfo = new UserInfo
        {
            IsAuthenticated = true
        };



        if (claimsPrincipal.Identity is ClaimsIdentity claimsIdentity)
        {
            userInfo.NameClaimType = claimsIdentity.NameClaimType;
            userInfo.RoleClaimType = claimsIdentity.RoleClaimType;
        }
        else
        {
            userInfo.NameClaimType = "name";
            userInfo.RoleClaimType = "role";
        }

        if (claimsPrincipal.Claims.Any())
        {
            var claims = new List<ClaimValue>();
            var nameClaims = claimsPrincipal.FindAll(userInfo.NameClaimType);
            foreach (var claim in nameClaims)
            {
                claims.Add(new ClaimValue(userInfo.NameClaimType, claim.Value));
            }

            foreach (var claim in claimsPrincipal.Claims.Except(nameClaims))
            {
                claims.Add(new ClaimValue(claim.Type, claim.Value));
            }

            userInfo.Claims = claims;
        }

        var token = await _tokenClaimsService.GetTokenAsync(claimsPrincipal.Identity.Name);
        userInfo.Token = token;

        return userInfo;



    }

}
