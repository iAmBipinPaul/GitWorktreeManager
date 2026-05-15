package com.iambipinpaul.gitworktreemanager

import com.iambipinpaul.gitworktreemanager.services.GitService
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.ValueSource

class GitServiceTest {
    @Test
    fun buildGitArguments_longPathSupportEnabled_addsScopedConfig() {
        val result = GitService.buildGitArguments(
            listOf("worktree", "list", "--porcelain"),
            enableLongPathSupport = true,
        )

        assertEquals(
            listOf("-c", "core.longpaths=true", "worktree", "list", "--porcelain"),
            result,
        )
    }

    @Test
    fun buildGitArguments_longPathSupportDisabled_leavesArgumentsUnchanged() {
        val arguments = listOf("worktree", "list", "--porcelain")

        val result = GitService.buildGitArguments(arguments, enableLongPathSupport = false)

        assertEquals(arguments, result)
    }

    @ParameterizedTest
    @ValueSource(
        strings = [
            "fatal: cannot create directory at 'src/deep/path': Filename too long",
            "fatal: unable to checkout working tree: file name too long",
            "The specified path, file name, or both are too long.",
            "error: path length is above the supported limit",
        ],
    )
    fun isLongPathError_longPathMessages_returnsTrue(errorMessage: String) {
        val result = GitService.isLongPathError(errorMessage)

        assertTrue(result)
    }

    @Test
    fun isLongPathError_unrelatedGitError_returnsFalse() {
        val result = GitService.isLongPathError("fatal: 'missing-branch' is not a commit")

        assertFalse(result)
    }

    @Test
    fun createUserFacingErrorMessage_longPathError_addsGuidance() {
        val result = GitService.createUserFacingErrorMessage(
            "fatal: Filename too long",
            longPathSupportWasEnabled = true,
        )

        assertTrue(result.contains("process-scoped core.longpaths=true"))
        assertTrue(result.contains("git config --global core.longpaths true"))
        assertTrue(result.contains("Git for Windows setting, not a JetBrains IDE setting"))
    }

    @Test
    fun createUserFacingErrorMessage_unrelatedError_preservesOriginalMessage() {
        val errorMessage = "fatal: 'missing-branch' is not a commit"

        val result = GitService.createUserFacingErrorMessage(
            errorMessage,
            longPathSupportWasEnabled = true,
        )

        assertEquals(errorMessage, result)
    }
}
