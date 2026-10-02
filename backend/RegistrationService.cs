using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CampusEventSystem.Backend
{
    // --- Data Models matching the Frontend API Payload ---

    public class RegistrationRequest
    {
        public int EventId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AttendeeDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // --- Database Repository Interface ---

    public interface IEventRepository
    {
        bool IsEventFull(int eventId);
        bool IsStudentAlreadyRegistered(int eventId, string email);
    }

    // --- Validation Logic ---

    public class RegistrationValidator
    {
        private readonly IEventRepository _repository;
        private const string RequiredDomain = "@univ.edu.ph";

        public RegistrationValidator(IEventRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public ValidationResult ValidateRegistration(int eventId, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Email cannot be empty." };
            }

            if (!email.EndsWith(RequiredDomain, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Email must belong to @univ.edu.ph." };
            }

            if (_repository.IsEventFull(eventId))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Event has reached maximum seat capacity." };
            }

            if (_repository.IsStudentAlreadyRegistered(eventId, email))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Student is already registered for this event." };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    // --- Core Backend Service Layer ---

    public class RegistrationService
    {
        private readonly string _connectionString;

        public RegistrationService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Endpoint Support: Handles POST /api/registrations
        public bool RegisterStudent(RegistrationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return false;
            }

            const string insertQuery = @"
                INSERT INTO Registrations (EventId, StudentId, FullName, Email, RegistrationDate)
                VALUES (@EventId, @StudentId, @FullName, @Email, @RegistrationDate);";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
            {
                cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = request.EventId;
                cmd.Parameters.Add("@StudentId", SqlDbType.VarChar, 50).Value = request.StudentId;
                cmd.Parameters.Add("@FullName", SqlDbType.VarChar, 100).Value = request.FullName;
                cmd.Parameters.Add("@Email", SqlDbType.VarChar, 255).Value = request.Email;
                cmd.Parameters.Add("@RegistrationDate", SqlDbType.DateTime).Value = DateTime.UtcNow;

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
        }

        // Endpoint Support: Handles GET /api/events/{eventId}/attendees
        public List<AttendeeDto> GetAttendeesByEvent(int eventId)
        {
            var attendees = new List<AttendeeDto>();

            const string query = @"
                SELECT StudentId, FullName, Email, RegistrationDate 
                FROM Registrations 
                WHERE EventId = @EventId 
                ORDER BY RegistrationDate ASC;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        attendees.Add(new AttendeeDto
                        {
                            StudentId = reader["StudentId"].ToString() ?? string.Empty,
                            FullName = reader["FullName"].ToString() ?? string.Empty,
                            Email = reader["Email"].ToString() ?? string.Empty,
                            RegistrationDate = Convert.ToDateTime(reader["RegistrationDate"])
                        });
                    }
                }
            }

            return attendees;
        }

        // Refactored legacy method fixing SQL Injection, Resource Leaks, and Null Exceptions
        public string GetUserRegistration(string inputEmail)
        {
            if (string.IsNullOrWhiteSpace(inputEmail))
            {
                return string.Empty;
            }

            const string query = "SELECT RegistrationId FROM Registrations WHERE Email = @Email";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@Email", SqlDbType.VarChar, 255).Value = inputEmail;

                conn.Open();
                object result = cmd.ExecuteScalar();

                return result != null ? result.ToString() : string.Empty;
            }
        }
    }
}