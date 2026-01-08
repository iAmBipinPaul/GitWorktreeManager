# Requirements Document

## Introduction

This document specifies the requirements for a Visual Studio 2022+ extension (VSIX) that provides a graphical interface for managing Git worktrees. The extension leverages the modern VisualStudio.Extensibility SDK (out-of-process model) for improved reliability and performance. It enables developers to list, add, remove, and open Git worktrees directly from within Visual Studio without leaving the IDE.

## Glossary

- **Extension**: The Visual Studio extension package (VSIX) that provides Git worktree management functionality
- **Tool_Window**: A dockable Visual Studio window that displays the worktree management interface
- **Worktree**: A Git working tree linked to a repository, allowing multiple branches to be checked out simultaneously
- **GitService**: The service component responsible for executing Git commands and parsing their output
- **ViewModel**: The data context class that manages UI state and exposes worktree data to the view
- **Remote_UI**: The VisualStudio.Extensibility UI model that runs XAML in the VS process while logic runs out-of-process
- **ActivityLog**: Visual Studio's diagnostic logging facility for extension troubleshooting
- **InfoBar**: Visual Studio's notification mechanism for displaying messages to users

## Requirements

### Requirement 1: Tool Window Display

**User Story:** As a developer, I want a dedicated tool window for managing Git worktrees, so that I can access worktree functionality without leaving Visual Studio.

#### Acceptance Criteria

1. THE Extension SHALL provide a tool window titled "Worktree Manager" accessible from the View menu
2. WHEN the tool window is opened, THE Extension SHALL display it docked to the Solution Explorer area by default
3. THE Tool_Window SHALL persist its visibility state across Visual Studio sessions
4. WHEN no Git repository is detected in the current solution, THE Tool_Window SHALL display an informative message indicating no repository is available

### Requirement 2: List Worktrees

**User Story:** As a developer, I want to see all existing worktrees for my repository, so that I can understand my current worktree setup.

#### Acceptance Criteria

1. WHEN the tool window loads with a valid Git repository, THE Extension SHALL execute `git worktree list --porcelain` to retrieve worktree information
2. THE Extension SHALL parse the porcelain output to extract worktree path, HEAD commit, and branch name for each worktree
3. THE Tool_Window SHALL display each worktree in a list showing: path, branch name, and whether it is the main worktree
4. WHEN a worktree is locked, THE Tool_Window SHALL display a lock indicator next to that worktree
5. WHEN a worktree is prunable, THE Tool_Window SHALL display a warning indicator next to that worktree
6. THE Extension SHALL serialize the parsed worktree data into a structured format for the ViewModel

### Requirement 3: Add Worktree

**User Story:** As a developer, I want to create new worktrees from within Visual Studio, so that I can quickly set up parallel working directories for different branches.

#### Acceptance Criteria

1. THE Tool_Window SHALL provide an "Add Worktree" button that opens a dialog
2. WHEN the Add Worktree dialog opens, THE Extension SHALL display a text field for the branch name
3. WHEN the Add Worktree dialog opens, THE Extension SHALL provide a folder picker for selecting the worktree location
4. WHEN the user confirms the dialog with valid inputs, THE Extension SHALL execute `git worktree add <path> <branch>`
5. IF the branch does not exist, THEN THE Extension SHALL offer to create a new branch with the `-b` flag
6. WHEN the worktree is successfully created, THE Extension SHALL refresh the worktree list automatically
7. IF the git command fails, THEN THE Extension SHALL display an error notification via InfoBar

### Requirement 4: Remove Worktree

**User Story:** As a developer, I want to remove worktrees I no longer need, so that I can keep my workspace organized.

#### Acceptance Criteria

1. THE Tool_Window SHALL provide a "Remove" action for each worktree in the list (except the main worktree)
2. WHEN the user initiates removal, THE Extension SHALL prompt for confirmation before proceeding
3. WHEN confirmed, THE Extension SHALL execute `git worktree remove <path>`
4. IF the worktree has uncommitted changes, THEN THE Extension SHALL warn the user and offer the `--force` option
5. WHEN the worktree is successfully removed, THE Extension SHALL refresh the worktree list automatically
6. THE Extension SHALL NOT allow removal of the main worktree

### Requirement 5: Open Worktree in New VS Instance

**User Story:** As a developer, I want to open a worktree in a new Visual Studio instance, so that I can work on multiple branches simultaneously.

#### Acceptance Criteria

1. THE Tool_Window SHALL provide an "Open in New Window" action for each worktree
2. WHEN the user triggers this action, THE Extension SHALL launch a new Visual Studio instance at the worktree path
3. THE Extension SHALL use `devenv.exe` with the worktree folder path as the argument
4. IF the worktree contains a solution file, THEN THE Extension SHALL open that solution file directly
5. IF multiple solution files exist, THEN THE Extension SHALL open the folder and let the user choose

### Requirement 6: Auto-Refresh on Solution Change

**User Story:** As a developer, I want the worktree list to update automatically when I switch solutions, so that I always see accurate information.

#### Acceptance Criteria

1. WHEN the active solution changes, THE Extension SHALL automatically refresh the worktree list
2. WHEN a solution is closed, THE Extension SHALL clear the worktree list and display the "no repository" message
3. THE Extension SHALL detect solution changes by subscribing to Visual Studio solution events
4. THE Extension SHALL debounce rapid solution change events to prevent excessive Git command execution

### Requirement 7: Error Handling and Notifications

**User Story:** As a developer, I want clear feedback when operations succeed or fail, so that I understand the state of my worktrees.

#### Acceptance Criteria

1. WHEN a Git command fails, THE Extension SHALL display an error message via Visual Studio's InfoBar
2. THE Extension SHALL include the Git error output in the error message for troubleshooting
3. WHEN a Git command succeeds with warnings, THE Extension SHALL display a warning notification
4. IF Git is not installed or not in PATH, THEN THE Extension SHALL display a clear error message explaining the issue

### Requirement 8: Logging with ActivityLog

**User Story:** As a developer or support engineer, I want extension operations logged, so that I can troubleshoot issues.

#### Acceptance Criteria

1. THE Extension SHALL log all Git command executions to the ActivityLog
2. THE Extension SHALL log command outputs (stdout and stderr) at appropriate log levels
3. THE Extension SHALL log extension initialization and disposal events
4. WHEN an exception occurs, THE Extension SHALL log the full exception details including stack trace
5. THE Extension SHALL use appropriate log levels: Information for normal operations, Warning for recoverable issues, Error for failures

### Requirement 9: MVVM Architecture

**User Story:** As a maintainer, I want the extension to follow MVVM patterns, so that the codebase is testable and maintainable.

#### Acceptance Criteria

1. THE Extension SHALL separate concerns into Model (worktree data), ViewModel (UI state and commands), and View (XAML)
2. THE ViewModel SHALL expose worktree data via an ObservableCollection for automatic UI updates
3. THE ViewModel SHALL expose commands for Add, Remove, Open, and Refresh operations
4. THE Extension SHALL use the Remote UI data binding model provided by VisualStudio.Extensibility
5. THE Extension SHALL implement INotifyPropertyChanged for all bindable ViewModel properties

### Requirement 10: Extension Packaging and Deployment

**User Story:** As a developer, I want to easily install and update the extension, so that I can start using it quickly.

#### Acceptance Criteria

1. THE Extension SHALL be packaged as a standard VSIX file
2. THE Extension SHALL target Visual Studio 2022 version 17.9 or later
3. THE Extension SHALL include appropriate metadata (name, description, icon, publisher)
4. THE Extension SHALL use the VisualStudio.Extensibility out-of-process model for improved reliability
5. THE Extension SHALL not require Visual Studio restart after installation
