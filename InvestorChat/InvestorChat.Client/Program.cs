using InvestorChat.Client;
using InvestorChat.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseAddress))
{
    var clientBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    apiBaseAddress = (builder.HostEnvironment.Environment, clientBaseAddress.Port) switch
    {
        ("Development", 5268) => "http://localhost:5207/",
        ("Development", 7022) => "https://localhost:7098/",
        ("Development", 53114) => "http://localhost:38559/",
        ("Development", 44308) => "https://localhost:44321/",
        _ => builder.HostEnvironment.BaseAddress
    };
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseAddress, UriKind.Absolute)
});
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
