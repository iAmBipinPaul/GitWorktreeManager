using Xunit;
using FluentAssertions;

namespace GitWorktreeManager.Tests;

/// <summary>
/// Sample test to verify the test project is configured correctly.
/// </summary>
public class SampleTest
{
    [Fact]
    public void TestProjectSetup_ShouldWork()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }
}
