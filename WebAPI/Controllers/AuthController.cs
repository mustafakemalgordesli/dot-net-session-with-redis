using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Attributes;
using WebAPI.ActionFilters;
using Microsoft.Extensions.Caching.Distributed;

namespace WebAPI.Controllers
{
    [ServiceFilter(typeof(LoginFilter))]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDistributedCache _distributedCache;

        public AuthController(IDistributedCache distributedCache, IHttpClientFactory httpClientFactory)
        {
            _distributedCache = distributedCache;
            _httpClientFactory = httpClientFactory;
        }

        [IgnoreAttribute]
        [HttpPost("login")]
        public IActionResult Login()
        {
            string name = "kemal";
            string password = "1234";
            //Static UserName Password Check            
            if (name == "kemal" && password == "1234")
            {
                string token = Guid.NewGuid().ToString() + "æ" + DateTime.Now;
                HttpContext.Session.Set("token", System.Text.Encoding.UTF8.GetBytes(token));
                _distributedCache.SetString(HttpContext.Session.Id, token);
                return Ok(new { token, id=HttpContext.Session.Id });
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public IActionResult GetUser()
        {
            var result = _distributedCache.GetString(HttpContext.Session.Id);
            return Ok(new { token=result, id=HttpContext.Session.Id });
        }
    }
}
