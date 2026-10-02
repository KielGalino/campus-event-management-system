using System;
using Xunit;
using Moq;
using CampusEventSystem.Backend;

namespace CampusEventSystem.Tests
{
    public class ValidationTests
    {
        private readonly Mock<IEventRepository> _mockRepo;
        private readonly RegistrationValidator _validator;

        public ValidationTests()
        {
            _mockRepo = new Mock<IEventRepository>();
            _validator = new RegistrationValidator(_mockRepo.Object);
        }

        [Fact]
        public void ValidateRegistration_ValidEmailAndAvailableSeats_ReturnsSuccess()
        {
            // Arrange
            int eventId = 101;
            string validEmail = "20241001@univ.edu.ph";

            _mockRepo.Setup(r => r.IsEventFull(eventId)).Returns(false);
            _mockRepo.Setup(r => r.IsStudentAlreadyRegistered(eventId, validEmail)).Returns(false);

            // Act
            ValidationResult result = _validator.ValidateRegistration(eventId, validEmail);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateRegistration_NullOrEmptyEmail_ReturnsFailure(string invalidEmail)
        {
            // Arrange
            int eventId = 101;

            // Act
            ValidationResult result = _validator.ValidateRegistration(eventId, invalidEmail);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Email cannot be empty.", result.ErrorMessage);
            _mockRepo.Verify(r => r.IsEventFull(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData("student@gmail.com")]
        [InlineData("student@univ.edu")]
        [InlineData("student@otheruniv.edu.ph.com")]
        public void ValidateRegistration_WrongDomain_ReturnsFailure(string wrongDomainEmail)
        {
            // Arrange
            int eventId = 101;

            // Act
            ValidationResult result = _validator.ValidateRegistration(eventId, wrongDomainEmail);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Email must belong to @univ.edu.ph.", result.ErrorMessage);
            _mockRepo.Verify(r => r.IsEventFull(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ValidateRegistration_EventIsFull_ReturnsFailure()
        {
            // Arrange
            int eventId = 101;
            string validEmail = "student@univ.edu.ph";

            _mockRepo.Setup(r => r.IsEventFull(eventId)).Returns(true);

            // Act
            ValidationResult result = _validator.ValidateRegistration(eventId, validEmail);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Event has reached maximum seat capacity.", result.ErrorMessage);
            _mockRepo.Verify(r => r.IsStudentAlreadyRegistered(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ValidateRegistration_AlreadyRegistered_ReturnsFailure()
        {
            // Arrange
            int eventId = 101;
            string validEmail = "student@univ.edu.ph";

            _mockRepo.Setup(r => r.IsEventFull(eventId)).Returns(false);
            _mockRepo.Setup(r => r.IsStudentAlreadyRegistered(eventId, validEmail)).Returns(true);

            // Act
            ValidationResult result = _validator.ValidateRegistration(eventId, validEmail);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Student is already registered for this event.", result.ErrorMessage);
        }
    }
}