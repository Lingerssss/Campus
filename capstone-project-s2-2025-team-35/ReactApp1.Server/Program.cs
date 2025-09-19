using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server;
using ReactApp1.Server.Data;
using ReactApp1.Server.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

const string ViteCors = "ViteCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ViteCors, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173",
                           "http://localhost:63647", "https://localhost:63647")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ✅ Use a single cookie scheme named "MyAuthentication"
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "MyAuthentication";
        options.DefaultSignInScheme       = "MyAuthentication";
        options.DefaultChallengeScheme    = "MyAuthentication"; // do NOT auto-redirect to Google for APIs
    })
    .AddCookie("MyAuthentication", options =>
    {
        options.Cookie.Name = "cems_auth";
        options.LoginPath = "/api/auth/google";
        options.LogoutPath = "/api/auth/logout";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);

        // For dev on http://localhost
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;

        // Return proper status codes to fetch() instead of HTML redirects
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; },
            OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; }
        };
    })
    // Keep Google available, but call it explicitly from /api/auth/google
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["Auth:Google:ClientId"]
                           ?? throw new InvalidOperationException("Google ClientId not configured");
        options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]
                               ?? throw new InvalidOperationException("Google ClientSecret not configured");
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly",  p => p.RequireRole("Student"));
    options.AddPolicy("OrganizerOnly", p => p.RequireRole("Organizer"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "events"));

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(ViteCors);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<EventDbContext>();
        db.Database.Migrate();
        SeedData.Initialize(db);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

app.Run();
