using System.Data;
using System.Data.SqlClient;

namespace CampusEvents.Backend
{
    // ---------- DTOs (property names match what index.html sends/reads) ----------
    public record RegistrationRequest(int EventId, string StudentId, string FullName, string Email);

    public record AttendeeDto(string StudentId, string FullName, string Email, DateTime RegistrationDate);

    public record ServiceResult(bool Success, int StatusCode, string Message)
    {
        public static ServiceResult Ok(int code, string message) => new(true, code, message);
        public static ServiceResult Fail(int code, string message) => new(false, code, message);
    }

    // ---------- Data access contract (mocked in unit tests) ----------
    public interface IEventRepository
    {
        bool EventExists(int eventId);
        int GetRemainingSeats(int eventId);
        bool IsAlreadyRegistered(int eventId, string email);
        void AddRegistration(RegistrationRequest request);
        IReadOnlyList<AttendeeDto> GetAttendees(int eventId);
        string? GetUserRegistration(string inputEmail);
    }

    // ---------- Validation rules ----------
    public class RegistrationValidator
    {
        private const string AllowedDomain = "@univ.edu.ph";
        private readonly IEventRepository _repo;

        public RegistrationValidator(IEventRepository repo) => _repo = repo;

        public bool IsValidStudentEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string e = email.Trim();
            return e.EndsWith(AllowedDomain, StringComparison.OrdinalIgnoreCase)
                && e.Length > AllowedDomain.Length
                && e.Count(c => c == '@') == 1;
        }

        public ServiceResult Validate(int eventId, string? email)
        {
            if (!IsValidStudentEmail(email))
                return ServiceResult.Fail(400, "Email must be a valid @univ.edu.ph address.");
            if (!_repo.EventExists(eventId))
                return ServiceResult.Fail(404, "Event not found.");
            if (_repo.IsAlreadyRegistered(eventId, email!.Trim()))
                return ServiceResult.Fail(409, "You are already registered for this event.");
            if (_repo.GetRemainingSeats(eventId) <= 0)
                return ServiceResult.Fail(409, "Sorry, this event is full.");
            return ServiceResult.Ok(200, "Valid");
        }
    }

    // ---------- Business logic ----------
    public class RegistrationService
    {
        private readonly IEventRepository _repo;
        private readonly RegistrationValidator _validator;

        public RegistrationService(IEventRepository repo, RegistrationValidator validator)
        {
            _repo = repo;
            _validator = validator;
        }

        public ServiceResult Register(RegistrationRequest? request)
        {
            if (request is null
                || string.IsNullOrWhiteSpace(request.StudentId)
                || string.IsNullOrWhiteSpace(request.FullName))
                return ServiceResult.Fail(400, "Event ID, Student ID, Full Name and Email are required.");

            var check = _validator.Validate(request.EventId, request.Email);
            if (!check.Success) return check;

            _repo.AddRegistration(request with
            {
                StudentId = request.StudentId.Trim(),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim()
            });
            return ServiceResult.Ok(201, "Registration successful!");
        }

        // Returns null when the event does not exist (controller maps this to 404).
        public IReadOnlyList<AttendeeDto>? GetAttendees(int eventId) =>
            _repo.EventExists(eventId) ? _repo.GetAttendees(eventId) : null;

        public string? GetUserRegistration(string inputEmail) => _repo.GetUserRegistration(inputEmail);
    }

    // ---------- SQL Server implementation: parameterized queries + using disposal ----------
    // NOTE: table/column names assume Users, Events, Registrations. Adjust to your /database/schema.sql.
    public class SqlEventRepository : IEventRepository
    {
        private readonly string _connectionString;

        public SqlEventRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public bool EventExists(int eventId)
        {
            const string sql = "SELECT COUNT(1) FROM Events WHERE EventId = @EventId";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public int GetRemainingSeats(int eventId)
        {
            const string sql = @"SELECT e.Capacity - COUNT(r.RegistrationId)
                                 FROM Events e
                                 LEFT JOIN Registrations r ON r.EventId = e.EventId
                                 WHERE e.EventId = @EventId
                                 GROUP BY e.Capacity";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
            conn.Open();
            object? result = cmd.ExecuteScalar();
            return result is null ? 0 : Convert.ToInt32(result);
        }

        public bool IsAlreadyRegistered(int eventId, string email)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM Registrations r
                                 INNER JOIN Users u ON u.UserId = r.UserId
                                 WHERE r.EventId = @EventId AND u.Email = @Email";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = email;
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public void AddRegistration(RegistrationRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                int userId;

                using (var find = new SqlCommand(
                    "SELECT UserId FROM Users WHERE StudentId = @StudentId", conn, tx))
                {
                    find.Parameters.Add("@StudentId", SqlDbType.NVarChar, 50).Value = request.StudentId;
                    object? existing = find.ExecuteScalar();

                    if (existing is not null)
                    {
                        userId = Convert.ToInt32(existing);
                    }
                    else
                    {
                        using var insertUser = new SqlCommand(
                            @"INSERT INTO Users (StudentId, FullName, Email)
                              OUTPUT INSERTED.UserId
                              VALUES (@StudentId, @FullName, @Email)", conn, tx);
                        insertUser.Parameters.Add("@StudentId", SqlDbType.NVarChar, 50).Value = request.StudentId;
                        insertUser.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = request.FullName;
                        insertUser.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = request.Email;
                        userId = Convert.ToInt32(insertUser.ExecuteScalar());
                    }
                }

                using (var insertReg = new SqlCommand(
                    "INSERT INTO Registrations (EventId, UserId) VALUES (@EventId, @UserId)", conn, tx))
                {
                    insertReg.Parameters.Add("@EventId", SqlDbType.Int).Value = request.EventId;
                    insertReg.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    insertReg.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public IReadOnlyList<AttendeeDto> GetAttendees(int eventId)
        {
            const string sql = @"SELECT u.StudentId, u.FullName, u.Email, r.RegistrationDate
                                 FROM Registrations r
                                 INNER JOIN Users u ON u.UserId = r.UserId
                                 WHERE r.EventId = @EventId
                                 ORDER BY r.RegistrationDate";
            var list = new List<AttendeeDto>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@EventId", SqlDbType.Int).Value = eventId;
            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AttendeeDto(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
            }
            return list;
        }

        // Refactored from the Task 4 flawed snippet:
        // parameterized query, 'using' disposal, null-safe result, no SELECT * with ExecuteScalar.
        public string? GetUserRegistration(string inputEmail)
        {
            if (string.IsNullOrWhiteSpace(inputEmail))
                throw new ArgumentException("Email is required.", nameof(inputEmail));

            const string sql = @"SELECT TOP 1 r.RegistrationId
                                 FROM Registrations r
                                 INNER JOIN Users u ON u.UserId = r.UserId
                                 WHERE u.Email = @Email";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = inputEmail;
                conn.Open();
                return cmd.ExecuteScalar()?.ToString();
            }
        }
    }
}