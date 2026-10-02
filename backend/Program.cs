using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Allow frontend connection
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

// 1. Updated to use the renamed MockRegistrationRecord class
var attendees = new List<MockRegistrationRecord> 
{
    new MockRegistrationRecord { 
        EventId = 1, 
        StudentId = 12345, 
        FullName = "John Doe", 
        Email = "johndoe@dlsud.edu.ph", 
        RegistrationDate = DateTime.Now.AddDays(new Random().Next(-14, -1)).ToString("yyyy-MM-dd") 
    }
};

// 2. Updated to use the renamed MockRegistrationRequest class
app.MapPost("/api/registrations", (MockRegistrationRequest req) => 
{
    string resolvedEmail = !string.IsNullOrEmpty(req.Email) ? req.Email : req.EmailAddress;

    attendees.Add(new MockRegistrationRecord { 
        EventId = req.EventId,
        StudentId = req.StudentId, 
        FullName = req.FullName, 
        Email = resolvedEmail,
        RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd") 
    });
    
    return Results.Ok(new { message = "Registration successful" });
});

// 3. Filter attendees
app.MapGet("/api/events/{id}/attendees", (int id) => 
{
    var filteredAttendees = attendees.Where(a => a.EventId == id).ToList();
    return Results.Ok(filteredAttendees);
});

// 4. Hardcoded port
app.Run("http://localhost:5140");

// --- RENAMED DATA MODELS TO AVOID TEAM CONFLICTS ---

public class MockRegistrationRequest
{
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public string Email { get; set; } 
}

public class MockRegistrationRecord
{
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string RegistrationDate { get; set; }
}