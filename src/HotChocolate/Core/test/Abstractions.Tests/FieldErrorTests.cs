namespace HotChocolate;

public class FieldErrorTests
{
    [Fact]
    public void Constructor_SingleError_Null_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FieldError(((object?)null)!));
    }

    [Fact]
    public void Constructor_ErrorList_Null_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FieldError(null!));
    }

    [Fact]
    public void Constructor_SingleError_Valid_DoesNotThrow()
    {
        // Arrange
        var error = new object();

        // Act & Assert
        var mutationError = new FieldError(error);
        Assert.Single(mutationError.Errors);
        Assert.Equal(error, mutationError.Errors[0]);
    }

    [Fact]
    public void Constructor_ErrorList_Valid_DoesNotThrow()
    {
        // Arrange
        var errors = new List<object> { new object(), new object(), };

        // Act
        var mutationError = new FieldError(errors);

        // Assert
        Assert.Equal(2, mutationError.Errors.Count);
    }

    [Fact]
    public void Constructor_ErrorList_Empty_ThrowsArgumentException()
    {
        // Arrange
        var errors = new List<object>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FieldError(errors));
    }

    [Fact]
    public void IsSuccess_ReturnsFalse()
    {
        // Arrange
        var mutationError = new FieldError(new object());

        // Act
        var isSuccess = mutationError.IsSuccess;

        // Assert
        Assert.False(isSuccess);
    }

    [Fact]
    public void IsError_ReturnsTrue()
    {
        // Arrange
        var mutationError = new FieldError(new object());

        // Act
        var isError = mutationError.IsError;

        // Assert
        Assert.True(isError);
    }
}
