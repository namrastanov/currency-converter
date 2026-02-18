using CurrencyConverter.Application.Interfaces;
using CurrencyConverter.Domain.Constants;
using CurrencyConverter.Infrastructure.Auth;
using FluentAssertions;
using Moq;

namespace CurrencyConverter.UnitTests.Infrastructure;

public class InMemoryUserRepositoryTests
{
    private readonly InMemoryUserRepository _repository;
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    public InMemoryUserRepositoryTests()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hashed_{p}");
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string p, string h) => h == $"hashed_{p}");
        _repository = new InMemoryUserRepository(_passwordHasher.Object, TimeProvider.System);
    }

    [Fact]
    public void Constructor_ShouldCreateDefaultAdminUser()
    {
        var admin = _repository.GetByUsername("admin");

        admin.Should().NotBeNull();
        admin!.Username.Should().Be("admin");
        admin.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void Create_ShouldAddNewUser()
    {
        var user = _repository.Create("testuser", "hashed_password123", AppRoles.User);

        user.Should().NotBeNull();
        user.Username.Should().Be("testuser");
        user.Role.Should().Be(AppRoles.User);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldAssignSpecifiedRole()
    {
        var user = _repository.Create("adminuser", "hashed_password", AppRoles.Admin);

        user.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void TryCreate_ShouldReturnCreatedTrue_WhenUsernameIsNew()
    {
        var (created, user) = _repository.TryCreate("newuser", "hashed_password", AppRoles.User);

        created.Should().BeTrue();
        user.Username.Should().Be("newuser");
    }

    [Fact]
    public void TryCreate_ShouldReturnCreatedFalse_WhenUsernameExists()
    {
        _repository.Create("existing", "hashed_password", AppRoles.User);

        var (created, user) = _repository.TryCreate("existing", "hashed_password", AppRoles.User);

        created.Should().BeFalse();
        user.Username.Should().Be("existing");
    }

    [Fact]
    public void GetByUsername_ShouldReturnNull_WhenNotFound()
    {
        var user = _repository.GetByUsername("nonexistent");

        user.Should().BeNull();
    }

    [Fact]
    public void GetByUsername_ShouldBeCaseInsensitive()
    {
        _repository.Create("TestUser", "hashed_password", AppRoles.User);

        var user = _repository.GetByUsername("testuser");

        user.Should().NotBeNull();
        user!.Username.Should().Be("TestUser");
    }

    [Fact]
    public void GetById_ShouldReturnUser_WhenExists()
    {
        var created = _repository.Create("testuser", "hashed_password", AppRoles.User);

        var user = _repository.GetById(created.Id);

        user.Should().NotBeNull();
        user!.Id.Should().Be(created.Id);
    }

    [Fact]
    public void GetById_ShouldReturnNull_WhenNotExists()
    {
        var user = _repository.GetById(Guid.NewGuid());

        user.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnAllUsers()
    {
        _repository.Create("user1", "hashed_password", AppRoles.User);
        _repository.Create("user2", "hashed_password", AppRoles.User);

        var all = _repository.GetAll();

        all.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void UpdateRole_ShouldReturnTrue_WhenUserExists()
    {
        var user = _repository.Create("testuser", "hashed_password", AppRoles.User);

        var result = _repository.UpdateRole(user.Id, AppRoles.Admin);

        result.Should().BeTrue();
        _repository.GetById(user.Id)!.Role.Should().Be(AppRoles.Admin);
    }

    [Fact]
    public void UpdateRole_ShouldReturnFalse_WhenUserNotExists()
    {
        var result = _repository.UpdateRole(Guid.NewGuid(), AppRoles.Admin);

        result.Should().BeFalse();
    }

    [Fact]
    public void Delete_ShouldReturnTrue_WhenUserExists()
    {
        var user = _repository.Create("testuser", "hashed_password", AppRoles.User);

        var result = _repository.Delete(user.Id);

        result.Should().BeTrue();
        _repository.GetById(user.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_ShouldReturnFalse_WhenUserNotExists()
    {
        var result = _repository.Delete(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
