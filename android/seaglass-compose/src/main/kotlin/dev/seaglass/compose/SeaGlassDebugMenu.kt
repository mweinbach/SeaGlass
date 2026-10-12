package dev.seaglass.compose

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Slider
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import java.util.Locale
import kotlin.math.roundToInt

/**
 * Reactive state holder for live tuning of a [SeaGlassPalette]'s color, transparency,
 * and optical parameters across an application.
 */
@Stable
class SeaGlassDebugState(
    initialPaletteId: String = "auto",
    initialCustomTintHex: Int = 0x2EE6B8
) {
    var paletteId by mutableStateOf(initialPaletteId)
    var customTintEnabled by mutableStateOf(false)
    var customTintHex by mutableIntStateOf(initialCustomTintHex and 0xFFFFFF)
    var transparency by mutableFloatStateOf(-1f)
    var tintDensity by mutableFloatStateOf(-1f)
    var coarseness by mutableFloatStateOf(-1f)
    var frostRadiusDp by mutableFloatStateOf(-1f)
    var ior by mutableFloatStateOf(-1f)
    var dispersion by mutableFloatStateOf(-1f)
    var rimChalkiness by mutableFloatStateOf(-1f)
    var showsOverlay by mutableStateOf(false)

    val hasOverrides: Boolean
        get() = paletteId != "auto" || customTintEnabled || transparency >= 0f || tintDensity >= 0f ||
            coarseness >= 0f || frostRadiusDp >= 0f || ior >= 1f || dispersion >= 0f || rimChalkiness >= 0f

    fun reset() {
        paletteId = "auto"
        customTintEnabled = false
        customTintHex = 0x2EE6B8
        transparency = -1f
        tintDensity = -1f
        coarseness = -1f
        frostRadiusDp = -1f
        ior = -1f
        dispersion = -1f
        rimChalkiness = -1f
    }

    /**
     * Resolves a [SeaGlassPalette] with all active debug overrides applied across its materials.
     */
    fun resolvePalette(isDark: Boolean): SeaGlassPalette {
        val cleanId = paletteId.trim().lowercase(Locale.US)
        val base = if (cleanId.isEmpty() || cleanId == "auto") {
            if (isDark) SeaGlassPalette.ObsidianTide else SeaGlassPalette.SeafoamMint
        } else {
            SeaGlassPalette.entries.firstOrNull { it.id.lowercase(Locale.US) == cleanId }
                ?: if (isDark) SeaGlassPalette.ObsidianTide else SeaGlassPalette.SeafoamMint
        }

        if (!customTintEnabled && transparency < 0f && tintDensity < 0f &&
            coarseness < 0f && frostRadiusDp < 0f && ior < 1f &&
            dispersion < 0f && rimChalkiness < 0f
        ) {
            return base
        }

        val customArgb = (0xFF000000L or (customTintHex.toLong() and 0xFFFFFFL)).toInt()

        fun overrideMaterial(mat: SeaGlassMaterial, isPrimary: Boolean): SeaGlassMaterial {
            val resolvedTint = if (customTintEnabled) {
                if (isPrimary) customArgb else SeaGlassColorMath.lerpOklab(mat.tintColorArgb, customArgb, 0.78f)
            } else {
                mat.tintColorArgb
            }
            return mat.copy(
                tintColorArgb = resolvedTint,
                transparency = if (transparency >= 0f) transparency.coerceIn(0f, 1f) else mat.transparency,
                tintDensity = if (tintDensity >= 0f) tintDensity.coerceIn(0f, 1f) else mat.tintDensity,
                coarseness = if (coarseness >= 0f) coarseness.coerceIn(0f, 1f) else mat.coarseness,
                frostRadiusDp = if (frostRadiusDp >= 0f) frostRadiusDp.coerceIn(0f, 48f) else mat.frostRadiusDp,
                ior = if (ior >= 1f) ior.coerceIn(1f, 2f) else mat.ior,
                chromaticDispersion = if (dispersion >= 0f) dispersion.coerceIn(0f, 1f) else mat.chromaticDispersion,
                rimChalkiness = if (rimChalkiness >= 0f) rimChalkiness.coerceIn(0f, 1f) else mat.rimChalkiness
            )
        }

        val primary = overrideMaterial(base.primaryGlass, isPrimary = true)
        val surface = overrideMaterial(base.surfaceGlass, isPrimary = false)
        return base.copy(
            primaryGlass = primary,
            secondaryGlass = overrideMaterial(base.secondaryGlass, isPrimary = false),
            tertiaryGlass = overrideMaterial(base.tertiaryGlass, isPrimary = false),
            surfaceGlass = surface,
            elevatedGlass = overrideMaterial(base.elevatedGlass, isPrimary = false),
            onPrimaryGlassArgb = SeaGlassColorMath.optimalContentColorFor(primary.tintColorArgb, primary.tintDensity),
            onSurfaceGlassArgb = SeaGlassColorMath.optimalContentColorFor(surface.tintColorArgb, surface.tintDensity)
        )
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun SeaGlassDebugMenu(
    state: SeaGlassDebugState,
    modifier: Modifier = Modifier,
    inSheet: Boolean = false,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current
) {
    val palette = LocalSeaGlassPalette.current
    val surf = palette.surfaceGlass

    val transparencyVal = if (state.transparency >= 0f) state.transparency else surf.transparency
    val tintDensityVal = if (state.tintDensity >= 0f) state.tintDensity else surf.tintDensity
    val coarsenessVal = if (state.coarseness >= 0f) state.coarseness else surf.coarseness
    val frostRadiusVal = if (state.frostRadiusDp >= 0f) state.frostRadiusDp else surf.frostRadiusDp
    val iorVal = if (state.ior >= 1f) state.ior else surf.ior
    val dispersionVal = if (state.dispersion >= 0f) state.dispersion else surf.chromaticDispersion
    val rimChalkinessVal = if (state.rimChalkiness >= 0f) state.rimChalkiness else surf.rimChalkiness

    val customTintHex = state.customTintHex and 0xFFFFFF
    val redChannel = (customTintHex ushr 16) and 0xFF
    val greenChannel = (customTintHex ushr 8) and 0xFF
    val blueChannel = customTintHex and 0xFF
    val hexString = remember(customTintHex) { String.format(Locale.US, "#%06X", customTintHex) }

    val textColor = if (inSheet) LocalContentColor.current else MaterialTheme.colorScheme.onSurface
    val subTextColor = if (inSheet) LocalContentColor.current.copy(alpha = 0.72f) else MaterialTheme.colorScheme.onSurfaceVariant

    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = if (inSheet) 4.dp else 16.dp, vertical = if (inSheet) 4.dp else 14.dp)
            .semantics { contentDescription = "Sea Glass Debug Menu" },
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Sea Glass Debug Menu",
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Bold),
                    color = textColor
                )
                Text(
                    text = "Live color, transparency & optical parameters",
                    style = MaterialTheme.typography.bodySmall,
                    color = subTextColor
                )
            }
            TextButton(
                onClick = { state.reset() },
                enabled = state.hasOverrides
            ) {
                Text("Reset")
            }
        }

        // Live Optical Specimen Preview Card
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .clip(RoundedCornerShape(22.dp))
                .background(
                    Brush.linearGradient(
                        colors = listOf(
                            Color(0xFF0F2942),
                            Color(0xFF1F6E7A),
                            Color(0xFFD97757),
                            Color(0xFF4338CA)
                        )
                    )
                )
                .padding(14.dp)
        ) {
            SeaGlassSurface(
                stageState = stageState,
                material = surf.copy(cornerRadiusDp = 18f),
                cornerAdaptationMode = SeaGlassCornerAdaptationMode.FIXED,
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text(
                            text = if (state.customTintEnabled) "Custom Tint ($hexString)" else palette.displayName,
                            style = MaterialTheme.typography.titleSmall.copy(fontWeight = FontWeight.Bold),
                            color = LocalContentColor.current
                        )
                        Text(
                            text = "Trans ${(transparencyVal * 100).roundToInt()}% · Tint ${(tintDensityVal * 100).roundToInt()}% · Grit ${(coarsenessVal * 100).roundToInt()}%",
                            style = MaterialTheme.typography.labelSmall.copy(fontFamily = FontFamily.Monospace),
                            color = LocalContentColor.current.copy(alpha = 0.78f)
                        )
                    }
                    Box(
                        modifier = Modifier
                            .size(32.dp)
                            .clip(CircleShape)
                            .background(Color(surf.tintColorArgb))
                            .border(1.5.dp, Color.White.copy(alpha = 0.65f), CircleShape)
                    )
                }
            }
        }

        // Preset Palettes
        val presets = remember {
            listOf("auto" to "Auto") + SeaGlassPalette.entries.map { it.id to it.displayName }
        }
        FlowRow(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            presets.forEach { (id, label) ->
                val selected = !state.customTintEnabled && state.paletteId == id
                val chipSwatch = SeaGlassPalette.entries.firstOrNull { it.id == id }?.primaryGlass?.tintColorArgb
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    modifier = Modifier
                        .clip(CircleShape)
                        .background(
                            if (selected) MaterialTheme.colorScheme.primaryContainer
                            else textColor.copy(alpha = 0.08f)
                        )
                        .border(
                            width = if (selected) 1.5.dp else 0.8.dp,
                            color = if (selected) MaterialTheme.colorScheme.primary else textColor.copy(alpha = 0.16f),
                            shape = CircleShape
                        )
                        .clickable {
                            state.customTintEnabled = false
                            state.paletteId = id
                        }
                        .padding(horizontal = 12.dp, vertical = 7.dp)
                ) {
                    if (chipSwatch != null) {
                        Box(
                            modifier = Modifier
                                .size(12.dp)
                                .clip(CircleShape)
                                .background(Color(chipSwatch))
                        )
                    }
                    Text(
                        text = label,
                        style = MaterialTheme.typography.labelMedium.copy(
                            fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium
                        ),
                        color = if (selected) MaterialTheme.colorScheme.onPrimaryContainer else textColor
                    )
                }
            }
        }

        // Custom RGB Tint Override
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Custom RGB Tint ($hexString)",
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.SemiBold),
                    color = textColor
                )
                Text(
                    text = "Override palette tint with exact R / G / B channels",
                    style = MaterialTheme.typography.bodySmall,
                    color = subTextColor
                )
            }
            Switch(
                checked = state.customTintEnabled,
                onCheckedChange = { state.customTintEnabled = it }
            )
        }

        if (state.customTintEnabled) {
            DebugSliderRow(
                label = "Red",
                valueText = "$redChannel",
                value = redChannel / 255f,
                valueRange = 0f..1f,
                textColor = textColor,
                subTextColor = subTextColor,
                onValueChange = { v ->
                    val r = (v * 255f).roundToInt().coerceIn(0, 255)
                    state.customTintHex = (r shl 16) or (greenChannel shl 8) or blueChannel
                }
            )
            DebugSliderRow(
                label = "Green",
                valueText = "$greenChannel",
                value = greenChannel / 255f,
                valueRange = 0f..1f,
                textColor = textColor,
                subTextColor = subTextColor,
                onValueChange = { v ->
                    val g = (v * 255f).roundToInt().coerceIn(0, 255)
                    state.customTintHex = (redChannel shl 16) or (g shl 8) or blueChannel
                }
            )
            DebugSliderRow(
                label = "Blue",
                valueText = "$blueChannel",
                value = blueChannel / 255f,
                valueRange = 0f..1f,
                textColor = textColor,
                subTextColor = subTextColor,
                onValueChange = { v ->
                    val b = (v * 255f).roundToInt().coerceIn(0, 255)
                    state.customTintHex = (redChannel shl 16) or (greenChannel shl 8) or b
                }
            )
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            FilledTonalButton(
                onClick = {
                    state.transparency = 0.94f
                    state.coarseness = 0.08f
                    state.frostRadiusDp = 8f
                    state.tintDensity = 0.34f
                },
                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp),
                modifier = Modifier.weight(1f).heightIn(min = 36.dp)
            ) {
                Text("Clear Liquid", maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.labelMedium)
            }
            FilledTonalButton(
                onClick = {
                    state.transparency = 0.68f
                    state.coarseness = 0.44f
                    state.frostRadiusDp = 26f
                    state.tintDensity = 0.56f
                },
                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp),
                modifier = Modifier.weight(1f).heightIn(min = 36.dp)
            ) {
                Text("Sea Glass", maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.labelMedium)
            }
            FilledTonalButton(
                onClick = {
                    state.transparency = 0.22f
                    state.coarseness = 0.86f
                    state.frostRadiusDp = 42f
                    state.tintDensity = 0.82f
                },
                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp),
                modifier = Modifier.weight(1f).heightIn(min = 36.dp)
            ) {
                Text("Dense Frost", maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.labelMedium)
            }
        }

        DebugSliderRow(
            label = "Transparency",
            valueText = "${(transparencyVal * 100).roundToInt()}%",
            value = transparencyVal,
            valueRange = 0f..1f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.transparency = it }
        )
        DebugSliderRow(
            label = "Color Density",
            valueText = "${(tintDensityVal * 100).roundToInt()}%",
            value = tintDensityVal,
            valueRange = 0f..1f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.tintDensity = it }
        )
        DebugSliderRow(
            label = "Sand Coarseness",
            valueText = "${(coarsenessVal * 100).roundToInt()}%",
            value = coarsenessVal,
            valueRange = 0f..1f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.coarseness = it }
        )
        DebugSliderRow(
            label = "Frost Blur Radius",
            valueText = "${frostRadiusVal.roundToInt()} dp",
            value = frostRadiusVal,
            valueRange = 0f..48f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.frostRadiusDp = it }
        )
        DebugSliderRow(
            label = "Refractive Index (IOR)",
            valueText = String.format(Locale.US, "%.2f", iorVal),
            value = iorVal,
            valueRange = 1.0f..2.0f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.ior = it }
        )
        DebugSliderRow(
            label = "Chromatic Dispersion",
            valueText = "${(dispersionVal * 100).roundToInt()}%",
            value = dispersionVal,
            valueRange = 0f..1f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.dispersion = it }
        )
        DebugSliderRow(
            label = "Rim Chalkiness",
            valueText = "${(rimChalkinessVal * 100).roundToInt()}%",
            value = rimChalkinessVal,
            valueRange = 0f..1f,
            textColor = textColor,
            subTextColor = subTextColor,
            onValueChange = { state.rimChalkiness = it }
        )
    }
}

@Composable
private fun DebugSliderRow(
    label: String,
    valueText: String,
    value: Float,
    valueRange: ClosedFloatingPointRange<Float>,
    textColor: Color,
    subTextColor: Color,
    onValueChange: (Float) -> Unit
) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = label,
                style = MaterialTheme.typography.bodyMedium.copy(fontWeight = FontWeight.Medium),
                color = textColor
            )
            Text(
                text = valueText,
                style = MaterialTheme.typography.labelMedium.copy(
                    fontFamily = FontFamily.Monospace,
                    fontWeight = FontWeight.SemiBold
                ),
                color = subTextColor
            )
        }
        Slider(
            value = value.coerceIn(valueRange.start, valueRange.endInclusive),
            onValueChange = onValueChange,
            valueRange = valueRange,
            modifier = Modifier
                .fillMaxWidth()
                .height(36.dp)
                .semantics { contentDescription = label }
        )
    }
}

/**
 * Floating SeaGlass sheet wrapper around [SeaGlassDebugMenu], supporting trigger-to-card lens
 * morphing and swipe-up full-page expansion.
 */
@Composable
fun SeaGlassDebugFloatingSheet(
    state: SeaGlassDebugState,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    triggerAnchorId: String = "header.debug",
    modifier: Modifier = Modifier
) {
    SeaGlassFloatingSheet(
        visible = state.showsOverlay,
        onDismissRequest = { state.showsOverlay = false },
        stageState = stageState,
        triggerRectInRoot = seaGlassMorphAnchor(triggerAnchorId),
        modifier = modifier
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(rememberScrollState())
        ) {
            SeaGlassDebugMenu(state = state, inSheet = true, stageState = stageState)
        }
    }
}
