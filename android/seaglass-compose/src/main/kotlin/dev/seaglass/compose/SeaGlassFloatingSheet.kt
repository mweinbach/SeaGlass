package dev.seaglass.compose

import android.graphics.Shader
import android.os.Build
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.scaleIn
import androidx.compose.animation.scaleOut
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectVerticalDragGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.asPaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.systemBars
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.backend.SeaGlassShaderProgramCache
import kotlinx.coroutines.launch
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.roundToInt

/**
 * Defines a quick-action button in a [SeaGlassSheetActionStrip] (such as `Pin`, `Unread`,
 * `Mute`, `Compact`, `Fork`, `Export`).
 */
@Immutable
data class SeaGlassSheetQuickAction(
    val id: String,
    val label: String,
    val glyph: SeaGlassGlyph,
    val active: Boolean = false
)

/**
 * iOS-Style Floating Concentric SeaGlass Detent Sheet & Page Overlay (`SeaGlassFloatingSheet`).
 *
 * Designed to be layered directly on top of any Jetpack Compose screen inside a [Box]:
 * 1. **Device-Display Concentric Corner Matching**:
 *    Floats inset by [outerInsetDp] (`8.dp` by default) from the left, right, and bottom edges of
 *    the display and uses [SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY] (`R_sheet = R_bezel - inset`)
 *    so its bottom and top corners curve concentrically with the physical device screen bezel.
 * 2. **Semi-Transparent Sand-Frosted Glass Over Live Backdrop**:
 *    Refracts and diffuses the live [stageState] (`LocalSeaGlassStageState.current`) behind it
 *    while softly dimming the background with an interactive tap-to-dismiss scrim.
 * 3. **Morph Animation & Temporal De-Aging**:
 *    When [enableMorphAnimation] is true, the sheet morphs in and out with lead-lag spatial springs,
 *    in-flight `DECAL` Gaussian content focus, and **Temporal De-Aging** ([SeaGlassMaterial.deaged]—
 *    shedding its coarse sand texture while malleable and moving, then re-crystallizing its
 *    sand-frosted finish as it settles into place).
 * 4. **Interactive Drag Detents & Swipe-to-Dismiss**:
 *    Dragging the sheet or top grabber pill rubber-bands vertically, de-ages the glass in real time
 *    proportional to drag displacement, and dismisses cleanly when pulled past threshold.
 */
@Composable
fun SeaGlassFloatingSheet(
    visible: Boolean,
    onDismissRequest: () -> Unit,
    modifier: Modifier = Modifier,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass.copy(
        cornerRadiusDp = 42f,
        transparency = 0.56f,
        coarseness = 0.44f,
        frostRadiusDp = 30f,
        thicknessDp = 22f,
        rimChalkiness = 0.86f
    ),
    outerInsetDp: Dp = 8.dp,
    enableMorphAnimation: Boolean = true,
    morphTransformOrigin: TransformOrigin = TransformOrigin(0.5f, 0.92f),
    scrimMaxAlpha: Float = 0.22f,
    content: @Composable ColumnScope.() -> Unit
) {
    val density = LocalDensity.current
    val scope = rememberCoroutineScope()
    val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()

    // Lead-lag spatial springs for morph / sheet presentation
    val targetProgress = if (visible) 1f else 0f
    val spatialWidthProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = spring(dampingRatio = 0.72f, stiffness = 420f),
        label = "floatingSheetWidthMorph"
    )
    val spatialHeightProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 340f),
        label = "floatingSheetHeightMorph"
    )
    val effectsProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = spring(dampingRatio = 1.0f, stiffness = 520f),
        label = "floatingSheetEffects"
    )

    val dragOffsetY = remember { Animatable(0f) }

    if (effectsProgress <= 0.005f && !visible) {
        return
    }

    val dismissThresholdPx = with(density) { 108.dp.toPx() }
    val dragFraction = (abs(dragOffsetY.value) / dismissThresholdPx).coerceIn(0f, 1f)
    val inFlightMorphEnergy = (4f * effectsProgress * (1f - effectsProgress)).coerceIn(0f, 1f)
    val totalMalleability = max(inFlightMorphEnergy, dragFraction * 0.90f).coerceIn(0f, 1f)

    // Temporal De-Aging: while morphing or being dragged, the sheet sheds its coarse sand texture
    // into clear malleable glass, then solidifies its sand-frosted treatment as it settles!
    val activeSheetMaterial = remember(material, totalMalleability) {
        material.deaged(totalMalleability)
    }

    val slideDistancePx = with(density) { 240.dp.toPx() }
    val enterTranslationY = (1f - spatialHeightProgress) * slideDistancePx + dragOffsetY.value
    val morphScaleX = if (enableMorphAnimation) {
        (0.76f + 0.24f * spatialWidthProgress) * (1f - 0.03f * dragFraction)
    } else {
        1f - 0.03f * dragFraction
    }
    val morphScaleY = if (enableMorphAnimation) {
        (0.68f + 0.32f * spatialHeightProgress) * (1f + 0.02f * dragFraction)
    } else {
        1f
    }
    val contentBlurPx = if (enableMorphAnimation) {
        ((1f - effectsProgress) * (1f - effectsProgress) * 14f * density.density).coerceAtLeast(0f)
    } else {
        0f
    }

    Box(
        modifier = modifier
            .fillMaxSize()
            .zIndex(80f)
    ) {
        // 1. Subtle interactive backdrop scrim (tap outside to dismiss)
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = (scrimMaxAlpha * effectsProgress * (1f - dragFraction * 0.5f)).coerceIn(0f, 0.6f)))
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onDismissRequest
                )
        )

        // 2. Floating Concentric SeaGlass Sheet Container anchored near the bottom device bezel
        Box(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .padding(
                    start = outerInsetDp,
                    end = outerInsetDp,
                    bottom = systemBarsPadding.calculateBottomPadding() + outerInsetDp
                )
                .offset { IntOffset(0, enterTranslationY.roundToInt()) }
                .graphicsLayer {
                    alpha = effectsProgress.coerceIn(0f, 1f)
                    transformOrigin = morphTransformOrigin
                    scaleX = morphScaleX
                    scaleY = morphScaleY
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && contentBlurPx > 0.6f) {
                        compositingStrategy = CompositingStrategy.Offscreen
                        renderEffect = SeaGlassShaderProgramCache
                            .obtainBlurRenderEffect(contentBlurPx, Shader.TileMode.DECAL)
                            .asComposeRenderEffect()
                    } else {
                        renderEffect = null
                    }
                }
                .pointerInput(onDismissRequest) {
                    detectVerticalDragGestures(
                        onDragEnd = {
                            if (dragOffsetY.value > dismissThresholdPx) {
                                scope.launch {
                                    dragOffsetY.snapTo(0f)
                                    onDismissRequest()
                                }
                            } else {
                                scope.launch {
                                    dragOffsetY.animateTo(
                                        targetValue = 0f,
                                        animationSpec = spring(dampingRatio = 0.64f, stiffness = 420f)
                                    )
                                }
                            }
                        },
                        onDragCancel = {
                            scope.launch {
                                dragOffsetY.animateTo(
                                    targetValue = 0f,
                                    animationSpec = spring(dampingRatio = 0.64f, stiffness = 420f)
                                )
                            }
                        },
                        onVerticalDrag = { change, dragAmount ->
                            change.consume()
                            scope.launch {
                                val next = dragOffsetY.value + dragAmount * (if (dragOffsetY.value < 0f) 0.28f else 0.82f)
                                dragOffsetY.snapTo(next.coerceAtLeast(-48f))
                            }
                        }
                    )
                }
                // Consume taps inside the sheet so they don't trigger the background scrim dismiss
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = {}
                )
        ) {
            SeaGlassFloatingSheetSurface(
                stageState = stageState,
                material = activeSheetMaterial,
                content = content
            )
        }
    }
}

/**
 * Concentric Device-Corner-Matched SeaGlass Sheet Surface (`SeaGlassFloatingSheetSurface`).
 * Can be used inside [SeaGlassFloatingSheet] or embedded directly as a native page card.
 */
@Composable
fun SeaGlassFloatingSheetSurface(
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass.copy(
        cornerRadiusDp = 42f,
        transparency = 0.56f,
        coarseness = 0.44f,
        frostRadiusDp = 30f,
        thicknessDp = 22f,
        rimChalkiness = 0.86f
    ),
    content: @Composable ColumnScope.() -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material,
        cornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
        cornerMask = SeaGlassCornerMask.ALL,
        cornerProximityThresholdDp = 64f,
        allowAsymmetricCorners = false,
        contentPadding = PaddingValues(horizontal = 18.dp, vertical = 12.dp),
        modifier = modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            // Top Centered iOS-Style Grabber Handle Pill
            Box(
                modifier = Modifier
                    .align(Alignment.CenterHorizontally)
                    .padding(top = 2.dp)
                    .size(width = 38.dp, height = 4.5.dp)
                    .clip(CircleShape)
                    .background(LocalContentColor.current.copy(alpha = 0.28f))
            )

            content()
        }
    }
}

/**
 * Header block for a [SeaGlassFloatingSheet]: displays a leading dotted-matrix emblem (or custom
 * glyph), bold multi-line title, and secondary path/subtitle.
 */
@Composable
fun SeaGlassSheetHeader(
    title: String,
    subtitle: String? = null,
    modifier: Modifier = Modifier,
    leadingGlyph: SeaGlassGlyph = SeaGlassGlyph.DotGrid
) {
    val contentColor = LocalContentColor.current
    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(6.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .padding(top = 2.dp)
                    .size(34.dp)
                    .clip(CircleShape)
                    .background(contentColor.copy(alpha = 0.08f))
            ) {
                SeaGlassIcon(
                    glyph = leadingGlyph,
                    modifier = Modifier.size(22.dp),
                    tint = contentColor.copy(alpha = 0.65f)
                )
            }

            Text(
                text = title,
                color = contentColor,
                style = MaterialTheme.typography.headlineSmall.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 21.sp,
                    lineHeight = 26.sp,
                    letterSpacing = (-0.3).sp
                ),
                modifier = Modifier.weight(1f)
            )
        }

        if (!subtitle.isNullOrBlank()) {
            Text(
                text = subtitle,
                color = contentColor.copy(alpha = 0.62f),
                maxLines = 1,
                overflow = TextOverflow.MiddleEllipsis,
                style = MaterialTheme.typography.bodySmall.copy(
                    fontWeight = FontWeight.Medium,
                    fontSize = 13.sp
                )
            )
        }
    }
}

/**
 * Horizontal 6-action icon + label strip (`Pin`, `Unread`, `Mute`, `Compact`, `Fork`, `Export`)
 * for a [SeaGlassFloatingSheet]. Each item responds with a spatial spring bounce and active highlight.
 */
@Composable
fun SeaGlassSheetActionStrip(
    actions: List<SeaGlassSheetQuickAction>,
    onActionClick: (SeaGlassSheetQuickAction) -> Unit,
    modifier: Modifier = Modifier,
    accentColor: Color = Color(LocalSeaGlassPalette.current.primaryGlass.tintColorArgb)
) {
    val contentColor = LocalContentColor.current
    Row(
        modifier = modifier
            .fillMaxWidth()
            .padding(vertical = 2.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        actions.forEach { action ->
            val interactionSource = remember { MutableInteractionSource() }
            val isPressed by interactionSource.collectIsPressedAsState()
            val scale by animateFloatAsState(
                targetValue = when {
                    isPressed -> 0.88f
                    action.active -> 1.06f
                    else -> 1f
                },
                animationSpec = spring(dampingRatio = 0.56f, stiffness = 520f),
                label = "sheetQuickActionScale"
            )
            val itemTint = if (action.active) {
                Color(
                    SeaGlassColorMath.lerpOklab(
                        contentColor.hashCode(),
                        LocalSeaGlassPalette.current.tertiaryGlass.tintColorArgb,
                        0.45f
                    )
                )
            } else {
                contentColor
            }

            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(6.dp),
                modifier = Modifier
                    .weight(1f)
                    .graphicsLayer {
                        scaleX = scale
                        scaleY = scale
                    }
                    .clip(RoundedCornerShape(14.dp))
                    .background(
                        if (action.active) accentColor.copy(alpha = 0.22f) else Color.Transparent
                    )
                    .clickable(
                        interactionSource = interactionSource,
                        indication = null,
                        onClick = { onActionClick(action) }
                    )
                    .padding(vertical = 6.dp, horizontal = 2.dp)
            ) {
                SeaGlassIcon(
                    glyph = action.glyph,
                    modifier = Modifier.size(22.dp),
                    tint = if (action.active) contentColor else contentColor.copy(alpha = 0.90f)
                )
                Text(
                    text = action.label,
                    maxLines = 1,
                    color = if (action.active) contentColor else contentColor.copy(alpha = 0.68f),
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = if (action.active) FontWeight.ExtraBold else FontWeight.Medium,
                        fontSize = 11.sp
                    )
                )
            }
        }
    }
}

/**
 * Rounded inset translucent card group inside a [SeaGlassFloatingSheet] that holds one or more
 * hairline-separated [SeaGlassSheetRow] items.
 */
@Composable
fun SeaGlassSheetGroup(
    modifier: Modifier = Modifier,
    content: @Composable ColumnScope.() -> Unit
) {
    val contentColor = LocalContentColor.current
    val isDark = contentColor.red > 0.7f
    val fillAlpha = if (isDark) 0.12f else 0.07f
    val borderAlpha = if (isDark) 0.16f else 0.12f

    Column(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(22.dp))
            .background(contentColor.copy(alpha = fillAlpha))
            .border(0.8.dp, contentColor.copy(alpha = borderAlpha), RoundedCornerShape(22.dp)),
        content = content
    )
}

/**
 * Single key-value or drill-down row inside a [SeaGlassSheetGroup] (e.g. `Model -> ✦ gemini-4-argon-eap ›`,
 * `Context -> 199,692 tokens`, `Cost -> $0.96`).
 */
@Composable
fun SeaGlassSheetRow(
    label: String,
    value: String,
    modifier: Modifier = Modifier,
    leadingValueGlyph: SeaGlassGlyph? = null,
    showChevron: Boolean = false,
    showBottomDivider: Boolean = true,
    onClick: (() -> Unit)? = null
) {
    val contentColor = LocalContentColor.current
    Column(modifier = modifier.fillMaxWidth()) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween,
            modifier = Modifier
                .fillMaxWidth()
                .then(
                    if (onClick != null) {
                        Modifier.clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = onClick
                        )
                    } else {
                        Modifier
                    }
                )
                .padding(horizontal = 18.dp, vertical = 14.dp)
        ) {
            Text(
                text = label,
                color = contentColor.copy(alpha = 0.62f),
                style = MaterialTheme.typography.bodyMedium.copy(
                    fontWeight = FontWeight.Medium,
                    fontSize = 15.sp
                )
            )

            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                if (leadingValueGlyph != null) {
                    SeaGlassIcon(
                        glyph = leadingValueGlyph,
                        modifier = Modifier.size(16.dp),
                        tint = contentColor
                    )
                }
                Text(
                    text = value,
                    color = contentColor,
                    maxLines = 1,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 15.sp
                    )
                )
                if (showChevron) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.ChevronRight,
                        modifier = Modifier.size(15.dp),
                        tint = contentColor.copy(alpha = 0.45f)
                    )
                }
            }
        }

        if (showBottomDivider) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 18.dp)
                    .height(0.7.dp)
                    .background(contentColor.copy(alpha = 0.10f))
            )
        }
    }
}

/**
 * Inset toggle row with an optional supporting caption below it (e.g., `"Always Notify"` +
 * `"Notify this phone whenever a reply finishes, even ones you didn't send from here."`).
 */
@Composable
fun SeaGlassSheetToggleSection(
    label: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    caption: String? = null,
    checkedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    uncheckedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val contentColor = LocalContentColor.current
    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SeaGlassSheetGroup {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween,
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = { onCheckedChange(!checked) }
                    )
                    .padding(horizontal = 18.dp, vertical = 10.dp)
            ) {
                Text(
                    text = label,
                    color = contentColor,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Medium,
                        fontSize = 15.sp
                    )
                )

                SeaGlassSwitch(
                    checked = checked,
                    onCheckedChange = onCheckedChange,
                    stageState = stageState,
                    checkedMaterial = checkedMaterial,
                    uncheckedMaterial = uncheckedMaterial
                )
            }
        }

        if (!caption.isNullOrBlank()) {
            Text(
                text = caption,
                color = contentColor.copy(alpha = 0.58f),
                style = MaterialTheme.typography.bodySmall.copy(
                    fontSize = 12.5.sp,
                    lineHeight = 17.sp
                ),
                modifier = Modifier.padding(horizontal = 12.dp)
            )
        }
    }
}

/**
 * Full-width inset action or destructive button for the bottom of a [SeaGlassFloatingSheet]
 * (e.g. `"Delete Session"`).
 */
@Composable
fun SeaGlassSheetActionButton(
    label: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    isDestructive: Boolean = true
) {
    val contentColor = LocalContentColor.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()
    val scale by animateFloatAsState(
        targetValue = if (isPressed) 0.97f else 1f,
        animationSpec = spring(dampingRatio = 0.62f, stiffness = 540f),
        label = "sheetActionBtnScale"
    )
    val textColor = if (isDestructive) Color(0xFFD92D20) else contentColor

    SeaGlassSheetGroup(
        modifier = modifier.graphicsLayer {
            scaleX = scale
            scaleY = scale
        }
    ) {
        Box(
            contentAlignment = Alignment.CenterStart,
            modifier = Modifier
                .fillMaxWidth()
                .clickable(
                    interactionSource = interactionSource,
                    indication = null,
                    onClick = onClick
                )
                .padding(horizontal = 18.dp, vertical = 15.dp)
        ) {
            Text(
                text = label,
                color = textColor,
                style = MaterialTheme.typography.bodyMedium.copy(
                    fontWeight = FontWeight.Medium,
                    fontSize = 15.5.sp
                )
            )
        }
    }
}

/**
 * Turnkey, Interactive iOS-Style Floating Concentric SeaGlass Inspector Sheet Content
 * (`SeaGlassInspectorSheetContent`).
 *
 * Includes:
 * - Header (`title`, `subtitle`, `DotGrid` emblem)
 * - Interactive 6-action strip (`Pin`, `Unread`, `Mute`, `Compact`, `Fork`, `Export`)
 * - Inset grouped table (`Model -> ✦ gemini-4-argon-eap ›`, `Context -> 199,692 tokens`, `Cost -> $0.96`)
 * - In-Place Sub-Page Morphing: tapping the `Model ›` row smoothly morphs the sheet in-place
 *   into a Model & Effort selector page and back!
 * - `"Always Notify"` toggle row + caption
 * - `"Delete Session"` destructive button
 */
@Composable
fun SeaGlassInspectorSheetContent(
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    title: String = "Windows UI Redesign for SeaGlass Elements",
    subtitle: String = "C:\\Users\\maxw6\\Projects\\SeaGlass ...\\projects\\seaglass",
    initialModel: String = "gemini-4-argon-eap",
    contextTokensText: String = "199,692 tokens",
    costText: String = "$0.96",
    onActionTriggered: (String) -> Unit = {},
    onDeleteSession: () -> Unit = {}
) {
    val palette = LocalSeaGlassPalette.current
    var activeSubPage by remember { mutableIntStateOf(0) } // 0 = Main Inspector, 1 = Model Picker Sub-Page
    var selectedModel by remember { mutableStateOf(initialModel) }
    var alwaysNotify by remember { mutableStateOf(false) }

    val activeQuickIds = remember { mutableStateListOf("pin") }
    val quickActions = remember(activeQuickIds.toList()) {
        listOf(
            SeaGlassSheetQuickAction("pin", "Pin", SeaGlassGlyph.Pin, "pin" in activeQuickIds),
            SeaGlassSheetQuickAction("unread", "Unread", SeaGlassGlyph.Unread, "unread" in activeQuickIds),
            SeaGlassSheetQuickAction("mute", "Mute", SeaGlassGlyph.Mute, "mute" in activeQuickIds),
            SeaGlassSheetQuickAction("compact", "Compact", SeaGlassGlyph.Compact, "compact" in activeQuickIds),
            SeaGlassSheetQuickAction("fork", "Fork", SeaGlassGlyph.Fork, "fork" in activeQuickIds),
            SeaGlassSheetQuickAction("export", "Export", SeaGlassGlyph.Export, "export" in activeQuickIds)
        )
    }

    val modelCatalog = remember {
        listOf(
            Triple("gemini-4-argon-eap", "Gemini 4 Argon EAP", "1M Context · High Effort"),
            Triple("claude-sonnet-4-6", "Claude Sonnet 4.6", "200K Context · Balanced"),
            Triple("gpt-5-4-pro", "GPT-5.4 Pro", "400K Context · Deep Reasoning"),
            Triple("exo-flash-free", "Exo 3.1 Flash", "128K Context · Ultra-Low Latency")
        )
    }

    AnimatedContent(
        targetState = activeSubPage,
        transitionSpec = {
            (fadeIn(animationSpec = tween(210)) +
                scaleIn(
                    initialScale = 0.94f,
                    animationSpec = spring(dampingRatio = 0.72f, stiffness = 400f)
                )).togetherWith(
                fadeOut(animationSpec = tween(140)) +
                    scaleOut(targetScale = 0.96f, animationSpec = tween(140))
            )
        },
        modifier = modifier.fillMaxWidth(),
        label = "inspectorSheetSubPageMorph"
    ) { page ->
        if (page == 0) {
            Column(
                modifier = Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                SeaGlassSheetHeader(
                    title = title,
                    subtitle = subtitle,
                    leadingGlyph = SeaGlassGlyph.DotGrid
                )

                SeaGlassSheetActionStrip(
                    actions = quickActions,
                    onActionClick = { clicked ->
                        if (clicked.id in activeQuickIds) {
                            activeQuickIds.remove(clicked.id)
                        } else {
                            activeQuickIds.add(clicked.id)
                        }
                        onActionTriggered(clicked.label)
                    },
                    accentColor = Color(palette.primaryGlass.tintColorArgb)
                )

                SeaGlassSheetGroup {
                    SeaGlassSheetRow(
                        label = "Model",
                        value = selectedModel,
                        leadingValueGlyph = SeaGlassGlyph.Sparkles,
                        showChevron = true,
                        showBottomDivider = true,
                        onClick = { activeSubPage = 1 }
                    )
                    SeaGlassSheetRow(
                        label = "Context",
                        value = contextTokensText,
                        showChevron = false,
                        showBottomDivider = true
                    )
                    SeaGlassSheetRow(
                        label = "Cost",
                        value = costText,
                        showChevron = false,
                        showBottomDivider = false
                    )
                }

                SeaGlassSheetToggleSection(
                    label = "Always Notify",
                    checked = alwaysNotify,
                    onCheckedChange = { alwaysNotify = it },
                    stageState = stageState,
                    caption = "Notify this device whenever a reply finishes, even ones you didn't send from here.",
                    checkedMaterial = palette.primaryGlass,
                    uncheckedMaterial = palette.surfaceGlass
                )

                SeaGlassSheetActionButton(
                    label = "Delete Session",
                    isDestructive = true,
                    onClick = onDeleteSession
                )
            }
        } else {
            // Sub-Page 1: In-Sheet Morphing Model Selector Page
            val contentColor = LocalContentColor.current
            Column(
                modifier = Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Box(
                        contentAlignment = Alignment.Center,
                        modifier = Modifier
                            .size(36.dp)
                            .clip(CircleShape)
                            .background(contentColor.copy(alpha = 0.10f))
                            .clickable(
                                interactionSource = remember { MutableInteractionSource() },
                                indication = null,
                                onClick = { activeSubPage = 0 }
                            )
                    ) {
                        SeaGlassIcon(
                            glyph = SeaGlassGlyph.Back,
                            modifier = Modifier.size(18.dp),
                            tint = contentColor
                        )
                    }
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = "Select Session Model",
                            color = contentColor,
                            style = MaterialTheme.typography.titleMedium.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 17.sp
                            )
                        )
                        Text(
                            text = "Tap a model to morph back to session inspector",
                            color = contentColor.copy(alpha = 0.62f),
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 11.5.sp)
                        )
                    }
                }

                SeaGlassSheetGroup {
                    modelCatalog.forEachIndexed { idx, (id, display, meta) ->
                        val isSelected = id == selectedModel
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Row(
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.SpaceBetween,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clickable(
                                        interactionSource = remember { MutableInteractionSource() },
                                        indication = null,
                                        onClick = {
                                            selectedModel = id
                                            onActionTriggered("Model: $id")
                                            activeSubPage = 0
                                        }
                                    )
                                    .padding(horizontal = 18.dp, vertical = 13.dp)
                            ) {
                                Row(
                                    verticalAlignment = Alignment.CenterVertically,
                                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                                    modifier = Modifier.weight(1f)
                                ) {
                                    SeaGlassIcon(
                                        glyph = SeaGlassGlyph.Sparkles,
                                        modifier = Modifier.size(17.dp),
                                        tint = if (isSelected) contentColor else contentColor.copy(alpha = 0.55f)
                                    )
                                    Column {
                                        Text(
                                            text = id,
                                            color = contentColor,
                                            style = MaterialTheme.typography.bodyMedium.copy(
                                                fontFamily = FontFamily.Monospace,
                                                fontWeight = if (isSelected) FontWeight.ExtraBold else FontWeight.SemiBold,
                                                fontSize = 14.sp
                                            )
                                        )
                                        Text(
                                            text = "$display · $meta",
                                            color = contentColor.copy(alpha = 0.62f),
                                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 11.sp)
                                        )
                                    }
                                }
                                if (isSelected) {
                                    SeaGlassIcon(
                                        glyph = SeaGlassGlyph.Check,
                                        modifier = Modifier.size(17.dp),
                                        tint = contentColor
                                    )
                                }
                            }
                            if (idx < modelCatalog.lastIndex) {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(horizontal = 18.dp)
                                        .height(0.7.dp)
                                        .background(contentColor.copy(alpha = 0.10f))
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}
