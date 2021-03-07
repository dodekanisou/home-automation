using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RpiHost.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RpiHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new Configuration.Config();
            Configuration.Bind("Config", config);
            services.AddSingleton(config);

            // Only a single controller can control the pins
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IGpioController, DummyGpioController>();
            }
            else
            {
                services.AddSingleton<IGpioController, PhysicalGpioController>();
            }

            // And only a single service can have the pins open
            services.AddSingleton<RelayService>();

            // This was originally added for iOS 12 endless redirection loop
            // Then it was repurposed to allow debug in non https endpoints.
            services.Configure<CookiePolicyOptions>(options =>
            {
                //// options.MinimumSameSitePolicy = SameSiteMode.None;
                //// Changed to address iOS 12 issue
                //// https://github.com/dotnet/aspnetcore/issues/14996
                //options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.OnAppendCookie = cookieContext =>
                    AllowSameSiteForLocalDebug(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    AllowSameSiteForLocalDebug(cookieContext.Context, cookieContext.CookieOptions);
            });


            services.AddHttpClient<MotionStreamService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = Configuration["Auth0:Authority"];
                options.Audience = Configuration["Auth0:Audience"];
            })
            .AddCookie()
            .AddOpenIdConnect("Auth0", options =>
            {
                // Set the authority to your Auth0 domain
                options.Authority = Configuration["Auth0:Authority"];

                // Configure the Auth0 Client ID and Client Secret
                options.ClientId = Configuration["Auth0:ClientId"];
                options.ClientSecret = Configuration["Auth0:ClientSecret"];

                // Set response type to code
                options.ResponseType = "code";

                // Configure the scope
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                // Set the callback path, so Auth0 will call back to http://localhost:3000/callback
                // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
                options.CallbackPath = new PathString("/callback");

                // Configure the Claims Issuer to be Auth0
                options.ClaimsIssuer = "Auth0";

                // Saves tokens to the AuthenticationProperties
                options.SaveTokens = true;

                // Auth0 returns the name in the name claim instead of the default
                options.TokenValidationParameters.NameClaimType = "name";

                options.Events = new OpenIdConnectEvents
                {
                    // Configure to request access to API
                    // Based on https://manage.auth0.com/dashboard/myapp/applications/random/quickstart
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("audience", Configuration["Auth0:Audience"]);

                        return Task.FromResult(0);
                    },

                    OnTokenValidated = (ctx) =>
                    {
                        // Copy permissions from access token to current identity
                        var handler = new JwtSecurityTokenHandler();
                        var accessToken = handler.ReadJwtToken(ctx.TokenEndpointResponse.AccessToken);

                        foreach (var c in accessToken.Claims.Where(i => i.Type == "permissions"))
                        {
                            ctx.Principal.Identities.FirstOrDefault().AddClaim(c);
                        }

                        ctx.Principal.Identities.FirstOrDefault().AddClaim(new System.Security.Claims.Claim("access_token", ctx.TokenEndpointResponse.AccessToken));

                        return Task.CompletedTask;
                    },

                    // handle the logout redirection 
                    OnRedirectToIdentityProviderForSignOut = (context) =>
                    {
                        var logoutUri = $"{Configuration["Auth0:Authority"]}v2/logout?client_id={Configuration["Auth0:ClientId"]}";

                        var postLogoutUri = context.Properties.RedirectUri;
                        if (!string.IsNullOrEmpty(postLogoutUri))
                        {
                            if (postLogoutUri.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                            {
                                // transform to absolute
                                var request = context.Request;
                                postLogoutUri = request.Scheme + "://" + request.Host + request.PathBase + postLogoutUri;
                            }
                            logoutUri += $"&returnTo={ Uri.EscapeDataString(postLogoutUri)}";
                        }

                        context.Response.Redirect(logoutUri);
                        context.HandleResponse();

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("relay:read", policy => policy.RequireClaim("permissions", "relay:read"));
                options.AddPolicy("relay:write", policy => policy.RequireClaim("permissions", "relay:write"));
                options.AddPolicy("motion:read:images", policy => policy.RequireClaim("permissions", "motion:read:images"));
                options.AddPolicy("motion:read:videos", policy => policy.RequireClaim("permissions", "motion:read:videos"));
                options.AddPolicy("motion:feed:video", policy => policy.RequireClaim("permissions", "motion:feed:video"));
                options.AddPolicy("use:webui", policy => policy.RequireClaim("permissions", "use:webui"));
                options.AddPolicy("debug", policy => policy.RequireAssertion((context) =>
                {
                    return true;
                }));
            });

            services.AddControllers();

            services.AddMvc();

            // Play nice behind nginx
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1#forwarded-headers-middleware-options
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // https://dev.to/ianknighton/hosting-a-net-core-app-with-nginx-and-let-s-encrypt-1m50
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseCookiePolicy();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // global CORS policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // Enable authentication middle-ware
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void AllowSameSiteForLocalDebug(HttpContext httpContext, CookieOptions options)
        {

            if (options.SameSite == SameSiteMode.None && !httpContext.Request.IsHttps)
            {
                options.SameSite = SameSiteMode.Unspecified;
            }

            // TODO: Remove this if not needed to iOS anymore.
            //if (options.SameSite == SameSiteMode.None)
            //{
            //    var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            //    // Fix only iPhones where the issue manifests mostly
            //    if (userAgent.Contains("iOS",StringComparison.OrdinalIgnoreCase) ||
            //        userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            //    {
            //        options.SameSite = SameSiteMode.Unspecified;
            //    }
            //}
        }

    }
}
