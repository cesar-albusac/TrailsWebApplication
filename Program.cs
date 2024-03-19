using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrailsWebApplication.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using NuGet.Protocol.Core.Types;
using Trails.Data;
using TrailsWebApplication.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var MyAllowSpecificOrigins = "origins";
// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("https://trailsstorageaccount.blob.core.windows.net/");

		});
});
builder.Services.AddScoped<ITrailRepository, TrailRepository>();
var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Trails/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(MyAllowSpecificOrigins);


app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Trails}/{action=Index}/{id?}");

app.Run();
