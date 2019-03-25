using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MvcHybrid.Controllers
{
    public class IdentityController : Controller
    {
        
        // GET
        [Authorize]
        public IActionResult Index()
        {
            object sub = 0;
            object preferred_username = "";
            object name = "";
            object email = "";
            object email_verified = false;
            object issuer = string.Empty;
            object expires_at = string.Empty;
            
            foreach (var claim in User.Claims)
            {
                if (claim.Type.Equals("sub"))
                {
                    sub = claim.Value;
                }
                else if (claim.Type.Equals("preferred_username"))
                {
                    preferred_username = claim.Value;
                }
                else if (claim.Type.Equals("name"))
                {
                    name = claim.Value;
                }
                else if (claim.Type.Equals("email"))
                {
                    email = claim.Value;
                }
                else if (claim.Type.Equals("email_verified"))
                {
                    email_verified = claim.Value;
                }
            }

            ClaimsIdentity claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;

            foreach (var item in claimsIdentity.Claims)
            {
                issuer = item.Issuer;
                break;
                ;
            }
            
            var id = new
            {   
                isAuthenticated = this.User.Identity.IsAuthenticated,
                profile = new {
                    name = this.User.Identity.Name,
                    userid = sub,
                    preferred_username = preferred_username,
                    email = email,
                    email_verified = email_verified
                },
                
                
                issuer = issuer
            };
            
            
            return this.Json(id);
        }
        
        
        // GET
        [Authorize]
        public async Task<IActionResult> Access_Token()
        {
            string result = string.Empty;
            string issued = null;
            string expires = null;
            
            foreach (var prop in (await HttpContext.AuthenticateAsync()).Properties.Items)
            {
                if (prop.Key.Equals(".Token.access_token"))
                {
                    result = prop.Value;
                }  
                else if (prop.Key.Equals(".issued"))
                {
                    issued = prop.Value;
                    
                }else if (prop.Key.Equals(".expires"))
                {    
                    expires = prop.Value;
                }
            }

            var obj = new
            {
                access_token = result,
                issued = GMT2Local(issued),
                expires = GMT2Local(expires)
            };
            
            
            return this.Json(obj);
        }
        
        /// <summary>  
        /// GMT时间转成本地时间  
        /// </summary>  
        /// <param name="gmt">字符串形式的GMT时间</param>  
        /// <returns></returns>  
        public static DateTime GMT2Local(string gmt)
        {
            
            //列举所有支持的时区列表
           
            
                
            if (gmt.ToLower().Contains("gmt"))
            {    
                var dt = DateTime.Parse(gmt);
                DateTime serverTime2 = TimeZoneInfo.ConvertTime(dt,TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"));//等价的建议写法

                return serverTime2;
            }
            else
            {    
                return DateTime.Parse(gmt);
            }
            
           
        }
        
        
        
    }
}