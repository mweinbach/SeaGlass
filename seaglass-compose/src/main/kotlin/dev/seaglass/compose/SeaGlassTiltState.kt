package dev.seaglass.compose

import android.content.Context
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalContext
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * Real-time 3D device tilt state driven by hardware `Sensor.TYPE_GRAVITY` / `TYPE_ACCELEROMETER`
 * plus optional interactive gimbal/touch override.
 *
 * Drives **tilt-adaptive side refraction** (shifting the Snell's law lens refraction and
 * chromatic dispersion layers around the sides of the glass as the device tilts) and
 * **tilt-adaptive angular `SweepGradient` specular rim rotation**.
 */
@Stable
class SeaGlassTiltState(
    val hasHardwareSensor: Boolean = false
) {
    internal var rawSensorTiltX by mutableFloatStateOf(0f)
    internal var rawSensorTiltY by mutableFloatStateOf(0f)

    /**
     * Manual / interactive tilt override in `[-1f..1f]` per axis (for emulator testing or gimbal drag).
     */
    var manualTiltOverride by mutableStateOf<Offset?>(null)

    /**
     * Whether subtle ambient gyroscopic breathing is active when the device is held completely still.
     */
    var ambientDriftEnabled by mutableStateOf(true)

    internal var smoothedTiltX by mutableFloatStateOf(-0.35f)
    internal var smoothedTiltY by mutableFloatStateOf(-0.45f)

    /**
     * Normalized horizontal tilt in `[-1f..1f]` (negative = tilted left, positive = tilted right).
     */
    val tiltX: Float get() = smoothedTiltX.coerceIn(-1f, 1f)

    /**
     * Normalized vertical tilt in `[-1f..1f]` (negative = tilted top-up, positive = tilted bottom-down).
     */
    val tiltY: Float get() = smoothedTiltY.coerceIn(-1f, 1f)

    /**
     * Current specular light angle in degrees `[0f..360f)` computed from `(tiltX, tiltY)`.
     * At default rest `(-0.35, -0.45)`, angle is `~232°` (top-left windward key light).
     */
    val specularAngleDegrees: Float
        get() {
            val rad = atan2(tiltY.toDouble(), tiltX.toDouble())
            val deg = Math.toDegrees(rad).toFloat()
            return (deg + 360f) % 360f
        }

    /**
     * Magnitude of current device tilt in `[0f..1f]`.
     */
    val tiltMagnitude: Float
        get() = sqrt(tiltX * tiltX + tiltY * tiltY).coerceIn(0f, 1f)
}

val LocalSeaGlassTiltState = staticCompositionLocalOf { SeaGlassTiltState() }

/**
 * Remembers and binds a live [SeaGlassTiltState] backed by Android's `TYPE_GRAVITY`
 * (or `TYPE_ACCELEROMETER`) sensor with spring-smoothed output.
 */
@Composable
fun rememberSeaGlassTiltState(): SeaGlassTiltState {
    val context = LocalContext.current
    val sensorManager = remember(context) {
        context.getSystemService(Context.SENSOR_SERVICE) as? SensorManager
    }
    val gravitySensor = remember(sensorManager) {
        sensorManager?.getDefaultSensor(Sensor.TYPE_GRAVITY)
            ?: sensorManager?.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)
    }

    val tiltState = remember(gravitySensor != null) {
        SeaGlassTiltState(hasHardwareSensor = gravitySensor != null)
    }

    DisposableEffect(sensorManager, gravitySensor) {
        if (sensorManager == null || gravitySensor == null) {
            onDispose { }
        } else {
            val listener = object : SensorEventListener {
                override fun onSensorChanged(event: SensorEvent?) {
                    val values = event?.values ?: return
                    if (values.size >= 2) {
                        // Android sensor X is right-to-left gravity (-9.8..+9.8 m/s^2)
                        // Normalize to [-1f..1f] for a comfortable +-35 degree hand tilt range
                        val normX = (-values[0] / 6.2f).coerceIn(-1f, 1f)
                        // Subtract natural 40-degree holding pitch (~5.5 m/s^2 on Y)
                        val normY = ((values[1] - 5.2f) / 6.2f).coerceIn(-1f, 1f)
                        tiltState.rawSensorTiltX = normX
                        tiltState.rawSensorTiltY = normY
                    }
                }

                override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) = Unit
            }

            sensorManager.registerListener(
                listener,
                gravitySensor,
                SensorManager.SENSOR_DELAY_GAME
            )
            onDispose {
                sensorManager.unregisterListener(listener)
            }
        }
    }

    // Subtle continuous gyroscopic drift so even when stationary on a desk or emulator,
    // the side refraction and HDR specular rim breathe organically
    val infiniteTransition = rememberInfiniteTransition(label = "gyroBreath")
    val driftPhase by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue = (2.0 * Math.PI).toFloat(),
        animationSpec = infiniteRepeatable(
            animation = tween(durationMillis = 9000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "driftPhase"
    )

    val manual = tiltState.manualTiltOverride
    val sensorMag = sqrt(
        tiltState.rawSensorTiltX * tiltState.rawSensorTiltX +
            tiltState.rawSensorTiltY * tiltState.rawSensorTiltY
    )

    val targetX = when {
        manual != null -> manual.x.coerceIn(-1f, 1f)
        sensorMag > 0.08f -> tiltState.rawSensorTiltX
        tiltState.ambientDriftEnabled -> -0.36f + cos(driftPhase) * 0.28f
        else -> -0.36f
    }

    val targetY = when {
        manual != null -> manual.y.coerceIn(-1f, 1f)
        sensorMag > 0.08f -> tiltState.rawSensorTiltY
        tiltState.ambientDriftEnabled -> -0.44f + sin(driftPhase * 1.3f) * 0.24f
        else -> -0.44f
    }

    val animX by animateFloatAsState(
        targetValue = targetX,
        animationSpec = spring(dampingRatio = 0.82f, stiffness = 280f),
        label = "tiltX"
    )
    val animY by animateFloatAsState(
        targetValue = targetY,
        animationSpec = spring(dampingRatio = 0.82f, stiffness = 280f),
        label = "tiltY"
    )

    tiltState.smoothedTiltX = animX
    tiltState.smoothedTiltY = animY

    return tiltState
}
