package com.watchdog.protector

import android.content.Context
import android.content.SharedPreferences

class PrefsManager(context: Context) {
    private val prefs: SharedPreferences = context.getSharedPreferences("watchdog_prefs", Context.MODE_PRIVATE)

    companion object {
        private const val KEY_USER_PASSWORD = "user_password"
        private const val KEY_DEBUG_MODE = "debug_mode"
    }

    var isDebugMode: Boolean
        get() = prefs.getBoolean(KEY_DEBUG_MODE, false)
        set(value) = prefs.edit().putBoolean(KEY_DEBUG_MODE, value).apply()

    var userPassword: String?
        get() = prefs.getString(KEY_USER_PASSWORD, null)
        set(value) = prefs.edit().putString(KEY_USER_PASSWORD, value).apply()
}
