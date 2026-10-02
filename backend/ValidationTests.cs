using CampusEvents.Backend;
using Moq;
using Xunit;

namespace CampusEvents.Tests
{
    public class ValidationTests
    {
        private readonly Mock<IEventRepository> _repo = new();
        private RegistrationValidator Validator() => new(_repo.Object);

        private void Arrange(bool eventExists = true, bool alreadyRegistered = false, int seats = 5)
        {
            _repo.Setup(r => r.EventExists(1)).Returns(eventExists);
            _repo.Setup(r => r.IsAlreadyRegistered(1, It.IsAny<string>())).Returns(alreadyRegistered);
            _repo.Setup(r => r.GetRemainingSeats(1)).Returns(seats);
        }

        [Theory]
        [InlineData("juan@univ.edu.ph", true)]
        [InlineData("JUAN@UNIV.EDU.PH", true)]
        [InlineData("juan@gmail.com", false)]
        [InlineData("@univ.edu.ph", false)]
        [InlineData("juan@univ.edu.ph.evil.com", false)]
        [InlineData("a@b@univ.edu.ph", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidStudentEmail_ChecksDomain(string? email, bool expected)
            => Assert.Equal(expected, Validator().IsValidStudentEmail(email));

        [Fact]
        public void Validate_SeatsAvailable_Succeeds()
        {
            Arrange();
            Assert.True(Validator().Validate(1, "a@univ.edu.ph").Success);
        }

        [Fact]
        public void Validate_EventFull_Returns409()
        {
            Arrange(seats: 0);
            var result = Validator().Validate(1, "a@univ.edu.ph");
            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
        }

        [Fact]
        public void Validate_AlreadyRegistered_Returns409()
        {
            Arrange(alreadyRegistered: true);
            Assert.Equal(409, Validator().Validate(1, "a@univ.edu.ph").StatusCode);
        }

        [Fact]
        public void Validate_UnknownEvent_Returns404()
        {
            Arrange(eventExists: false);
            Assert.Equal(404, Validator().Validate(1, "a@univ.edu.ph").StatusCode);
        }

        [Fact]
        public void Validate_InvalidEmail_Returns400_AndNeverHitsDatabase()
        {
            var result = Validator().Validate(1, "a@gmail.com");
            Assert.Equal(400, result.StatusCode);
            _repo.Verify(r => r.EventExists(It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.GetRemainingSeats(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void Register_ValidRequest_SavesOnce_AndReturns201()
        {
            Arrange();
            var service = new RegistrationService(_repo.Object, Validator());

            var result = service.Register(new RegistrationRequest(1, "2021-0001", "Juan Dela Cruz", "juan@univ.edu.ph"));

            Assert.Equal(201, result.StatusCode);
            _repo.Verify(r => r.AddRegistration(It.IsAny<RegistrationRequest>()), Times.Once);
        }

        [Fact]
        public void Register_EventFull_DoesNotSave()
        {
            Arrange(seats: 0);
            var service = new RegistrationService(_repo.Object, Validator());

            var result = service.Register(new RegistrationRequest(1, "2021-0001", "Juan Dela Cruz", "juan@univ.edu.ph"));

            Assert.Equal(409, result.StatusCode);
            _repo.Verify(r => r.AddRegistration(It.IsAny<RegistrationRequest>()), Times.Never);
        }

        [Fact]
        public void Register_MissingName_Returns400()
        {
            var service = new RegistrationService(_repo.Object, Validator());
            var result = service.Register(new RegistrationRequest(1, "2021-0001", "  ", "juan@univ.edu.ph"));
            Assert.Equal(400, result.StatusCode);
        }
    }
}