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
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

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

    // Check if a PIN is set. If so, start in locked state.
    val storedPin = remember { prefsManager.userPin }
    var isLocked by remember { mutableStateOf(!storedPin.isNullOrEmpty()) }
    var showSetPinDialog by remember { mutableStateOf(false) }

    MaterialTheme {
        if (isLocked && !storedPin.isNullOrEmpty()) {
            PinLockScreen(
                correctPin = storedPin!!,
                onUnlock = { isLocked = false }
            )
        } else {
            MainScreenContent(
                prefsManager = prefsManager,
                onSetPinClick = { showSetPinDialog = true }
            )

            if (showSetPinDialog) {
                SetPinDialog(
                    onDismiss = { showSetPinDialog = false },
                    onPinSet = { newPin ->
                        prefsManager.userPin = newPin
                        showSetPinDialog = false
                    }
                )
            }
        }
    }
}

@Composable
fun MainScreenContent(
    prefsManager: PrefsManager,
    onSetPinClick: () -> Unit
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
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Ignore Battery Optimizations")
            }
             Text(
                text = "Required for persistence on Samsung devices.",
                style = MaterialTheme.typography.bodySmall,
                 modifier = Modifier.padding(bottom = 16.dp)
            )

            // Set/Change PIN Button
            OutlinedButton(
                onClick = onSetPinClick,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(if (prefsManager.userPin.isNullOrEmpty()) "Set Security PIN" else "Change Security PIN")
            }
        }
    }
}

@Composable
fun PinLockScreen(correctPin: String, onUnlock: () -> Unit) {
    val context = LocalContext.current
    val prefsManager = remember { PrefsManager(context) }

    var enteredPin by remember { mutableStateOf("") }
    var showError by remember { mutableStateOf(false) }
    var failedAttempts by remember { mutableIntStateOf(prefsManager.failedPinAttempts) }
    var lockoutEndTime by remember { mutableLongStateOf(prefsManager.lockoutEndTime) }
    var currentTime by remember { mutableLongStateOf(System.currentTimeMillis()) }

    // Update current time every second to show countdown
    LaunchedEffect(lockoutEndTime) {
        while (lockoutEndTime > System.currentTimeMillis()) {
            currentTime = System.currentTimeMillis()
            kotlinx.coroutines.delay(1000L)
        }
        currentTime = System.currentTimeMillis()
    }

    val isLockedOut = lockoutEndTime > currentTime
    val remainingLockoutSeconds = if (isLockedOut) ((lockoutEndTime - currentTime) / 1000).toInt() else 0
    val maxAttempts = 5
    val lockoutDurationMs = 30_000L // 30 seconds lockout

    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background
    ) {
        Column(
            modifier = Modifier.fillMaxSize().padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Text("Enter PIN", style = MaterialTheme.typography.headlineMedium)

            Spacer(modifier = Modifier.height(24.dp))

            // PIN Display (dots)
            Row(
                horizontalArrangement = Arrangement.Center,
                modifier = Modifier.fillMaxWidth()
            ) {
                repeat(4) { index ->
                    val filled = index < enteredPin.length
                    Box(
                        modifier = Modifier
                            .padding(8.dp)
                            .size(16.dp)
                            .clip(CircleShape)
                            .background(if (filled) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surfaceVariant)
                    )
                }
            }

            if (isLockedOut) {
                Text(
                    "Too many failed attempts. Try again in ${remainingLockoutSeconds}s",
                    color = MaterialTheme.colorScheme.error
                )
            } else if (showError) {
                val attemptsRemaining = maxAttempts - failedAttempts
                Text(
                    "Incorrect PIN ($attemptsRemaining attempts remaining)",
                    color = MaterialTheme.colorScheme.error
                )
            } else {
                Spacer(modifier = Modifier.height(20.dp)) // Placeholder for error text
            }

            Spacer(modifier = Modifier.height(32.dp))

            // Numeric Keypad
            val keys = listOf(
                listOf("1", "2", "3"),
                listOf("4", "5", "6"),
                listOf("7", "8", "9"),
                listOf("", "0", "<")
            )

            for (row in keys) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceEvenly
                ) {
                    for (key in row) {
                        if (key.isNotEmpty()) {
                            KeypadButton(key, enabled = !isLockedOut) {
                                if (key == "<") {
                                    if (enteredPin.isNotEmpty()) {
                                        enteredPin = enteredPin.dropLast(1)
                                        showError = false
                                    }
                                } else {
                                    if (enteredPin.length < 4) {
                                        enteredPin += key
                                        showError = false
                                        // Auto-check if length is 4
                                        if (enteredPin.length == 4) {
                                            if (enteredPin == correctPin) {
                                                // Reset failed attempts on successful unlock
                                                prefsManager.resetFailedAttempts()
                                                onUnlock()
                                            } else {
                                                enteredPin = ""
                                                failedAttempts++
                                                prefsManager.failedPinAttempts = failedAttempts
                                                showError = true

                                                // Check if we should trigger lockout
                                                if (failedAttempts >= maxAttempts) {
                                                    val newLockoutEnd = System.currentTimeMillis() + lockoutDurationMs
                                                    lockoutEndTime = newLockoutEnd
                                                    prefsManager.lockoutEndTime = newLockoutEnd
                                                    // Reset failed attempts after lockout is triggered
                                                    failedAttempts = 0
                                                    prefsManager.failedPinAttempts = 0
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        } else {
                            // Empty placeholder for grid alignment
                            Spacer(modifier = Modifier.size(80.dp))
                        }
                    }
                }
                Spacer(modifier = Modifier.height(16.dp))
            }
        }
    }
}

@Composable
fun KeypadButton(text: String, enabled: Boolean = true, onClick: () -> Unit) {
    Box(
        modifier = Modifier
            .size(80.dp)
            .clip(CircleShape)
            .background(
                if (enabled) MaterialTheme.colorScheme.surfaceVariant
                else MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
            )
            .clickable(enabled = enabled, onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            style = MaterialTheme.typography.headlineLarge,
            color = if (enabled) Color.Unspecified else MaterialTheme.colorScheme.onSurface.copy(alpha = 0.38f)
        )
    }
}

@Composable
fun SetPinDialog(onDismiss: () -> Unit, onPinSet: (String) -> Unit) {
    var pin by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Set Security PIN") },
        text = {
            Column {
                Text("Enter a 4-digit PIN to lock the app:")
                Spacer(modifier = Modifier.height(8.dp))
                OutlinedTextField(
                    value = pin,
                    onValueChange = { if (it.length <= 4 && it.all { c -> c.isDigit() }) pin = it },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.NumberPassword),
                    visualTransformation = PasswordVisualTransformation(),
                    singleLine = true
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onPinSet(pin) },
                enabled = pin.length == 4
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
