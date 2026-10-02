using CampusEvents.Backend;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from appsettings.json ("ConnectionStrings:CampusEvents")
// or the environment variable ConnectionStrings__CampusEvents. Never hard-code it.
string connectionString = builder.Configuration.GetConnectionString("CampusEvents")
    ?? throw new InvalidOperationException("Missing connection string 'CampusEvents'.");

builder.Services.AddSingleton<IEventRepository>(_ => new SqlEventRepository(connectionString));
builder.Services.AddSingleton<RegistrationValidator>();
builder.Services.AddSingleton<RegistrationService>();

// index.html runs on a different origin than the API, so CORS must be enabled.
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Return a safe JSON error instead of leaking stack traces to the browser.
app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new { message = "Unexpected server error. Please try again." });
}));

app.UseCors("Frontend");

// POST /api/registrations  (called by the Student Registration form)
app.MapPost("/api/registrations", (RegistrationRequest? request, RegistrationService service) =>
{
    ServiceResult result = service.Register(request);
    return Results.Json(new { message = result.Message }, statusCode: result.StatusCode);
});

// GET /api/events/{eventId}/attendees  (called by the Administrator viewer)
app.MapGet("/api/events/{eventId:int}/attendees", (int eventId, RegistrationService service) =>
{
    var attendees = service.GetAttendees(eventId);
    return attendees is null
        ? Results.Json(new { message = "Event not found. Check Event ID." }, statusCode: 404)
        : Results.Ok(attendees);
});

app.Run("http://localhost:5000");