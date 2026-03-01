package com.iambipinpaul.gitworktreemanager.services

import com.iambipinpaul.gitworktreemanager.models.Worktree

object WorktreeParser {
    fun parsePorcelainOutput(output: String): List<Worktree> {
        if (output.isBlank()) {
            return emptyList()
        }

        val blocks = output.split(Regex("\\r?\\n\\r?\\n")).filter { it.isNotBlank() }
        val worktrees = blocks.mapNotNull { parseWorktreeBlock(it) }.toMutableList()

        if (worktrees.isNotEmpty()) {
            worktrees[0] = worktrees[0].copy(isMainWorktree = true)
        }

        return worktrees
    }

    private fun parseWorktreeBlock(block: String): Worktree? {
        val lines = block.split(Regex("\\r?\\n")).filter { it.isNotBlank() }

        var path: String? = null
        var head: String? = null
        var branch: String? = null
        var isLocked = false
        var lockReason: String? = null
        var isPrunable = false

        for (line in lines) {
            when {
                line.startsWith("worktree ") -> path = line.substring(9)
                line.startsWith("HEAD ") -> head = line.substring(5)
                line.startsWith("branch ") -> branch = extractBranchName(line.substring(7))
                line == "locked" -> isLocked = true
                line.startsWith("locked reason: ") -> {
                    isLocked = true
                    lockReason = line.substring(15)
                }
                line == "prunable" -> isPrunable = true
            }
        }

        if (path == null || head == null) {
            return null
        }

        return Worktree(
            path = path,
            headCommit = head,
            branch = branch,
            isLocked = isLocked,
            lockReason = lockReason,
            isPrunable = isPrunable,
        )
    }

    private fun extractBranchName(fullRef: String): String {
        val prefix = "refs/heads/"
        return if (fullRef.startsWith(prefix)) {
            fullRef.substring(prefix.length)
        } else {
            fullRef
        }
    }
}