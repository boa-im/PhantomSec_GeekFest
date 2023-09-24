using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using BellHackathon.DataAccess;
using BellHackathon.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string connStr = builder.Configuration.GetConnectionString("BellDb");
builder.Services.AddDbContext<BellDbContext>(options => options.UseSqlServer(connStr));

builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<BellDbContext>()
        .AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    await BellDbContext.CreateAdminUser(scope.ServiceProvider);
}

app.Run();
