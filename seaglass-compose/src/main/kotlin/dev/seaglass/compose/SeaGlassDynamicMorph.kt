package dev.seaglass.compose

import android.graphics.Path
import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.Shader
import android.os.Build
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
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
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
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.asComposeRenderEffect
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
import dev.seaglass.core.SeaGlassShapeStage
import kotlinx.coroutines.delay
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
    anchorAlignment: Alignment = Alignment.Center,
    reserveCollapsedLayoutSpace: Boolean = false,
    collapsedContent: @Composable BoxScope.() -> Unit,
    expandedContent: @Composable BoxScope.() -> Unit
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current

    // Spatial spring (allows organic M3 Expressive bounce on geometry)
    val spatialProgress by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 360f),
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

    // Parabolic mid-flight energy (peaks at progress = 0.5)
    val inFlightEnergy = (4f * effectsProgress * (1f - effectsProgress)).coerceIn(0f, 1f)

    val flexMaterial = remember(
        collapsedMaterial,
        expandedMaterial,
        effectsProgress,
        pressFraction,
        currentAreaRatio,
        inFlightEnergy
    ) {
        val baseFlex = SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = collapsedMaterial,
            endMaterial = expandedMaterial,
            progress = effectsProgress,
            areaExpansionRatio = currentAreaRatio
        ).pressed(pressFraction)
        // Swell corner radius into a plump oval liquid droplet mid-flight (just like iOS 26 Liquid Glass!)
        baseFlex.copy(
            cornerRadiusDp = baseFlex.cornerRadiusDp + 22f * inFlightEnergy
        )
    }

    val curW = (collapsedMeasuredSize.width +
        (expandedMeasuredSize.width - collapsedMeasuredSize.width) * spatialProgress)
        .coerceAtLeast(32f)
    val curH = (collapsedMeasuredSize.height +
        (expandedMeasuredSize.height - collapsedMeasuredSize.height) * spatialProgress)
        .coerceAtLeast(32f)

    // Build transient 2-node sminCubic liquid teardrop bridge anchored to anchorAlignment!
    // Node 0 stays locked at the trigger button corner (e.g. TopEnd / BottomEnd / Center) while
    // Node 1 swells outward into the menu/popover/sheet—producing the exact liquid teardrop neck!
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
        morphMode,
        anchorAlignment
    ) {
        if (morphMode == SeaGlassMorphMode.LIQUID_COALESCE && inFlightEnergy > 0.04f) {
            val colW = collapsedMeasuredSize.width.toFloat().coerceAtLeast(32f)
            val colH = collapsedMeasuredSize.height.toFloat().coerceAtLeast(32f)

            val anchorHalfW = (colW * 0.5f * (1f - effectsProgress * 0.22f)).coerceAtMost(curW * 0.5f)
            val anchorHalfH = (colH * 0.5f * (1f - effectsProgress * 0.22f)).coerceAtMost(curH * 0.5f)

            val anchorCenterX = when (anchorAlignment) {
                Alignment.TopEnd, Alignment.BottomEnd, Alignment.CenterEnd ->
                    (curW - anchorHalfW).coerceAtLeast(anchorHalfW)
                Alignment.TopStart, Alignment.BottomStart, Alignment.CenterStart ->
                    anchorHalfW
                else -> curW * 0.5f
            }
            val anchorCenterY = when (anchorAlignment) {
                Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd ->
                    anchorHalfH
                Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd ->
                    (curH - anchorHalfH).coerceAtLeast(anchorHalfH)
                else -> curH * 0.5f
            }

            val targetHalfW = (curW * 0.5f * (0.42f + 0.58f * effectsProgress))
            val targetHalfH = (curH * 0.5f * (0.42f + 0.58f * effectsProgress))
            val lobeCenterX = anchorCenterX + (curW * 0.5f - anchorCenterX) * (0.35f + 0.65f * effectsProgress)
            val lobeCenterY = anchorCenterY + (curH * 0.5f - anchorCenterY) * (0.35f + 0.65f * effectsProgress)
            val ovalMidFlightRadius = min(targetHalfW, targetHalfH) * (0.42f + 0.52f * inFlightEnergy)

            listOf(
                SeaGlassSdfEngine.SdfNode(
                    centerX = anchorCenterX,
                    centerY = anchorCenterY,
                    halfWidth = anchorHalfW,
                    halfHeight = anchorHalfH,
                    uniformCornerRadius = min(anchorHalfW, anchorHalfH) * 0.95f,
                    cornerSmoothing = 2.4f,
                    tintColorArgb = collapsedMaterial.tintColorArgb,
                    thicknessPx = flexMaterial.thicknessDp * 2.8f
                ),
                SeaGlassSdfEngine.SdfNode(
                    centerX = lobeCenterX,
                    centerY = lobeCenterY,
                    halfWidth = targetHalfW,
                    halfHeight = targetHalfH,
                    uniformCornerRadius = ovalMidFlightRadius.coerceAtLeast(flexMaterial.cornerRadiusDp * 2f),
                    cornerSmoothing = 2.5f,
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
        val looseConstraints = if (reserveCollapsedLayoutSpace) {
            Constraints()
        } else {
            constraints.copy(minWidth = 0, minHeight = 0)
        }

        val collapsedPlaceables = subcompose("collapsed_measure") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor) {
                Box(content = collapsedContent)
            }
        }.map { it.measure(looseConstraints) }

        val expandedPlaceables = subcompose("expanded_measure") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor) {
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

        val originPivotX = when (anchorAlignment) {
            Alignment.TopEnd, Alignment.BottomEnd, Alignment.CenterEnd -> 1f
            Alignment.TopStart, Alignment.BottomStart, Alignment.CenterStart -> 0f
            else -> 0.5f
        }
        val originPivotY = when (anchorAlignment) {
            Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd -> 0f
            Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd -> 1f
            else -> 0.5f
        }

        val hostPlaceables = subcompose("morph_host") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor) {
                Box(
                    contentAlignment = anchorAlignment,
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
                            scaleX = 1f + 0.03f * pressFraction + 0.045f * inFlightEnergy
                            scaleY = 1f - 0.025f * pressFraction - 0.030f * inFlightEnergy
                            rotationX = -normY * 5.0f * pressFraction
                            rotationY = normX * 5.0f * pressFraction
                            cameraDistance = 14f * this.density
                        }
                        .seaGlass(
                            stageState = stageState,
                            material = flexMaterial,
                            nodesOptional = coalescingNodes,
                            smoothRadiusDp = 38f,
                            touchPointInLocal = touchOffset,
                            touchIntensity = max(pressFraction, inFlightEnergy * 0.85f),
                            customContourPathBuilder = polygonBuilder
                        )
                ) {
                    val collapsedAlpha = (1f - effectsProgress * 1.55f).coerceIn(0f, 1f)
                    val expandedAlpha = ((effectsProgress - 0.06f) * 1.12f).coerceIn(0f, 1f)

                    // In-flight Gaussian content defocus (peaks mid-morph, resolves to 0px at rest!)
                    val collapsedBlurPx = (effectsProgress * 13f * density.density).coerceAtLeast(0f)
                    val expandedBlurPx = ((1f - effectsProgress) * (1f - effectsProgress) * 16f * density.density)
                        .coerceAtLeast(0f)

                    if (collapsedAlpha > 0.01f) {
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .size(
                                    width = with(density) { cW.toDp() },
                                    height = with(density) { cH.toDp() }
                                )
                                .graphicsLayer {
                                    alpha = collapsedAlpha
                                    transformOrigin = TransformOrigin(originPivotX, originPivotY)
                                    val s = 0.85f + 0.15f * collapsedAlpha + 0.12f * effectsProgress
                                    scaleX = s
                                    scaleY = s
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && collapsedBlurPx > 0.6f) {
                                        compositingStrategy = CompositingStrategy.Offscreen
                                        renderEffect = AndroidRenderEffect.createBlurEffect(
                                            collapsedBlurPx,
                                            collapsedBlurPx,
                                            Shader.TileMode.DECAL
                                        ).asComposeRenderEffect()
                                    } else {
                                        renderEffect = null
                                    }
                                },
                            content = collapsedContent
                        )
                    }

                    if (expandedAlpha > 0.01f) {
                        val propScaleX = (animatedWidthPx.toFloat() / max(eW.toFloat(), 1f)).coerceIn(0.20f, 1.08f)
                        val propScaleY = (animatedHeightPx.toFloat() / max(eH.toFloat(), 1f)).coerceIn(0.20f, 1.08f)

                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .size(
                                    width = with(density) { eW.toDp() },
                                    height = with(density) { eH.toDp() }
                                )
                                .graphicsLayer {
                                    alpha = expandedAlpha
                                    transformOrigin = TransformOrigin(originPivotX, originPivotY)
                                    scaleX = propScaleX
                                    scaleY = propScaleY
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && expandedBlurPx > 0.6f) {
                                        compositingStrategy = CompositingStrategy.Offscreen
                                        renderEffect = AndroidRenderEffect.createBlurEffect(
                                            expandedBlurPx,
                                            expandedBlurPx,
                                            Shader.TileMode.DECAL
                                        ).asComposeRenderEffect()
                                    } else {
                                        renderEffect = null
                                    }
                                },
                            content = expandedContent
                        )
                    }
                }
            }
        }.map {
            it.measure(Constraints.fixed(animatedWidthPx, animatedHeightPx))
        }

        val layoutW = if (reserveCollapsedLayoutSpace) cW else animatedWidthPx
        val layoutH = if (reserveCollapsedLayoutSpace) cH else animatedHeightPx
        val placeX = if (reserveCollapsedLayoutSpace) {
            when (anchorAlignment) {
                Alignment.TopEnd, Alignment.BottomEnd, Alignment.CenterEnd -> cW - animatedWidthPx
                Alignment.TopStart, Alignment.BottomStart, Alignment.CenterStart -> 0
                else -> (cW - animatedWidthPx) / 2
            }
        } else {
            0
        }
        val placeY = if (reserveCollapsedLayoutSpace) {
            when (anchorAlignment) {
                Alignment.BottomStart, Alignment.BottomCenter, Alignment.BottomEnd -> cH - animatedHeightPx
                Alignment.TopStart, Alignment.TopCenter, Alignment.TopEnd -> 0
                else -> (cH - animatedHeightPx) / 2
            }
        } else {
            0
        }

        layout(layoutW, layoutH) {
            hostPlaceables.forEach {
                it.placeRelative(
                    x = placeX,
                    y = placeY,
                    zIndex = if (reserveCollapsedLayoutSpace && spatialProgress > 0.01f) 25f else 0f
                )
            }
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
    restingScale: Float = 1.0f,
    activeScale: Float = 1.06f,
    spinDegreesOnMorph: Float = 45f,
    viaCircleBridge: Boolean = true,
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
            isPressed -> (if (active) 0.58f else 0.42f)
            active -> 1f
            else -> 0f
        },
        animationSpec = spring(dampingRatio = 0.50f, stiffness = 340f),
        label = "morphBtnShapeProgress"
    )

    val startStage = remember(restingShape, restingScale, viaCircleBridge) {
        SeaGlassShapeStage(
            shape = restingShape,
            scale = restingScale,
            rotationDegrees = 0f,
            viaCircleBridge = viaCircleBridge
        )
    }
    val endStage = remember(activeShape, activeScale, spinDegreesOnMorph, viaCircleBridge) {
        SeaGlassShapeStage(
            shape = activeShape,
            scale = activeScale,
            rotationDegrees = spinDegreesOnMorph,
            viaCircleBridge = viaCircleBridge
        )
    }

    val choreo = remember(startStage, endStage, morphProgress) {
        SeaGlassShapeMorph.evaluateChoreographedTransform(
            startStage = startStage,
            endStage = endStage,
            progress = morphProgress
        )
    }

    val currentMaterial = remember(
        restingMaterial,
        activeMaterial,
        morphProgress,
        choreo.morphEnergy,
        pressFraction
    ) {
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = restingMaterial,
            endMaterial = activeMaterial,
            progress = morphProgress.coerceIn(0f, 1f),
            velocityPxPerSec = choreo.morphEnergy * 1400f
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

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
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
                    scaleX = choreo.scaleX * bounce
                    scaleY = choreo.scaleY * (1f - 0.035f * pressFraction)
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
                    touchIntensity = max(pressFraction, choreo.morphEnergy * 0.7f),
                    customContourPathBuilder = { wPx, hPx, outPath ->
                        SeaGlassShapeMorph.buildChoreographedPolygonPath(
                            startStage = startStage,
                            endStage = endStage,
                            progress = morphProgress,
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
 * Material 3 Expressive Choreographed Shape Shifter Orb (`Flower -> Small Circle -> Cookie9 ->
 * Large Circle -> Clover4 -> Liquid Pill -> Sunny -> Heart`).
 *
 * Implements the signature M3 Expressive **Morph-Down to Circle & Morph-Back-Out** choreography:
 * - Between any two lobed shapes, retracts petals inward into a taut `Circle` while dipping scale,
 *   twirls angularly, and blooms back out into the next shape (or into a small/large circle or pill)
 *   with under-damped spatial spring overshoot!
 * - Can run continuously in [autoPlay] loop mode or advance on tap.
 */
@Composable
fun SeaGlassChoreographedShapeOrb(
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    stages: List<SeaGlassShapeStage> = SeaGlassShapeMorph.DefaultPlayfulChoreography,
    autoPlay: Boolean = true,
    stepIntervalMillis: Long = 1750L,
    primaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    secondaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass
) {
    require(stages.size >= 2) { "stages must contain at least 2 keyframes" }

    var prevIndex by remember { mutableIntStateOf(0) }
    var targetIndex by remember { mutableIntStateOf(0) }
    val transitionProgress = remember { Animatable(1f) }
    val scope = rememberCoroutineScope()

    fun advanceTo(nextIdx: Int) {
        val clampedNext = (nextIdx + stages.size) % stages.size
        if (clampedNext == targetIndex && transitionProgress.value > 0.9f) {
            prevIndex = targetIndex
            targetIndex = (targetIndex + 1) % stages.size
        } else {
            prevIndex = targetIndex
            targetIndex = clampedNext
        }
        scope.launch {
            transitionProgress.snapTo(0f)
            transitionProgress.animateTo(
                targetValue = 1f,
                animationSpec = spring(
                    dampingRatio = 0.48f,
                    stiffness = 290f
                )
            )
        }
    }

    LaunchedEffect(autoPlay, stages.size, stepIntervalMillis) {
        if (autoPlay) {
            while (true) {
                delay(stepIntervalMillis)
                advanceTo((targetIndex + 1) % stages.size)
            }
        }
    }

    val startStage = stages[prevIndex.coerceIn(0, stages.size - 1)]
    val endStage = stages[targetIndex.coerceIn(0, stages.size - 1)]
    val rawP = transitionProgress.value

    val choreo = remember(startStage, endStage, rawP) {
        SeaGlassShapeMorph.evaluateChoreographedTransform(
            startStage = startStage,
            endStage = endStage,
            progress = rawP
        )
    }

    val activeMaterial = remember(
        primaryMaterial,
        secondaryMaterial,
        targetIndex,
        rawP,
        choreo.morphEnergy
    ) {
        val useAlt = targetIndex % 2 == 1
        val fromMat = if (useAlt) primaryMaterial else secondaryMaterial
        val toMat = if (useAlt) secondaryMaterial else primaryMaterial
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = fromMat,
            endMaterial = toMat,
            progress = rawP.coerceIn(0f, 1f),
            velocityPxPerSec = choreo.morphEnergy * 1500f,
            areaExpansionRatio = choreo.scaleX * choreo.scaleY
        )
    }

    val labelColor = remember(activeMaterial.tintColorArgb, activeMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                activeMaterial.tintColorArgb,
                activeMaterial.tintDensity
            )
        )
    }

    var touchOffset by remember { mutableStateOf<Offset?>(null) }

    Column(
        modifier = modifier.fillMaxWidth(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .fillMaxWidth()
                .height(142.dp)
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(116.dp)
                    .pointerInput(Unit) {
                        awaitPointerEventScope {
                            while (true) {
                                val event = awaitPointerEvent(PointerEventPass.Initial)
                                val down = event.changes.firstOrNull { it.pressed }
                                if (down != null) touchOffset = down.position
                            }
                        }
                    }
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = { advanceTo((targetIndex + 1) % stages.size) }
                    )
                    .graphicsLayer {
                        scaleX = choreo.scaleX
                        scaleY = choreo.scaleY
                        rotationZ = choreo.rotationDegrees
                    }
                    .seaGlass(
                        stageState = stageState,
                        material = activeMaterial,
                        touchPointInLocal = touchOffset,
                        touchIntensity = choreo.morphEnergy * 0.85f,
                        customContourPathBuilder = { wPx, hPx, outPath ->
                            SeaGlassShapeMorph.buildChoreographedPolygonPath(
                                startStage = startStage,
                                endStage = endStage,
                                progress = rawP,
                                widthPx = wPx,
                                heightPx = hPx,
                                outPath = outPath
                            )
                        }
                    )
            ) {
                // Counter-rotate inner typography so labels stay level while the glass shape twirls!
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.graphicsLayer {
                        rotationZ = -choreo.rotationDegrees
                        val invX = 1f / choreo.scaleX.coerceAtLeast(0.65f)
                        val invY = 1f / choreo.scaleY.coerceAtLeast(0.65f)
                        scaleX = invX.coerceIn(0.78f, 1.22f)
                        scaleY = invY.coerceIn(0.78f, 1.22f)
                    }
                ) {
                    Text(
                        text = endStage.label.uppercase( java.util.Locale.US ),
                        style = MaterialTheme.typography.labelMedium.copy(
                            fontWeight = FontWeight.Black,
                            fontSize = 11.sp
                        ),
                        color = labelColor
                    )
                    Text(
                        text = String.format(java.util.Locale.US, "%.2f× · Tap ↻", endStage.scale),
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = FontWeight.Bold,
                            fontSize = 9.sp
                        ),
                        color = labelColor.copy(alpha = 0.82f)
                    )
                }
            }
        }

        // Quick Stage Choreography Strip (Flower -> Small Circle -> Cookie 9 -> Large Orb -> Clover)
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            stages.take(5).forEachIndexed { idx, st ->
                SeaGlassPill(
                    label = st.label.substringBefore(" "),
                    selected = targetIndex == idx,
                    onClick = { advanceTo(idx) },
                    stageState = stageState,
                    selectedMaterial = primaryMaterial,
                    unselectedMaterial = secondaryMaterial.copy(transparency = 0.72f),
                    modifier = Modifier.weight(1f)
                )
            }
        }
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

/**
 * Multi-Stage Material 3 Expressive Shape Sequence Morpher.
 *
 * Smoothly morphs through an arbitrary ordered sequence of [shapes] (`List<SeaGlassShapeKind>`)
 * driven either by [activeShapeIndex] (animated with an under-damped spatial spring that allows
 * elastic vertex overshoot!) or a continuous [continuousIndexOverride] float.
 */
@Composable
fun SeaGlassMultiShapeMorph(
    shapes: List<SeaGlassShapeKind>,
    activeShapeIndex: Int,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    continuousIndexOverride: Float? = null,
    onClick: (() -> Unit)? = null,
    primaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    secondaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass,
    content: @Composable BoxScope.(currentShape: SeaGlassShapeKind) -> Unit
) {
    require(shapes.isNotEmpty()) { "shapes must not be empty" }
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()

    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "multiShapePress"
    )

    val safeIndex = activeShapeIndex.coerceIn(0, shapes.size - 1)
    val animatedIndex by animateFloatAsState(
        targetValue = safeIndex.toFloat(),
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 340f),
        label = "multiShapeContinuousIndex"
    )

    val effectiveContinuousIndex = continuousIndexOverride ?: animatedIndex
    val fractionalPart = abs(effectiveContinuousIndex - effectiveContinuousIndex.roundToInt())

    val blendedMaterial = remember(
        primaryMaterial,
        secondaryMaterial,
        effectiveContinuousIndex,
        fractionalPart,
        pressFraction
    ) {
        val colorWave = (kotlin.math.sin(effectiveContinuousIndex * Math.PI * 0.5).toFloat() * 0.5f + 0.5f)
            .coerceIn(0f, 1f)
        SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = primaryMaterial,
            endMaterial = secondaryMaterial,
            progress = colorWave,
            velocityPxPerSec = fractionalPart * 1200f
        ).pressed(pressFraction)
    }

    val contentColor = remember(blendedMaterial.tintColorArgb, blendedMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = blendedMaterial.tintColorArgb,
                tintDensity = blendedMaterial.tintDensity
            )
        )
    }

    var touchOffset by remember { mutableStateOf<Offset?>(null) }
    val currentDisplayShape = shapes[safeIndex]

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
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
                    val pulse = 1f + 0.04f * pressFraction
                    scaleX = pulse
                    scaleY = 1f - 0.03f * pressFraction
                }
                .then(
                    if (onClick != null) {
                        Modifier.clickable(
                            interactionSource = interactionSource,
                            indication = null,
                            onClick = onClick
                        )
                    } else {
                        Modifier
                    }
                )
                .seaGlass(
                    stageState = stageState,
                    material = blendedMaterial,
                    touchPointInLocal = touchOffset,
                    touchIntensity = max(pressFraction, fractionalPart * 0.8f),
                    customContourPathBuilder = { wPx, hPx, outPath ->
                        SeaGlassShapeMorph.buildSequenceMorphedPolygonPath(
                            shapes = shapes,
                            continuousIndex = effectiveContinuousIndex,
                            widthPx = wPx,
                            heightPx = hPx,
                            outPath = outPath
                        )
                    }
                ),
            content = { content(currentDisplayShape) }
        )
    }
}
