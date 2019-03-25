using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Collections.Generic;
using Clients;
using Newtonsoft.Json.Linq;
using IdentityModel.Client;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;

namespace MvcHybrid.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDiscoveryCache _discoveryCache;

        public HomeController(IHttpClientFactory httpClientFactory, IDiscoveryCache discoveryCache)
        {
            _httpClientFactory = httpClientFactory;
            _discoveryCache = discoveryCache;
        }

        public IActionResult Index()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            var apps = System.IO.Directory.GetDirectories(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwapp"), "_*"
                );

            foreach (var app in apps)
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(app);

                var home_default = di.GetFiles("default.htm*");
                var home_index = di.GetFiles("index.htm*");

                if (home_default.Length > 0)
                    dic.Add(di.Name, di.Name + "/" + home_default[0].Name);
                else if (home_index.Length > 0)
                    dic.Add(di.Name, di.Name + "/" + home_index[0].Name);
                else
                    continue;
            }

            return View(dic);
        }


        /// <summary>
        /// 添加应用
        /// </summary>
        /// <returns></returns>
        public IActionResult AppNew()
        {
            return View();
        }

        [HttpPost]
        public  IActionResult AppUpload(string AppCode, IFormFile AppZip)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwapp", "_" + AppCode);

            var filePath = Path.GetTempFileName();

            using (var stream = new FileStream(filePath, FileMode.Create))
            {   
                AppZip.CopyTo(stream);
            }
            
            ZipFile.ExtractToDirectory(filePath, path,true);
            
            return RedirectToAction("Index");
        }
        
        
        
        

        [Authorize]
        public IActionResult Secure()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> CallApi()
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            var client = _httpClientFactory.CreateClient();
            client.SetBearerToken(token);

            var response = await client.GetStringAsync(Constants.SampleApi + "identity");
            ViewBag.Json = JArray.Parse(response).ToString();

            return View();
        }

        public async Task<IActionResult> RenewTokens()
        {
            var disco = await _discoveryCache.GetAsync();
            if (disco.IsError) throw new Exception(disco.Error);

            var rt = await HttpContext.GetTokenAsync("http","refresh_token");
            var tokenClient = _httpClientFactory.CreateClient();

            var tokenResult = await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "mvc.hybrid",
                ClientSecret = "secret",
                RefreshToken = rt
            });

            if (!tokenResult.IsError)
            {
                var old_id_token = await HttpContext.GetTokenAsync("id_token");
                var new_access_token = tokenResult.AccessToken;
                var new_refresh_token = tokenResult.RefreshToken;
                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);

                var info = await HttpContext.AuthenticateAsync("Cookies");

                info.Properties.UpdateTokenValue("refresh_token", new_refresh_token);
                info.Properties.UpdateTokenValue("access_token", new_access_token);
                info.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

                await HttpContext.SignInAsync("Cookies", info.Principal, info.Properties);
                return Redirect("~/Home/Secure");
            }

            ViewData["Error"] = tokenResult.Error;
            return View("Error");
        }
        
        public IActionResult Logout()
        {
            return new SignOutResult(new[] { "Cookies", "oidc" });
        }

        public IActionResult Error()
        {
            return View();
        }


        #region 新增加的框架接口
            
        
        
            
        #endregion
        
    }
}
