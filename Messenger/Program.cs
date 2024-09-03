using Messenger.Domain.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System.Text.Json;
using Messenger.DAL;
using Microsoft.EntityFrameworkCore;
using Messenger.Services.Interfaces;
using Messenger.Services.Implementations;
using Messenger.DAL.Repositories;
using Messenger.DAL.Interfaces;
using Messenger.Hubs;
using Messenger.UserOnlineTracking;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

builder.Services.AddAuthentication(CookieAuthenticationDefaults
    .AuthenticationScheme)
    .AddCookie(cookieOptions => 
    {
        cookieOptions.LoginPath = "/login";
        cookieOptions.Cookie.HttpOnly = false;
        cookieOptions.Cookie.Name = "Auth";
    });

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.None;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin =>
            {
                if (origin.StartsWith("http://localhost"))
                {
                    return true;
                }
                return false;
            });

    });
});



string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddSingleton<UserConnections>();
builder.Services.AddSingleton<UserContacts>();
builder.Services.AddSingleton<UserOnlineContacts>();
builder.Services.AddSingleton<UserLoginsOnline>();

builder.Services.AddAuthorization();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.UseCookiePolicy();
app.MapControllers();

app.UseCors();

app.MapHub<MessengerHub>("/chat");

app.Run();
