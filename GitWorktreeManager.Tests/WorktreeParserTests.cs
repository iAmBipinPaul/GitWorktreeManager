using Xunit;
using FluentAssertions;
using GitWorktreeManager.Services;

namespace GitWorktreeManager.Tests;

/// <summary>
/// Unit tests for WorktreeParser.ParsePorcelainOutput method.
/// </summary>
public class WorktreeParserTests
{
    [Fact]
    public void ParsePorcelainOutput_WithEmptyString_ReturnsEmptyList()
    {
        // Arrange
        var output = "";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParsePorcelainOutput_WithWhitespaceOnly_ReturnsEmptyList()
    {
        // Arrange
        var output = "   \n\n   ";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParsePorcelainOutput_WithSingleWorktree_ParsesCorrectly()
    {
        // Arrange
        var output = "worktree /path/to/main\nHEAD abc123def456\nbranch refs/heads/main\n";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be("/path/to/main");
        result[0].HeadCommit.Should().Be("abc123def456");
        result[0].Branch.Should().Be("main");
        result[0].IsMainWorktree.Should().BeTrue();
        result[0].IsDetached.Should().BeFalse();
    }

    [Fact]
    public void ParsePorcelainOutput_WithMultipleWorktrees_MarksFirstAsMain()
    {
        // Arrange
        var output = @"worktree /path/to/main
HEAD abc123def456
branch refs/heads/main

worktree /path/to/feature
HEAD def456abc789
branch refs/heads/feature-branch";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(2);
        result[0].IsMainWorktree.Should().BeTrue();
        result[1].IsMainWorktree.Should().BeFalse();
    }


    [Fact]
    public void ParsePorcelainOutput_WithDetachedHead_SetsIsDetachedTrue()
    {
        // Arrange
        var output = @"worktree /path/to/detached
HEAD 789abc123def
detached";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Branch.Should().BeNull();
        result[0].IsDetached.Should().BeTrue();
    }

    [Fact]
    public void ParsePorcelainOutput_WithLockedWorktree_SetsIsLockedTrue()
    {
        // Arrange
        var output = @"worktree /path/to/locked
HEAD abc123
branch refs/heads/feature
locked";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsLocked.Should().BeTrue();
        result[0].LockReason.Should().BeNull();
    }

    [Fact]
    public void ParsePorcelainOutput_WithLockedWorktreeAndReason_ParsesLockReason()
    {
        // Arrange
        var output = @"worktree /path/to/locked
HEAD abc123
branch refs/heads/feature
locked
locked reason: Working on critical fix";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsLocked.Should().BeTrue();
        result[0].LockReason.Should().Be("Working on critical fix");
    }

    [Fact]
    public void ParsePorcelainOutput_WithPrunableWorktree_SetsIsPrunableTrue()
    {
        // Arrange
        var output = @"worktree /path/to/prunable
HEAD abc123
branch refs/heads/old-branch
prunable";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsPrunable.Should().BeTrue();
    }

    [Fact]
    public void ParsePorcelainOutput_WithComplexScenario_ParsesAllFields()
    {
        // Arrange - matches the example from design doc
        var output = @"worktree /path/to/main
HEAD abc123def456
branch refs/heads/main

worktree /path/to/feature
HEAD def456abc789
branch refs/heads/feature-branch
locked
locked reason: Working on critical fix

worktree /path/to/detached
HEAD 789abc123def
detached
prunable";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(3);

        // Main worktree
        result[0].Path.Should().Be("/path/to/main");
        result[0].HeadCommit.Should().Be("abc123def456");
        result[0].Branch.Should().Be("main");
        result[0].IsMainWorktree.Should().BeTrue();
        result[0].IsLocked.Should().BeFalse();
        result[0].IsPrunable.Should().BeFalse();

        // Feature worktree (locked)
        result[1].Path.Should().Be("/path/to/feature");
        result[1].HeadCommit.Should().Be("def456abc789");
        result[1].Branch.Should().Be("feature-branch");
        result[1].IsMainWorktree.Should().BeFalse();
        result[1].IsLocked.Should().BeTrue();
        result[1].LockReason.Should().Be("Working on critical fix");

        // Detached worktree (prunable)
        result[2].Path.Should().Be("/path/to/detached");
        result[2].HeadCommit.Should().Be("789abc123def");
        result[2].Branch.Should().BeNull();
        result[2].IsDetached.Should().BeTrue();
        result[2].IsPrunable.Should().BeTrue();
    }

    [Fact]
    public void ParsePorcelainOutput_WithWindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        var output = "worktree C:\\path\\to\\main\r\nHEAD abc123\r\nbranch refs/heads/main\r\n\r\nworktree C:\\path\\to\\feature\r\nHEAD def456\r\nbranch refs/heads/feature";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(2);
        result[0].Path.Should().Be("C:\\path\\to\\main");
        result[1].Path.Should().Be("C:\\path\\to\\feature");
    }

    [Fact]
    public void ParsePorcelainOutput_WithMissingPath_SkipsInvalidBlock()
    {
        // Arrange
        var output = @"HEAD abc123
branch refs/heads/main

worktree /valid/path
HEAD def456
branch refs/heads/feature";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be("/valid/path");
    }

    [Fact]
    public void ParsePorcelainOutput_WithMissingHead_SkipsInvalidBlock()
    {
        // Arrange
        var output = @"worktree /invalid/path
branch refs/heads/main

worktree /valid/path
HEAD def456
branch refs/heads/feature";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Path.Should().Be("/valid/path");
    }

    [Fact]
    public void ParsePorcelainOutput_ExtractsBranchNameWithoutRefsHeadsPrefix()
    {
        // Arrange
        var output = @"worktree /path/to/repo
HEAD abc123
branch refs/heads/feature/my-feature";

        // Act
        var result = WorktreeParser.ParsePorcelainOutput(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Branch.Should().Be("feature/my-feature");
    }
}
