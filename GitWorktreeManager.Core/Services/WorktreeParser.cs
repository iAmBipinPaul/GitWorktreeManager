namespace GitWorktreeManager.Services;

using GitWorktreeManager.Models;

/// <summary>
/// Parses Git worktree porcelain output into Worktree model objects.
/// </summary>
public static class WorktreeParser
{
    /// <summary>
    /// Parses the porcelain output from 'git worktree list --porcelain' into a list of Worktree objects.
    /// </summary>
    /// <param name="output">The raw porcelain output from git worktree list --porcelain.</param>
    /// <returns>A read-only list of parsed Worktree objects.</returns>
    public static IReadOnlyList<Worktree> ParsePorcelainOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Array.Empty<Worktree>();
        }

        var worktrees = new List<Worktree>();
        
        // Split output into blocks by double newlines (handles both \n\n and \r\n\r\n)
        var blocks = output.Split(
            new[] { "\n\n", "\r\n\r\n" },
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var worktree = ParseWorktreeBlock(block);
            if (worktree != null)
            {
                worktrees.Add(worktree);
            }
        }

        // First worktree is always the main worktree
        if (worktrees.Count > 0)
        {
            worktrees[0] = worktrees[0] with { IsMainWorktree = true };
        }

        return worktrees;
    }

    /// <summary>
    /// Parses a single worktree block from the porcelain output.
    /// </summary>
    private static Worktree? ParseWorktreeBlock(string block)
    {
        var lines = block.Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        string? path = null;
        string? head = null;
        string? branch = null;
        bool isLocked = false;
        string? lockReason = null;
        bool isPrunable = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("worktree "))
            {
                path = line[9..];
            }
            else if (line.StartsWith("HEAD "))
            {
                head = line[5..];
            }
            else if (line.StartsWith("branch "))
            {
                branch = ExtractBranchName(line[7..]);
            }
            else if (line == "locked")
            {
                isLocked = true;
            }
            else if (line.StartsWith("locked reason: "))
            {
                isLocked = true; // Ensure locked is set when reason is provided
                lockReason = line[15..];
            }
            else if (line == "prunable")
            {
                isPrunable = true;
            }
            // "detached" line is handled implicitly - branch will remain null
        }

        // Path and HEAD are required fields
        if (path == null || head == null)
        {
            return null;
        }

        return new Worktree
        {
            Path = path,
            HeadCommit = head,
            Branch = branch,
            IsLocked = isLocked,
            LockReason = lockReason,
            IsPrunable = isPrunable,
            IsMainWorktree = false // Will be set to true for first worktree after parsing all blocks
        };
    }

    /// <summary>
    /// Extracts the branch name from a full Git reference.
    /// Converts refs/heads/branch-name to branch-name.
    /// </summary>
    private static string ExtractBranchName(string fullRef)
    {
        const string prefix = "refs/heads/";
        return fullRef.StartsWith(prefix)
            ? fullRef[prefix.Length..]
            : fullRef;
    }
}
