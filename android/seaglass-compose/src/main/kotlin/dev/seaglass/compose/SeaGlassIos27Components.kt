package dev.seaglass.compose

import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.SizeTransform
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.animateDpAsState
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
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectHorizontalDragGestures
import androidx.compose.foundation.gestures.detectVerticalDragGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeMorph
import kotlinx.coroutines.launch
import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sin

// =============================================================================================
// 1. iOS 27 Optical Magnifier Loupe & Scrubbing Bar (`SeaGlassMagnifierScrubber`)
// =============================================================================================

/**
 * iOS 27 Optical Magnifier Loupe & Precision Scrubbing Bar (`SeaGlassMagnifierScrubber`).
 *
 * - **At Rest**: Sleek sand-frosted glass scrubbing track with precision tick marks and a
 *   rested glass thumb.
 * - **While Scrubbing (or when [loupeActive] is toggled)**: A crystal-clear, temporally
 *   **de-aged (`deaged(1.0f)`) magnifying glass loupe droplet** rises above the thumb across a
 *   live 2-node `sminCubic` liquid bridge, magnifying the active token/frame/value at `1.32×`
 *   before sucking back into the track and re-crystallizing its sand-frosted finish on release!
 */
@Composable
fun SeaGlassMagnifierScrubber(
    value: Float,
    onValueChange: (Float) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    label: String = "OPTICAL TIMELINE & LOUPE SCRUBBER",
    valueFormatter: (Float) -> String = { v ->
        val frame = (v * 240).roundToInt()
        String.format(java.util.Locale.US, "Frame %03d · %.0f%%", frame, v * 100f)
    },
    trackMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    loupeMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val density = LocalDensity.current
    var isDragging by remember { mutableStateOf(false) }
    var previewPinned by remember { mutableStateOf(true) }
    val showLoupe = isDragging || previewPinned

    val rawLoupeLiftProgress by animateFloatAsState(
        targetValue = if (showLoupe) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 420f),
        label = "ios27LoupeLift"
    )
    val loupeLiftProgress = SeaGlassShapeMorph.physicsSCurve(rawLoupeLiftProgress, overshootGain = 0.45f)

    var containerWidthPx by remember { mutableFloatStateOf(900f) }
    val totalHeightDp = 112.dp
    val totalHeightPx = with(density) { totalHeightDp.toPx() }
    val trackHeightPx = with(density) { 42.dp.toPx() }
    val trackCenterY = totalHeightPx - trackHeightPx * 0.5f - with(density) { 4.dp.toPx() }

    val thumbPaddingPx = with(density) { 48.dp.toPx() }
    val usableTrackPx = (containerWidthPx - thumbPaddingPx * 2f).coerceAtLeast(1f)
    val thumbX = thumbPaddingPx + usableTrackPx * value.coerceIn(0f, 1f)

    // Loupe rises out of the track via a 2-node sminCubic liquid bridge!
    val loupeHalfW = with(density) { 68.dp.toPx() } * (0.45f + 0.55f * loupeLiftProgress)
    val loupeHalfH = with(density) { 24.dp.toPx() } * (0.45f + 0.55f * loupeLiftProgress)
    val loupeCenterY = trackCenterY - with(density) { 52.dp.toPx() } * loupeLiftProgress

    val coalescingNodes = remember(
        containerWidthPx,
        totalHeightPx,
        trackCenterY,
        trackHeightPx,
        thumbX,
        loupeCenterY,
        loupeHalfW,
        loupeHalfH,
        loupeLiftProgress,
        trackMaterial,
        loupeMaterial
    ) {
        val trackNode = SeaGlassSdfEngine.SdfNode(
            centerX = containerWidthPx * 0.5f,
            centerY = trackCenterY,
            halfWidth = containerWidthPx * 0.5f,
            halfHeight = trackHeightPx * 0.5f,
            uniformCornerRadius = trackHeightPx * 0.5f,
            cornerSmoothing = 2.4f,
            tintColorArgb = trackMaterial.tintColorArgb,
            thicknessPx = trackHeightPx * 0.48f
        )
        if (loupeLiftProgress > 0.03f) {
            val clampedLoupeX = thumbX.coerceIn(loupeHalfW + 8f, (containerWidthPx - loupeHalfW - 8f).coerceAtLeast(loupeHalfW + 8f))
            val loupeNode = SeaGlassSdfEngine.SdfNode(
                centerX = clampedLoupeX,
                centerY = loupeCenterY,
                halfWidth = loupeHalfW,
                halfHeight = loupeHalfH,
                uniformCornerRadius = loupeHalfH,
                cornerSmoothing = 2.4f,
                tintColorArgb = loupeMaterial.tintColorArgb,
                thicknessPx = loupeHalfH * 0.85f
            )
            listOf(trackNode, loupeNode)
        } else {
            listOf(trackNode)
        }
    }

    // Temporal De-Aging: while the loupe is raised or scrubbing, the glass clarifies so the
    // loupe acts as a crystal-clear optical magnifier!
    val activeMaterial = remember(trackMaterial, loupeLiftProgress, isDragging) {
        val malleability = if (isDragging) 0.92f else (loupeLiftProgress * 0.65f)
        trackMaterial.deaged(malleability)
    }

    val contentColor = remember(trackMaterial.tintColorArgb, trackMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                trackMaterial.tintColorArgb,
                trackMaterial.tintDensity
            )
        )
    }

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = label,
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.ExtraBold,
                    letterSpacing = 1.1.sp,
                    fontSize = 10.sp
                ),
                color = contentColor.copy(alpha = 0.78f)
            )
            Text(
                text = if (showLoupe) "Loupe Active · Drag Track ⇄" else "Tap or Drag to Raise Loupe",
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.Bold,
                    fontSize = 10.sp
                ),
                color = contentColor.copy(alpha = 0.72f),
                modifier = Modifier.clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = { previewPinned = !previewPinned }
                )
            )
        }

        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(totalHeightDp)
                .onSizeChanged { containerWidthPx = it.width.toFloat().coerceAtLeast(240f) }
                .pointerInput(Unit) {
                    detectHorizontalDragGestures(
                        onDragStart = { offset ->
                            isDragging = true
                            val next = ((offset.x - thumbPaddingPx) / usableTrackPx).coerceIn(0f, 1f)
                            onValueChange(next)
                        },
                        onDragEnd = { isDragging = false },
                        onDragCancel = { isDragging = false },
                        onHorizontalDrag = { change, _ ->
                            change.consume()
                            isDragging = true
                            val next = ((change.position.x - thumbPaddingPx) / usableTrackPx).coerceIn(0f, 1f)
                            onValueChange(next)
                        }
                    )
                }
                .seaGlass(
                    stageState = stageState,
                    material = activeMaterial,
                    nodesOptional = coalescingNodes,
                    smoothRadiusDp = 28f,
                    contentRole = SeaGlassContentRole.PURE_OPTICAL_LENS
                )
        ) {
            // 1. Raised Magnifier Loupe Readout (1.28x optical zoom inside the de-aged droplet)
            if (loupeLiftProgress > 0.08f) {
                val clampedLoupeX = thumbX.coerceIn(
                    loupeHalfW + 8f,
                    (containerWidthPx - loupeHalfW - 8f).coerceAtLeast(loupeHalfW + 8f)
                )
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .offset {
                            IntOffset(
                                (clampedLoupeX - loupeHalfW).roundToInt(),
                                (loupeCenterY - loupeHalfH).roundToInt()
                            )
                        }
                        .size(
                            width = with(density) { (loupeHalfW * 2f).toDp() },
                            height = with(density) { (loupeHalfH * 2f).toDp() }
                        )
                        .graphicsLayer {
                            alpha = loupeLiftProgress.coerceIn(0f, 1f)
                            val zoom = 0.85f + 0.22f * loupeLiftProgress
                            scaleX = zoom
                            scaleY = zoom
                        }
                ) {
                    Text(
                        text = valueFormatter(value),
                        maxLines = 1,
                        color = contentColor,
                        style = MaterialTheme.typography.labelMedium.copy(
                            fontFamily = FontFamily.Monospace,
                            fontWeight = FontWeight.Black,
                            fontSize = 11.5.sp
                        )
                    )
                }
            }

            // 2. Precision Scrubber Track Ticks & Active Thumb
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween,
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .fillMaxWidth()
                    .height(46.dp)
                    .padding(horizontal = 20.dp)
            ) {
                for (tick in 0..20) {
                    val tickFrac = tick / 20f
                    val distToThumb = abs(tickFrac - value)
                    val proximityBoost = (1f - distToThumb * 5f).coerceIn(0f, 1f)
                    val tickHeight = (if (tick % 5 == 0) 14.dp else 8.dp) + (8.dp * proximityBoost)
                    Box(
                        modifier = Modifier
                            .width(if (proximityBoost > 0.5f) 3.dp else 1.8.dp)
                            .height(tickHeight)
                            .clip(CircleShape)
                            .background(
                                contentColor.copy(
                                    alpha = (0.28f + 0.68f * proximityBoost).coerceIn(0.25f, 0.96f)
                                )
                            )
                    )
                }
            }
        }
    }
}

// =============================================================================================
// 2. iOS 27 Concentric Notification Rollup Stack (`SeaGlassNotificationStack`)
// =============================================================================================

@Immutable
data class SeaGlassNotificationItem(
    val id: String,
    val appName: String,
    val timeAgo: String,
    val title: String,
    val body: String,
    val glyph: SeaGlassGlyph,
    val badgeText: String? = null
)

/**
 * iOS 27 Concentric Notification Rollup Stack (`SeaGlassNotificationStack`).
 *
 * - **Collapsed (Rollup Mode)**: Stacks notifications with 3D perspective depth (`100%`, `93%`,
 *   `86%` width and progressive vertical offset), concentric bottom device-corner matching, and
 *   a `"3 Notifications · Tap to Fan Out"` summary pill.
 * - **Expanded (Fanned-Out Mode)**: Fans out all notifications with staggered spatial springs
 *   and **Temporal De-Aging** (`deaged` while fanning out, solidifying sand-frosted finish at rest).
 */
@Composable
fun SeaGlassNotificationStack(
    expanded: Boolean,
    onExpandedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    items: List<SeaGlassNotificationItem> = defaultIos27Notifications(),
    primaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    surfaceMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val rawExpandProgress by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.72f, stiffness = 420f),
        label = "ios27NotifStackExpand"
    )
    val expandProgress = SeaGlassShapeMorph.physicsSCurve(rawExpandProgress, overshootGain = 0.40f)
    val inFlightMalleability = SeaGlassShapeMorph.sCurveBell(rawExpandProgress)

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        // Header Bar with Rollup / Collapse Toggle Pill
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 4.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "IOS 27 CONCENTRIC NOTIFICATION STACK",
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.ExtraBold,
                    letterSpacing = 1.1.sp,
                    fontSize = 10.sp
                ),
                color = LocalContentColor.current.copy(alpha = 0.78f)
            )
            SeaGlassBadge(
                text = if (expanded) "Collapse Stack ↑" else "${items.size} Stacked ↓",
                stageState = stageState,
                material = primaryMaterial,
                modifier = Modifier.clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = { onExpandedChange(!expanded) }
                )
            )
        }

        AnimatedContent(
            targetState = expanded,
            transitionSpec = {
                (fadeIn(animationSpec = tween(200, easing = SeaGlassPhysicsSCurveEasing)) +
                    scaleIn(initialScale = 0.94f, animationSpec = motion.subPageSpatial))
                    .togetherWith(
                        fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing)) +
                            scaleOut(targetScale = 0.96f, animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
                    )
                    .using(SizeTransform(clip = false) { _, _ -> motion.subPageSpatialSpec() })
            },
            label = "ios27NotifStackMorph"
        ) { isExpanded ->
            if (!isExpanded) {
                // Rolled-up 3D Perspective Stack (back cards peek out underneath the front card)
                Box(
                    contentAlignment = Alignment.TopCenter,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(126.dp)
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onExpandedChange(true) }
                        )
                ) {
                    items.take(3).asReversed().forEachIndexed { revIdx, item ->
                        val depthIndex = (items.take(3).size - 1) - revIdx // 0 = front, 1 = mid, 2 = back
                        val cardScale = 1f - depthIndex * 0.065f * (1f - expandProgress.coerceIn(0f, 1f))
                        val topOffsetDp = (depthIndex * 14).dp

                        Box(
                            modifier = Modifier
                                .padding(top = topOffsetDp)
                                .fillMaxWidth()
                                .graphicsLayer {
                                    scaleX = cardScale
                                    scaleY = cardScale
                                    alpha = (1f - depthIndex * 0.22f).coerceIn(0.4f, 1f)
                                }
                                .zIndex((10 - depthIndex).toFloat())
                        ) {
                            Ios27NotificationCard(
                                item = item,
                                stageState = stageState,
                                material = surfaceMaterial.copy(
                                    cornerRadiusDp = 28f,
                                    thicknessDp = (18f - depthIndex * 3f).coerceAtLeast(10f)
                                ).deaged(inFlightMalleability),
                                onClick = { onExpandedChange(true) }
                            )
                        }
                    }
                }
            } else {
                // Fanned-out Interactive Notification Cards
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items.forEachIndexed { index, item ->
                        val cardStagger = SeaGlassShapeMorph.sCurveWindow(
                            progress = rawExpandProgress,
                            start = index * 0.06f,
                            end = 1f,
                            overshootGain = 0.35f
                        )
                        Box(
                            modifier = Modifier.graphicsLayer {
                                val s = 0.94f + 0.06f * cardStagger
                                scaleX = s
                                scaleY = s
                            }
                        ) {
                            Ios27NotificationCard(
                                item = item,
                                stageState = stageState,
                                material = surfaceMaterial.copy(
                                    cornerRadiusDp = 28f,
                                    thicknessDp = 18f
                                ).deaged(inFlightMalleability),
                                onClick = { if (index == 0) onExpandedChange(false) }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun Ios27NotificationCard(
    item: SeaGlassNotificationItem,
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    onClick: () -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material,
        cornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
        cornerMask = SeaGlassCornerMask.BOTTOM_ONLY,
        cornerProximityThresholdDp = 48f,
        allowAsymmetricCorners = false,
        onClick = onClick,
        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 13.dp),
        modifier = Modifier.fillMaxWidth()
    ) {
        val contentColor = LocalContentColor.current
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(40.dp)
                    .clip(RoundedCornerShape(13.dp))
                    .background(contentColor.copy(alpha = 0.12f))
            ) {
                SeaGlassIcon(
                    glyph = item.glyph,
                    modifier = Modifier.size(20.dp),
                    tint = contentColor
                )
            }

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(2.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = item.appName.uppercase(java.util.Locale.US),
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = FontWeight.ExtraBold,
                            letterSpacing = 0.8.sp,
                            fontSize = 10.sp
                        ),
                        color = contentColor.copy(alpha = 0.65f)
                    )
                    Text(
                        text = item.timeAgo,
                        style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.5.sp),
                        color = contentColor.copy(alpha = 0.58f)
                    )
                }
                Text(
                    text = item.title,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 14.sp
                    ),
                    color = contentColor
                )
                Text(
                    text = item.body,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp),
                    color = contentColor.copy(alpha = 0.80f)
                )
            }
        }
    }
}

fun defaultIos27Notifications(): List<SeaGlassNotificationItem> = listOf(
    SeaGlassNotificationItem(
        id = "n1",
        appName = "OpenCode · Argon EAP",
        timeAgo = "now",
        title = "Shader Compile & UBO Sync Complete",
        body = "Compiled AGSL 1.0, Vulkan 450 & GLES 3.00 with 0 dirty slots.",
        glyph = SeaGlassGlyph.Sparkles
    ),
    SeaGlassNotificationItem(
        id = "n2",
        appName = "SeaGlass Studio",
        timeAgo = "2m ago",
        title = "Concentric Bezel Optics Locked",
        body = "Matched bottom display corners at 8dp inset (47dp radius).",
        glyph = SeaGlassGlyph.Layers
    ),
    SeaGlassNotificationItem(
        id = "n3",
        appName = "Temporal Physics",
        timeAgo = "5m ago",
        title = "De-Aging & Solidification Calibrated",
        body = "Coarseness melts in flight and re-crystallizes on settle.",
        glyph = SeaGlassGlyph.Tune
    )
)

// =============================================================================================
// 3. iOS 27 Cylindrical Refractive Glass Slot / Drum Picker (`SeaGlassWheelPicker`)
// =============================================================================================

/**
 * iOS 27 3D Cylindrical Refractive Glass Drum-Wheel Picker (`SeaGlassWheelPicker`).
 *
 * Renders a 3D cylindrical drum of options where a central **de-aged, high-IOR refractive
 * SeaGlass Magnifier Bar** magnifies the selected item (`1.12×`) while upper and lower drum
 * rows curve away in 3D perspective (`rotationX`, `scale`, `alpha`). Supports vertical drag
 * spinning and tap-to-select!
 */
@Composable
fun SeaGlassWheelPicker(
    items: List<String>,
    selectedIndex: Int,
    onSelectIndex: (Int) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    title: String = "IOS 27 REFRACTIVE DRUM-WHEEL PICKER",
    containerMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    lensMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val count = items.size.coerceAtLeast(1)
    val rawAnimatedIndex by animateFloatAsState(
        targetValue = selectedIndex.coerceIn(0, count - 1).toFloat(),
        animationSpec = spring(dampingRatio = 0.76f, stiffness = 400f),
        label = "ios27WheelIndex"
    )
    val baseWheelIdx = floor(rawAnimatedIndex)
    val wheelFrac = rawAnimatedIndex - baseWheelIdx
    val animatedIndex = baseWheelIdx + SeaGlassShapeMorph.physicsSCurve(wheelFrac, overshootGain = 0.35f)
    var dragAccumulatorPx by remember { mutableFloatStateOf(0f) }
    val spinMalleability = SeaGlassShapeMorph.sCurve(abs(selectedIndex - animatedIndex))

    val activeContainerMaterial = remember(containerMaterial, spinMalleability) {
        containerMaterial.copy(cornerRadiusDp = 28f, thicknessDp = 18f).deaged(spinMalleability * 0.65f)
    }
    val activeLensMaterial = remember(lensMaterial, spinMalleability) {
        lensMaterial.copy(cornerRadiusDp = 18f, thicknessDp = 20f).deaged(0.55f + 0.45f * spinMalleability)
    }

    SeaGlassSurface(
        stageState = stageState,
        material = activeContainerMaterial,
        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
        modifier = modifier
            .fillMaxWidth()
            .pointerInput(count, selectedIndex) {
                detectVerticalDragGestures(
                    onDragStart = { dragAccumulatorPx = 0f },
                    onVerticalDrag = { change, dragAmount ->
                        change.consume()
                        dragAccumulatorPx += dragAmount
                        val stepPx = 56f
                        if (abs(dragAccumulatorPx) >= stepPx) {
                            val steps = (dragAccumulatorPx / stepPx).toInt()
                            dragAccumulatorPx -= steps * stepPx
                            val next = (selectedIndex - steps).coerceIn(0, count - 1)
                            if (next != selectedIndex) {
                                onSelectIndex(next)
                            }
                        }
                    }
                )
            }
    ) {
        val contentColor = LocalContentColor.current
        Column(
            modifier = Modifier.fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.1.sp,
                        fontSize = 10.sp
                    ),
                    color = contentColor.copy(alpha = 0.76f)
                )
                Text(
                    text = "Drag or Tap Drum ↕",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.Bold,
                        fontSize = 10.sp
                    ),
                    color = contentColor.copy(alpha = 0.65f)
                )
            }

            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(148.dp)
            ) {
                // Center De-Aged High-IOR Magnifier Lens Bar
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(42.dp)
                        .seaGlass(
                            stageState = stageState,
                            material = activeLensMaterial,
                            contentRole = SeaGlassContentRole.PURE_OPTICAL_LENS
                        )
                )

                // 3D Cylindrical Drum Rows (-2 .. +2 around animatedIndex)
                items.forEachIndexed { idx, label ->
                    val offsetFromCenter = idx - animatedIndex
                    if (abs(offsetFromCenter) <= 2.3f) {
                        val angleRad = (offsetFromCenter * 0.48f).coerceIn(-1.25f, 1.25f)
                        val sinA = sin(angleRad)
                        val cosA = cos(angleRad).coerceAtLeast(0.15f)
                        val yOffsetDp = (sinA * 56f).dp
                        val isCenter = abs(offsetFromCenter) < 0.45f
                        val scale = if (isCenter) 1.08f else (0.78f + 0.22f * cosA)

                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.Center,
                            modifier = Modifier
                                .fillMaxWidth()
                                .offset(y = yOffsetDp)
                                .graphicsLayer {
                                    rotationX = -offsetFromCenter * 24f
                                    scaleX = scale
                                    scaleY = scale
                                    alpha = (cosA * cosA).coerceIn(0.22f, 1f)
                                    cameraDistance = 14f * density
                                }
                                .clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = { onSelectIndex(idx) }
                                )
                                .padding(vertical = 6.dp, horizontal = 14.dp)
                        ) {
                            if (isCenter) {
                                SeaGlassIcon(
                                    glyph = SeaGlassGlyph.Sparkles,
                                    modifier = Modifier.size(15.dp),
                                    tint = contentColor
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                            }
                            Text(
                                text = label,
                                maxLines = 1,
                                color = contentColor,
                                style = MaterialTheme.typography.titleSmall.copy(
                                    fontWeight = if (isCenter) FontWeight.Black else FontWeight.SemiBold,
                                    fontSize = if (isCenter) 15.5.sp else 13.5.sp
                                )
                            )
                        }
                    }
                }
            }
        }
    }
}

// =============================================================================================
// 4. iOS 27 Control Center Morphing Tile Grid & Vertical Pill Sliders (`SeaGlassControlCenterGrid`)
// =============================================================================================

/**
 * iOS 27 Control Center Modular Glass Grid (`SeaGlassControlCenterGrid`):
 * - **Left**: 2×2 Expandable Control Platter that morphs between compact toggle tiles and a
 *   detailed optical control card with lead-lag spatial springs and **Temporal De-Aging**.
 * - **Right**: Twin iOS 27 Vertical Liquid Capsule Sliders (`Frost` & `Refract`) that fill
 *   vertically, squish/stretch on over-pull, and de-age into clear glass while dragged!
 */
@Composable
fun SeaGlassControlCenterGrid(
    frostFraction: Float,
    onFrostFractionChange: (Float) -> Unit,
    refractFraction: Float,
    onRefractFractionChange: (Float) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    primaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    surfaceMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    tertiaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass
) {
    var expandedTileIndex by remember { mutableIntStateOf(-1) }
    var tile0Active by remember { mutableStateOf(true) }
    var tile1Active by remember { mutableStateOf(true) }
    var tile2Active by remember { mutableStateOf(false) }
    var tile3Active by remember { mutableStateOf(true) }

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text(
            text = "IOS 27 CONTROL CENTER · MORPHING TILES & VERTICAL PILLS",
            style = MaterialTheme.typography.labelSmall.copy(
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 1.1.sp,
                fontSize = 10.sp
            ),
            color = LocalContentColor.current.copy(alpha = 0.78f),
            modifier = Modifier.padding(horizontal = 4.dp)
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Left: 2x2 Modular Control Platter (tapping any tile morphs or toggles)
            val motion = LocalSeaGlassMotionScheme.current
            Box(modifier = Modifier.weight(1f)) {
                AnimatedContent(
                    targetState = expandedTileIndex,
                    transitionSpec = {
                        (fadeIn(animationSpec = tween(200, easing = SeaGlassPhysicsSCurveEasing)) +
                            scaleIn(
                                initialScale = 0.90f,
                                animationSpec = motion.subPageSpatial
                            )).togetherWith(
                            fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing)) +
                                scaleOut(targetScale = 0.94f, animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
                        ).using(
                            SizeTransform(clip = true) { _, _ -> motion.subPageSpatialSpec() }
                        )
                    },
                    label = "controlCenterPlatterMorph"
                ) { expandedIdx ->
                    if (expandedIdx < 0) {
                        Column(
                            verticalArrangement = Arrangement.spacedBy(10.dp),
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Row(
                                horizontalArrangement = Arrangement.spacedBy(10.dp),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Ios27ControlTile(
                                    title = "De-Age",
                                    subtitle = if (tile0Active) "Active" else "Off",
                                    glyph = SeaGlassGlyph.Sparkles,
                                    active = tile0Active,
                                    onTap = { tile0Active = !tile0Active },
                                    onExpand = { expandedTileIndex = 0 },
                                    stageState = stageState,
                                    activeMaterial = primaryMaterial,
                                    inactiveMaterial = surfaceMaterial,
                                    modifier = Modifier.weight(1f)
                                )
                                Ios27ControlTile(
                                    title = "Bezel Sync",
                                    subtitle = if (tile1Active) "46dp" else "Fixed",
                                    glyph = SeaGlassGlyph.Layers,
                                    active = tile1Active,
                                    onTap = { tile1Active = !tile1Active },
                                    onExpand = { expandedTileIndex = 1 },
                                    stageState = stageState,
                                    activeMaterial = primaryMaterial,
                                    inactiveMaterial = surfaceMaterial,
                                    modifier = Modifier.weight(1f)
                                )
                            }
                            Row(
                                horizontalArrangement = Arrangement.spacedBy(10.dp),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Ios27ControlTile(
                                    title = "UBO Cache",
                                    subtitle = if (tile2Active) "Delta" else "Full",
                                    glyph = SeaGlassGlyph.Tune,
                                    active = tile2Active,
                                    onTap = { tile2Active = !tile2Active },
                                    onExpand = { expandedTileIndex = 2 },
                                    stageState = stageState,
                                    activeMaterial = tertiaryMaterial,
                                    inactiveMaterial = surfaceMaterial,
                                    modifier = Modifier.weight(1f)
                                )
                                Ios27ControlTile(
                                    title = "Spatial",
                                    subtitle = if (tile3Active) "120Hz" else "60Hz",
                                    glyph = SeaGlassGlyph.AudioWave,
                                    active = tile3Active,
                                    onTap = { expandedTileIndex = 3 },
                                    onExpand = { expandedTileIndex = 3 },
                                    stageState = stageState,
                                    activeMaterial = primaryMaterial,
                                    inactiveMaterial = surfaceMaterial,
                                    modifier = Modifier.weight(1f)
                                )
                            }
                        }
                    } else {
                        // Expanded 2x2 Control Platter Card (tap to morph back to 2x2 grid)
                        SeaGlassSurface(
                            stageState = stageState,
                            material = primaryMaterial.copy(cornerRadiusDp = 28f, thicknessDp = 20f),
                            onClick = { expandedTileIndex = -1 },
                            contentPadding = PaddingValues(16.dp),
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(156.dp)
                        ) {
                            Column(
                                verticalArrangement = Arrangement.SpaceBetween,
                                modifier = Modifier.fillMaxSize()
                            ) {
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    SeaGlassIcon(
                                        glyph = SeaGlassGlyph.Sparkles,
                                        modifier = Modifier.size(22.dp),
                                        tint = LocalContentColor.current
                                    )
                                    Text(
                                        text = "Tap to Collapse ↙",
                                        style = MaterialTheme.typography.labelSmall.copy(
                                            fontWeight = FontWeight.Bold,
                                            fontSize = 10.sp
                                        ),
                                        color = LocalContentColor.current.copy(alpha = 0.78f)
                                    )
                                }
                                Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                                    Text(
                                        text = "Expanded iOS 27 Platter",
                                        style = MaterialTheme.typography.titleMedium.copy(
                                            fontWeight = FontWeight.Black,
                                            fontSize = 16.sp
                                        ),
                                        color = LocalContentColor.current
                                    )
                                    Text(
                                        text = "De-aged in flight while morphing from 1×1 tile to 2×2 platter, then solidified at rest.",
                                        style = MaterialTheme.typography.bodySmall.copy(
                                            fontSize = 11.5.sp,
                                            lineHeight = 15.sp
                                        ),
                                        color = LocalContentColor.current.copy(alpha = 0.85f)
                                    )
                                }
                            }
                        }
                    }
                }
            }

            // Right: Twin iOS 27 Vertical Liquid Capsule Sliders (Frost & Refract)
            Ios27VerticalPillSlider(
                label = "Frost",
                glyph = SeaGlassGlyph.Sparkles,
                fraction = frostFraction,
                onFractionChange = onFrostFractionChange,
                stageState = stageState,
                trackMaterial = surfaceMaterial,
                fillMaterial = primaryMaterial
            )

            Ios27VerticalPillSlider(
                label = "IOR",
                glyph = SeaGlassGlyph.Layers,
                fraction = refractFraction,
                onFractionChange = onRefractFractionChange,
                stageState = stageState,
                trackMaterial = surfaceMaterial,
                fillMaterial = tertiaryMaterial
            )
        }
    }
}

@Composable
private fun Ios27ControlTile(
    title: String,
    subtitle: String,
    glyph: SeaGlassGlyph,
    active: Boolean,
    onTap: () -> Unit,
    onExpand: () -> Unit,
    stageState: SeaGlassStageState?,
    activeMaterial: SeaGlassMaterial,
    inactiveMaterial: SeaGlassMaterial,
    modifier: Modifier = Modifier
) {
    val mat = if (active) {
        activeMaterial.copy(cornerRadiusDp = 24f, thicknessDp = 18f)
    } else {
        inactiveMaterial.copy(cornerRadiusDp = 22f, thicknessDp = 13f)
    }
    SeaGlassSurface(
        stageState = stageState,
        material = mat,
        onClick = onTap,
        contentPadding = PaddingValues(horizontal = 12.dp, vertical = 11.dp),
        modifier = modifier.height(73.dp)
    ) {
        val contentColor = LocalContentColor.current
        Column(
            verticalArrangement = Arrangement.SpaceBetween,
            modifier = Modifier.fillMaxSize()
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                SeaGlassIcon(
                    glyph = glyph,
                    modifier = Modifier.size(18.dp),
                    tint = contentColor
                )
                Text(
                    text = "↗",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 11.sp
                    ),
                    color = contentColor.copy(alpha = 0.62f),
                    modifier = Modifier.clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onExpand
                    )
                )
            }
            Column {
                Text(
                    text = title,
                    maxLines = 1,
                    style = MaterialTheme.typography.labelMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 12.sp
                    ),
                    color = contentColor
                )
                Text(
                    text = subtitle,
                    maxLines = 1,
                    style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp),
                    color = contentColor.copy(alpha = 0.72f)
                )
            }
        }
    }
}

@Composable
private fun Ios27VerticalPillSlider(
    label: String,
    glyph: SeaGlassGlyph,
    fraction: Float,
    onFractionChange: (Float) -> Unit,
    stageState: SeaGlassStageState?,
    trackMaterial: SeaGlassMaterial,
    fillMaterial: SeaGlassMaterial,
    modifier: Modifier = Modifier
) {
    var isDragging by remember { mutableStateOf(false) }
    var pillHeightPx by remember { mutableFloatStateOf(420f) }

    val rawDragStretch by animateFloatAsState(
        targetValue = if (isDragging) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 480f),
        label = "verticalPillStretch"
    )
    val dragStretch = SeaGlassShapeMorph.physicsSCurve(rawDragStretch, overshootGain = 0.40f)
    val rawAnimatedFill by animateFloatAsState(
        targetValue = fraction.coerceIn(0.08f, 1f),
        animationSpec = spring(dampingRatio = 0.76f, stiffness = 440f),
        label = "verticalPillFill"
    )
    val animatedFill = SeaGlassShapeMorph.physicsSCurve(rawAnimatedFill, overshootGain = 0.25f).coerceIn(0.08f, 1f)

    val activeTrackMat = remember(trackMaterial, dragStretch) {
        trackMaterial.copy(cornerRadiusDp = 28f, thicknessDp = 18f).deaged(dragStretch * 0.90f)
    }
    val contentColor = remember(trackMaterial.tintColorArgb, trackMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                trackMaterial.tintColorArgb,
                trackMaterial.tintDensity
            )
        )
    }

    val fillTop = Color(
        SeaGlassColorMath.lerpOklab(fillMaterial.tintColorArgb, 0xFFFFFFFF.toInt(), 0.28f)
    ).copy(alpha = 0.76f)
    val fillBottom = Color(fillMaterial.tintColorArgb).copy(alpha = 0.62f)

    Box(
        contentAlignment = Alignment.BottomCenter,
        modifier = modifier
            .width(58.dp)
            .height(156.dp)
            .onSizeChanged { pillHeightPx = it.height.toFloat().coerceAtLeast(1f) }
            .graphicsLayer {
                scaleX = 1f - 0.04f * dragStretch
                scaleY = 1f + 0.05f * dragStretch
            }
            .pointerInput(Unit) {
                detectVerticalDragGestures(
                    onDragStart = { offset ->
                        isDragging = true
                        val next = (1f - (offset.y / pillHeightPx)).coerceIn(0f, 1f)
                        onFractionChange(next)
                    },
                    onDragEnd = { isDragging = false },
                    onDragCancel = { isDragging = false },
                    onVerticalDrag = { change, _ ->
                        change.consume()
                        isDragging = true
                        val next = (1f - (change.position.y / pillHeightPx)).coerceIn(0f, 1f)
                        onFractionChange(next)
                    }
                )
            }
            .seaGlass(
                stageState = stageState,
                material = activeTrackMat,
                contentRole = SeaGlassContentRole.NAVIGATION_CHROME
            )
            .clip(RoundedCornerShape(28.dp))
    ) {
        // Vertical Liquid Fill Column
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .fillMaxHeight(animatedFill)
                .background(Brush.verticalGradient(listOf(fillTop, fillBottom)))
        )

        // Bottom Glyph + Percentage Readout
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(2.dp),
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .padding(bottom = 12.dp)
        ) {
            SeaGlassIcon(
                glyph = glyph,
                modifier = Modifier.size(18.dp),
                tint = contentColor
            )
            Text(
                text = label,
                maxLines = 1,
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 10.sp
                ),
                color = contentColor
            )
            Text(
                text = "${(fraction * 100).roundToInt()}%",
                maxLines = 1,
                style = MaterialTheme.typography.labelSmall.copy(
                    fontFamily = FontFamily.Monospace,
                    fontWeight = FontWeight.Bold,
                    fontSize = 9.sp
                ),
                color = contentColor.copy(alpha = 0.78f)
            )
        }
    }
}
