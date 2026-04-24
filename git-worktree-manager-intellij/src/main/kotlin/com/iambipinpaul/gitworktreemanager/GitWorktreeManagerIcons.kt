package com.iambipinpaul.gitworktreemanager

import com.intellij.openapi.util.IconLoader
import javax.swing.Icon

object GitWorktreeManagerIcons {
    private fun load(path: String): Icon =
        IconLoader.getIcon(path, GitWorktreeManagerIcons::class.java)

    @JvmField
    val ToolWindow: Icon = load("/icons/toolWindow.svg")

    @JvmField
    val Action: Icon = load("/icons/action.svg")
}
