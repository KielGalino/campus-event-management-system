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

// 1. Use a strongly-typed list to store EventId alongside attendee details
var attendees = new List<RegistrationRecord> 
{
    new RegistrationRecord { 
        EventId = 1, 
        StudentId = 12345, 
        FullName = "John Doe", 
        Email = "johndoe@dlsud.edu.ph", 
        // Generates a random date between 1 and 14 days ago for the mock data
        RegistrationDate = DateTime.Now.AddDays(new Random().Next(-14, -1)).ToString("yyyy-MM-dd") 
    }
};

// 2. Accept registrations and map the correct email property
app.MapPost("/api/registrations", (RegistrationRequest req) => 
{
    // Fallback logic: accepts either 'Email' or 'EmailAddress' from the frontend JSON
    string resolvedEmail = !string.IsNullOrEmpty(req.Email) ? req.Email : req.EmailAddress;

    attendees.Add(new RegistrationRecord { 
        EventId = req.EventId,
        StudentId = req.StudentId, 
        FullName = req.FullName, 
        Email = resolvedEmail, // String data type naturally accepts any length/domain
        RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd") 
    });
    
    return Results.Ok(new { message = "Registration successful" });
});

// 3. Filter attendees based on the requested Event ID from the search box
app.MapGet("/api/events/{id}/attendees", (int id) => 
{
    // Only returns students whose EventId matches the administrator's search
    var filteredAttendees = attendees.Where(a => a.EventId == id).ToList();
    return Results.Ok(filteredAttendees);
});

// 4. Hardcoded port to ensure the frontend fetch commands never break
app.Run("http://localhost:5140");

// --- DATA MODELS ---

// Defines the expected incoming JSON structure from the frontend
public class RegistrationRequest
{
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public string Email { get; set; } // Added to catch varying JSON payload keys
}

// Defines the structure of the data stored in the server's memory
public class RegistrationRecord
{
    public int EventId { get; set; }
    public int StudentId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string RegistrationDate { get; set; }
}