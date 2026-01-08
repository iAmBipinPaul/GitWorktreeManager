# Implementation Plan: Git Worktree Manager

## Overview

This implementation plan breaks down the Git Worktree Manager VS extension into discrete coding tasks. The extension uses VisualStudio.Extensibility SDK (out-of-process model) with C# and XAML. Tasks are ordered to build incrementally, with property tests placed close to implementation for early bug detection.

## Tasks

- [x] 1. Set up project structure and dependencies
  - Create VSIX project using VisualStudio.Extensibility template
  - Configure project for .NET 8.0 and VS 2022 17.9+
  - Add NuGet packages: Microsoft.VisualStudio.Extensibility, FsCheck.Xunit, xUnit, FluentAssertions, NSubstitute
  - Create folder structure: Models/, Services/, ViewModels/, ToolWindows/, Commands/
  - _Requirements: 10.1, 10.2, 10.4_

- [x] 2. Implement core data models
  - [x] 2.1 Create Worktree record with Path, HeadCommit, Branch, IsMainWorktree, IsLocked, LockReason, IsPrunable properties
    - Implement as C# record for immutability
    - Add IsDetached computed property
    - _Requirements: 2.2, 2.6_

  - [x] 2.2 Create GitCommandResult and GitCommandResult<T> records
    - Include Success, ErrorMessage, ExitCode, and Data properties
    - _Requirements: 7.1, 7.2_

  - [x] 2.3 Create WorktreeItemViewModel class with DataContract attributes
    - Include Path, DisplayPath, BranchName, HeadCommit, IsMainWorktree, IsLocked, IsPrunable, CanRemove
    - _Requirements: 2.3, 4.1, 4.6_

- [x] 3. Implement WorktreeParser for porcelain output
  - [x] 3.1 Implement ParsePorcelainOutput static method
    - Split output into blocks by double newlines
    - Parse each block for worktree, HEAD, branch, locked, prunable
    - Mark first worktree as main worktree
    - Handle detached HEAD state
    - _Requirements: 2.2_

  - [ ]* 3.2 Write property test for porcelain parsing round-trip
    - **Property 1: Porcelain Parsing Round-Trip**
    - Generate valid porcelain output strings
    - Verify parse → format → parse produces equivalent results
    - **Validates: Requirements 2.2**

- [x] 4. Implement IGitService interface and GitService class
  - [x] 4.1 Create IGitService interface
    - Define GetWorktreesAsync, AddWorktreeAsync, RemoveWorktreeAsync, GetRepositoryRootAsync, IsGitInstalledAsync
    - _Requirements: 2.1, 3.4, 4.3_

  - [x] 4.2 Implement GitService with Process execution
    - Execute git commands using System.Diagnostics.Process
    - Capture stdout and stderr
    - Handle timeouts and cancellation
    - _Requirements: 2.1, 7.1, 7.2_

  - [x] 4.3 Implement GetWorktreesAsync method
    - Execute `git worktree list --porcelain`
    - Parse output using WorktreeParser
    - Return GitCommandResult with worktree list
    - _Requirements: 2.1, 2.2_

  - [x] 4.4 Implement AddWorktreeAsync method
    - Build command: `git worktree add <path> <branch>` or `git worktree add -b <branch> <path>`
    - Handle createBranch flag
    - _Requirements: 3.4, 3.5_

  - [x] 4.5 Implement RemoveWorktreeAsync method
    - Build command: `git worktree remove <path>` with optional --force
    - _Requirements: 4.3, 4.4_

  - [ ]* 4.6 Write property test for command construction
    - **Property 4: Git Command Construction Correctness**
    - Generate valid paths and branch names
    - Verify command strings are correctly formatted
    - **Validates: Requirements 3.4, 4.3, 5.3**

- [x] 5. Checkpoint - Ensure core services compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement WorktreeViewModel
  - [x] 6.1 Create WorktreeViewModel class with INotifyPropertyChanged
    - Add ObservableCollection<WorktreeItemViewModel> Worktrees
    - Add IsLoading, HasRepository, ErrorMessage, RepositoryPath properties
    - Implement PropertyChanged event raising
    - _Requirements: 9.2, 9.5_

  - [x] 6.2 Implement RefreshAsync method
    - Call GitService.GetWorktreesAsync
    - Map Worktree models to WorktreeItemViewModel
    - Update ObservableCollection
    - Handle errors and update ErrorMessage
    - _Requirements: 2.3, 2.6, 7.1_

  - [x] 6.3 Implement AddWorktreeAsync method
    - Call GitService.AddWorktreeAsync
    - Refresh list on success
    - Show error notification on failure
    - _Requirements: 3.4, 3.6, 3.7_

  - [x] 6.4 Implement RemoveWorktreeAsync method
    - Validate not main worktree
    - Call GitService.RemoveWorktreeAsync
    - Refresh list on success
    - _Requirements: 4.3, 4.5, 4.6_

  - [x] 6.5 Implement OpenInNewWindowAsync method
    - Find solution files in worktree path
    - Launch devenv.exe with appropriate arguments
    - _Requirements: 5.2, 5.3, 5.4, 5.5_

  - [ ]* 6.6 Write property test for main worktree protection
    - **Property 5: Main Worktree Protection**
    - Generate worktrees with various IsMainWorktree values
    - Verify CanRemove is inverse of IsMainWorktree
    - **Validates: Requirements 4.1, 4.6**

  - [ ]* 6.7 Write property test for status indicators
    - **Property 3: Status Indicators Correctness**
    - Generate worktrees with various status flags
    - Verify ViewModel preserves all status flags
    - **Validates: Requirements 2.4, 2.5**

  - [ ]* 6.8 Write property test for PropertyChanged notifications
    - **Property 9: PropertyChanged Notification**
    - Change each DataMember property
    - Verify PropertyChanged fires with correct name
    - **Validates: Requirements 9.5**

- [x] 7. Implement logging service
  - [x] 7.1 Create ILoggerService interface and ActivityLogService implementation
    - Wrap VS ActivityLog
    - Support Information, Warning, Error levels
    - _Requirements: 8.1, 8.5_

  - [x] 7.2 Add logging to GitService
    - Log command execution with arguments
    - Log stdout/stderr output
    - Log exceptions with stack traces
    - _Requirements: 8.1, 8.2, 8.4_

  - [ ]* 7.3 Write property test for logging completeness
    - **Property 7: Logging Completeness**
    - Execute various git commands
    - Verify logger receives appropriate entries
    - **Validates: Requirements 8.1, 8.2, 8.4, 8.5**

- [x] 8. Checkpoint - Ensure ViewModel and logging work correctly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement Tool Window and Remote UI
  - [x] 9.1 Create WorktreeToolWindow class extending ToolWindow
    - Configure placement docked to Solution Explorer
    - Set title to "Worktree Manager"
    - _Requirements: 1.1, 1.2_

  - [x] 9.2 Create WorktreeToolWindowControl extending RemoteUserControl
    - Pass WorktreeViewModel as data context
    - _Requirements: 9.4_

  - [x] 9.3 Create WorktreeToolWindowControl.xaml DataTemplate
    - ListView bound to Worktrees collection
    - Display path, branch, status indicators (lock icon, warning icon)
    - Add Worktree button
    - Context menu with Remove and Open in New Window actions
    - Loading indicator
    - No repository message
    - _Requirements: 1.4, 2.3, 2.4, 2.5, 3.1, 4.1, 5.1_

  - [ ]* 9.4 Write property test for display completeness
    - **Property 10: Worktree Display Completeness**
    - Generate lists of worktrees
    - Verify ViewModel collection has same count and all required fields
    - **Validates: Requirements 2.3**

- [x] 10. Implement Show Tool Window command
  - [x] 10.1 Create ShowWorktreeToolWindowCommand extending Command
    - Place in View menu
    - Call Shell.ShowToolWindowAsync<WorktreeToolWindow>
    - _Requirements: 1.1_

- [x] 11. Implement Add Worktree dialog
  - [x] 11.1 Create AddWorktreeDialog using VS prompts/dialogs
    - Text input for branch name
    - Folder picker for worktree location
    - Checkbox for "Create new branch"
    - OK/Cancel buttons
    - _Requirements: 3.1, 3.2, 3.3, 3.5_

  - [x] 11.2 Wire dialog to ViewModel.AddWorktreeAsync
    - Validate inputs before calling
    - Show confirmation on success
    - _Requirements: 3.4, 3.6_

- [x] 12. Implement solution change detection
  - [x] 12.1 Subscribe to solution events in extension
    - Handle solution opened, closed, and changed events
    - Update ViewModel.RepositoryPath
    - _Requirements: 6.1, 6.2, 6.3_

  - [x] 12.2 Implement debouncing for rapid events
    - Use timer-based debounce (500ms)
    - Cancel pending refresh on new event
    - _Requirements: 6.4_

  - [ ]* 12.3 Write property test for event debouncing
    - **Property 8: Event Debouncing**
    - Simulate rapid solution change events
    - Verify GetWorktreesAsync called at most once per debounce window
    - **Validates: Requirements 6.4**

- [x] 13. Implement error handling and notifications
  - [x] 13.1 Create NotificationService for InfoBar messages
    - Support error, warning, and info severity levels
    - Include action buttons where appropriate
    - _Requirements: 7.1, 7.3_

  - [x] 13.2 Add Git-not-installed detection
    - Check for git in PATH on extension load
    - Show clear error message if not found
    - _Requirements: 7.4_

  - [x] 13.3 Wire error handling throughout ViewModel
    - Display errors from all git operations
    - Include git stderr in error messages
    - _Requirements: 3.7, 7.1, 7.2_

  - [ ]* 13.4 Write property test for error handling
    - **Property 6: Error Handling Completeness**
    - Generate failed GitCommandResults
    - Verify error notifications contain original error message
    - **Validates: Requirements 3.7, 7.1, 7.2, 7.3**

- [x] 14. Checkpoint - Ensure UI and error handling work correctly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 15. Implement extension entry point and packaging
  - [x] 15.1 Create GitWorktreeManagerExtension class
    - Configure extension metadata (name, version, publisher, description)
    - Register services with dependency injection
    - _Requirements: 10.3, 10.4_

  - [x] 15.2 Configure source.extension.vsixmanifest
    - Set supported VS versions (17.9+)
    - Add icon and license
    - Configure installation targets
    - _Requirements: 10.1, 10.2, 10.3_

  - [x] 15.3 Add extension lifecycle logging
    - Log initialization and disposal
    - _Requirements: 8.3_

- [x] 16. Write integration tests
  - [x]* 16.1 Write integration test for full worktree workflow
    - Create test git repository
    - Add, list, and remove worktrees
    - Verify end-to-end functionality
    - _Requirements: 2.1, 3.4, 4.3_

- [x] 17. Final checkpoint - Full test suite and build verification
  - Ensure all tests pass, ask the user if questions arise.
  - Verify VSIX builds successfully
  - Test installation in VS Experimental Instance

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties (FsCheck with 100+ iterations)
- Unit tests validate specific examples and edge cases
- The extension uses VisualStudio.Extensibility out-of-process model for reliability
