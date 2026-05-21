using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiRental.Domain.Models;
using SkiRental.Logic.Interfaces;
using SkiRental.Logic.Services;
using Xunit;

namespace SkiRental.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_repo.Object, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateUserAsync_StoresBcryptHash_VerifiableWithPlainPassword()
    {
        AppUser? captured = null;
        _repo.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, CancellationToken>((u, _) =>
            {
                u.Id = 7;
                captured = u;
            })
            .ReturnsAsync((AppUser u, CancellationToken _) => u);

        var result = await _sut.CreateUserAsync("newuser", "password123", AppRole.Customer);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.PasswordHash.Should().StartWith("$2");
        BCrypt.Net.BCrypt.Verify("password123", captured.PasswordHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("wrong", captured.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task RecordFailedLoginAsync_AfterFiveAttempts_LocksAccount()
    {
        var user = new AppUser
        {
            Id = 1,
            Username = "tester",
            PasswordHash = "hash",
            FailedLoginAttempts = 4,
        };

        _repo.Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RecordFailedLoginAsync(user);

        user.FailedLoginAttempts.Should().Be(5);
        user.IsLocked.Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
        user.IsCurrentlyLocked.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSuccessfulLoginAsync_ResetsLockoutAndAttempts()
    {
        var user = new AppUser
        {
            Id = 2,
            Username = "locked",
            PasswordHash = "hash",
            FailedLoginAttempts = 5,
            IsLocked = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(15),
        };

        _repo.Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RecordSuccessfulLoginAsync(user);

        user.FailedLoginAttempts.Should().Be(0);
        user.IsLocked.Should().BeFalse();
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().NotBeNull();
        user.IsCurrentlyLocked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_ShortPassword_FailsValidation()
    {
        var result = await _sut.CreateUserAsync("ab", "short", AppRole.Customer);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("3 znaki");
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_Fails()
    {
        _repo.Setup(r => r.UsernameExistsAsync("taken", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateUserAsync("taken", "password123", AppRole.Customer);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("zajęta");
    }
}
