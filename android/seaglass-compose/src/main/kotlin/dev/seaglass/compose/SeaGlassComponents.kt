package dev.seaglass.compose

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
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
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.PointerEventPass
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * Provides [LocalContentColor] for crisp, un-haloed typography and icons on SeaGlass surfaces.
 * Legibility is achieved optically through the glass's deep subsurface blur, `BlendMode.Screen`
 * luminance lift, and rich Oklab sea-glass body tint—never via artificial text shadows.
 */
@Composable
fun ProvideSeaGlassLegibleContent(
    contentColor: Color,
    content: @Composable () -> Unit
) {
    CompositionLocalProvider(
        LocalContentColor provides contentColor,
        content = content
    )
}

/**
 * Non-consuming pointer observer that tracks the live 2D touch point `(touchX, touchY)` and
 * press state on a SeaGlass surface so off-center taps tilt the glass in 3D, shift side
 * refraction, and bloom an HDR caustic wave right under the finger—without blocking child
 * buttons or sliders.
 */
private fun Modifier.observeSeaGlassTouch(
    onTouchUpdate: (Offset?, Boolean) -> Unit
): Modifier = this.pointerInput(Unit) {
    awaitPointerEventScope {
        while (true) {
            val event = awaitPointerEvent(PointerEventPass.Initial)
            val pressedChange = event.changes.firstOrNull { it.pressed }
            if (pressedChange != null) {
                onTouchUpdate(pressedChange.position, true)
            } else {
                onTouchUpdate(null, false)
            }
        }
    }
}

/**
 * Foundational SeaGlass container surface with Material 3 Expressive state morphing,
 * adaptive touch-point 3D perspective tilt, HDR touch caustic flare, and automatic
 * Oklab contrast-safe foreground content color.
 */
@Composable
fun SeaGlassSurface(
    stageState: SeaGlassStageState?,
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
    var lastTouchOffset by remember { mutableStateOf<Offset?>(null) }
    var isPointerDown by remember { mutableStateOf(false) }

    val isPressed = isClickablePressed || isPointerDown
    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "surfacePress"
    )

    val effectiveMaterial by animateSeaGlassMaterialAsState(
        targetMaterial = material.pressed(pressFraction),
        motionScheme = motion
    )

    // Compute normalized off-center touch vector [-1..+1] for 3D perspective press tilt
    val cx = max(surfaceSize.width * 0.5f, 1f)
    val cy = max(surfaceSize.height * 0.5f, 1f)
    val normTouchX = lastTouchOffset?.let { ((it.x - cx) / cx).coerceIn(-1f, 1f) } ?: 0f
    val normTouchY = lastTouchOffset?.let { ((it.y - cy) / cy).coerceIn(-1f, 1f) } ?: 0f

    val tiltRotX by animateFloatAsState(
        targetValue = -normTouchY * 4.8f * pressFraction,
        animationSpec = motion.fastSpatial,
        label = "tiltRotX"
    )
    val tiltRotY by animateFloatAsState(
        targetValue = normTouchX * 4.8f * pressFraction,
        animationSpec = motion.fastSpatial,
        label = "tiltRotY"
    )

    val scaleX = 1f + 0.022f * pressFraction
    val scaleY = 1f - 0.018f * pressFraction

    val contentColor = remember(effectiveMaterial.tintColorArgb, effectiveMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = effectiveMaterial.tintColorArgb,
                tintDensity = effectiveMaterial.tintDensity
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

    val parentScope = LocalSeaGlassContainerCornerScope.current

    val containerScope = remember(
        surfaceOriginInRoot,
        surfaceSize,
        effectiveMaterial.cornerRadiusDp,
        deviceCornerSpec,
        parentScope,
        cornerAdaptationMode,
        cornerMask,
        cornerProximityThresholdDp,
        cornerFullMatchDistanceDp,
        cornerAdaptationStrength,
        allowAsymmetricCorners,
        adaptToParentContainer
    ) {
        val wPx = surfaceSize.width.toFloat().coerceAtLeast(1f)
        val hPx = surfaceSize.height.toFloat().coerceAtLeast(1f)
        val baseCornerPx = min(effectiveMaterial.cornerRadiusDp * density.density, min(wPx, hPx) * 0.5f)
        val stageW = (stageState?.backdropSize?.width?.toFloat()
            ?: context.resources.displayMetrics.widthPixels.toFloat()).coerceAtLeast(1f)
        val stageH = (stageState?.backdropSize?.height?.toFloat()
            ?: context.resources.displayMetrics.heightPixels.toFloat()).coerceAtLeast(1f)
        val stageOrigin = stageState?.backdropOriginInRoot ?: Offset.Zero
        val leftInStage = surfaceOriginInRoot.x - stageOrigin.x
        val topInStage = surfaceOriginInRoot.y - stageOrigin.y

        val deviceSolved = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
            elementLeftPx = leftInStage,
            elementTopPx = topInStage,
            elementRightPx = leftInStage + wPx,
            elementBottomPx = topInStage + hPx,
            containerWidthPx = stageW,
            containerHeightPx = stageH,
            containerCornersPx = deviceCornerSpec.toRadiiPx(),
            baseCornerRadiusPx = baseCornerPx,
            minCornerFloorPx = 8f * density.density,
            influenceDistancePx = cornerProximityThresholdDp.coerceAtLeast(0f) * density.density,
            fullMatchDistancePx = cornerFullMatchDistanceDp.coerceAtLeast(0f) * density.density,
            adaptationStrength = cornerAdaptationStrength,
            mode = cornerAdaptationMode,
            cornerMask = cornerMask,
            allowAsymmetricCorners = allowAsymmetricCorners
        )

        val solvedRadii = if (
            (adaptToParentContainer || cornerAdaptationMode == SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) &&
            parentScope != null &&
            parentScope.widthPx > wPx + 4f &&
            parentScope.heightPx > hPx + 4f &&
            cornerAdaptationMode != SeaGlassCornerAdaptationMode.FIXED &&
            !cornerMask.isNone
        ) {
            val relLeft = surfaceOriginInRoot.x - parentScope.originInRoot.x
            val relTop = surfaceOriginInRoot.y - parentScope.originInRoot.y
            SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                elementLeftPx = relLeft,
                elementTopPx = relTop,
                elementRightPx = relLeft + wPx,
                elementBottomPx = relTop + hPx,
                containerWidthPx = parentScope.widthPx,
                containerHeightPx = parentScope.heightPx,
                containerCornersPx = parentScope.cornerRadiiPx,
                baseCornerRadiusPx = baseCornerPx,
                minCornerFloorPx = 7f * density.density,
                influenceDistancePx = cornerProximityThresholdDp.coerceAtLeast(0f) * density.density,
                fullMatchDistancePx = cornerFullMatchDistanceDp.coerceAtLeast(0f) * density.density,
                adaptationStrength = cornerAdaptationStrength,
                mode = cornerAdaptationMode,
                cornerMask = cornerMask,
                allowAsymmetricCorners = allowAsymmetricCorners
            )
        } else {
            deviceSolved
        }

        SeaGlassContainerCornerScope(
            originInRoot = surfaceOriginInRoot,
            widthPx = wPx,
            heightPx = hPx,
            cornerRadiiPx = solvedRadii
        )
    }

    val effectivePadding = if (cornerAdaptationMode == SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED) {
        val layoutDir = androidx.compose.ui.platform.LocalLayoutDirection.current
        val baseL = with(density) { contentPadding.calculateLeftPadding(layoutDir).toPx() }
        val baseT = with(density) { contentPadding.calculateTopPadding().toPx() }
        val baseR = with(density) { contentPadding.calculateRightPadding(layoutDir).toPx() }
        val baseB = with(density) { contentPadding.calculateBottomPadding().toPx() }
        val safe = SeaGlassCornerAdaptationSolver.computeCornerSafePaddingPx(
            corners = containerScope.cornerRadiiPx,
            baseLeftPx = baseL,
            baseTopPx = baseT,
            baseRightPx = baseR,
            baseBottomPx = baseB
        )
        PaddingValues(
            start = with(density) { safe.leftPx.toDp() },
            top = with(density) { safe.topPx.toDp() },
            end = with(density) { safe.rightPx.toDp() },
            bottom = with(density) { safe.bottomPx.toDp() }
        )
    } else {
        contentPadding
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            contentAlignment = contentAlignment,
            modifier = modifier
                .onGloballyPositioned { coords ->
                    surfaceOriginInRoot = coords.positionInRoot()
                    surfaceSize = coords.size
                }
                .observeSeaGlassTouch { pos, down ->
                    if (pos != null) lastTouchOffset = pos
                    isPointerDown = down
                }
                .graphicsLayer {
                    this.scaleX = scaleX
                    this.scaleY = scaleY
                    this.rotationX = tiltRotX
                    this.rotationY = tiltRotY
                    this.cameraDistance = 14f * this.density
                }
                .then(clickableModifier)
                .seaGlass(
                    stageState = stageState,
                    material = effectiveMaterial,
                    touchPointInLocal = lastTouchOffset,
                    touchIntensity = pressFraction,
                    contentRole = SeaGlassContentRole.TEXT_AND_ICONS,
                    cornerAdaptationMode = cornerAdaptationMode,
                    cornerMask = cornerMask,
                    cornerProximityThresholdDp = cornerProximityThresholdDp,
                    cornerFullMatchDistanceDp = cornerFullMatchDistanceDp,
                    cornerAdaptationStrength = cornerAdaptationStrength,
                    allowAsymmetricCorners = allowAsymmetricCorners,
                    adaptToParentContainer = adaptToParentContainer
                )
                .padding(effectivePadding)
        ) {
            CompositionLocalProvider(LocalSeaGlassContainerCornerScope provides containerScope) {
                content()
            }
        }
    }
}

/**
 * Tactile tumbled sea-glass button that physically compresses on touch with adaptive
 * touch-point HDR caustic flare and 3D perspective tilt.
 */
@Composable
fun SeaGlassButton(
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
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

    var buttonSize by remember { mutableStateOf(IntSize.Zero) }
    var lastTouchOffset by remember { mutableStateOf<Offset?>(null) }
    var isPointerDown by remember { mutableStateOf(false) }

    val isPressed = isClickablePressed || isPointerDown
    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "buttonPress"
    )

    val animatedMaterial by animateSeaGlassMaterialAsState(
        targetMaterial = material.pressed(pressFraction),
        motionScheme = motion
    )

    val cx = max(buttonSize.width * 0.5f, 1f)
    val cy = max(buttonSize.height * 0.5f, 1f)
    val normTouchX = lastTouchOffset?.let { ((it.x - cx) / cx).coerceIn(-1f, 1f) } ?: 0f
    val normTouchY = lastTouchOffset?.let { ((it.y - cy) / cy).coerceIn(-1f, 1f) } ?: 0f

    val tiltRotX by animateFloatAsState(
        targetValue = -normTouchY * 6.0f * pressFraction,
        animationSpec = motion.fastSpatial,
        label = "btnTiltX"
    )
    val tiltRotY by animateFloatAsState(
        targetValue = normTouchX * 6.0f * pressFraction,
        animationSpec = motion.fastSpatial,
        label = "btnTiltY"
    )

    val contentColor = remember(animatedMaterial.tintColorArgb, animatedMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = animatedMaterial.tintColorArgb,
                tintDensity = animatedMaterial.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.Center,
            modifier = modifier
                .onSizeChanged { buttonSize = it }
                .observeSeaGlassTouch { pos, down ->
                    if (pos != null) lastTouchOffset = pos
                    isPointerDown = down
                }
                .graphicsLayer {
                    scaleX = 1f + 0.045f * pressFraction
                    scaleY = 1f - 0.038f * pressFraction
                    rotationX = tiltRotX
                    rotationY = tiltRotY
                    cameraDistance = 12f * density
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
                .padding(contentPadding),
            content = content
        )
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
    stageState: SeaGlassStageState?,
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
    stageState: SeaGlassStageState?,
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
            // Album Artwork Thumbnail
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(40.dp)
                    .clip(RoundedCornerShape(10.dp))
                    .background(
                        Brush.linearGradient(
                            colors = listOf(
                                Color(0xFFE86A50),
                                Color(0xFFD84E38)
                            )
                        )
                    )
            ) {
                Canvas(modifier = Modifier.size(22.dp)) {
                    drawCircle(
                        color = Color(0xFF28120C).copy(alpha = 0.82f),
                        radius = size.minDimension * 0.42f,
                        style = Stroke(width = 2.dp.toPx())
                    )
                    drawCircle(
                        color = Color(0xFFFFDFC8),
                        radius = size.minDimension * 0.14f
                    )
                }
            }

            Spacer(modifier = Modifier.width(12.dp))

            // Track Title & Artist Subtitle
            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = title,
                    color = contentColor,
                    maxLines = 1,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Bold,
                        fontSize = 14.sp,
                        letterSpacing = (-0.1).sp
                    )
                )
                Text(
                    text = subtitle,
                    color = contentColor.copy(alpha = 0.78f),
                    maxLines = 1,
                    style = MaterialTheme.typography.bodySmall.copy(
                        fontWeight = FontWeight.Normal,
                        fontSize = 12.sp
                    )
                )
            }

            // Play / Pause Vector Button
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
                    if (isPlaying) {
                        drawRoundRect(
                            color = contentColor,
                            topLeft = Offset(w * 0.16f, h * 0.12f),
                            size = Size(w * 0.24f, h * 0.76f),
                            cornerRadius = CornerRadius(2.dp.toPx(), 2.dp.toPx())
                        )
                        drawRoundRect(
                            color = contentColor,
                            topLeft = Offset(w * 0.60f, h * 0.12f),
                            size = Size(w * 0.24f, h * 0.76f),
                            cornerRadius = CornerRadius(2.dp.toPx(), 2.dp.toPx())
                        )
                    } else {
                        val playPath = Path().apply {
                            moveTo(w * 0.18f, h * 0.10f)
                            lineTo(w * 0.88f, h * 0.50f)
                            lineTo(w * 0.18f, h * 0.90f)
                            close()
                        }
                        drawPath(playPath, color = contentColor)
                    }
                }
            }

            // Fast-Forward (▶▶) Vector Button
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
    val leadIndex by animateFloatAsState(
        targetValue = targetTabFloat,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 540f),
        label = "fluidBarLeadEdge"
    )
    val trailIndex by animateFloatAsState(
        targetValue = targetTabFloat,
        animationSpec = spring(dampingRatio = 0.72f, stiffness = 310f),
        label = "fluidBarTrailEdge"
    )

    val actionSeparationDp by animateDpAsState(
        targetValue = if (actionDetached) 18.dp else 0.dp,
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "actionOrbSeparation"
    )

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

    val barInFlightMalleability = kotlin.math.abs(leadIndex - trailIndex).coerceIn(0f, 1f) * 0.72f
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
            val pillTopColor = if (isDarkBar) {
                Color(indicatorMaterial.tintColorArgb).copy(alpha = 0.56f)
            } else {
                Color(
                    SeaGlassColorMath.lerpOklab(
                        indicatorMaterial.tintColorArgb,
                        0xFFFFFFFF.toInt(),
                        0.32f
                    )
                ).copy(alpha = 0.68f)
            }
            val pillBottomColor = if (isDarkBar) {
                Color(0xFF0A1E30).copy(alpha = 0.40f)
            } else {
                Color(indicatorMaterial.tintColorArgb).copy(alpha = 0.52f)
            }

            // Dual-Edge Lead/Trail Liquid Glass Active Tab Lozenge Pill
            Box(
                modifier = Modifier
                    .offset { IntOffset(x = indicatorOffsetX, y = 0) }
                    .width(indicatorWidthDp)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleY = 1f - stretchSpan * 0.10f
                    }
                    .clip(RoundedCornerShape(30.dp))
                    .background(
                        Brush.verticalGradient(
                            colors = listOf(pillTopColor, pillBottomColor)
                        )
                    )
            )

            Row(
                modifier = Modifier.fillMaxSize(),
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
                        val tint = if (selected) activeAccentColor else baseInkColor.copy(alpha = 0.72f)

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
    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        val strokeW = 2.1.dp.toPx()

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
}
