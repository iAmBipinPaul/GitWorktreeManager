package com.iambipinpaul.gitworktreemanager.notifications

import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.openapi.project.Project

class NotificationService(private val project: Project) {
    private val group = NotificationGroupManager.getInstance()
        .getNotificationGroup("Git Worktree Manager")

    fun info(message: String) {
        group.createNotification(message, NotificationType.INFORMATION).notify(project)
    }

    fun warn(message: String) {
        group.createNotification(message, NotificationType.WARNING).notify(project)
    }

    fun error(message: String) {
        group.createNotification(message, NotificationType.ERROR).notify(project)
    }
}
