using FastFood.Lieux;
using FastFood.Notes;
using FastFood.Web;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddMemoryCache();
services.AddSingleton<IClientOverpass>(_ => new ClientOverpass(CreerHttpOverpass(), ClientOverpass.Instances));
services.AddSingleton<ICatalogueLieux, CatalogueLieux>();
services.AddSingleton<IDepotNotes>(_ =>
    new DepotNotes(builder.Configuration["FastFood:Base"] ?? "fastfood.db", TimeProvider.System));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = CreerTypesMime(),
    OnPrepareResponse = fichier => fichier.Context.Response.Headers.CacheControl = "no-cache",
});
app.MapApi();
app.Run();

static FileExtensionContentTypeProvider CreerTypesMime()
{
    var types = new FileExtensionContentTypeProvider();
    types.Mappings[".webmanifest"] = "application/manifest+json";
    return types;
}

static HttpClient CreerHttpOverpass()
{
    var http = new HttpClient { Timeout = ClientOverpass.Delai };
    http.DefaultRequestHeaders.UserAgent.ParseAdd(ClientOverpass.UserAgent);
    return http;
}

public partial class Program;
