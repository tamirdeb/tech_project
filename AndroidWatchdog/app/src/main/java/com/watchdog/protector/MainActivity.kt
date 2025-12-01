package com.watchdog.protector

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp

class MainActivity : ComponentActivity() {
    private lateinit var prefsManager: PrefsManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        prefsManager = PrefsManager(this)

        setContent {
            MaterialTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    MainAppContent(prefsManager)
                }
            }
        }
    }
}

@Composable
fun MainAppContent(prefsManager: PrefsManager) {
    // If a password is set, default to locked state
    var isLocked by remember { mutableStateOf(prefsManager.hasPassword()) }
    var showSetPasswordDialog by remember { mutableStateOf(false) }
    var showUnlockDialog by remember { mutableStateOf(false) }

    // Logic: If locked, show settings but disabled.
    SettingsScreen(
        prefsManager = prefsManager,
        isLocked = isLocked,
        onUnlockRequest = { showUnlockDialog = true },
        onSetPasswordRequest = { showSetPasswordDialog = true }
    )

    if (showUnlockDialog) {
        UnlockDialog(
            correctPassword = prefsManager.userPassword ?: "",
            onDismiss = { showUnlockDialog = false },
            onUnlock = {
                isLocked = false
                showUnlockDialog = false
            }
        )
    }

    if (showSetPasswordDialog) {
        SetPasswordDialog(
            onDismiss = { showSetPasswordDialog = false },
            onSave = { newPassword ->
                prefsManager.userPassword = newPassword
                showSetPasswordDialog = false
                // Re-lock immediately to enforce security
                isLocked = true
            }
        )
    }
}

@Composable
fun SettingsScreen(
    prefsManager: PrefsManager,
    isLocked: Boolean,
    onUnlockRequest: () -> Unit,
    onSetPasswordRequest: () -> Unit
) {
    val context = LocalContext.current
    var isDebug by remember { mutableStateOf(prefsManager.isDebugMode) }

    Column(modifier = Modifier.fillMaxSize().padding(24.dp)) {
        Text("Control Panel", style = MaterialTheme.typography.headlineLarge)
        Spacer(modifier = Modifier.height(32.dp))

        // --- Debug Mode Switch ---
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(text = "Debug Mode", style = MaterialTheme.typography.titleMedium)
                Text(
                    text = "Enable to view logs.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.secondary
                )
            }
            Switch(
                checked = isDebug,
                enabled = !isLocked, // Disabled when locked
                onCheckedChange = {
                    isDebug = it
                    prefsManager.isDebugMode = it
                }
            )
        }

        Spacer(modifier = Modifier.height(24.dp))
        Divider()
        Spacer(modifier = Modifier.height(24.dp))

        // --- Security Section ---
        Text(text = "Security", style = MaterialTheme.typography.titleMedium)
        Spacer(modifier = Modifier.height(8.dp))

        if (isLocked) {
            Button(
                onClick = onUnlockRequest,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Unlock Settings")
            }
        } else {
            Button(
                onClick = onSetPasswordRequest,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (prefsManager.hasPassword()) "Change Password" else "Set Password")
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        // --- System Buttons ---
        OutlinedButton(
            onClick = {
                val intent = Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)
                context.startActivity(intent)
            },
            enabled = !isLocked, // Disabled when locked
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Open Accessibility Settings")
        }

        Spacer(modifier = Modifier.height(8.dp))

        OutlinedButton(
            onClick = {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                    val intent = Intent()
                    val packageName = context.packageName
                    val pm = context.getSystemService(Context.POWER_SERVICE) as PowerManager
                    if (!pm.isIgnoringBatteryOptimizations(packageName)) {
                        intent.action = Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS
                        intent.data = Uri.parse("package:$packageName")
                        context.startActivity(intent)
                    } else {
                        android.widget.Toast.makeText(context, "Already ignored optimizations", android.widget.Toast.LENGTH_SHORT).show()
                    }
                }
            },
            enabled = !isLocked, // Disabled when locked
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Ignore Battery Optimizations")
        }
    }
}

@Composable
fun SetPasswordDialog(onDismiss: () -> Unit, onSave: (String) -> Unit) {
    var password by remember { mutableStateOf("") }
    val isValid = password.length >= 25

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Set New Password") },
        text = {
            Column {
                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Enter Password") },
                    placeholder = { Text("Min 25 characters") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                    visualTransformation = PasswordVisualTransformation(),
                    singleLine = true,
                    isError = password.isNotEmpty() && !isValid
                )
                if (password.isNotEmpty() && !isValid) {
                    Text(
                        text = "Must be at least 25 characters (${password.length}/25)",
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = { onSave(password) },
                enabled = isValid
            ) {
                Text("Save")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}

@Composable
fun UnlockDialog(correctPassword: String, onDismiss: () -> Unit, onUnlock: () -> Unit) {
    var password by remember { mutableStateOf("") }
    var isError by remember { mutableStateOf(false) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Unlock Settings") },
        text = {
            Column {
                OutlinedTextField(
                    value = password,
                    onValueChange = {
                        password = it
                        isError = false
                    },
                    label = { Text("Enter Password") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                    visualTransformation = PasswordVisualTransformation(),
                    singleLine = true,
                    isError = isError
                )
                if (isError) {
                    Text(
                        text = "Incorrect Password",
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(top = 4.dp)
                    )
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    if (password == correctPassword) {
                        onUnlock()
                    } else {
                        isError = true
                    }
                }
            ) {
                Text("Unlock")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}
