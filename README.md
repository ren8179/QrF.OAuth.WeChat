# QrF.OAuth.WeChat
.net Core2.2 WebApi通过OAuth2.0实现微信登录

> 该项目不适用 .net Core 3.1 ,我在 [Qf.Core](https://github.com/ren8179/Qf.Core) 项目中重新实现了微信登录功能,用法保持一致

### 前言

微信相关配置请参考 [微信公众平台](https://mp.weixin.qq.com/wiki?t=resource/res_main&id=mp1421140842) 的这篇文章。注意授权回调域名一定要修改正确。
微信网页授权是通过OAuth2.0机制实现的，所以我们可以使用 [https://github.com/china-live/QQConnect](https://github.com/china-live/QQConnect) 这个开源项目提供的中间件来实现微信第三方登录的流程。

### 开发流程
1、新建一个.net core webapi 项目。在NuGet中查找并安装 ```AspNetCore.Authentication.WeChat``` 包。
2、修改 ```appsettings.json``` 配置文件，增加以下配置：
```
"Authentication": {
    "WeChat": {
      "AppId": "微信AppID",
      "AppSecret": "微信AppSecret"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug", //日志级别从低到高，依次为：Debug,Information,Warning,Error,None
      "Microsoft.EntityFrameworkCore": "Error",
      "System": "Error"
    }
  }
```
3、修改 ```Startup``` 
```
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddAuthentication()
                .AddWeChat(wechatOptions =>
                {
                    wechatOptions.AppId = Configuration["Authentication:WeChat:AppId"];
                    wechatOptions.AppSecret = Configuration["Authentication:WeChat:AppSecret"];
                    wechatOptions.UseCachedStateDataFormat = true;
                });
```
4、新增 ```AccountController```
```
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
```
5、将网站发布到外网，请求```https://你的授权域名/api/account/LoginByWeChat?redirectUrl=授权成功后要跳转的页面```即可调起微信授权页面。
### 注意
> 微信授权必须使用https

> 微信开放平台和微信公众平台都有提供网站用微信登录的接口，前者适用于任何网站，后者只适用于微信服务号的内嵌网站。
