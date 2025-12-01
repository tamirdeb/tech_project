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

// הוסר: import com.watchdog.protector.ui.theme.WatchdogProtectorTheme (השורה הבעייתית)

class MainActivity : ComponentActivity() {
    private lateinit var prefsManager: PrefsManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        prefsManager = PrefsManager(this)

        setContent {
            // תיקון: שימוש ב-MaterialTheme הסטנדרטי במקום WatchdogProtectorTheme החסר
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
    // אם יש PIN שמור בזיכרון -> מתחילים נעולים. אחרת -> פתוחים.
    var isLocked by remember { mutableStateOf(prefsManager.hasPin()) }

    if (isLocked) {
        PinLockScreen(
            storedPin = prefsManager.pinCode ?: "",
            onUnlock = { isLocked = false }
        )
    } else {
        SettingsScreen(prefsManager)
    }
}

@Composable
fun PinLockScreen(storedPin: String, onUnlock: () -> Unit) {
    var inputPin by remember { mutableStateOf("") }
    var isError by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier.fillMaxSize().padding(32.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(text = "Watchdog Locked", style = MaterialTheme.typography.headlineMedium)
        Spacer(modifier = Modifier.height(24.dp))

        OutlinedTextField(
            value = inputPin,
            onValueChange = {
                inputPin = it
                isError = false
            },
            label = { Text("Enter PIN") },
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
            isError = isError
        )

        if (isError) {
            Text(
                text = "Incorrect PIN",
                color = MaterialTheme.colorScheme.error,
                modifier = Modifier.padding(top = 8.dp)
            )
        }

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = {
                if (inputPin == storedPin) {
                    onUnlock()
                } else {
                    isError = true
                    inputPin = ""
                }
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Unlock")
        }
    }
}

@Composable
fun SettingsScreen(prefsManager: PrefsManager) {
    val context = LocalContext.current
    var isDebug by remember { mutableStateOf(prefsManager.isDebugMode) }
    var showSetPinDialog by remember { mutableStateOf(false) }

    Column(modifier = Modifier.fillMaxSize().padding(24.dp)) {
        Text("Control Panel", style = MaterialTheme.typography.headlineLarge)
        Spacer(modifier = Modifier.height(32.dp))

        // --- מתג Debug Mode ---
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
                onCheckedChange = {
                    isDebug = it
                    prefsManager.isDebugMode = it
                }
            )
        }

        Spacer(modifier = Modifier.height(24.dp))
        Divider()
        Spacer(modifier = Modifier.height(24.dp))

        // --- כפתור הגדרת סיסמה ---
        Text(text = "Security", style = MaterialTheme.typography.titleMedium)
        Spacer(modifier = Modifier.height(8.dp))

        Button(
            onClick = { showSetPinDialog = true },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(if (prefsManager.hasPin()) "Change PIN Code" else "Set PIN Code")
        }

        Spacer(modifier = Modifier.height(16.dp))

        // --- כפתורי מערכת (נגישות וסוללה) ---
        OutlinedButton(
            onClick = {
                val intent = Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)
                context.startActivity(intent)
            },
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
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Ignore Battery Optimizations")
        }
    }

    // --- דיאלוג להגדרת סיסמה ---
    if (showSetPinDialog) {
        SetPinDialog(
            onDismiss = { showSetPinDialog = false },
            onSave = { newPin ->
                prefsManager.pinCode = newPin
                showSetPinDialog = false
            }
        )
    }
}

@Composable
fun SetPinDialog(onDismiss: () -> Unit, onSave: (String) -> Unit) {
    var text by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Set New PIN") },
        text = {
            OutlinedTextField(
                value = text,
                onValueChange = { text = it },
                label = { Text("Enter 4-digit PIN") },
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                singleLine = true
            )
        },
        confirmButton = {
            Button(
                onClick = {
                    if (text.isNotEmpty()) {
                        onSave(text)
                    }
                }
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