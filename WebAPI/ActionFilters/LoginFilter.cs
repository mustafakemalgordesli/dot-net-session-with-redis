using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using WebAPI.Attributes;
using WebAPI.Models.Dtos;
using System.Net;
using StackExchange.Redis;
using WebAPI.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace WebAPI.ActionFilters;

public class LoginFilter : IActionFilter
{
    private readonly IDistributedCache _distributedCache;
    private readonly IHttpClientFactory _httpClientFactory;

    public LoginFilter(IDistributedCache distributedCache, IHttpClientFactory httpClientFactory)
    {
        _distributedCache = distributedCache;
        _httpClientFactory = httpClientFactory;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {

        if (HasIgnoreAttribute(context)) { return; }

        context.HttpContext.Session.TryGetValue("token", out var token);
        var result = _distributedCache.GetString(context.HttpContext.Session.Id);

        if (result == null || token == null || System.Text.Encoding.UTF8.GetString(token) != result)
        {
            context.Result = new ObjectResult(new
            {
                Success = false
            });
        }
        else
        {
            string refreshToken = IsRefreshToken(result, context);
            if (!String.IsNullOrWhiteSpace(refreshToken))
            {
                context.HttpContext.Session.Set("token", System.Text.Encoding.UTF8.GetBytes(refreshToken));
                _distributedCache.SetString(context.HttpContext.Session.Id, refreshToken);
            }
        }

    }

    public void OnActionExecuted(ActionExecutedContext context)
    {

    }

    public bool HasIgnoreAttribute(ActionExecutingContext context)
    {
        foreach (var filterDescriptors in ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo.CustomAttributes)
        {
            if (filterDescriptors.AttributeType == typeof(IgnoreAttribute))
            {
                return true;
            }
        }
        return false;
    }
    public string IsRefreshToken(string sessionToken, ActionExecutingContext context)
    {
        //string tokenSession = System.Text.Encoding.UTF8.GetString(sessionToken).Split('æ')[1];
        string tokenSession = sessionToken.Split('æ')[1];
        DateTime sessionCreateTime = DateTime.Parse(tokenSession);
        TimeSpan remainingTime = DateTime.Now - sessionCreateTime;
        if (remainingTime.TotalMinutes >= 1)
        {

                string token = Guid.NewGuid().ToString() + "æ" + DateTime.Now;
                context.HttpContext.Session.Set("token", System.Text.Encoding.UTF8.GetBytes(token));
                _distributedCache.SetString(context.HttpContext.Session.Id, token);
                return token;
        }
        return string.Empty;
    }

}
