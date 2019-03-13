using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QrF.Core.Utils.Extension;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QrF.OAuth.WeChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private const string LoginProviderKey = "LoginProvider";
        private const string Provider_WeChat = "WeChat";
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _contextAccessor;

        public AccountController(ILogger<AccountController> logger,
            IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
        }
        /// <summary>
        /// 微信登录
        /// </summary>
        /// <param name="redirectUrl">授权成功后的跳转地址</param>
        /// <returns></returns>
        [HttpGet("LoginByWeChat")]
        public IActionResult LoginByWeChat(string redirectUrl)
        {
            var request = _contextAccessor.HttpContext.Request;
            var url = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}Callback?provider={Provider_WeChat}&redirectUrl={redirectUrl}";
            var properties = new AuthenticationProperties { RedirectUri = url };
            properties.Items[LoginProviderKey] = Provider_WeChat;
            return Challenge(properties, Provider_WeChat);
        }
        /// <summary>
        /// 微信授权成功后自动回调的地址
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="redirectUrl">授权成功后的跳转地址</param>
        /// <returns></returns>
        [HttpGet("LoginByWeChatCallback")]
        public async Task<IActionResult> LoginByWeChatCallbackAsync(string provider = null, string redirectUrl = "")
        {
            var authenticateResult = await _contextAccessor.HttpContext.AuthenticateAsync(provider);
            if (!authenticateResult.Succeeded) return Redirect(redirectUrl);
            var openIdClaim = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier);
            if (openIdClaim == null || openIdClaim.Value.IsNullOrWhiteSpace())
                return Redirect(redirectUrl);
            //TODO 记录授权成功后的微信信息 
            var city = authenticateResult.Principal.FindFirst("urn:wechat:city")?.Value;
            var country = authenticateResult.Principal.FindFirst(ClaimTypes.Country)?.Value;
            var headimgurl = authenticateResult.Principal.FindFirst(ClaimTypes.Uri)?.Value;
            var nickName = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var openId = authenticateResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var privilege = authenticateResult.Principal.FindFirst("urn:wechat:privilege")?.Value;
            var province = authenticateResult.Principal.FindFirst("urn:wechat:province")?.Value;
            var sexClaim = authenticateResult.Principal.FindFirst(ClaimTypes.Gender);
            int sex = 0;
            if (sexClaim != null && !sexClaim.Value.IsNullOrWhiteSpace())
                sex = int.Parse(sexClaim.Value);
            var unionId = authenticateResult.Principal.FindFirst("urn:wechat:unionid")?.Value;
            _logger.LogDebug($"WeChat Info=> openId: {openId},nickName: {nickName}");
            return Redirect($"{redirectUrl}?openId={openIdClaim.Value}");
        }
    }
}
