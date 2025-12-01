package com.watchdog.protector

import android.accessibilityservice.AccessibilityService
import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.util.Log
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo
import android.widget.Toast
import androidx.core.app.NotificationCompat

class WatchdogService : AccessibilityService() {

    private lateinit var prefsManager: PrefsManager
    private val TAG = "WatchdogService"
    private val CHANNEL_ID = "WatchdogChannel"
    private val NOTIFICATION_ID = 1

    // Hardcoded list of banned activities (Package Name + Class Name)
    // Structure: Pair(PackageName, ClassName)
    private val bannedScreens = listOf<Pair<String, String>>(
        // Example: Pair("com.example.badapp", "com.example.badapp.BadActivity")
    )

    override fun onServiceConnected() {
        super.onServiceConnected()
        prefsManager = PrefsManager(this)
        startForegroundService()
        Log.d(TAG, "Watchdog Service Connected")
    }

    private fun startForegroundService() {
        createNotificationChannel()

        val notificationIntent = Intent(this, MainActivity::class.java)
        val pendingIntent = PendingIntent.getActivity(
            this,
            0,
            notificationIntent,
            PendingIntent.FLAG_IMMUTABLE
        )

        val notification: Notification = NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("Watchdog Protector Active")
            .setContentText("Monitoring for toxic screens...")
            .setSmallIcon(android.R.drawable.ic_lock_idle_lock)
            .setContentIntent(pendingIntent)
            .setPriority(NotificationCompat.PRIORITY_MAX)
            .setOngoing(true)
            .build()

        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val name = "Watchdog Service"
            val descriptionText = "Keeps the Watchdog service running"
            val importance = NotificationManager.IMPORTANCE_HIGH
            val channel = NotificationChannel(CHANNEL_ID, name, importance).apply {
                description = descriptionText
            }
            val notificationManager: NotificationManager =
                getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            notificationManager.createNotificationChannel(channel)
        }
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event == null) return

        val packageName = event.packageName?.toString() ?: return
        val className = event.className?.toString() ?: return

        // 1. Logging Mode
        if (prefsManager.isDebugMode) {
             // Only log Window State Changes to avoid spamming for every content change
            if (event.eventType == AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED) {
                val logMsg = "Visited: $packageName / $className"
                Log.i(TAG, logMsg)
                Toast.makeText(this, logMsg, Toast.LENGTH_SHORT).show()
            }
        }

        // 2. Check Toxic Screen Blocker
        if (isBannedScreen(packageName, className)) {
            Log.w(TAG, "BLOCKED Toxic Screen: $packageName / $className")
            performGlobalAction(GLOBAL_ACTION_BACK)
            return
        }

        // 3. Anti-Disable Protection
        // Check if we are in Settings
        if (packageName == "com.android.settings") {
            // We need to inspect the content to see if "Watchdog Protector" is on screen.
            // This is resource intensive, so we do it carefully.
            val rootNode = rootInActiveWindow
            if (rootNode != null) {
                val found = findTextInNode(rootNode, "Watchdog Protector")
                if (found) {
                    Log.w(TAG, "BLOCKED Anti-Disable attempt in Settings")
                    performGlobalAction(GLOBAL_ACTION_BACK)
                }
                rootNode.recycle()
            }
        }
    }

    private fun isBannedScreen(packageName: String, className: String): Boolean {
        return bannedScreens.any { it.first == packageName && it.second == className }
    }

    private fun findTextInNode(node: AccessibilityNodeInfo, textToFind: String): Boolean {
        if (node.text != null && node.text.toString().contains(textToFind, ignoreCase = true)) {
            return true
        }

        for (i in 0 until node.childCount) {
            val child = node.getChild(i)
            if (child != null) {
                if (findTextInNode(child, textToFind)) {
                    child.recycle()
                    return true
                }
                child.recycle()
            }
        }
        return false
    }

    override fun onInterrupt() {
        Log.w(TAG, "Watchdog Service Interrupted")
    }
}
