package com.iambipinpaul.gitworktreemanager.models

data class Worktree(
    val path: String,
    val headCommit: String,
    val branch: String? = null,
    val isMainWorktree: Boolean = false,
    val isLocked: Boolean = false,
    val lockReason: String? = null,
    val isPrunable: Boolean = false,
) {
    val isDetached: Boolean
        get() = branch == null
}