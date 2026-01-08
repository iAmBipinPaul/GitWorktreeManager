namespace GitWorktreeManager.Services;

using System.Diagnostics;
using System.Text;
using GitWorktreeManager.Models;

/// <summary>
/// Service for executing Git worktree commands using the Git CLI.
/// </summary>
public class GitService : IGitService
{
    private const int DefaultTimeoutMs = 30000; // 30 seconds
    private const string GitExecutable = "git";
    private readonly ILoggerService? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitService"/> class.
    /// </summary>
    public GitService() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GitService"/> class with logging support.
    /// </summary>
    /// <param name="logger">The logger service for recording operations.</param>
    public GitService(ILoggerService? logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GitCommandResult<IReadOnlyList<Worktree>>> GetWorktreesAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation($"Getting worktrees for repository: {repositoryPath}");

        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            "worktree list --porcelain",
            cancellationToken);

        if (!result.Success)
        {
            _logger?.LogError($"Failed to list worktrees: {result.ErrorMessage}");
            return GitCommandResult<IReadOnlyList<Worktree>>.Fail(
                result.ErrorMessage ?? "Failed to list worktrees",
                result.ExitCode);
        }

        var worktrees = WorktreeParser.ParsePorcelainOutput(result.Output ?? string.Empty);
        _logger?.LogInformation($"Found {worktrees.Count} worktree(s)");
        return GitCommandResult<IReadOnlyList<Worktree>>.Ok(worktrees);
    }

    /// <inheritdoc />
    public async Task<GitCommandResult> AddWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        string branchName,
        bool createBranch = false,
        string? baseBranch = null,
        CancellationToken cancellationToken = default)
    {
        // Build command: git worktree add [-b <branch>] <path> [<commit-ish>]
        // With -b: git worktree add -b <new-branch> <path> [<base-branch>]
        // Without -b: git worktree add <path> <branch>
        string arguments;
        
        if (createBranch)
        {
            // Create new branch based on another branch
            if (!string.IsNullOrEmpty(baseBranch))
            {
                arguments = $"worktree add -b \"{branchName}\" \"{worktreePath}\" \"{baseBranch}\"";
            }
            else
            {
                arguments = $"worktree add -b \"{branchName}\" \"{worktreePath}\"";
            }
        }
        else
        {
            // Checkout existing branch
            arguments = $"worktree add \"{worktreePath}\" \"{branchName}\"";
        }

        _logger?.LogInformation($"Adding worktree: path='{worktreePath}', branch='{branchName}', createBranch={createBranch}, baseBranch='{baseBranch}'");

        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            arguments,
            cancellationToken);

        if (result.Success)
        {
            _logger?.LogInformation($"Successfully added worktree at '{worktreePath}'");
            return GitCommandResult.Ok();
        }
        else
        {
            _logger?.LogError($"Failed to add worktree: {result.ErrorMessage}");
            return GitCommandResult.Fail(result.ErrorMessage ?? "Failed to add worktree", result.ExitCode);
        }
    }

    /// <inheritdoc />
    public async Task<GitCommandResult> RemoveWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        // Build command: git worktree remove [--force] <path>
        var arguments = force
            ? $"worktree remove --force \"{worktreePath}\""
            : $"worktree remove \"{worktreePath}\"";

        _logger?.LogInformation($"Removing worktree: path='{worktreePath}', force={force}");

        var result = await ExecuteGitCommandAsync(
            repositoryPath,
            arguments,
            cancellationToken);

        if (result.Success)
        {
            _logger?.LogInformation($"Successfully removed worktree at '{worktreePath}'");
            return GitCommandResult.Ok();
        }
        else
        {
            _logger?.LogError($"Failed to remove worktree: {result.ErrorMessage}");
            return GitCommandResult.Fail(result.ErrorMessage ?? "Failed to remove worktree", result.ExitCode);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetRepositoryRootAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation($"Getting repository root for path: {path}");

        var result = await ExecuteGitCommandAsync(
            path,
            "rev-parse --show-toplevel",
            cancellationToken);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            _logger?.LogWarning($"Path '{path}' is not within a Git repository");
            return null;
        }

        var root = result.Output.Trim();
        _logger?.LogInformation($"Repository root: {root}");
        return root;
    }

    /// <inheritdoc />
    public async Task<bool> IsGitInstalledAsync(
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Checking if Git is installed");

        try
        {
            var result = await ExecuteGitCommandAsync(
                Directory.GetCurrentDirectory(),
                "--version",
                cancellationToken);

            if (result.Success)
            {
                _logger?.LogInformation($"Git is installed: {result.Output?.Trim()}");
            }
            else
            {
                _logger?.LogWarning("Git is not installed or not in PATH");
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Error checking Git installation");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<GitCommandResult<IReadOnlyList<string>>> GetBranchesAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation($"Getting branches for repository: {repositoryPath}");

        // Get local branches
        var localResult = await ExecuteGitCommandAsync(
            repositoryPath,
            "branch --format=%(refname:short)",
            cancellationToken);

        // Get remote branches
        var remoteResult = await ExecuteGitCommandAsync(
            repositoryPath,
            "branch -r --format=%(refname:short)",
            cancellationToken);

        var branches = new List<string>();

        if (localResult.Success && !string.IsNullOrWhiteSpace(localResult.Output))
        {
            var localBranches = localResult.Output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim())
                .Where(b => !string.IsNullOrEmpty(b));
            branches.AddRange(localBranches);
        }

        if (remoteResult.Success && !string.IsNullOrWhiteSpace(remoteResult.Output))
        {
            var remoteBranches = remoteResult.Output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim())
                .Where(b => !string.IsNullOrEmpty(b) && !b.Contains("HEAD"))
                // Remove origin/ prefix for cleaner display but keep track it's remote
                .Select(b => b.StartsWith("origin/") ? b.Substring(7) : b)
                .Where(b => !branches.Contains(b)); // Don't duplicate local branches
            branches.AddRange(remoteBranches);
        }

        _logger?.LogInformation($"Found {branches.Count} branch(es)");
        return GitCommandResult<IReadOnlyList<string>>.Ok(branches.Distinct().OrderBy(b => b).ToList());
    }


    /// <summary>
    /// Executes a Git command and returns the result.
    /// </summary>
    /// <param name="workingDirectory">The working directory for the command.</param>
    /// <param name="arguments">The Git command arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the command execution.</returns>
    private async Task<GitProcessResult> ExecuteGitCommandAsync(
        string workingDirectory,
        string arguments,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation($"Executing: git {arguments} (in {workingDirectory})");

        var startInfo = new ProcessStartInfo
        {
            FileName = GitExecutable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdout.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderr.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process with timeout and cancellation support
            using var timeoutCts = new CancellationTokenSource(DefaultTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                TryKillProcess(process);
                _logger?.LogError($"Git command timed out after {DefaultTimeoutMs}ms: git {arguments}");
                return new GitProcessResult
                {
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = "Git command timed out"
                };
            }
            catch (OperationCanceledException)
            {
                TryKillProcess(process);
                _logger?.LogWarning($"Git command was cancelled: git {arguments}");
                return new GitProcessResult
                {
                    Success = false,
                    ExitCode = -1,
                    ErrorMessage = "Git command was cancelled"
                };
            }

            var exitCode = process.ExitCode;
            var output = stdout.ToString();
            var error = stderr.ToString();

            // Log stdout if present
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger?.LogInformation($"Git stdout:\n{output.Trim()}");
            }

            // Log stderr if present (as warning for exit code 0, error otherwise)
            if (!string.IsNullOrWhiteSpace(error))
            {
                if (exitCode == 0)
                {
                    _logger?.LogWarning($"Git stderr (exit code 0):\n{error.Trim()}");
                }
                else
                {
                    _logger?.LogError($"Git stderr (exit code {exitCode}):\n{error.Trim()}");
                }
            }

            _logger?.LogInformation($"Git command completed with exit code: {exitCode}");

            return new GitProcessResult
            {
                Success = exitCode == 0,
                ExitCode = exitCode,
                Output = output,
                ErrorMessage = exitCode != 0 ? error.Trim() : null
            };
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, $"Failed to execute Git command: git {arguments}");
            return new GitProcessResult
            {
                Success = false,
                ExitCode = -1,
                ErrorMessage = $"Failed to execute Git command: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Attempts to kill a process safely.
    /// </summary>
    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore errors when killing process
        }
    }

    /// <summary>
    /// Internal result type for Git process execution.
    /// </summary>
    private record GitProcessResult
    {
        public bool Success { get; init; }
        public int ExitCode { get; init; }
        public string? Output { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
