using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CampusEventSystem.Backend;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure CORS to allow requests from any frontend port (Live Server / static local files)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register services
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=CampusEvents;Trusted_Connection=True;";

builder.Services.AddSingleton(new RegistrationService(connectionString));

var app = builder.Build();

app.UseCors("AllowFrontend");

// Endpoint 1: Student Event Registration (POST)
app.MapPost("/api/registrations", (RegistrationRequest request, RegistrationService service) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Invalid registration data provided." });
    }

    bool success = service.RegisterStudent(request);
    
    if (!success)
    {
        return Results.BadRequest(new { message = "Registration failed. Verify student details or event status." });
    }

    return Results.Created($"/api/registrations/{request.StudentId}", new { message = "Registration successful!" });
});

// Endpoint 2: Administrator Attendee Retrieval (GET)
app.MapGet("/api/events/{eventId}/attendees", (int eventId, RegistrationService service) =>
{
    var attendees = service.GetAttendeesByEvent(eventId);
    return Results.Ok(attendees);
});

// Force API to run on localhost:5000
app.Run("http://localhost:5000");