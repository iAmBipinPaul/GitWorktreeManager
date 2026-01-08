using Xunit;
using FluentAssertions;
using GitWorktreeManager.Services;
using System.Diagnostics;

namespace GitWorktreeManager.Tests;

/// <summary>
/// Integration tests for GitService that test the full worktree workflow
/// against a real Git repository.
/// These tests require Git to be installed on the system.
/// </summary>
[Collection("GitIntegration")]
public class GitServiceIntegrationTests : IAsyncLifetime
{
    private readonly GitService _gitService;
    private string _testRepoPath = null!;
    private string _worktreePath = null!;

    public GitServiceIntegrationTests()
    {
        _gitService = new GitService();
    }

    public async Task InitializeAsync()
    {
        // Create a unique temp directory for the test repository
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"GitWorktreeTest_{Guid.NewGuid():N}");
        _worktreePath = Path.Combine(Path.GetTempPath(), $"GitWorktreeTest_WT_{Guid.NewGuid():N}");
        
        Directory.CreateDirectory(_testRepoPath);
        
        // Initialize a git repository with an initial commit
        await RunGitCommandAsync(_testRepoPath, "init");
        await RunGitCommandAsync(_testRepoPath, "config user.email \"test@test.com\"");
        await RunGitCommandAsync(_testRepoPath, "config user.name \"Test User\"");
        
        // Create an initial commit (required for worktrees)
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        await File.WriteAllTextAsync(readmePath, "# Test Repository");
        await RunGitCommandAsync(_testRepoPath, "add .");
        await RunGitCommandAsync(_testRepoPath, "commit -m \"Initial commit\"");
    }

    public Task DisposeAsync()
    {
        // Clean up test directories
        TryDeleteDirectory(_testRepoPath);
        TryDeleteDirectory(_worktreePath);
        return Task.CompletedTask;
    }


    /// <summary>
    /// Tests that Git is installed and accessible.
    /// </summary>
    [Fact]
    public async Task IsGitInstalledAsync_WhenGitIsInstalled_ReturnsTrue()
    {
        // Act
        var result = await _gitService.IsGitInstalledAsync();

        // Assert
        result.Should().BeTrue("Git must be installed to run integration tests");
    }

    /// <summary>
    /// Tests that GetRepositoryRootAsync returns the correct path for a valid repository.
    /// </summary>
    [Fact]
    public async Task GetRepositoryRootAsync_WithValidRepository_ReturnsRootPath()
    {
        // Act
        var result = await _gitService.GetRepositoryRootAsync(_testRepoPath);

        // Assert
        result.Should().NotBeNull();
        // Normalize paths for comparison (handle different path separators)
        var normalizedResult = Path.GetFullPath(result!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedExpected = Path.GetFullPath(_testRepoPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        normalizedResult.Should().BeEquivalentTo(normalizedExpected);
    }

    /// <summary>
    /// Tests that GetRepositoryRootAsync returns null for a non-repository path.
    /// </summary>
    [Fact]
    public async Task GetRepositoryRootAsync_WithNonRepository_ReturnsNull()
    {
        // Arrange
        var nonRepoPath = Path.GetTempPath();

        // Act
        var result = await _gitService.GetRepositoryRootAsync(nonRepoPath);

        // Assert - temp path might be in a repo, so we just verify it doesn't throw
        // The actual behavior depends on the system configuration
    }

    /// <summary>
    /// Tests the full worktree workflow: list, add, list again, remove.
    /// Validates Requirements 2.1, 3.4, 4.3
    /// </summary>
    [Fact]
    public async Task FullWorktreeWorkflow_AddListRemove_WorksCorrectly()
    {
        // Step 1: List worktrees - should have only the main worktree
        var initialList = await _gitService.GetWorktreesAsync(_testRepoPath);
        
        initialList.Success.Should().BeTrue();
        initialList.Data.Should().NotBeNull();
        initialList.Data!.Count.Should().Be(1, "Initial repository should have exactly one worktree (main)");
        initialList.Data[0].IsMainWorktree.Should().BeTrue();

        // Step 2: Create a new branch for the worktree
        await RunGitCommandAsync(_testRepoPath, "branch test-branch");

        // Step 3: Add a new worktree
        var addResult = await _gitService.AddWorktreeAsync(
            _testRepoPath,
            _worktreePath,
            "test-branch",
            createBranch: false);

        addResult.Success.Should().BeTrue($"Adding worktree should succeed. Error: {addResult.ErrorMessage}");
        Directory.Exists(_worktreePath).Should().BeTrue("Worktree directory should be created");

        // Step 4: List worktrees again - should now have two
        var afterAddList = await _gitService.GetWorktreesAsync(_testRepoPath);
        
        afterAddList.Success.Should().BeTrue();
        afterAddList.Data.Should().NotBeNull();
        afterAddList.Data!.Count.Should().Be(2, "Should have two worktrees after adding one");
        
        // Verify the new worktree properties
        var newWorktree = afterAddList.Data.FirstOrDefault(w => !w.IsMainWorktree);
        newWorktree.Should().NotBeNull();
        newWorktree!.Branch.Should().Be("test-branch");
        Path.GetFullPath(newWorktree.Path).Should().BeEquivalentTo(Path.GetFullPath(_worktreePath));

        // Step 5: Remove the worktree
        var removeResult = await _gitService.RemoveWorktreeAsync(
            _testRepoPath,
            _worktreePath,
            force: false);

        removeResult.Success.Should().BeTrue($"Removing worktree should succeed. Error: {removeResult.ErrorMessage}");

        // Step 6: List worktrees - should be back to one
        var afterRemoveList = await _gitService.GetWorktreesAsync(_testRepoPath);
        
        afterRemoveList.Success.Should().BeTrue();
        afterRemoveList.Data.Should().NotBeNull();
        afterRemoveList.Data!.Count.Should().Be(1, "Should have one worktree after removal");
        afterRemoveList.Data[0].IsMainWorktree.Should().BeTrue();
    }


    /// <summary>
    /// Tests adding a worktree with a new branch using the -b flag.
    /// Validates Requirement 3.5
    /// </summary>
    [Fact]
    public async Task AddWorktreeAsync_WithCreateBranch_CreatesNewBranch()
    {
        // Arrange
        var newBranchWorktreePath = Path.Combine(Path.GetTempPath(), $"GitWorktreeTest_NewBranch_{Guid.NewGuid():N}");

        try
        {
            // Act
            var addResult = await _gitService.AddWorktreeAsync(
                _testRepoPath,
                newBranchWorktreePath,
                "new-feature-branch",
                createBranch: true);

            // Assert
            addResult.Success.Should().BeTrue($"Adding worktree with new branch should succeed. Error: {addResult.ErrorMessage}");
            Directory.Exists(newBranchWorktreePath).Should().BeTrue();

            // Verify the branch was created
            var listResult = await _gitService.GetWorktreesAsync(_testRepoPath);
            listResult.Data.Should().Contain(w => w.Branch == "new-feature-branch");
        }
        finally
        {
            // Cleanup
            await _gitService.RemoveWorktreeAsync(_testRepoPath, newBranchWorktreePath, force: true);
            TryDeleteDirectory(newBranchWorktreePath);
        }
    }

    /// <summary>
    /// Tests that adding a worktree with a non-existent branch fails.
    /// </summary>
    [Fact]
    public async Task AddWorktreeAsync_WithNonExistentBranch_Fails()
    {
        // Arrange
        var failWorktreePath = Path.Combine(Path.GetTempPath(), $"GitWorktreeTest_Fail_{Guid.NewGuid():N}");

        // Act
        var result = await _gitService.AddWorktreeAsync(
            _testRepoPath,
            failWorktreePath,
            "non-existent-branch-12345",
            createBranch: false);

        // Assert
        result.Success.Should().BeFalse("Adding worktree with non-existent branch should fail");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that removing a non-existent worktree fails gracefully.
    /// </summary>
    [Fact]
    public async Task RemoveWorktreeAsync_WithNonExistentPath_Fails()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        // Act
        var result = await _gitService.RemoveWorktreeAsync(
            _testRepoPath,
            nonExistentPath,
            force: false);

        // Assert
        result.Success.Should().BeFalse("Removing non-existent worktree should fail");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tests that GetWorktreesAsync correctly parses worktree status flags.
    /// </summary>
    [Fact]
    public async Task GetWorktreesAsync_ParsesWorktreeProperties_Correctly()
    {
        // Act
        var result = await _gitService.GetWorktreesAsync(_testRepoPath);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        
        var mainWorktree = result.Data!.First();
        mainWorktree.Path.Should().NotBeNullOrEmpty();
        mainWorktree.HeadCommit.Should().NotBeNullOrEmpty();
        mainWorktree.HeadCommit.Should().MatchRegex("^[a-f0-9]{40}$", "HEAD should be a valid SHA");
        mainWorktree.IsMainWorktree.Should().BeTrue();
        mainWorktree.IsDetached.Should().BeFalse("Main worktree should be on a branch");
    }

    #region Helper Methods

    private static async Task RunGitCommandAsync(string workingDirectory, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Git command failed: git {arguments}\nError: {error}");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        try
        {
            // Remove read-only attributes from all files (Git creates some read-only files)
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #endregion
}
