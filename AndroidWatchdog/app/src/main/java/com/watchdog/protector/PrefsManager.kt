package com.watchdog.protector

import android.content.Context
import android.content.SharedPreferences

class PrefsManager(context: Context) {
    private val PREFS_NAME = "WatchdogPrefs"
    private val KEY_IS_DEBUG = "is_debug_mode"
    private val KEY_PIN_CODE = "user_pin_code"

    private val prefs: SharedPreferences = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    // ניהול מצב Debug
    var isDebugMode: Boolean
        get() = prefs.getBoolean(KEY_IS_DEBUG, false)
        set(value) = prefs.edit().putBoolean(KEY_IS_DEBUG, value).apply()

    // ניהול קוד PIN (יכול להיות null אם לא הוגדר)
    var pinCode: String?
        get() = prefs.getString(KEY_PIN_CODE, null)
        set(value) = prefs.edit().putString(KEY_PIN_CODE, value).apply()

    // בדיקה האם הוגדרה סיסמה
    fun hasPin(): Boolean {
        return !pinCode.isNullOrEmpty()
    }
}