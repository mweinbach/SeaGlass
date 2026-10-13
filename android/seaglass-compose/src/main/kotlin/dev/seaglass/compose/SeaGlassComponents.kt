package dev.seaglass.compose

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
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
import androidx.compose.material3.LocalTextStyle
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ProvideTextStyle
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.Shadow
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.PointerEventPass
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassContourPathBuilder
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeMorph
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * Provides [LocalContentColor] for crisp, tonal glass-engraved typography and iconography on SeaGlass surfaces.
 */
@Composable
fun ProvideSeaGlassLegibleContent(
    contentColor: Color,
    content: @Composable () -> Unit
) {
    val insideEtched = LocalInsideSeaGlassEtched.current
    if (insideEtched) {
        CompositionLocalProvider(
            LocalContentColor provides contentColor,
            content = content
        )
    } else {
        val isLightInk = (contentColor.red * 0.299f + contentColor.green * 0.587f + contentColor.blue * 0.114f) > 0.55f
        val etchShadow = remember(isLightInk) {
            if (isLightInk) {
                Shadow(
                    color = Color(0x90040C18),
                    offset = Offset(-0.7f, -1.6f),
                    blurRadius = 0.1f
                )
            } else {
                Shadow(
                    color = Color(0xA8FFFFFF),
                    offset = Offset(0.75f, 1.85f),
                    blurRadius = 0.1f
                )
            }
        }
        val currentStyle = LocalTextStyle.current
        val etchedStyle = remember(currentStyle, etchShadow) {
            currentStyle.copy(shadow = etchShadow)
        }
        CompositionLocalProvider(LocalContentColor provides contentColor) {
            ProvideTextStyle(value = etchedStyle, content = content)
        }
    }
}

/**
 * Non-consuming pointer observer that tracks the live 2D touch point `(touchX, touchY)`,
 * press state, and whether the pointer has dragged past touch slop (`> 6dp`) so surfaces
 * stay flat on simple taps and only expand/stretch when tapped and dragged (or for interactive
 * controls), snapping back with a spatial spring on release.
 */
private fun Modifier.observeSeaGlassTouch(
    onTouchUpdate: (pos: Offset?, down: Boolean, dragging: Boolean) -> Unit
): Modifier = this.pointerInput(Unit) {
    val dragThresholdPx = 6.dp.toPx()
    awaitPointerEventScope {
        var downOrigin: Offset? = null
        var isDragging = false
        while (true) {
            val event = awaitPointerEvent(PointerEventPass.Initial)
            val pressedChange = event.changes.firstOrNull { it.pressed }
            if (pressedChange != null) {
                val pos = pressedChange.position
                val origin = downOrigin ?: pos.also { downOrigin = it }
                if (!isDragging && (pos - origin).getDistance() >= dragThresholdPx) {
                    isDragging = true
                }
                onTouchUpdate(pos, true, isDragging)
            } else {
                downOrigin = null
                isDragging = false
                onTouchUpdate(null, false, false)
            }
        }
    }
}

/**
 * Foundational SeaGlass container surface with Material 3 Expressive state morphing,
 * uniform press/drag expansion that snaps back on release, and automatic
 * Oklab contrast-safe foreground content color. Never applies 3D perspective tilt.
 */
@Composable
fun SeaGlassSurface(
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    onClick: (() -> Unit)? = null,
    contentPadding: PaddingValues = PaddingValues(0.dp),
    contentAlignment: Alignment = Alignment.TopStart,
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = LocalSeaGlassCornerAdaptationConfig.current.mode,
    cornerMask: SeaGlassCornerMask = LocalSeaGlassCornerAdaptationConfig.current.corners,
    cornerProximityThresholdDp: Float = LocalSeaGlassCornerAdaptationConfig.current.proximityThresholdDp,
    cornerFullMatchDistanceDp: Float = LocalSeaGlassCornerAdaptationConfig.current.fullMatchDistanceDp,
    cornerAdaptationStrength: Float = LocalSeaGlassCornerAdaptationConfig.current.strength,
    allowAsymmetricCorners: Boolean = LocalSeaGlassCornerAdaptationConfig.current.allowAsymmetricCorners,
    adaptToParentContainer: Boolean = LocalSeaGlassCornerAdaptationConfig.current.adaptToParentContainer,
    content: @Composable BoxScope.() -> Unit
) {
    val context = LocalContext.current
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current
    val deviceCornerSpec = LocalSeaGlassDeviceCornerSpec.current
    val interactionSource = remember { MutableInteractionSource() }
    val isClickablePressed by interactionSource.collectIsPressedAsState()

    var surfaceOriginInRoot by remember { mutableStateOf(Offset.Zero) }
    var surfaceSize by remember { mutableStateOf(IntSize.Zero) }

    val isActiveInteraction = onClick != null && isClickablePressed
    val pressFraction = if (onClick != null) {
        val animated by animateFloatAsState(
            targetValue = if (isActiveInteraction) 1f else 0f,
            animationSpec = motion.fastSpatial,
            label = "surfacePress"
        )
        SeaGlassShapeMorph.physicsSCurve(animated, overshootGain = 0.35f)
    } else {
        0f
    }

    // Uniform 2D expansion on active button press that snaps back cleanly (zero 3D rotation).
    val expandScale = 1f + 0.024f * pressFraction

    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = material.tintColorArgb,
                tintDensity = material.tintDensity
            )
        )
    }

    val clickableModifier = if (onClick != null) {
        Modifier.clickable(
            interactionSource = interactionSource,
            indication = null,
            onClick = onClick
        )
    } else {
        Modifier
    }

    val needsCornerScope = cornerAdaptationMode == SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED || adaptToParentContainer

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            contentAlignment = contentAlignment,
            modifier = modifier
                .then(
                    if (needsCornerScope) {
                        Modifier.onGloballyPositioned { coords ->
                            surfaceOriginInRoot = coords.positionInRoot()
                            surfaceSize = coords.size
                        }
                    } else {
                        Modifier
                    }
                )
                .then(
                    if (expandScale != 1f) {
                        Modifier.graphicsLayer {
                            this.scaleX = expandScale
                            this.scaleY = expandScale
                        }
                    } else {
                        Modifier
                    }
                )
                .then(clickableModifier)
                .seaGlass(
                    stageState = stageState,
                    material = material,
                    contentRole = SeaGlassContentRole.TEXT_AND_ICONS,
                    cornerAdaptationMode = cornerAdaptationMode,
                    cornerMask = cornerMask,
                    cornerProximityThresholdDp = cornerProximityThresholdDp,
                    cornerFullMatchDistanceDp = cornerFullMatchDistanceDp,
                    cornerAdaptationStrength = cornerAdaptationStrength,
                    allowAsymmetricCorners = allowAsymmetricCorners,
                    adaptToParentContainer = adaptToParentContainer
                )
                .padding(contentPadding),
            content = content
        )
    }
}

/**
 * Tactile tumbled sea-glass button that expands uniformly on press or drag and snaps back on release,
 * with zero 3D perspective tilt.
 */
@Composable
fun SeaGlassButton(
    onClick: () -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    contentPadding: PaddingValues = PaddingValues(horizontal = 22.dp, vertical = 13.dp),
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.FIXED,
    cornerMask: SeaGlassCornerMask = SeaGlassCornerMask.NONE,
    cornerProximityThresholdDp: Float = SeaGlassCornerAdaptationConfig.DEFAULT_PROXIMITY_THRESHOLD_DP,
    content: @Composable RowScope.() -> Unit
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isClickablePressed by interactionSource.collectIsPressedAsState()

    var lastTouchOffset by remember { mutableStateOf<Offset?>(null) }
    var isPointerDown by remember { mutableStateOf(false) }

    val isPressed = isClickablePressed || isPointerDown
    val rawPressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "buttonPress"
    )
    val pressFraction = SeaGlassShapeMorph.physicsSCurve(rawPressFraction, overshootGain = 0.35f)

    val animatedMaterial by animateSeaGlassMaterialAsState(
        targetMaterial = material.pressed(pressFraction),
        motionScheme = motion
    )

    val expandScale = 1f + 0.035f * pressFraction

    val contentColor = remember(animatedMaterial.tintColorArgb, animatedMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = animatedMaterial.tintColorArgb,
                tintDensity = animatedMaterial.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        CompositionLocalProvider(LocalInsideSeaGlassEtched provides true) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center,
                modifier = modifier
                    .observeSeaGlassTouch { pos, down, _ ->
                        if (pos != null) lastTouchOffset = pos
                        isPointerDown = down
                    }
                    .graphicsLayer {
                        scaleX = expandScale
                        scaleY = expandScale
                    }
                    .clickable(
                        interactionSource = interactionSource,
                        indication = null,
                        onClick = onClick
                    )
                    .seaGlass(
                        stageState = stageState,
                        material = animatedMaterial,
                        touchPointInLocal = lastTouchOffset,
                        touchIntensity = pressFraction,
                        contentRole = SeaGlassContentRole.TEXT_AND_ICONS,
                        cornerAdaptationMode = cornerAdaptationMode,
                        cornerMask = cornerMask,
                        cornerProximityThresholdDp = cornerProximityThresholdDp
                    )
                    .seaGlassEtched(
                        tintColorArgb = animatedMaterial.tintColorArgb,
                        coarseness = animatedMaterial.coarseness
                    )
                    .padding(contentPadding),
                content = content
            )
        }
    }
}

/**
 * Expressive SeaGlass pill chip that smoothly morphs its tint, coarseness, corner radius,
 * and thickness between unselected and selected states.
 */
@Composable
fun SeaGlassPill(
    label: String,
    selected: Boolean,
    onClick: () -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    selectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    unselectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val targetMaterial = if (selected) {
        selectedMaterial.copy(cornerRadiusDp = 26f, thicknessDp = 16f)
    } else {
        unselectedMaterial.copy(cornerRadiusDp = 18f, thicknessDp = 10f)
    }

    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = targetMaterial,
        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 10.dp),
        modifier = modifier
    ) {
        Text(
            text = label,
            maxLines = 1,
            style = MaterialTheme.typography.labelLarge.copy(
                fontWeight = if (selected) FontWeight.Bold else FontWeight.Medium,
                letterSpacing = 0.2.sp
            ),
            color = LocalContentColor.current
        )
    }
}

/**
 * Expressive SeaGlass card with a tumbled squircle silhouette and adaptive frost depth.
 */
@Composable
fun SeaGlassCard(
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    onClick: (() -> Unit)? = null,
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = LocalSeaGlassCornerAdaptationConfig.current.mode,
    cornerMask: SeaGlassCornerMask = LocalSeaGlassCornerAdaptationConfig.current.corners,
    cornerProximityThresholdDp: Float = LocalSeaGlassCornerAdaptationConfig.current.proximityThresholdDp,
    allowAsymmetricCorners: Boolean = LocalSeaGlassCornerAdaptationConfig.current.allowAsymmetricCorners,
    content: @Composable ColumnScope.() -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material,
        onClick = onClick,
        contentPadding = PaddingValues(20.dp),
        cornerAdaptationMode = cornerAdaptationMode,
        cornerMask = cornerMask,
        cornerProximityThresholdDp = cornerProximityThresholdDp,
        allowAsymmetricCorners = allowAsymmetricCorners,
        modifier = modifier
    ) {
        Column(
            verticalArrangement = Arrangement.spacedBy(10.dp),
            content = content
        )
    }
}

/**
 * Floating Liquid SeaGlass Mini-Player Capsule modeled after the iOS 26 Apple Music Mini-Player.
 *
 * Features a full-capsule silhouette (`56.dp` tall) whose top and bottom `13.5dp` meniscus bevels
 * stretch and refract background headlines & artwork, paired with an album cover thumbnail,
 * track title/subtitle, and crisp vector Play (`▶`/`❚❚`) and Fast-Forward (`▶▶`) controls.
 */
@Composable
fun SeaGlassMiniPlayerBar(
    title: String,
    subtitle: String,
    isPlaying: Boolean,
    onTogglePlay: () -> Unit,
    onNextTrack: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val barHeight = 56.dp
    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = material.tintColorArgb,
                tintDensity = material.tintDensity
            )
        )
    }

    SeaGlassSurface(
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = 28f,
            thicknessDp = 18f,
            rimChalkiness = 0.88f
        ),
        onClick = onTogglePlay,
        contentPadding = PaddingValues(horizontal = 12.dp, vertical = 7.dp),
        contentAlignment = Alignment.CenterStart,
        modifier = modifier
            .fillMaxWidth()
            .height(barHeight)
    ) {
        Row(
            modifier = Modifier.fillMaxSize(),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Etched Sea-Glass Album Artwork Medallion (recessed intaglio well + etched vinyl disc)
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(40.dp)
                    .seaGlassBeveledInlay(
                        tintColorArgb = material.tintColorArgb,
                        coarseness = material.coarseness,
                        cornerRadiusDp = 12f
                    )
            ) {
                Canvas(modifier = Modifier.size(22.dp)) {
                    val isLightInk = (contentColor.red * 0.299f + contentColor.green * 0.587f + contentColor.blue * 0.114f) > 0.55f
                    val lipColor = if (isLightInk) Color(0xFF040C18).copy(alpha = 0.58f) else Color.White.copy(alpha = 0.85f)
                    val sw = 2.1.dp.toPx()
                    translate(left = 0.4.dp.toPx(), top = 1.0.dp.toPx()) {
                        drawCircle(
                            color = lipColor,
                            radius = size.minDimension * 0.42f,
                            style = Stroke(width = sw)
                        )
                        drawCircle(
                            color = lipColor,
                            radius = size.minDimension * 0.15f
                        )
                    }
                    drawCircle(
                        color = contentColor,
                        radius = size.minDimension * 0.42f,
                        style = Stroke(width = sw)
                    )
                    drawCircle(
                        color = contentColor,
                        radius = size.minDimension * 0.15f
                    )
                }
            }

            Spacer(modifier = Modifier.width(12.dp))

            // Track Title & Artist Subtitle (Etched Into Glass)
            Column(
                modifier = Modifier
                    .weight(1f)
                    .seaGlassEtched(
                        tintColorArgb = material.tintColorArgb,
                        coarseness = material.coarseness
                    ),
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = title,
                    color = contentColor,
                    maxLines = 1,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 14.sp,
                        letterSpacing = (-0.1).sp
                    )
                )
                Text(
                    text = subtitle,
                    color = contentColor.copy(alpha = 0.90f),
                    maxLines = 1,
                    style = MaterialTheme.typography.bodySmall.copy(
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 12.sp
                    )
                )
            }

            // Play / Pause Vector Button (Etched Into Glass)
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(38.dp)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onTogglePlay
                    )
            ) {
                Canvas(modifier = Modifier.size(18.dp)) {
                    val w = size.width
                    val h = size.height
                    val isLightInk = (contentColor.red * 0.299f + contentColor.green * 0.587f + contentColor.blue * 0.114f) > 0.55f
                    val lipColor = if (isLightInk) Color(0xFF040C18).copy(alpha = 0.62f) else Color.White.copy(alpha = 0.85f)
                    if (isPlaying) {
                        val r = CornerRadius(2.dp.toPx(), 2.dp.toPx())
                        translate(left = 0.45.dp.toPx(), top = 1.05.dp.toPx()) {
                            drawRoundRect(lipColor, Offset(w * 0.16f, h * 0.12f), Size(w * 0.24f, h * 0.76f), r)
                            drawRoundRect(lipColor, Offset(w * 0.60f, h * 0.12f), Size(w * 0.24f, h * 0.76f), r)
                        }
                        drawRoundRect(contentColor, Offset(w * 0.16f, h * 0.12f), Size(w * 0.24f, h * 0.76f), r)
                        drawRoundRect(contentColor, Offset(w * 0.60f, h * 0.12f), Size(w * 0.24f, h * 0.76f), r)
                    } else {
                        val playPath = Path().apply {
                            moveTo(w * 0.18f, h * 0.10f)
                            lineTo(w * 0.88f, h * 0.50f)
                            lineTo(w * 0.18f, h * 0.90f)
                            close()
                        }
                        translate(left = 0.45.dp.toPx(), top = 1.05.dp.toPx()) {
                            drawPath(playPath, color = lipColor)
                        }
                        drawPath(playPath, color = contentColor)
                    }
                }
            }

            // Fast-Forward (▶▶) Vector Button (Etched Into Glass)
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(40.dp)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onNextTrack
                    )
            ) {
                Canvas(modifier = Modifier.size(22.dp)) {
                    val w = size.width
                    val h = size.height
                    val isLightInk = (contentColor.red * 0.299f + contentColor.green * 0.587f + contentColor.blue * 0.114f) > 0.55f
                    val lipColor = if (isLightInk) Color(0xFF040C18).copy(alpha = 0.62f) else Color.White.copy(alpha = 0.85f)
                    val tri1 = Path().apply {
                        moveTo(w * 0.06f, h * 0.16f)
                        lineTo(w * 0.50f, h * 0.50f)
                        lineTo(w * 0.06f, h * 0.84f)
                        close()
                    }
                    val tri2 = Path().apply {
                        moveTo(w * 0.50f, h * 0.16f)
                        lineTo(w * 0.94f, h * 0.50f)
                        lineTo(w * 0.50f, h * 0.84f)
                        close()
                    }
                    translate(left = 0.45.dp.toPx(), top = 1.05.dp.toPx()) {
                        drawPath(tri1, color = lipColor)
                        drawPath(tri2, color = lipColor)
                    }
                    drawPath(tri1, color = contentColor)
                    drawPath(tri2, color = contentColor)
                }
            }
        }
    }
}

/**
 * Liquid-Glass-inspired adaptive Material 3 Expressive navigation bar rendered in frosted SeaGlass.
 *
 * Features stacked custom vector icons + labels, a plump active-tab glass lozenge pill that
 * glides with velocity stretch, and an optional coalescing action orb on the right.
 */
@Composable
fun SeaGlassFluidBar(
    items: List<String>,
    selectedIndex: Int,
    onSelect: (Int) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    barMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    indicatorMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    actionLabel: String = "✦",
    actionDetached: Boolean = false,
    showActionOrb: Boolean = false,
    onActionClick: (() -> Unit)? = null
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    val targetTabFloat = selectedIndex.coerceIn(0, (items.size - 1).coerceAtLeast(0)).toFloat()
    val rawLeadIndex by animateFloatAsState(
        targetValue = targetTabFloat,
        animationSpec = spring(dampingRatio = 0.72f, stiffness = 520f),
        label = "fluidBarLeadEdge"
    )
    val rawTrailIndex by animateFloatAsState(
        targetValue = targetTabFloat,
        animationSpec = spring(dampingRatio = 0.76f, stiffness = 340f),
        label = "fluidBarTrailEdge"
    )
    val leadBase = floor(rawLeadIndex)
    val leadIndex = leadBase + SeaGlassShapeMorph.physicsSCurve(rawLeadIndex - leadBase, overshootGain = 0.35f)
    val trailBase = floor(rawTrailIndex)
    val trailIndex = trailBase + SeaGlassShapeMorph.physicsSCurve(rawTrailIndex - trailBase, overshootGain = 0.35f)

    val rawActionDetached by animateFloatAsState(
        targetValue = if (actionDetached) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.62f, stiffness = 340f),
        label = "actionOrbSeparation"
    )
    val actionSeparationDp = (18f * SeaGlassShapeMorph.physicsSCurve(rawActionDetached, overshootGain = 0.45f)).dp

    var totalWidthPx by remember { mutableFloatStateOf(0f) }
    var totalHeightPx by remember { mutableFloatStateOf(0f) }

    val barHeight = 68.dp
    val orbSize = 62.dp
    val barHeightPx = with(density) { barHeight.toPx() }
    val orbSizePx = with(density) { orbSize.toPx() }
    val gapPx = with(density) { actionSeparationDp.toPx() }

    val mainBarWidthPx = if (showActionOrb) {
        (totalWidthPx - orbSizePx - gapPx.coerceAtLeast(0f) - with(density) { 4.dp.toPx() })
            .coerceAtLeast(orbSizePx * 2f)
    } else {
        totalWidthPx
    }

    val coalescingNodes = remember(
        totalWidthPx,
        totalHeightPx,
        mainBarWidthPx,
        barHeightPx,
        orbSizePx,
        gapPx,
        barMaterial,
        indicatorMaterial,
        showActionOrb
    ) {
        if (totalWidthPx <= 1f || totalHeightPx <= 1f) {
            emptyList()
        } else if (!showActionOrb) {
            val centerY = totalHeightPx * 0.5f
            val barHalfW = totalWidthPx * 0.5f
            val barHalfH = barHeightPx * 0.5f
            listOf(
                SeaGlassSdfEngine.SdfNode(
                    centerX = barHalfW,
                    centerY = centerY,
                    halfWidth = barHalfW,
                    halfHeight = barHalfH,
                    uniformCornerRadius = barHalfH,
                    cornerSmoothing = barMaterial.cornerSmoothing,
                    tintColorArgb = barMaterial.tintColorArgb,
                    thicknessPx = barHeightPx * 0.48f
                )
            )
        } else {
            val centerY = totalHeightPx * 0.5f
            val barHalfW = mainBarWidthPx * 0.5f
            val barHalfH = barHeightPx * 0.5f
            val orbHalf = orbSizePx * 0.5f
            val orbCenterX = (mainBarWidthPx + gapPx + orbHalf)
                .coerceAtMost(totalWidthPx - orbHalf)

            listOf(
                SeaGlassSdfEngine.SdfNode(
                    centerX = barHalfW,
                    centerY = centerY,
                    halfWidth = barHalfW,
                    halfHeight = barHalfH,
                    uniformCornerRadius = barHalfH,
                    cornerSmoothing = barMaterial.cornerSmoothing,
                    tintColorArgb = barMaterial.tintColorArgb,
                    thicknessPx = barHeightPx * 0.48f
                ),
                SeaGlassSdfEngine.SdfNode(
                    centerX = orbCenterX,
                    centerY = centerY,
                    halfWidth = orbHalf,
                    halfHeight = orbHalf,
                    uniformCornerRadius = orbHalf,
                    cornerSmoothing = indicatorMaterial.cornerSmoothing,
                    tintColorArgb = indicatorMaterial.tintColorArgb,
                    thicknessPx = orbSizePx * 0.52f
                )
            )
        }
    }

    val barInFlightMalleability = SeaGlassShapeMorph.sCurve(kotlin.math.abs(leadIndex - trailIndex)) * 0.72f
    val activeBarMaterial = remember(barMaterial, barInFlightMalleability) {
        barMaterial.copy(
            cornerRadiusDp = 34f,
            thicknessDp = 20f,
            rimChalkiness = 0.90f
        ).deaged(barInFlightMalleability)
    }

    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(barHeight)
            .onSizeChanged {
                totalWidthPx = it.width.toFloat()
                totalHeightPx = it.height.toFloat()
            }
            .seaGlass(
                stageState = stageState,
                material = activeBarMaterial,
                nodesOptional = coalescingNodes,
                smoothRadiusDp = 42f,
                contentRole = SeaGlassContentRole.NAVIGATION_CHROME
            )
    ) {
        val mainBarWidthDp = with(density) { mainBarWidthPx.toDp() }
        val itemCount = items.size.coerceAtLeast(1)
        val innerPadding = 4.dp
        val trackWidthDp = (mainBarWidthDp - innerPadding * 2).coerceAtLeast(40.dp)
        val itemWidthDp = trackWidthDp / itemCount

        Box(
            modifier = Modifier
                .width(mainBarWidthDp)
                .fillMaxHeight()
                .padding(innerPadding)
        ) {
            val slotWidthPx = with(density) { itemWidthDp.toPx() }
            val leftEdgeTab = min(leadIndex, trailIndex)
            val rightEdgeTab = max(leadIndex, trailIndex) + 1f
            val stretchSpan = kotlin.math.abs(leadIndex - trailIndex).coerceIn(0f, 1.5f)
            val indicatorOffsetX = (slotWidthPx * leftEdgeTab).roundToInt()
            val indicatorWidthDp = with(density) { ((rightEdgeTab - leftEdgeTab) * slotWidthPx).toDp() }

            val isDarkBar = SeaGlassColorMath.argbToOklab(barMaterial.tintColorArgb).l < 0.48f

            // Dual-Edge Lead/Trail 3D Pillow-Beveled Sea-Glass Active Tab Lozenge Pill
            Box(
                modifier = Modifier
                    .offset { IntOffset(x = indicatorOffsetX, y = 0) }
                    .width(indicatorWidthDp)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleY = 1f - stretchSpan * 0.10f
                    }
                    .seaGlassBeveledInlay(
                        tintColorArgb = barMaterial.tintColorArgb,
                        coarseness = barMaterial.coarseness,
                        cornerRadiusDp = 30f
                    )
            )

            CompositionLocalProvider(LocalInsideSeaGlassEtched provides true) {
                Row(
                    modifier = Modifier
                        .fillMaxSize()
                        .seaGlassEtched(
                            tintColorArgb = barMaterial.tintColorArgb,
                            coarseness = barMaterial.coarseness
                        ),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    val baseInkColor = Color(
                        SeaGlassColorMath.optimalContentColorFor(
                            barMaterial.tintColorArgb,
                            barMaterial.tintDensity
                        )
                    )
                    // Palette-harmonized high-contrast active tab accent color
                    val activeAccentColor = if (isDarkBar) {
                        Color(0xFFECF6FF)
                    } else {
                        Color(
                            SeaGlassColorMath.optimalContentColorFor(
                                indicatorMaterial.tintColorArgb,
                                0.90f
                            )
                        )
                    }

                    ProvideSeaGlassLegibleContent(contentColor = baseInkColor) {
                        items.forEachIndexed { index, title ->
                            val selected = index == selectedIndex
                            val tint = if (selected) activeAccentColor else baseInkColor.copy(alpha = 0.88f)

                            Box(
                                contentAlignment = Alignment.Center,
                                modifier = Modifier
                                    .weight(1f)
                                    .fillMaxHeight()
                                    .clickable(
                                        interactionSource = remember { MutableInteractionSource() },
                                        indication = null,
                                        onClick = { onSelect(index) }
                                    )
                            ) {
                                Column(
                                    horizontalAlignment = Alignment.CenterHorizontally,
                                    verticalArrangement = Arrangement.Center
                                ) {
                                    NavBarTabVectorIcon(
                                        index = index,
                                        color = tint,
                                        modifier = Modifier.size(21.dp)
                                    )
                                    Spacer(modifier = Modifier.height(3.dp))
                                    Text(
                                        text = title,
                                        color = tint,
                                        style = MaterialTheme.typography.labelSmall.copy(
                                            fontWeight = if (selected) FontWeight.Bold else FontWeight.SemiBold,
                                            fontSize = 11.sp
                                        )
                                    )
                                }
                            }
                        }
                    }
                }
            }
        }

        if (showActionOrb) {
            val orbLeftPx = (mainBarWidthPx + gapPx).coerceAtMost(totalWidthPx - orbSizePx)
            val orbContentColor = Color(
                SeaGlassColorMath.optimalContentColorFor(
                    indicatorMaterial.tintColorArgb,
                    indicatorMaterial.tintDensity
                )
            )

            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .offset { IntOffset(x = orbLeftPx.roundToInt().coerceAtLeast(0), y = 0) }
                    .size(orbSize)
                    .align(Alignment.CenterStart)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = { onActionClick?.invoke() }
                    )
            ) {
                Text(
                    text = actionLabel,
                    color = orbContentColor,
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.Black
                    )
                )
            }
        }
    }
}

@Composable
private fun NavBarTabVectorIcon(
    index: Int,
    color: Color,
    modifier: Modifier = Modifier
) {
    val insideEtched = LocalInsideSeaGlassEtched.current
    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        val strokeW = 2.1.dp.toPx()
        if (!insideEtched) {
            val isLightInk = (color.red * 0.299f + color.green * 0.587f + color.blue * 0.114f) > 0.55f
            val lipColor = if (isLightInk) {
                Color(0xFF040C18).copy(alpha = 0.68f * color.alpha)
            } else {
                Color.White.copy(alpha = 0.92f * color.alpha)
            }
            val lipOffset = if (isLightInk) {
                Offset(-0.35.dp.toPx(), -0.85.dp.toPx())
            } else {
                Offset(0.45.dp.toPx(), 1.15.dp.toPx())
            }
            translate(lipOffset.x, lipOffset.y) {
                drawNavBarTabVectorGeometry(index, w, h, strokeW, lipColor)
            }
        }
        drawNavBarTabVectorGeometry(index, w, h, strokeW, color)
    }
}

private fun DrawScope.drawNavBarTabVectorGeometry(
    index: Int,
    w: Float,
    h: Float,
    strokeW: Float,
    color: Color
) {
    when (index % 5) {
        0 -> {
            val house = Path().apply {
                moveTo(w * 0.50f, h * 0.10f)
                lineTo(w * 0.90f, h * 0.46f)
                lineTo(w * 0.80f, h * 0.46f)
                lineTo(w * 0.80f, h * 0.88f)
                lineTo(w * 0.58f, h * 0.88f)
                lineTo(w * 0.58f, h * 0.60f)
                lineTo(w * 0.42f, h * 0.60f)
                lineTo(w * 0.42f, h * 0.88f)
                lineTo(w * 0.20f, h * 0.88f)
                lineTo(w * 0.20f, h * 0.46f)
                lineTo(w * 0.10f, h * 0.46f)
                close()
            }
            drawPath(house, color = color)
        }

        1 -> {
            val r = CornerRadius(2.2.dp.toPx(), 2.2.dp.toPx())
            val cell = w * 0.36f
            drawRoundRect(color, Offset(w * 0.10f, h * 0.10f), Size(cell, cell), r)
            drawRoundRect(color, Offset(w * 0.54f, h * 0.10f), Size(cell, cell), r)
            drawRoundRect(color, Offset(w * 0.10f, h * 0.54f), Size(cell, cell), r)
            drawRoundRect(color, Offset(w * 0.54f, h * 0.54f), Size(cell, cell), r)
        }

        2 -> {
            drawCircle(
                color = color,
                radius = w * 0.11f,
                center = Offset(w * 0.50f, h * 0.50f)
            )
            drawArc(
                color = color,
                startAngle = -50f,
                sweepAngle = 100f,
                useCenter = false,
                topLeft = Offset(w * 0.24f, h * 0.24f),
                size = Size(w * 0.52f, h * 0.52f),
                style = Stroke(width = strokeW, cap = StrokeCap.Round)
            )
            drawArc(
                color = color,
                startAngle = 130f,
                sweepAngle = 100f,
                useCenter = false,
                topLeft = Offset(w * 0.24f, h * 0.24f),
                size = Size(w * 0.52f, h * 0.52f),
                style = Stroke(width = strokeW, cap = StrokeCap.Round)
            )
            drawArc(
                color = color,
                startAngle = -55f,
                sweepAngle = 110f,
                useCenter = false,
                topLeft = Offset(w * 0.06f, h * 0.06f),
                size = Size(w * 0.88f, h * 0.88f),
                style = Stroke(width = strokeW, cap = StrokeCap.Round)
            )
            drawArc(
                color = color,
                startAngle = 125f,
                sweepAngle = 110f,
                useCenter = false,
                topLeft = Offset(w * 0.06f, h * 0.06f),
                size = Size(w * 0.88f, h * 0.88f),
                style = Stroke(width = strokeW, cap = StrokeCap.Round)
            )
        }

        3 -> {
            drawLine(
                color = color,
                start = Offset(w * 0.30f, h * 0.12f),
                end = Offset(w * 0.70f, h * 0.12f),
                strokeWidth = strokeW,
                cap = StrokeCap.Round
            )
            drawLine(
                color = color,
                start = Offset(w * 0.22f, h * 0.24f),
                end = Offset(w * 0.78f, h * 0.24f),
                strokeWidth = strokeW,
                cap = StrokeCap.Round
            )
            drawRoundRect(
                color = color,
                topLeft = Offset(w * 0.14f, h * 0.34f),
                size = Size(w * 0.72f, h * 0.56f),
                cornerRadius = CornerRadius(3.5.dp.toPx(), 3.5.dp.toPx())
            )
        }

        else -> {
            drawCircle(
                color = color,
                radius = w * 0.28f,
                center = Offset(w * 0.42f, h * 0.42f),
                style = Stroke(width = strokeW * 1.1f)
            )
            drawLine(
                color = color,
                start = Offset(w * 0.63f, h * 0.63f),
                end = Offset(w * 0.88f, h * 0.88f),
                strokeWidth = strokeW * 1.2f,
                cap = StrokeCap.Round
            )
        }
    }
}

/**
 * Ocean-tumbled circular/pebble SeaGlass medallion button (modeled after the Coral-Peach "G"
 * pebble in the SeaGlass concept triad).
 *
 * Uses [SeaGlassContourPathBuilder.buildOrganicPebblePath] for an authentic beach-tumbled
 * silhouette, a rich Beer-Lambert volumetric core, a wide powdery chalk-frosted bevel rind,
 * high-frequency sand-stipple texture, and deep tonal iconography.
 */
@Composable
fun SeaGlassPebbleOrb(
    onClick: () -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    size: Dp = 68.dp,
    glyph: SeaGlassGlyph = SeaGlassGlyph.BrandG,
    material: SeaGlassMaterial = SeaGlassMaterial(
        tintColorArgb = 0xFFF7A89A.toInt(),
        tintDensity = 0.72f,
        coarseness = 0.58f,
        frostRadiusDp = 26f,
        ior = 1.54f,
        chromaticDispersion = 0.22f,
        rimChalkiness = 0.90f,
        thicknessDp = 18f,
        cornerRadiusDp = size.value * 0.5f,
        cornerSmoothing = 2.4f,
        transparency = 0.52f
    ),
    contentColorOverride: Color? = null
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()
    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "pebbleOrbPress"
    )
    val expandScale = 1f + 0.04f * pressFraction

    val contentColor = contentColorOverride ?: remember(material.tintColorArgb, material.tintDensity) {
        Color(SeaGlassColorMath.optimalContentColorFor(material.tintColorArgb, material.tintDensity))
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = modifier
                .size(size)
                .graphicsLayer {
                    scaleX = expandScale
                    scaleY = expandScale
                }
                .clickable(
                    interactionSource = interactionSource,
                    indication = null,
                    onClick = onClick
                )
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(cornerRadiusDp = size.value * 0.5f),
                    touchIntensity = pressFraction,
                    contentRole = SeaGlassContentRole.TEXT_AND_ICONS
                )
        ) {
            SeaGlassIcon(
                glyph = glyph,
                modifier = Modifier.size(size * 0.48f),
                tint = contentColor
            )
        }
    }
}

/**
 * Tumbled SeaGlass Inline Pill Navigation Bar (modeled after the Orchid-Lavender
 * `Photos | Collections | Create` concept bar).
 *
 * Renders an ocean-tumbled capsule with a rich pastel volumetric core, a wide powdery
 * chalk-frosted bevel rind, fine sand-stipple grain, and an elastically gliding
 * milky-frosted sea-glass inlay capsule around the active tab's inline icon and label.
 */
@Composable
fun SeaGlassPillNavBar(
    items: List<Pair<SeaGlassGlyph?, String>>,
    selectedIndex: Int,
    onSelect: (Int) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = SeaGlassMaterial(
        tintColorArgb = 0xFFEA96FF.toInt(),
        tintDensity = 0.72f,
        coarseness = 0.58f,
        frostRadiusDp = 28f,
        ior = 1.54f,
        chromaticDispersion = 0.22f,
        rimChalkiness = 0.90f,
        thicknessDp = 18f,
        cornerRadiusDp = 30f,
        cornerSmoothing = 2.5f,
        transparency = 0.52f
    )
) {
    val density = LocalDensity.current
    val count = items.size.coerceAtLeast(1)
    val targetIdx = selectedIndex.coerceIn(0, count - 1).toFloat()

    val leadIdx by animateFloatAsState(
        targetValue = targetIdx,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 520f),
        label = "pillNavLead"
    )
    val trailIdx by animateFloatAsState(
        targetValue = targetIdx,
        animationSpec = spring(dampingRatio = 0.74f, stiffness = 310f),
        label = "pillNavTrail"
    )

    var barWidthPx by remember { mutableFloatStateOf(900f) }
    val barHeight = 60.dp

    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(SeaGlassColorMath.optimalContentColorFor(material.tintColorArgb, material.tintDensity))
    }

    val isDarkBar = remember(material.tintColorArgb) {
        SeaGlassColorMath.argbToOklab(material.tintColorArgb).l < 0.48f
    }
    val inlayTopColor = if (isDarkBar) {
        Color(material.tintColorArgb).copy(alpha = 0.56f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(material.tintColorArgb, material.coarseness, 0.74f)
        ).copy(alpha = 0.48f)
    }
    val inlayBottomColor = if (isDarkBar) {
        Color(0xFF0A1E30).copy(alpha = 0.42f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(material.tintColorArgb, material.coarseness, 0.60f)
        ).copy(alpha = 0.38f)
    }
    val inlayRimColor = if (isDarkBar) {
        Color.White.copy(alpha = 0.32f)
    } else {
        Color.White.copy(alpha = 0.38f)
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            modifier = modifier
                .fillMaxWidth()
                .height(barHeight)
                .onSizeChanged { barWidthPx = it.width.toFloat().coerceAtLeast(1f) }
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(cornerRadiusDp = 30f),
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(horizontal = 6.dp, vertical = 6.dp)
        ) {
            val innerW = (barWidthPx - with(density) { 12.dp.toPx() }).coerceAtLeast(1f)
            val slotW = innerW / count
            val leftTab = min(leadIdx, trailIdx)
            val rightTab = max(leadIdx, trailIdx) + 1f
            val stretch = kotlin.math.abs(leadIdx - trailIdx).coerceIn(0f, 1.5f)
            val offsetX = (leftTab * slotW).roundToInt()
            val pillW = with(density) { ((rightTab - leftTab) * slotW).toDp() }

            // 3D Pillow-Beveled Sea-Glass Inlay Capsule (matches the active "Mail" / "Photos" pill in reference)
            Box(
                modifier = Modifier
                    .offset { IntOffset(offsetX, 0) }
                    .width(pillW)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleY = 1f - stretch * 0.08f
                    }
                    .seaGlassBeveledInlay(
                        tintColorArgb = material.tintColorArgb,
                        coarseness = material.coarseness,
                        cornerRadiusDp = 24f
                    )
            )

            CompositionLocalProvider(LocalInsideSeaGlassEtched provides true) {
                Row(
                    modifier = Modifier
                        .fillMaxSize()
                        .seaGlassEtched(
                            tintColorArgb = material.tintColorArgb,
                            coarseness = material.coarseness
                        ),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    items.forEachIndexed { idx, (glyph, title) ->
                        val selected = idx == selectedIndex
                        val itemTint = if (selected) contentColor else contentColor.copy(alpha = 0.94f)
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .weight(1f)
                                .fillMaxHeight()
                                .clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = { onSelect(idx) }
                                )
                        ) {
                            Row(
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(7.dp)
                            ) {
                                if (glyph != null && (selected || idx == 0)) {
                                    SeaGlassIcon(
                                        glyph = glyph,
                                        modifier = Modifier.size(18.dp),
                                        tint = itemTint
                                    )
                                }
                                Text(
                                    text = title,
                                    color = itemTint,
                                    maxLines = 1,
                                    style = MaterialTheme.typography.titleSmall.copy(
                                        fontWeight = if (selected) FontWeight.Bold else FontWeight.SemiBold,
                                        fontSize = 15.sp,
                                        letterSpacing = (-0.1).sp
                                    )
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

/**
 * Tumbled SeaGlass Prompt & Voice Capsule Bar (modeled after the Periwinkle-Blue
 * `+ Ask Gemini  🎤  ( ||| )` concept bar).
 *
 * Features an ocean-tumbled capsule contour, rich volumetric core, wide powdery chalk-frosted
 * bevel rind, fine sand stipple, leading `+` action, tonal prompt label, inline microphone button,
 * and a nested circular milky-frosted sea-glass voice waveform medallion on the right.
 */
@Composable
fun SeaGlassPromptBar(
    promptText: String = "Ask Gemini",
    onPromptClick: () -> Unit = {},
    onPlusClick: () -> Unit = {},
    onMicClick: () -> Unit = {},
    onVoiceWaveClick: () -> Unit = {},
    isVoiceActive: Boolean = false,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = SeaGlassMaterial(
        tintColorArgb = 0xFF9AB8FF.toInt(),
        tintDensity = 0.72f,
        coarseness = 0.58f,
        frostRadiusDp = 28f,
        ior = 1.54f,
        chromaticDispersion = 0.22f,
        rimChalkiness = 0.90f,
        thicknessDp = 18f,
        cornerRadiusDp = 31f,
        cornerSmoothing = 2.5f,
        transparency = 0.52f
    )
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current
    val waveScale by animateFloatAsState(
        targetValue = if (isVoiceActive) 1.07f else 1.0f,
        animationSpec = motion.fastSpatial,
        label = "voiceMedallionPulse"
    )

    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(SeaGlassColorMath.optimalContentColorFor(material.tintColorArgb, material.tintDensity))
    }

    val isDarkBar = remember(material.tintColorArgb) {
        SeaGlassColorMath.argbToOklab(material.tintColorArgb).l < 0.48f
    }
    val medallionTopColor = if (isDarkBar) {
        Color(material.tintColorArgb).copy(alpha = 0.58f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(material.tintColorArgb, material.coarseness, 0.74f)
        ).copy(alpha = 0.50f)
    }
    val medallionBottomColor = if (isDarkBar) {
        Color(0xFF0A1E30).copy(alpha = 0.44f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(material.tintColorArgb, material.coarseness, 0.60f)
        ).copy(alpha = 0.40f)
    }
    val medallionRimColor = if (isDarkBar) {
        Color.White.copy(alpha = 0.34f)
    } else {
        Color.White.copy(alpha = 0.40f)
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = modifier
                .fillMaxWidth()
                .height(62.dp)
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(cornerRadiusDp = 31f),
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(start = 18.dp, end = 7.dp)
        ) {
            // Leading "+" button
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(32.dp)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onPlusClick
                    )
            ) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Plus,
                    modifier = Modifier.size(20.dp),
                    tint = contentColor
                )
            }

            Spacer(modifier = Modifier.width(10.dp))

            // Tonal prompt text ("Ask Gemini") etched into the glass
            Box(
                contentAlignment = Alignment.CenterStart,
                modifier = Modifier
                    .weight(1f)
                    .fillMaxHeight()
                    .seaGlassEtched(
                        tintColorArgb = material.tintColorArgb,
                        coarseness = material.coarseness
                    )
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onPromptClick
                    )
            ) {
                Text(
                    text = promptText,
                    color = contentColor,
                    maxLines = 1,
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.SemiBold,
                        fontSize = 16.sp,
                        letterSpacing = (-0.1).sp
                    )
                )
            }

            // Inline Microphone button
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(38.dp)
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onMicClick
                    )
            ) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Microphone,
                    modifier = Modifier.size(20.dp),
                    tint = contentColor
                )
            }

            Spacer(modifier = Modifier.width(6.dp))

            // Nested Circular 3D Pillow-Beveled Sea-Glass Voice Waveform Medallion
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(48.dp)
                    .graphicsLayer {
                        scaleX = waveScale
                        scaleY = waveScale
                    }
                    .seaGlassBeveledInlay(
                        tintColorArgb = material.tintColorArgb,
                        coarseness = material.coarseness,
                        cornerRadiusDp = 24f
                    )
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = onVoiceWaveClick
                    )
            ) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.AudioWave,
                    modifier = Modifier.size(21.dp),
                    tint = contentColor
                )
            }
        }
    }
}
