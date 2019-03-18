using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using Clients;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using IdentityModel.Client;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Rewrite;

namespace MvcHybrid
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string identityServerBaseUrl =
                        Configuration.GetSection("AuthorityConfiguration:IdentityServerBaseUrl").Value;
            string clientId =
                Configuration.GetSection("AuthorityConfiguration:ClientId").Value;
            string clientSecret =
                Configuration.GetSection("AuthorityConfiguration:ClientSecret").Value;


            services.AddMvc();
            services.AddHttpClient();

            services.AddSingleton<IDiscoveryCache>(r =>
            {
                var factory = r.GetRequiredService<IHttpClientFactory>();
                return new DiscoveryCache(identityServerBaseUrl, () => factory.CreateClient());
            });

            services.AddTransient<CookieEventHandler>();
            services.AddSingleton<LogoutSessionManager>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "oidc";
            })
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.Cookie.Name = "mvchybridbc";

                    options.EventsType = typeof(CookieEventHandler);
                })
                .AddOpenIdConnect("oidc", options =>
                {


                    options.Authority = identityServerBaseUrl;
                    options.RequireHttpsMetadata = false;

                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;


                    options.ResponseType = "code id_token";

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.Scope.Add("api1");
                    options.Scope.Add("offline_access");

                    options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");

                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Name,
                        RoleClaimType = JwtClaimTypes.Role,
                    };
                });
            
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();

            //app.UseDefaultFiles("/_");

            app.UseStaticFiles(new StaticFileOptions
            {   
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwapp")),
                RequestPath = ""
            });
           
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
            /*
            var rewriteOption = new RewriteOptions()
               .AddRewrite(
                @"^_(.*)/(.*)", // RegEx to match URL
                "_$1/$2", // URL to rewrite
                false // Stop processing any aditional rules
            );

            app.UseRewriter(rewriteOption);
            */

        }
    }
}
