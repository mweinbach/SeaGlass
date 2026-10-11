package dev.seaglass.compose

import android.graphics.Path
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.VectorConverter
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
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
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.PointerEventPass
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.SubcomposeLayout
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
import dev.seaglass.core.SeaGlassShapeMorph
import kotlinx.coroutines.launch
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sqrt

/**
 * Defines how [SeaGlassTwoElementMorph] transitions between two arbitrary UI elements.
 */
enum class SeaGlassMorphMode {
    /**
     * Synthesizes a multi-node `sminCubic` liquid sea-glass bridge during the transition so
     * the departing element stretches a liquid neck as it morphs into the arriving element.
     */
    LIQUID_COALESCE,

    /**
     * Morphs between two Material 3 Expressive `RoundedPolygon` silhouettes
     * ([SeaGlassShapeKind]) while flexing glass thickness, IOR lensing, and chromatic dispersion.
     */
    POLYGON_SHAPE_MORPH
}

/**
 * Automatic 2-Element Dynamic Liquid SeaGlass Morph Primitive.
 *
 * Give it any two composables ([collapsedContent] and [expandedContent]) and the framework
 * automatically:
 * 1. Measures both elements dynamically via [SubcomposeLayout] (zero hardcoded sizes required).
 * 2. Animates the container bounds `(width, height)`, corner radii, and topology with a
 *    Material 3 Expressive bouncy spatial spring (`dampingRatio = 0.60f, stiffness = 360f`).
 * 3. Flexes the optical material in flight via [SeaGlassShapeMorph.computeDynamicFlexMaterial]—
 *    increasing glass thickness, IOR water-drop lensing, and internal chromatic aberration at
 *    peak transition velocity just like Apple's WWDC25 Liquid Glass.
 * 4. Cross-fades and spring-scales the inner composables while maintaining interactive tap/tilt
 *    and HDR touch caustic waves.
 */
@Composable
fun SeaGlassTwoElementMorph(
    expanded: Boolean,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    onToggle: (() -> Unit)? = null,
    collapsedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    expandedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    collapsedShape: SeaGlassShapeKind = SeaGlassShapeKind.Capsule,
    expandedShape: SeaGlassShapeKind = SeaGlassShapeKind.Squircle,
    morphMode: SeaGlassMorphMode = SeaGlassMorphMode.LIQUID_COALESCE,
    collapsedContent: @Composable BoxScope.() -> Unit,
    expandedContent: @Composable BoxScope.() -> Unit
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current

    // Spatial spring (allows organic M3 Expressive bounce on geometry)
    val spatialProgress by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.60f, stiffness = 350f),
        label = "twoElementSpatialMorph"
    )

    // Effects spring (critically damped, zero overshoot for optical material & cross-fade)
    val effectsProgress by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "twoElementEffectsMorph"
    )

    var collapsedMeasuredSize by remember { mutableStateOf(IntSize(420, 150)) }
    var expandedMeasuredSize by remember { mutableStateOf(IntSize(900, 380)) }

    var touchOffset by remember { mutableStateOf<Offset?>(null) }
    var isPressed by remember { mutableStateOf(false) }
    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "morphPress"
    )

    // Compute dynamic area expansion ratio and flex material (boosts lensing & dispersion mid-morph!)
    val collapsedArea = max(1f, (collapsedMeasuredSize.width * collapsedMeasuredSize.height).toFloat())
    val expandedArea = max(1f, (expandedMeasuredSize.width * expandedMeasuredSize.height).toFloat())
    val currentAreaRatio = 1f + (expandedArea / collapsedArea - 1f) * effectsProgress

    val flexMaterial = remember(
        collapsedMaterial,
        expandedMaterial,
        effectsProgress,
        pressFraction,
        currentAreaRatio
    ) {
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = collapsedMaterial,
            endMaterial = expandedMaterial,
            progress = effectsProgress,
            areaExpansionRatio = currentAreaRatio
        ).pressed(pressFraction)
    }

    val curW = (collapsedMeasuredSize.width +
        (expandedMeasuredSize.width - collapsedMeasuredSize.width) * spatialProgress)
        .coerceAtLeast(32f)
    val curH = (collapsedMeasuredSize.height +
        (expandedMeasuredSize.height - collapsedMeasuredSize.height) * spatialProgress)
        .coerceAtLeast(32f)

    // Parabolic mid-flight energy (peaks at progress = 0.5)
    val inFlightEnergy = (4f * effectsProgress * (1f - effectsProgress)).coerceIn(0f, 1f)

    // Build transient 2-node sminCubic liquid bridge during LIQUID_COALESCE transitions
    val coalescingNodes = remember(
        curW,
        curH,
        collapsedMeasuredSize,
        expandedMeasuredSize,
        effectsProgress,
        inFlightEnergy,
        flexMaterial,
        collapsedMaterial,
        expandedMaterial,
        morphMode
    ) {
        if (morphMode == SeaGlassMorphMode.LIQUID_COALESCE && inFlightEnergy > 0.06f) {
            val colW = collapsedMeasuredSize.width.toFloat().coerceAtLeast(32f)
            val colH = collapsedMeasuredSize.height.toFloat().coerceAtLeast(32f)
            // Node 0: Departing anchor core; Node 1: Expanding target lobe budding outward
            val anchorHalfW = (colW * 0.5f * (1f - effectsProgress * 0.35f)).coerceAtMost(curW * 0.48f)
            val anchorHalfH = (colH * 0.5f * (1f - effectsProgress * 0.35f)).coerceAtMost(curH * 0.48f)
            val targetHalfW = (curW * 0.5f * (0.45f + 0.55f * effectsProgress))
            val targetHalfH = (curH * 0.5f * (0.45f + 0.55f * effectsProgress))
            val budShiftY = (curH - colH) * 0.22f * inFlightEnergy

            listOf(
                SeaGlassSdfEngine.SdfNode(
                    centerX = curW * 0.5f,
                    centerY = curH * 0.5f - budShiftY,
                    halfWidth = anchorHalfW,
                    halfHeight = anchorHalfH,
                    uniformCornerRadius = min(anchorHalfW, anchorHalfH) * 0.85f,
                    cornerSmoothing = 2.3f,
                    tintColorArgb = collapsedMaterial.tintColorArgb,
                    thicknessPx = flexMaterial.thicknessDp * 2.8f
                ),
                SeaGlassSdfEngine.SdfNode(
                    centerX = curW * 0.5f,
                    centerY = curH * 0.5f + budShiftY * 0.6f,
                    halfWidth = targetHalfW,
                    halfHeight = targetHalfH,
                    uniformCornerRadius = min(targetHalfW, targetHalfH) * (0.85f - 0.35f * effectsProgress),
                    cornerSmoothing = 2.4f,
                    tintColorArgb = flexMaterial.tintColorArgb,
                    thicknessPx = flexMaterial.thicknessDp * 2.8f
                )
            )
        } else {
            null
        }
    }

    val polygonBuilder: ((Float, Float, Path) -> Unit)? = remember(
        morphMode,
        collapsedShape,
        expandedShape,
        effectsProgress
    ) {
        if (morphMode == SeaGlassMorphMode.POLYGON_SHAPE_MORPH) {
            { wPx, hPx, outPath ->
                SeaGlassShapeMorph.buildMorphedPolygonPath(
                    startKind = collapsedShape,
                    endKind = expandedShape,
                    progress = effectsProgress,
                    widthPx = wPx,
                    heightPx = hPx,
                    outPath = outPath
                )
            }
        } else {
            null
        }
    }

    val contentColor = remember(flexMaterial.tintColorArgb, flexMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = flexMaterial.tintColorArgb,
                tintDensity = flexMaterial.tintDensity
            )
        )
    }

    // Off-center 3D perspective press tilt
    val cx = max(curW * 0.5f, 1f)
    val cy = max(curH * 0.5f, 1f)
    val normX = touchOffset?.let { ((it.x - cx) / cx).coerceIn(-1f, 1f) } ?: 0f
    val normY = touchOffset?.let { ((it.y - cy) / cy).coerceIn(-1f, 1f) } ?: 0f

    // SubcomposeLayout measures both collapsed and expanded branches invisibly so their exact
    // intrinsic target bounds are always known without manual sizing!
    SubcomposeLayout(modifier = modifier) { constraints ->
        val looseConstraints = constraints.copy(minWidth = 0, minHeight = 0)

        val collapsedPlaceables = subcompose("collapsed_measure") {
            CompositionLocalProvider(LocalContentColor provides contentColor) {
                Box(content = collapsedContent)
            }
        }.map { it.measure(looseConstraints) }

        val expandedPlaceables = subcompose("expanded_measure") {
            CompositionLocalProvider(LocalContentColor provides contentColor) {
                Box(content = expandedContent)
            }
        }.map { it.measure(looseConstraints) }

        val cW = collapsedPlaceables.maxOfOrNull { it.width }?.coerceAtLeast(48) ?: 240
        val cH = collapsedPlaceables.maxOfOrNull { it.height }?.coerceAtLeast(48) ?: 120
        val eW = expandedPlaceables.maxOfOrNull { it.width }?.coerceAtLeast(48) ?: cW
        val eH = expandedPlaceables.maxOfOrNull { it.height }?.coerceAtLeast(48) ?: cH

        collapsedMeasuredSize = IntSize(cW, cH)
        expandedMeasuredSize = IntSize(eW, eH)

        val animatedWidthPx = (cW + (eW - cW) * spatialProgress).roundToInt().coerceAtLeast(32)
        val animatedHeightPx = (cH + (eH - cH) * spatialProgress).roundToInt().coerceAtLeast(32)

        val hostPlaceables = subcompose("morph_host") {
            CompositionLocalProvider(LocalContentColor provides contentColor) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(
                            width = with(density) { animatedWidthPx.toDp() },
                            height = with(density) { animatedHeightPx.toDp() }
                        )
                        .pointerInput(Unit) {
                            awaitPointerEventScope {
                                while (true) {
                                    val event = awaitPointerEvent(PointerEventPass.Initial)
                                    val down = event.changes.firstOrNull { it.pressed }
                                    if (down != null) {
                                        touchOffset = down.position
                                        isPressed = true
                                    } else {
                                        isPressed = false
                                    }
                                }
                            }
                        }
                        .then(
                            if (onToggle != null) {
                                Modifier.clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = onToggle
                                )
                            } else {
                                Modifier
                            }
                        )
                        .graphicsLayer {
                            // Velocity-driven liquid stretch during morph + press squish
                            scaleX = 1f + 0.03f * pressFraction + 0.04f * inFlightEnergy
                            scaleY = 1f - 0.025f * pressFraction - 0.025f * inFlightEnergy
                            rotationX = -normY * 5.0f * pressFraction
                            rotationY = normX * 5.0f * pressFraction
                            cameraDistance = 14f * this.density
                        }
                        .seaGlass(
                            stageState = stageState,
                            material = flexMaterial,
                            nodesOptional = coalescingNodes,
                            smoothRadiusDp = 42f,
                            touchPointInLocal = touchOffset,
                            touchIntensity = max(pressFraction, inFlightEnergy * 0.75f),
                            customContourPathBuilder = polygonBuilder
                        )
                ) {
                    val collapsedAlpha = (1f - effectsProgress * 1.45f).coerceIn(0f, 1f)
                    val expandedAlpha = ((effectsProgress - 0.25f) * 1.35f).coerceIn(0f, 1f)

                    if (collapsedAlpha > 0.01f) {
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .graphicsLayer {
                                    alpha = collapsedAlpha
                                    val s = 0.85f + 0.15f * collapsedAlpha
                                    scaleX = s
                                    scaleY = s
                                },
                            content = collapsedContent
                        )
                    }

                    if (expandedAlpha > 0.01f) {
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .graphicsLayer {
                                    alpha = expandedAlpha
                                    val s = 0.88f + 0.12f * expandedAlpha
                                    scaleX = s
                                    scaleY = s
                                },
                            content = expandedContent
                        )
                    }
                }
            }
        }.map {
            it.measure(Constraints.fixed(animatedWidthPx, animatedHeightPx))
        }

        layout(animatedWidthPx, animatedHeightPx) {
            hostPlaceables.forEach { it.placeRelative(0, 0) }
        }
    }
}

/**
 * Interactive Liquid Glass Physics Modifier: combines **Adaptive Tap** (3D perspective press tilt +
 * HDR touch-point caustic wave), **Elastic Drag with Velocity Stretch** (stretches along the
 * drag vector like a drop of mercury), and **Spring Snap-Back** (snaps back with a Material 3
 * Expressive bouncy spatial spring when released).
 */
@Composable
fun Modifier.seaGlassInteractive(
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    shapeKind: SeaGlassShapeKind? = null,
    enableElasticDrag: Boolean = false,
    snapBackOnRelease: Boolean = true,
    onTap: (() -> Unit)? = null
): Modifier {
    val scope = rememberCoroutineScope()
    val motion = LocalSeaGlassMotionScheme.current

    val dragOffset = remember { Animatable(Offset.Zero, Offset.VectorConverter) }
    var dragVelocity by remember { mutableStateOf(Offset.Zero) }
    var elementSize by remember { mutableStateOf(IntSize.Zero) }
    var touchOffset by remember { mutableStateOf<Offset?>(null) }
    var isPressed by remember { mutableStateOf(false) }

    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "interactivePress"
    )

    val cx = max(elementSize.width * 0.5f, 1f)
    val cy = max(elementSize.height * 0.5f, 1f)
    val normX = touchOffset?.let { ((it.x - cx) / cx).coerceIn(-1f, 1f) } ?: 0f
    val normY = touchOffset?.let { ((it.y - cy) / cy).coerceIn(-1f, 1f) } ?: 0f

    // Drag stretch based on displacement / velocity
    val dispMag = sqrt(
        dragOffset.value.x * dragOffset.value.x + dragOffset.value.y * dragOffset.value.y
    )
    val stretchFactor = (dispMag / 260f).coerceIn(0f, 0.28f)
    val stretchX = 1f + stretchFactor * (abs(dragOffset.value.x) / max(dispMag, 1f)) -
        stretchFactor * 0.45f * (abs(dragOffset.value.y) / max(dispMag, 1f))
    val stretchY = 1f + stretchFactor * (abs(dragOffset.value.y) / max(dispMag, 1f)) -
        stretchFactor * 0.45f * (abs(dragOffset.value.x) / max(dispMag, 1f))

    val activeMaterial = remember(material, pressFraction, stretchFactor) {
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = material,
            endMaterial = material.pressed(1f),
            progress = max(pressFraction, stretchFactor * 2f).coerceIn(0f, 1f)
        )
    }

    val customShapeBuilder: ((Float, Float, Path) -> Unit)? = remember(shapeKind) {
        if (shapeKind != null) {
            { wPx, hPx, outPath ->
                SeaGlassShapeMorph.buildMorphedPolygonPath(
                    startKind = shapeKind,
                    endKind = shapeKind,
                    progress = 0f,
                    widthPx = wPx,
                    heightPx = hPx,
                    outPath = outPath
                )
            }
        } else {
            null
        }
    }

    return this
        .onSizeChanged { elementSize = it }
        .offset {
            IntOffset(
                dragOffset.value.x.roundToInt(),
                dragOffset.value.y.roundToInt()
            )
        }
        .pointerInput(enableElasticDrag, snapBackOnRelease, onTap) {
            if (enableElasticDrag) {
                detectDragGestures(
                    onDragStart = { pos ->
                        touchOffset = pos
                        isPressed = true
                    },
                    onDragEnd = {
                        isPressed = false
                        if (snapBackOnRelease) {
                            scope.launch {
                                dragOffset.animateTo(
                                    targetValue = Offset.Zero,
                                    animationSpec = spring(
                                        dampingRatio = 0.52f,
                                        stiffness = 380f
                                    )
                                )
                            }
                        }
                    },
                    onDragCancel = {
                        isPressed = false
                        if (snapBackOnRelease) {
                            scope.launch {
                                dragOffset.animateTo(
                                    targetValue = Offset.Zero,
                                    animationSpec = spring(
                                        dampingRatio = 0.52f,
                                        stiffness = 380f
                                    )
                                )
                            }
                        }
                    },
                    onDrag = { change, dragAmount ->
                        change.consume()
                        touchOffset = change.position
                        isPressed = true
                        dragVelocity = dragAmount
                        scope.launch {
                            // Rubber-band resistance whensnapBackOnRelease is enabled
                            val scale = if (snapBackOnRelease) 0.68f else 1.0f
                            dragOffset.snapTo(dragOffset.value + dragAmount * scale)
                        }
                    }
                )
            } else {
                detectTapGestures(
                    onPress = { pos ->
                        touchOffset = pos
                        isPressed = true
                        tryAwaitRelease()
                        isPressed = false
                    },
                    onTap = { onTap?.invoke() }
                )
            }
        }
        .graphicsLayer {
            scaleX = stretchX * (1f + 0.035f * pressFraction)
            scaleY = stretchY * (1f - 0.028f * pressFraction)
            rotationX = -normY * 6.0f * pressFraction
            rotationY = normX * 6.0f * pressFraction
            cameraDistance = 12f * density
        }
        .seaGlass(
            stageState = stageState,
            material = activeMaterial,
            touchPointInLocal = touchOffset,
            touchIntensity = max(pressFraction, stretchFactor * 2.5f).coerceIn(0f, 1f),
            customContourPathBuilder = customShapeBuilder
        )
}

/**
 * Material 3 Expressive Shape-Morphing SeaGlass Button.
 *
 * Smoothly morphs its geometry between [restingShape] and [activeShape] (using
 * `androidx.graphics.shapes.Morph`) and flexes its water-drop sea-glass optics when toggled
 * or pressed.
 */
@Composable
fun SeaGlassMorphingButton(
    active: Boolean,
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    restingShape: SeaGlassShapeKind = SeaGlassShapeKind.Cookie9,
    activeShape: SeaGlassShapeKind = SeaGlassShapeKind.Squircle,
    restingMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    activeMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass,
    content: @Composable BoxScope.() -> Unit
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()

    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "morphBtnPress"
    )

    val morphProgress by animateFloatAsState(
        targetValue = when {
            isPressed -> (if (active) 0.65f else 0.35f)
            active -> 1f
            else -> 0f
        },
        animationSpec = spring(dampingRatio = 0.56f, stiffness = 380f),
        label = "morphBtnShapeProgress"
    )

    val currentMaterial = remember(
        restingMaterial,
        activeMaterial,
        morphProgress,
        pressFraction
    ) {
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = restingMaterial,
            endMaterial = activeMaterial,
            progress = morphProgress.coerceIn(0f, 1f)
        ).pressed(pressFraction)
    }

    val contentColor = remember(currentMaterial.tintColorArgb, currentMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = currentMaterial.tintColorArgb,
                tintDensity = currentMaterial.tintDensity
            )
        )
    }

    var touchOffset by remember { mutableStateOf<Offset?>(null) }

    CompositionLocalProvider(LocalContentColor provides contentColor) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = modifier
                .pointerInput(Unit) {
                    awaitPointerEventScope {
                        while (true) {
                            val event = awaitPointerEvent(PointerEventPass.Initial)
                            val down = event.changes.firstOrNull { it.pressed }
                            if (down != null) touchOffset = down.position
                        }
                    }
                }
                .graphicsLayer {
                    val bounce = 1f + 0.05f * pressFraction
                    scaleX = bounce
                    scaleY = 1f - 0.035f * pressFraction
                }
                .clickable(
                    interactionSource = interactionSource,
                    indication = null,
                    onClick = onClick
                )
                .seaGlass(
                    stageState = stageState,
                    material = currentMaterial,
                    touchPointInLocal = touchOffset,
                    touchIntensity = pressFraction,
                    customContourPathBuilder = { wPx, hPx, outPath ->
                        SeaGlassShapeMorph.buildMorphedPolygonPath(
                            startKind = restingShape,
                            endKind = activeShape,
                            progress = morphProgress.coerceIn(0f, 1f),
                            widthPx = wPx,
                            heightPx = hPx,
                            outPath = outPath
                        )
                    }
                ),
            content = content
        )
    }
}

/**
 * Apple Liquid Glass `GlassEffectContainer` + `.glassEffectID` Budding Action Cluster.
 *
 * When [expanded] is false, renders as a single compact droplet. When [expanded] becomes true,
 * satellite droplets **bud out of the anchor's center**, stretching live `sminCubic` liquid
 * bridges as they emerge and settling with staggered Material 3 Expressive spatial springs!
 */
@Composable
fun SeaGlassBuddingCluster(
    expanded: Boolean,
    onToggleExpanded: () -> Unit,
    items: List<Pair<String, String>>,
    onItemClick: (Int) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    anchorMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    satelliteMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass
) {
    val density = LocalDensity.current

    val expandProgress by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.56f, stiffness = 320f),
        label = "budClusterExpand"
    )

    var containerWidthPx by remember { mutableFloatStateOf(960f) }
    val clusterHeight = 96.dp
    val clusterHeightPx = with(density) { clusterHeight.toPx() }
    val centerY = clusterHeightPx * 0.5f

    val anchorRadiusPx = with(density) { 36.dp.toPx() }
    val satRadiusPx = with(density) { 34.dp.toPx() }
    val count = items.size.coerceIn(1, 3)

    // Anchor sits on the left; satellites bud out to the right with staggered spring travel
    val anchorCenterX = anchorRadiusPx + with(density) { 12.dp.toPx() }
    val stepSpanPx = ((containerWidthPx - anchorCenterX - satRadiusPx - with(density) { 16.dp.toPx() }) /
        count.toFloat()).coerceAtLeast(satRadiusPx * 2.1f)

    val activeNodes = remember(
        containerWidthPx,
        clusterHeightPx,
        expandProgress,
        anchorMaterial,
        satelliteMaterial,
        count
    ) {
        val list = ArrayList<SeaGlassSdfEngine.SdfNode>(count + 1)
        list.add(
            SeaGlassSdfEngine.SdfNode(
                centerX = anchorCenterX,
                centerY = centerY,
                halfWidth = anchorRadiusPx,
                halfHeight = anchorRadiusPx,
                uniformCornerRadius = anchorRadiusPx,
                cornerSmoothing = 2.2f,
                tintColorArgb = anchorMaterial.tintColorArgb,
                thicknessPx = anchorRadiusPx * 0.65f
            )
        )
        if (expandProgress > 0.02f) {
            for (i in 0 until count) {
                val stagger = ((expandProgress - i * 0.08f) / (1f - i * 0.08f)).coerceIn(0f, 1f)
                val r = satRadiusPx * (0.45f + 0.55f * stagger)
                val cx = anchorCenterX + stepSpanPx * (i + 1) * stagger
                list.add(
                    SeaGlassSdfEngine.SdfNode(
                        centerX = cx,
                        centerY = centerY,
                        halfWidth = r * 1.24f,
                        halfHeight = r,
                        uniformCornerRadius = r,
                        cornerSmoothing = 2.2f,
                        tintColorArgb = if (i % 2 == 0) {
                            satelliteMaterial.tintColorArgb
                        } else {
                            anchorMaterial.tintColorArgb
                        },
                        thicknessPx = r * 0.65f
                    )
                )
            }
        }
        list
    }

    val labelColor = remember(anchorMaterial.tintColorArgb, anchorMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                anchorMaterial.tintColorArgb,
                anchorMaterial.tintDensity
            )
        )
    }

    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(clusterHeight)
            .onSizeChanged { containerWidthPx = it.width.toFloat() }
            .seaGlass(
                stageState = stageState,
                material = anchorMaterial,
                nodesOptional = activeNodes,
                smoothRadiusDp = 46f
            )
    ) {
        // Anchor Toggle Orb
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset {
                    IntOffset(
                        (anchorCenterX - anchorRadiusPx).roundToInt(),
                        (centerY - anchorRadiusPx).roundToInt()
                    )
                }
                .size(with(density) { (anchorRadiusPx * 2f).toDp() })
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onToggleExpanded
                )
        ) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text(
                    text = if (expanded) "✕" else "✦",
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black),
                    color = labelColor
                )
                Text(
                    text = if (expanded) "FOLD" else "BUD",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 8.sp
                    ),
                    color = labelColor.copy(alpha = 0.80f)
                )
            }
        }

        // Budding Satellite Droplets
        if (expandProgress > 0.12f) {
            for (i in 0 until count) {
                val stagger = ((expandProgress - i * 0.08f) / (1f - i * 0.08f)).coerceIn(0f, 1f)
                val cx = anchorCenterX + stepSpanPx * (i + 1) * stagger
                val item = items[i]
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .offset {
                            IntOffset(
                                (cx - satRadiusPx * 1.18f).roundToInt(),
                                (centerY - satRadiusPx).roundToInt()
                            )
                        }
                        .size(
                            width = with(density) { (satRadiusPx * 2.36f).toDp() },
                            height = with(density) { (satRadiusPx * 2f).toDp() }
                        )
                        .alpha(stagger.coerceIn(0f, 1f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onItemClick(i) }
                        )
                ) {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Text(
                            text = item.first,
                            style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Black),
                            color = labelColor
                        )
                        Text(
                            text = item.second,
                            style = MaterialTheme.typography.labelSmall.copy(
                                fontWeight = FontWeight.Bold,
                                fontSize = 9.sp
                            ),
                            color = labelColor.copy(alpha = 0.82f)
                        )
                    }
                }
            }
        }
    }
}

/**
 * Interactive Liquid SeaGlass Slider whose thumb is a live 3D water-drop sea-glass lens
 * that magnifies the track underneath, stretches elastically with drag velocity, and blooms
 * with HDR light on touch.
 */
@Composable
fun SeaGlassLiquidSlider(
    title: String,
    valueText: String,
    value: Float,
    valueRange: ClosedFloatingPointRange<Float>,
    onValueChange: (Float) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    thumbMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current
    val span = (valueRange.endInclusive - valueRange.start).coerceAtLeast(1e-4f)
    val fraction = ((value - valueRange.start) / span).coerceIn(0f, 1f)

    var trackWidthPx by remember { mutableFloatStateOf(600f) }
    var isDragging by remember { mutableStateOf(false) }

    val dragBoost by animateFloatAsState(
        targetValue = if (isDragging) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "sliderDragBoost"
    )

    val labelColor = LocalContentColor.current

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodySmall.copy(fontWeight = FontWeight.SemiBold),
                color = labelColor
            )
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .background(labelColor.copy(alpha = 0.14f))
                    .padding(horizontal = 8.dp, vertical = 2.dp)
            ) {
                Text(
                    text = valueText,
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontFamily = FontFamily.Monospace,
                        fontWeight = FontWeight.Bold
                    ),
                    color = labelColor
                )
            }
        }

        val thumbWidthDp = 34.dp
        val thumbHeightDp = 22.dp
        val thumbWidthPx = with(density) { thumbWidthDp.toPx() }
        val usableTrackPx = (trackWidthPx - thumbWidthPx).coerceAtLeast(1f)
        val thumbLeftPx = usableTrackPx * fraction

        Box(
            contentAlignment = Alignment.CenterStart,
            modifier = Modifier
                .fillMaxWidth()
                .height(26.dp)
                .onSizeChanged { trackWidthPx = it.width.toFloat() }
                .pointerInput(valueRange) {
                    detectTapGestures { pos ->
                        val newFrac = ((pos.x - thumbWidthPx * 0.5f) / usableTrackPx).coerceIn(0f, 1f)
                        onValueChange(valueRange.start + newFrac * span)
                    }
                }
                .pointerInput(valueRange) {
                    detectDragGestures(
                        onDragStart = { pos ->
                            isDragging = true
                            val newFrac = ((pos.x - thumbWidthPx * 0.5f) / usableTrackPx).coerceIn(0f, 1f)
                            onValueChange(valueRange.start + newFrac * span)
                        },
                        onDragEnd = { isDragging = false },
                        onDragCancel = { isDragging = false },
                        onDrag = { change, _ ->
                            change.consume()
                            isDragging = true
                            val newFrac = ((change.position.x - thumbWidthPx * 0.5f) / usableTrackPx)
                                .coerceIn(0f, 1f)
                            onValueChange(valueRange.start + newFrac * span)
                        }
                    )
                }
        ) {
            // Capsule Track Background
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(10.dp)
                    .clip(CircleShape)
                    .background(labelColor.copy(alpha = 0.18f))
            ) {
                Box(
                    modifier = Modifier
                        .fillMaxWidth(fraction.coerceIn(0.03f, 1f))
                        .height(10.dp)
                        .clip(CircleShape)
                        .background(labelColor.copy(alpha = 0.72f))
                )
            }

            // 3D Water-Drop SeaGlass Lens Thumb (stretches & magnifies when dragged!)
            Box(
                modifier = Modifier
                    .offset { IntOffset(thumbLeftPx.roundToInt(), 0) }
                    .size(width = thumbWidthDp, height = thumbHeightDp)
                    .graphicsLayer {
                        scaleX = 1f + 0.24f * dragBoost
                        scaleY = 1f + 0.12f * dragBoost
                    }
                    .seaGlass(
                        stageState = stageState,
                        material = thumbMaterial.copy(
                            cornerRadiusDp = 14f,
                            thicknessDp = 18f + 6f * dragBoost,
                            transparency = (thumbMaterial.transparency + 0.16f).coerceAtMost(0.92f),
                            ior = (thumbMaterial.ior + 0.12f * dragBoost).coerceAtMost(1.88f),
                            chromaticDispersion = (thumbMaterial.chromaticDispersion + 0.18f * dragBoost)
                                .coerceAtMost(1f)
                        ),
                        touchPointInLocal = Offset(thumbWidthPx * 0.5f, with(density) { 11.dp.toPx() }),
                        touchIntensity = dragBoost
                    )
            )
        }
    }
}
