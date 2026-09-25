using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Phonebook.Data;
using Phonebook.Endpoints;
using Phonebook.Middleware;
using Phonebook.Services;

var builder = WebApplication.CreateBuilder(args);

// Database settings use the same DATABASE_* environment variables as the Java
// backend, so docker-compose.yml stays unchanged. appsettings.json holds the
// same defaults application.properties had.
var dbHost = builder.Configuration["DATABASE_HOST"] ?? "localhost";
var dbPort = builder.Configuration["DATABASE_PORT"] ?? "5432";
var dbName = builder.Configuration["DATABASE_NAME"] ?? "phonebook";
var dbUser = builder.Configuration["DATABASE_USER"] ?? "phonebook";
var dbPassword = builder.Configuration["DATABASE_PASSWORD"] ?? "change-me";

builder.Services.AddDbContext<PhonebookDbContext>(options =>
    options.UseNpgsql(
        $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}"));

// Minimal APIs serialize with their own Http.Json options (the MVC AddJsonOptions
// call is gone). The settings are the same: snake_case field names replacing Spring's
// Jackson SNAKE_CASE strategy, and the local-time date format the frontend parses.
// PropertyNameCaseInsensitive covers request bodies — that is what MVC's System.Text.Json
// input formatter did (a {"NAME": ...} payload binds Name).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new DateTimeJsonConverter());
});

builder.Services.AddScoped<ContactService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Replaces Spring's ddl-auto=update: create the schema on a fresh database.
// On an existing database this is a no-op and leaves current data untouched.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PhonebookDbContext>();
    db.Database.EnsureCreated();
}

app.MapRootEndpoints();
app.MapContactEndpoints();

app.Run();
