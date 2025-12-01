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
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp

// Watchdog Protector - Main Activity
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            WatchdogApp()
        }
    }
}

@Composable
fun WatchdogApp() {
    val context = LocalContext.current
    val prefsManager = remember { PrefsManager(context) }

    // Check if a Password is set.
    val storedPassword = remember { prefsManager.userPassword }

    // Default locked if password exists
    var isLocked by remember { mutableStateOf(!storedPassword.isNullOrEmpty()) }

    var showSetPasswordDialog by remember { mutableStateOf(false) }
    var showUnlockDialog by remember { mutableStateOf(false) }

    MaterialTheme {
        MainScreenContent(
            prefsManager = prefsManager,
            isLocked = isLocked,
            hasPassword = !storedPassword.isNullOrEmpty(),
            onUnlockClick = { showUnlockDialog = true },
            onSetPasswordClick = { showSetPasswordDialog = true }
        )

        if (showSetPasswordDialog) {
            SetPasswordDialog(
                onDismiss = { showSetPasswordDialog = false },
                onPasswordSet = { newPassword ->
                    prefsManager.userPassword = newPassword
                    showSetPasswordDialog = false
                    // Re-lock immediately after setting password as per requirements ("will be grayed out")
                    isLocked = true
                }
            )
        }

        if (showUnlockDialog) {
            UnlockDialog(
                correctPassword = storedPassword ?: "",
                onDismiss = { showUnlockDialog = false },
                onUnlock = {
                    isLocked = false
                    showUnlockDialog = false
                }
            )
        }
    }
}

@Composable
fun MainScreenContent(
    prefsManager: PrefsManager,
    isLocked: Boolean,
    hasPassword: Boolean,
    onUnlockClick: () -> Unit,
    onSetPasswordClick: () -> Unit
) {
    val context = LocalContext.current
    var isDebugMode by remember { mutableStateOf(prefsManager.isDebugMode) }

    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "Watchdog Protector",
                style = MaterialTheme.typography.headlineMedium
            )

            Spacer(modifier = Modifier.height(32.dp))

            // Debug Mode Toggle
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(text = "Debug Mode (Log Screens)")
                Switch(
                    checked = isDebugMode,
                    enabled = !isLocked,
                    onCheckedChange = {
                        isDebugMode = it
                        prefsManager.isDebugMode = it
                    }
                )
            }
            Text(
                text = "Enable this to see Package/Class names in Logcat and Toast messages.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Spacer(modifier = Modifier.height(24.dp))

            // Accessibility Settings Button
            Button(
                onClick = {
                    val intent = Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)
                    context.startActivity(intent)
                },
                enabled = !isLocked,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Open Accessibility Settings")
            }
            Text(
                text = "Enable 'Watchdog Protector' service here.",
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.padding(bottom = 16.dp)
            )

            // Battery Optimization Button
            Button(
                onClick = {
                    requestIgnoreBatteryOptimizations(context)
                },
                enabled = !isLocked,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Ignore Battery Optimizations")
            }
             Text(
                text = "Required for persistence on Samsung devices.",
                style = MaterialTheme.typography.bodySmall,
                 modifier = Modifier.padding(bottom = 16.dp)
            )

            // Security Action Button
            if (isLocked) {
                OutlinedButton(
                    onClick = onUnlockClick,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Unlock Settings")
                }
            } else {
                OutlinedButton(
                    onClick = onSetPasswordClick,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(if (hasPassword) "Change Security Password" else "Set Security Password")
                }
            }
        }
    }
}

@Composable
fun SetPasswordDialog(onDismiss: () -> Unit, onPasswordSet: (String) -> Unit) {
    var password by remember { mutableStateOf("") }
    val isValid = password.length >= 25

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Set Security Password") },
        text = {
            Column {
                Text("Enter a secure password (minimum 25 characters) to lock the app:")
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                    visualTransformation = PasswordVisualTransformation(),
                    singleLine = true,
                    isError = password.isNotEmpty() && !isValid,
                    supportingText = {
                        if (password.isNotEmpty() && !isValid) {
                            Text("Must be at least 25 characters (${password.length}/25)")
                        }
                    }
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onPasswordSet(password) },
                enabled = isValid
            ) {
                Text("Save & Lock")
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
                Text("Enter your password to enable settings:")
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = password,
                    onValueChange = {
                        password = it
                        isError = false
                    },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                    visualTransformation = PasswordVisualTransformation(),
                    singleLine = true,
                    isError = isError,
                    supportingText = {
                        if (isError) Text("Incorrect Password")
                    }
                )
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

fun requestIgnoreBatteryOptimizations(context: Context) {
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
}
