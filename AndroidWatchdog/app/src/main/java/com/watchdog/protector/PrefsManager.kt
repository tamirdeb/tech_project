package com.watchdog.protector

import android.content.Context
import android.content.SharedPreferences

class PrefsManager(context: Context) {
    private val prefs: SharedPreferences = context.getSharedPreferences("watchdog_prefs", Context.MODE_PRIVATE)

    companion object {
        private const val KEY_DEBUG_MODE = "debug_mode"
        private const val KEY_USER_PIN = "user_pin"
        private const val KEY_FAILED_PIN_ATTEMPTS = "failed_pin_attempts"
        private const val KEY_LOCKOUT_END_TIME = "lockout_end_time"
    }

    var isDebugMode: Boolean
        get() = prefs.getBoolean(KEY_DEBUG_MODE, false)
        set(value) = prefs.edit().putBoolean(KEY_DEBUG_MODE, value).apply()

    var userPin: String?
        get() = prefs.getString(KEY_USER_PIN, null)
        set(value) = prefs.edit().putString(KEY_USER_PIN, value).apply()

    var failedPinAttempts: Int
        get() = prefs.getInt(KEY_FAILED_PIN_ATTEMPTS, 0)
        set(value) = prefs.edit().putInt(KEY_FAILED_PIN_ATTEMPTS, value).apply()

    var lockoutEndTime: Long
        get() = prefs.getLong(KEY_LOCKOUT_END_TIME, 0L)
        set(value) = prefs.edit().putLong(KEY_LOCKOUT_END_TIME, value).apply()

    fun resetFailedAttempts() {
        failedPinAttempts = 0
        lockoutEndTime = 0L
    }
}
