package com.watchdog.protector

import android.content.Context
import android.content.SharedPreferences

class PrefsManager(context: Context) {
    private val PREFS_NAME = "WatchdogPrefs"
    private val KEY_IS_DEBUG = "is_debug_mode"
    private val KEY_USER_PASSWORD = "user_password"

    private val prefs: SharedPreferences = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    // Manage Debug Mode
    var isDebugMode: Boolean
        get() = prefs.getBoolean(KEY_IS_DEBUG, false)
        set(value) = prefs.edit().putBoolean(KEY_IS_DEBUG, value).apply()

    // Manage Password (nullable if not set)
    var userPassword: String?
        get() = prefs.getString(KEY_USER_PASSWORD, null)
        set(value) = prefs.edit().putString(KEY_USER_PASSWORD, value).apply()

    // Check if password is set
    fun hasPassword(): Boolean {
        return !userPassword.isNullOrEmpty()
    }
}
