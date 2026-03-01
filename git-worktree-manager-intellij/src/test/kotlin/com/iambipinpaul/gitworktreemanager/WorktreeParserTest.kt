package com.iambipinpaul.gitworktreemanager

import com.iambipinpaul.gitworktreemanager.services.WorktreeParser
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Test

class WorktreeParserTest {
    @Test
    fun parsePorcelainOutput_emptyString_returnsEmptyList() {
        val output = ""

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertTrue(result.isEmpty())
    }

    @Test
    fun parsePorcelainOutput_whitespaceOnly_returnsEmptyList() {
        val output = "   \n\n   "

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertTrue(result.isEmpty())
    }

    @Test
    fun parsePorcelainOutput_singleWorktree_parsesCorrectly() {
        val output = "worktree /path/to/main\nHEAD abc123def456\nbranch refs/heads/main\n"

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertEquals("/path/to/main", result[0].path)
        assertEquals("abc123def456", result[0].headCommit)
        assertEquals("main", result[0].branch)
        assertTrue(result[0].isMainWorktree)
        assertFalse(result[0].isDetached)
    }

    @Test
    fun parsePorcelainOutput_multipleWorktrees_marksFirstAsMain() {
        val output = """
            worktree /path/to/main
            HEAD abc123def456
            branch refs/heads/main

            worktree /path/to/feature
            HEAD def456abc789
            branch refs/heads/feature-branch
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(2, result.size)
        assertTrue(result[0].isMainWorktree)
        assertFalse(result[1].isMainWorktree)
    }

    @Test
    fun parsePorcelainOutput_detachedHead_setsIsDetachedTrue() {
        val output = """
            worktree /path/to/detached
            HEAD 789abc123def
            detached
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertNull(result[0].branch)
        assertTrue(result[0].isDetached)
    }

    @Test
    fun parsePorcelainOutput_lockedWorktree_setsIsLockedTrue() {
        val output = """
            worktree /path/to/locked
            HEAD abc123
            branch refs/heads/feature
            locked
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertTrue(result[0].isLocked)
        assertNull(result[0].lockReason)
    }

    @Test
    fun parsePorcelainOutput_lockedWorktreeWithReason_parsesLockReason() {
        val output = """
            worktree /path/to/locked
            HEAD abc123
            branch refs/heads/feature
            locked
            locked reason: Working on critical fix
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertTrue(result[0].isLocked)
        assertEquals("Working on critical fix", result[0].lockReason)
    }

    @Test
    fun parsePorcelainOutput_prunableWorktree_setsIsPrunableTrue() {
        val output = """
            worktree /path/to/prunable
            HEAD abc123
            branch refs/heads/old-branch
            prunable
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertTrue(result[0].isPrunable)
    }

    @Test
    fun parsePorcelainOutput_complexScenario_parsesAllFields() {
        val output = """
            worktree /path/to/main
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
            prunable
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(3, result.size)

        assertEquals("/path/to/main", result[0].path)
        assertEquals("abc123def456", result[0].headCommit)
        assertEquals("main", result[0].branch)
        assertTrue(result[0].isMainWorktree)
        assertFalse(result[0].isLocked)
        assertFalse(result[0].isPrunable)

        assertEquals("/path/to/feature", result[1].path)
        assertEquals("def456abc789", result[1].headCommit)
        assertEquals("feature-branch", result[1].branch)
        assertFalse(result[1].isMainWorktree)
        assertTrue(result[1].isLocked)
        assertEquals("Working on critical fix", result[1].lockReason)

        assertEquals("/path/to/detached", result[2].path)
        assertEquals("789abc123def", result[2].headCommit)
        assertNull(result[2].branch)
        assertTrue(result[2].isDetached)
        assertTrue(result[2].isPrunable)
    }

    @Test
    fun parsePorcelainOutput_windowsLineEndings_parsesCorrectly() {
        val output =
            "worktree C:\\path\\to\\main\r\nHEAD abc123\r\nbranch refs/heads/main\r\n\r\n" +
                "worktree C:\\path\\to\\feature\r\nHEAD def456\r\nbranch refs/heads/feature"

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(2, result.size)
        assertEquals("C:\\path\\to\\main", result[0].path)
        assertEquals("C:\\path\\to\\feature", result[1].path)
    }

    @Test
    fun parsePorcelainOutput_missingPath_skipsInvalidBlock() {
        val output = """
            HEAD abc123
            branch refs/heads/main

            worktree /valid/path
            HEAD def456
            branch refs/heads/feature
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertEquals("/valid/path", result[0].path)
    }

    @Test
    fun parsePorcelainOutput_missingHead_skipsInvalidBlock() {
        val output = """
            worktree /invalid/path
            branch refs/heads/main

            worktree /valid/path
            HEAD def456
            branch refs/heads/feature
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertEquals("/valid/path", result[0].path)
    }

    @Test
    fun parsePorcelainOutput_extractsBranchNameWithoutRefsHeadsPrefix() {
        val output = """
            worktree /path/to/repo
            HEAD abc123
            branch refs/heads/feature/my-feature
        """.trimIndent()

        val result = WorktreeParser.parsePorcelainOutput(output)

        assertEquals(1, result.size)
        assertEquals("feature/my-feature", result[0].branch)
    }
}