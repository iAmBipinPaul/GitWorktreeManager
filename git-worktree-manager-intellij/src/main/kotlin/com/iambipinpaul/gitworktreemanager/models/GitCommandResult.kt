package com.iambipinpaul.gitworktreemanager.models

sealed class GitCommandResult<out T> {
    abstract val exitCode: Int
    abstract val errorMessage: String?

    val success: Boolean
        get() = this is Success<*>
    data class Success<T>(val data: T? = null, override val exitCode: Int = 0) : GitCommandResult<T>() {
        override val errorMessage: String? = null
    }

    data class Failure(override val errorMessage: String, override val exitCode: Int = -1) :
        GitCommandResult<Nothing>()

    companion object {
        fun ok(): GitCommandResult<Unit> = Success(Unit, 0)

        fun <T> ok(data: T): GitCommandResult<T> = Success(data, 0)

        fun fail(errorMessage: String, exitCode: Int = -1): GitCommandResult<Nothing> =
            Failure(errorMessage, exitCode)
    }
}
