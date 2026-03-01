package com.iambipinpaul.gitworktreemanager.dialogs

import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.openapi.ui.ValidationInfo
import com.intellij.ui.DocumentAdapter
import com.intellij.ui.components.JBCheckBox
import com.intellij.ui.components.JBLabel
import com.intellij.util.ui.FormBuilder
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import java.awt.BorderLayout
import java.io.File
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.JTextField
import javax.swing.event.DocumentEvent

class AddWorktreeDialog(
    project: Project,
    private val repositoryRoot: String,
    branches: List<String>,
) : DialogWrapper(project) {
    private val createBranchCheckBox = JBCheckBox("Create new branch")
    private val newBranchField = JTextField()
    private val baseBranchCombo = ComboBox(branches.toTypedArray())
    private val existingBranchCombo = ComboBox(branches.toTypedArray())
    private val openAfterCheckBox = JBCheckBox("Open in new IDE window", true)
    private val branchNameLabel = JBLabel("Branch name")
    private val baseBranchLabel = JBLabel("Base branch")
    private val existingBranchLabel = JBLabel("Branch")
    private val locationLabel = JBLabel()

    val branchName: String
        get() = if (createBranchCheckBox.isSelected) {
            sanitizeBranchNameInput(newBranchField.text.trim())
        } else {
            existingBranchCombo.selectedItem?.toString()?.trim().orEmpty()
        }

    val createBranch: Boolean
        get() = createBranchCheckBox.isSelected

    val baseBranch: String?
        get() = if (createBranchCheckBox.isSelected) {
            baseBranchCombo.selectedItem?.toString()?.trim().takeIf { !it.isNullOrBlank() }
        } else {
            null
        }

    val worktreePath: String
        get() = branchName.takeIf { it.isNotBlank() }?.let { suggestPath(it) }.orEmpty()

    val openAfterCreation: Boolean
        get() = openAfterCheckBox.isSelected

    init {
        title = "Add Worktree"
        createBranchCheckBox.isSelected = true
        locationLabel.font = JBUI.Fonts.smallFont()
        locationLabel.foreground = UIUtil.getContextHelpForeground()

        val defaultBranch = branches.firstOrNull { it == "main" }
            ?: branches.firstOrNull { it == "master" }
            ?: branches.firstOrNull()

        baseBranchCombo.selectedItem = defaultBranch
        existingBranchCombo.selectedItem = defaultBranch
        newBranchField.text = ""

        newBranchField.document.addDocumentListener(object : DocumentAdapter() {
            override fun textChanged(e: DocumentEvent) {
                updateLocationPreview()
                initValidation()
            }
        })

        createBranchCheckBox.addActionListener {
            updateModeState()
            updateLocationPreview()
            initValidation()
        }
        baseBranchCombo.addActionListener {
            initValidation()
        }
        existingBranchCombo.addActionListener {
            updateLocationPreview()
            initValidation()
        }

        updateModeState()
        updateLocationPreview()
        init()
        initValidation()
    }

    override fun createCenterPanel(): JComponent {
        val panel = JPanel(BorderLayout())
        panel.border = JBUI.Borders.empty(8)

        val form = FormBuilder.createFormBuilder()
            .addComponent(createBranchCheckBox, 1)
            .addLabeledComponent(branchNameLabel, newBranchField, 1, false)
            .addLabeledComponent(baseBranchLabel, baseBranchCombo, 1, false)
            .addLabeledComponent(existingBranchLabel, existingBranchCombo, 1, false)
            .addLabeledComponent(JBLabel("Worktree location"), locationLabel, 1, false)
            .addComponent(openAfterCheckBox, 1)
            .panel

        panel.add(form, BorderLayout.CENTER)
        return panel
    }

    override fun doValidate(): ValidationInfo? {
        if (createBranchCheckBox.isSelected) {
            val rawBranch = newBranchField.text.trim()
            if (rawBranch.isBlank()) {
                return ValidationInfo("Branch name is required.", newBranchField)
            }

            val sanitized = sanitizeBranchNameInput(rawBranch)
            if (sanitized.startsWith("-") || sanitized.startsWith(".")) {
                return ValidationInfo("Branch name cannot start with '-' or '.'.", newBranchField)
            }

            if (baseBranchCombo.selectedItem?.toString().isNullOrBlank() && baseBranchCombo.itemCount > 0) {
                return ValidationInfo("Select a base branch.", baseBranchCombo)
            }
        } else if (existingBranchCombo.selectedItem?.toString().isNullOrBlank()) {
            return ValidationInfo("Select a branch.", existingBranchCombo)
        }

        if (worktreePath.isBlank()) {
            val component = if (createBranchCheckBox.isSelected) newBranchField else existingBranchCombo
            return ValidationInfo("Worktree location could not be generated.", component)
        }

        return null
    }

    private fun updateModeState() {
        val createNew = createBranchCheckBox.isSelected
        branchNameLabel.isVisible = createNew
        newBranchField.isVisible = createNew
        baseBranchLabel.isVisible = createNew
        baseBranchCombo.isVisible = createNew
        existingBranchLabel.isVisible = !createNew
        existingBranchCombo.isVisible = !createNew
    }

    private fun updateLocationPreview() {
        val branch = branchName
        locationLabel.text = if (branch.isBlank()) "" else suggestPath(branch)
    }

    private fun sanitizeBranchNameInput(input: String): String {
        if (input.isBlank()) {
            return input
        }

        val result = StringBuilder(input.length)
        for (char in input) {
            if (
                char == ' ' ||
                char == '\\' ||
                char == ':' ||
                char == '*' ||
                char == '?' ||
                char == '"' ||
                char == '<' ||
                char == '>' ||
                char == '|' ||
                char == '~' ||
                char == '^' ||
                char == '[' ||
                char == '@' ||
                char.isISOControl()
            ) {
                result.append('-')
            } else {
                result.append(char)
            }
        }
        return result.toString()
    }

    private fun suggestPath(branch: String): String {
        val repo = File(repositoryRoot)
        val parent = repo.parentFile ?: repo
        val safeBranch = branch.replace(Regex("[^A-Za-z0-9._-]"), "-")
        val dirName = "${repo.name}-$safeBranch"
        return File(parent, dirName).absolutePath
    }
}
