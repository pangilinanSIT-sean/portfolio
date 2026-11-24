using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using portfolio.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database: the connection string comes from User Secrets, never from a file in the repository.
// It tries the key names Portfolio, Library and DefaultConnection. Keep your own lines here if you prefer.
var connectionString = new[] { "Portfolio", "Library", "DefaultConnection" }
    .Select(name => builder.Configuration.GetConnectionString(name))
    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
if (connectionString == null)
{
    throw new InvalidOperationException("No connection string found. Add it in Manage User Secrets.");
}
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// One repository per table, new for each web request.
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ProjectImageRepository>();

// Signing in for the editor side: a cookie, checked against the password kept in User Secrets.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Anything unexpected shows the friendly page. A 404 or another status shows its own page.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();