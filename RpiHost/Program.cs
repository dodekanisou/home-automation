using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

namespace RpiHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.TraversePath().Load();
            var builder = WebApplication.CreateBuilder(args);

            builder.WebHost.UseUrls("http://*:5000");

            var startup = new Startup(builder.Configuration);
            startup.ConfigureServices(builder.Services);

            var app = builder.Build();

            startup.Configure(app, builder.Environment);

            app.Run();
        }
    }
}
