package com.iambipinpaul.gitworktreemanager

import com.intellij.ui.IconManager
import javax.swing.Icon

object GitWorktreeManagerIcons {
    private val classLoader = GitWorktreeManagerIcons::class.java.classLoader

    private fun load(path: String, expUiPath: String? = null, flags: Int = 2): Icon {
        val cacheKey = (path + (expUiPath ?: "")).hashCode()
        return if (expUiPath == null) {
            IconManager.getInstance().loadRasterizedIcon(path, classLoader, cacheKey, flags)
        } else {
            IconManager.getInstance().loadRasterizedIcon(path, expUiPath, classLoader, cacheKey, flags)
        }
    }

    @JvmField
    val ToolWindow: Icon = load("icons/toolWindow.svg", "icons/expui/toolwindow/gitWorktrees.svg")

    @JvmField
    val Action: Icon = load("icons/action.svg", "icons/expui/action.svg")
}
