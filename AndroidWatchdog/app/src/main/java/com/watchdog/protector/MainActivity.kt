package com.watchdog.protector

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import androidx.activity.compose.setContent
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
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
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity

class MainActivity : FragmentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            WatchdogApp(activity = this)
        }
    }
}

@Composable
fun WatchdogApp(activity: FragmentActivity) {
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
                onUnlock = { isLocked = false },
                activity = activity
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
fun PinLockScreen(correctPin: String, onUnlock: () -> Unit, activity: FragmentActivity) {
    var enteredPin by remember { mutableStateOf("") }
    var showError by remember { mutableStateOf(false) }
    val context = LocalContext.current
    
    // Check if biometric authentication is available
    val biometricManager = remember { BiometricManager.from(context) }
    val isBiometricAvailable = remember {
        val authenticators = BiometricManager.Authenticators.BIOMETRIC_STRONG or 
                            BiometricManager.Authenticators.BIOMETRIC_WEAK
        biometricManager.canAuthenticate(authenticators) == BiometricManager.BIOMETRIC_SUCCESS
    }
    
    // Biometric prompt callback - stable reference to avoid recreations
    val biometricCallback = remember {
        object : BiometricPrompt.AuthenticationCallback() {
            override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) {
                super.onAuthenticationSucceeded(result)
                onUnlock()
            }
            
            override fun onAuthenticationError(errorCode: Int, errString: CharSequence) {
                super.onAuthenticationError(errorCode, errString)
                // User can still try PIN
            }
            
            override fun onAuthenticationFailed() {
                super.onAuthenticationFailed()
                // User can still try PIN
            }
        }
    }
    
    // Function to trigger biometric authentication - wrapped to avoid recreations
    val showBiometricPrompt = remember(activity, context, biometricCallback) {
        {
            val executor = ContextCompat.getMainExecutor(context)
            val biometricPrompt = BiometricPrompt(activity, executor, biometricCallback)
            
            val promptInfo = BiometricPrompt.PromptInfo.Builder()
                .setTitle("Unlock Watchdog")
                .setSubtitle("Use biometric to unlock (PIN recovery)")
                .setNegativeButtonText("Use PIN instead")
                .build()
            
            biometricPrompt.authenticate(promptInfo)
        }
    }

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

            if (showError) {
                Text("Incorrect PIN", color = MaterialTheme.colorScheme.error)
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
                            KeypadButton(key) {
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
                                                onUnlock()
                                            } else {
                                                enteredPin = ""
                                                showError = true
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
            
            // Biometric unlock option (for PIN recovery)
            if (isBiometricAvailable) {
                Spacer(modifier = Modifier.height(24.dp))
                TextButton(onClick = showBiometricPrompt) {
                    Text("Forgot PIN? Use Fingerprint")
                }
            }
        }
    }
}

@Composable
fun KeypadButton(text: String, onClick: () -> Unit) {
    Box(
        modifier = Modifier
            .size(80.dp)
            .clip(CircleShape)
            .background(MaterialTheme.colorScheme.surfaceVariant)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Text(text = text, style = MaterialTheme.typography.headlineLarge)
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
