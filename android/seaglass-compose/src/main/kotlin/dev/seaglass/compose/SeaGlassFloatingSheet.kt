package dev.seaglass.compose

import android.graphics.Shader
import android.os.Build
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.SizeTransform
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
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.drawscope.clipPath
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.input.nestedscroll.NestedScrollConnection
import androidx.compose.ui.input.nestedscroll.NestedScrollSource
import androidx.compose.ui.input.nestedscroll.nestedScroll
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.Velocity
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.heightIn
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassShapeMorph
import dev.seaglass.core.backend.SeaGlassShaderProgramCache
import kotlinx.coroutines.launch
import kotlin.math.abs
import kotlin.math.max
import kotlin.math.min
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
private class MorphLensClipHolder {
    var curW by mutableFloatStateOf(0f)
    var curH by mutableFloatStateOf(0f)
    var endW by mutableFloatStateOf(0f)
    var endH by mutableFloatStateOf(0f)
    var animatedCornerDp by mutableFloatStateOf(42f)
    var glassRootOrigin by mutableStateOf(Offset.Zero)
}

/**
 * Reusable Trigger-to-Card SeaGlass Lens Morph Container (`SeaGlassMorphContainer`).
 *
 * Measures [content] once at its resting card dimensions `(endW, endH)` so text and rows never
 * reflow, while animating the `SeaGlass` optical lens directly from [triggerRectInRoot] `(startW, startH)`
 * into the resting card `(endW, endH)`. Clips the inner content to the exact animated glass contour.
 */
@Composable
fun SeaGlassMorphContainer(
    morphProgress: Float,
    effectsProgress: Float,
    triggerRectInRoot: Rect?,
    stageState: SeaGlassStageState?,
    material: SeaGlassMaterial,
    concentricCorners: Boolean = false,
    extraTranslationY: Float = 0f,
    dragScale: Float = 1f,
    contentPadding: PaddingValues = PaddingValues(0.dp),
    triggerGhost: (@Composable BoxScope.() -> Unit)? = null,
    lensOverlay: (@Composable BoxScope.(curW: Float, curH: Float, endW: Float, endH: Float, morphProgress: Float) -> Unit)? = null,
    modifier: Modifier = Modifier,
    content: @Composable BoxScope.() -> Unit
) {
    val density = LocalDensity.current
    var restingOriginInRoot by remember { mutableStateOf(Offset.Zero) }
    val clipHolder = remember { MorphLensClipHolder() }
    val clipPath = remember { Path() }

    val p = morphProgress.coerceAtLeast(0f)
    val clampedP = SeaGlassShapeMorph.sCurve(p)
    val shaped = SeaGlassShapeMorph.sCurveWindow(p, start = 0.08f, end = 0.96f)
    val baseCornerDp = material.cornerRadiusDp
    val startCornerDp = if (triggerRectInRoot != null) {
        (min(triggerRectInRoot.width, triggerRectInRoot.height) * 0.5f / density.density).coerceIn(18f, 44f)
    } else {
        min(baseCornerDp, 32f)
    }
    val animatedCornerDp = startCornerDp + (baseCornerDp - startCornerDp) * shaped
    clipHolder.animatedCornerDp = animatedCornerDp

    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = material.tintColorArgb,
                tintDensity = material.tintDensity
            )
        )
    }

    val contentAlpha = if (triggerGhost != null || lensOverlay != null) {
        SeaGlassShapeMorph.sCurveWindow(p, start = 0.16f, end = 0.72f)
    } else {
        SeaGlassShapeMorph.sCurveWindow(p, start = 0.08f, end = 0.64f)
    }
    val ghostAlpha = 1f - SeaGlassShapeMorph.sCurveWindow(p, start = 0.04f, end = 0.34f)
    val glassAlpha = if (triggerGhost != null && triggerRectInRoot != null) {
        1f
    } else {
        SeaGlassShapeMorph.sCurveWindow(max(p, effectsProgress), start = 0.0f, end = 0.18f)
    }
    val topAnchoredContent = lensOverlay == null

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Layout(
            modifier = modifier.onGloballyPositioned { coords ->
                restingOriginInRoot = coords.positionInRoot()
            },
            content = {
                // Child 0: Moving/morphing SeaGlass optical lens (keeps material stable so it never recomposes per frame)
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .graphicsLayer {
                            alpha = glassAlpha
                            compositingStrategy = if (glassAlpha < 0.995f) {
                                CompositingStrategy.Offscreen
                            } else {
                                CompositingStrategy.Auto
                            }
                        }
                        .seaGlass(
                            stageState = stageState,
                            material = material,
                            cornerAdaptationMode = if (concentricCorners) {
                                SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY
                            } else {
                                SeaGlassCornerAdaptationMode.FIXED
                            },
                            cornerMask = SeaGlassCornerMask.ALL,
                            cornerProximityThresholdDp = 64f,
                            allowAsymmetricCorners = false,
                            overrideOriginInRoot = { clipHolder.glassRootOrigin },
                            dynamicCornerRadiusDp = { clipHolder.animatedCornerDp }
                        )
                )

                // Child 1: Full-size card content, scaled and clipped to the exact lens contour
                Box(
                    contentAlignment = Alignment.TopCenter,
                    modifier = Modifier
                        .seaGlassEtchedSiblingContent(material.tintColorArgb, material.coarseness)
                        .drawWithContent {
                            if (contentAlpha <= 0.005f) return@drawWithContent
                            val cW = clipHolder.curW
                            val cH = clipHolder.curH
                            val eW = clipHolder.endW.coerceAtLeast(1f)
                            val eH = clipHolder.endH.coerceAtLeast(1f)
                            if (cW > 1f && cH > 1f) {
                                val localClipW = min(eW, cW)
                                val localClipH = min(eH, cH)
                                val left = (eW - localClipW) * 0.5f
                                val top = if (topAnchoredContent) 0f else (eH - localClipH) * 0.5f
                                val cornerPx = min(
                                    clipHolder.animatedCornerDp * density.density,
                                    min(localClipW, localClipH) * 0.5f
                                )
                                clipPath.reset()
                                clipPath.addRoundRect(
                                    RoundRect(
                                        left = left,
                                        top = top,
                                        right = left + localClipW,
                                        bottom = top + localClipH,
                                        cornerRadius = CornerRadius(cornerPx, cornerPx)
                                    )
                                )
                                clipPath(clipPath) {
                                    this@drawWithContent.drawContent()
                                }
                            } else {
                                this@drawWithContent.drawContent()
                            }
                        }
                        .graphicsLayer {
                            val eW = clipHolder.endW.coerceAtLeast(1f)
                            val eH = clipHolder.endH.coerceAtLeast(1f)
                            val fitW = (clipHolder.curW / eW).coerceIn(0.16f, 1.05f)
                            val fitH = (clipHolder.curH / eH).coerceIn(0.16f, 1.05f)
                            transformOrigin = if (topAnchoredContent) {
                                TransformOrigin(0.5f, 0f)
                            } else {
                                TransformOrigin(0.5f, 0.5f)
                            }
                            alpha = contentAlpha
                            scaleX = fitW
                            scaleY = if (topAnchoredContent) max(fitW, fitH) else fitW
                        }
                        .padding(contentPadding),
                    content = content
                )

                // Child 2 (optional): Trigger ghost glyph or continuous lens overlay riding inside the glass bounds
                if (triggerGhost != null || lensOverlay != null) {
                    Box(
                        contentAlignment = Alignment.Center,
                        modifier = Modifier
                            .fillMaxSize()
                            .graphicsLayer {
                                alpha = glassAlpha
                            }
                    ) {
                        if (triggerGhost != null && ghostAlpha > 0.005f) {
                            Box(
                                contentAlignment = Alignment.Center,
                                modifier = Modifier
                                    .fillMaxSize()
                                    .graphicsLayer {
                                        alpha = ghostAlpha
                                    },
                                content = triggerGhost
                            )
                        }
                        if (lensOverlay != null) {
                            lensOverlay(
                                clipHolder.curW,
                                clipHolder.curH,
                                clipHolder.endW,
                                clipHolder.endH,
                                p
                            )
                        }
                    }
                }
            }
        ) { measurables, constraints ->
            val contentPlaceable = measurables[1].measure(constraints.copy(minWidth = 0))
            val endW = contentPlaceable.width.toFloat().coerceAtLeast(1f)
            val endH = contentPlaceable.height.toFloat().coerceAtLeast(1f)

            val travelX = SeaGlassShapeMorph.sCurveWindow(p, start = 0.0f, end = 0.94f, overshootGain = 0.28f)
            val travelY = SeaGlassShapeMorph.sCurveWindow(p, start = 0.0f, end = 0.92f, overshootGain = 0.28f)
            val spreadW = SeaGlassShapeMorph.sCurveWindow(p, start = 0.0f, end = 0.96f, overshootGain = 0.26f)
            val spreadH = SeaGlassShapeMorph.sCurveWindow(p, start = 0.04f, end = 1.00f, overshootGain = 0.30f)
            val bounce = 1f + (p - 1f).coerceIn(-0.02f, 0.02f)

            val startW = triggerRectInRoot?.width?.coerceAtLeast(28f) ?: (endW * 0.88f).coerceAtLeast(48f * density.density)
            val startH = triggerRectInRoot?.height?.coerceAtLeast(28f) ?: (endH * 0.72f).coerceAtLeast(48f * density.density)
            val maxW = max(28f, endW * 1.08f)
            val maxH = max(28f, endH * 1.08f)
            val curW = ((startW + (endW - startW) * spreadW) * dragScale * bounce).coerceIn(28f, maxW)
            val curH = ((startH + (endH - startH) * spreadH) * dragScale * bounce).coerceIn(28f, maxH)

            clipHolder.curW = curW
            clipHolder.curH = curH
            clipHolder.endW = endW
            clipHolder.endH = endH

            val glassWInt = curW.roundToInt().coerceAtLeast(1)
            val glassHInt = curH.roundToInt().coerceAtLeast(1)
            val glassPlaceable = measurables[0].measure(Constraints.fixed(glassWInt, glassHInt))
            val ghostPlaceable = if (measurables.size > 2) {
                measurables[2].measure(Constraints.fixed(glassWInt, glassHInt))
            } else null

            layout(contentPlaceable.width, contentPlaceable.height) {
                val liveOrigin = coordinates?.positionInRoot()?.also {
                    if (it.x != restingOriginInRoot.x || it.y != restingOriginInRoot.y) {
                        restingOriginInRoot = it
                    }
                } ?: restingOriginInRoot

                val endCx = liveOrigin.x + endW * 0.5f
                val endCy = liveOrigin.y + endH * 0.5f
                val startCx = triggerRectInRoot?.center?.x ?: endCx
                val startCy = triggerRectInRoot?.center?.y ?: (endCy + extraTranslationY + 140f * density.density)

                val curCx = startCx + (endCx - startCx) * travelX
                val curCy = startCy + (endCy + extraTranslationY - startCy) * travelY
                clipHolder.glassRootOrigin = Offset(curCx - curW * 0.5f, curCy - curH * 0.5f)

                val glassX = (curCx - liveOrigin.x - curW * 0.5f).roundToInt()
                val glassY = (curCy - liveOrigin.y - curH * 0.5f).roundToInt()
                val contentX = (curCx - liveOrigin.x - endW * 0.5f).roundToInt()
                val contentY = if (topAnchoredContent) {
                    glassY
                } else {
                    (curCy - liveOrigin.y - endH * 0.5f).roundToInt()
                }

                glassPlaceable.place(glassX, glassY)
                contentPlaceable.place(contentX, contentY)
                ghostPlaceable?.place(glassX, glassY)
            }
        }
    }
}

@Composable
fun SeaGlassFloatingSheet(
    visible: Boolean,
    onDismissRequest: () -> Unit,
    modifier: Modifier = Modifier,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass.copy(
        cornerRadiusDp = 42f,
        thicknessDp = 22f
    ),
    outerInsetDp: Dp = 10.dp,
    enableMorphAnimation: Boolean = true,
    morphTransformOrigin: TransformOrigin = TransformOrigin(0.5f, 0.92f),
    triggerRectInRoot: Rect? = null,
    triggerGhost: (@Composable BoxScope.() -> Unit)? = null,
    scrimMaxAlpha: Float = 0.22f,
    content: @Composable ColumnScope.() -> Unit
) {
    val density = LocalDensity.current
    val scope = rememberCoroutineScope()
    val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()

    var isFullPage by remember { mutableStateOf(false) }
    var dismissedBySwipe by remember { mutableStateOf(false) }
    val expansion = remember { Animatable(0f) }
    val dismissDrag = remember { Animatable(0f) }

    androidx.compose.runtime.LaunchedEffect(visible) {
        if (visible) {
            isFullPage = false
            dismissedBySwipe = false
            expansion.snapTo(0f)
            dismissDrag.snapTo(0f)
        }
    }

    val motion = LocalSeaGlassMotionScheme.current
    val targetProgress = if (visible) 1f else 0f
    val morphProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = if (visible) {
            motion.morphOpenSpatial
        } else {
            motion.morphCloseSpatial
        },
        label = "floatingSheetMorph"
    )
    val effectsProgress = SeaGlassShapeMorph.sCurve(morphProgress)

    if (morphProgress <= 0.04f && !visible) {
        return
    }

    val dismissThresholdPx = with(density) { 96.dp.toPx() }
    val expandThresholdPx = with(density) { 48.dp.toPx() }
    val expandSpanPx = with(density) { 210.dp.toPx() }

    val liveExpansion = SeaGlassShapeMorph.sCurve(expansion.value.coerceIn(0f, 1f))
    val dismissDragPx = dismissDrag.value.coerceAtLeast(0f)
    val dragFraction = (dismissDragPx / dismissThresholdPx).coerceIn(0f, 1f)

    fun applyVerticalDrag(dy: Float) {
        if (dy == 0f || !visible) return
        scope.launch {
            if (dy < 0f) {
                var remainingUp = -dy
                if (dismissDrag.value > 0f) {
                    val pullBack = min(dismissDrag.value, remainingUp * 0.88f)
                    dismissDrag.snapTo((dismissDrag.value - pullBack).coerceAtLeast(0f))
                    remainingUp -= pullBack / 0.88f
                }
                if (remainingUp > 0f) {
                    val deltaExp = (remainingUp * 0.88f) / expandSpanPx
                    expansion.snapTo((expansion.value + deltaExp).coerceIn(0f, 1f))
                }
            } else {
                var remainingDown = dy
                if (expansion.value > 0f) {
                    val deltaExp = (remainingDown * 0.88f) / expandSpanPx
                    val usedExp = min(expansion.value, deltaExp)
                    expansion.snapTo((expansion.value - usedExp).coerceIn(0f, 1f))
                    remainingDown -= (usedExp * expandSpanPx) / 0.88f
                }
                if (remainingDown > 0f) {
                    dismissDrag.snapTo((dismissDrag.value + remainingDown * 0.85f).coerceAtLeast(0f))
                }
            }
        }
    }

    fun settleDrag(velocityY: Float = 0f) {
        if (!visible) return
        val curDismiss = dismissDrag.value
        val curExp = expansion.value
        when {
            curDismiss > 0f -> {
                if (curDismiss > dismissThresholdPx || (curDismiss > expandThresholdPx * 0.5f && velocityY > 650f)) {
                    dismissedBySwipe = true
                    onDismissRequest()
                } else {
                    scope.launch {
                        dismissDrag.animateTo(0f, spring(dampingRatio = 0.84f, stiffness = 560f))
                    }
                }
            }
            else -> {
                val expandThresholdFrac = (expandThresholdPx / expandSpanPx).coerceIn(0.14f, 0.35f)
                val nextFullPage = if (!isFullPage) {
                    curExp >= expandThresholdFrac || velocityY < -520f
                } else {
                    !(curExp <= 1f - expandThresholdFrac || velocityY > 420f)
                }
                isFullPage = nextFullPage
                val targetExp = if (nextFullPage) 1f else 0f
                scope.launch {
                    expansion.animateTo(targetExp, spring(dampingRatio = 0.84f, stiffness = 520f))
                }
            }
        }
    }

    val nestedScrollConnection = remember(visible, onDismissRequest) {
        object : NestedScrollConnection {
            override fun onPreScroll(available: Offset, source: NestedScrollSource): Offset {
                if (source != NestedScrollSource.UserInput || !visible) return Offset.Zero
                val dy = available.y
                // Swiping up while compact or while pulled down: return to compact / expand toward full page first
                if (dy < 0f && (dismissDrag.value > 0f || expansion.value < 0.995f)) {
                    applyVerticalDrag(dy)
                    return Offset(0f, dy)
                }
                // Swiping down while mid-expansion or already pulling down: move the sheet first
                if (dy > 0f && (dismissDrag.value > 0f || (expansion.value > 0.005f && expansion.value < 0.995f))) {
                    applyVerticalDrag(dy)
                    return Offset(0f, dy)
                }
                return Offset.Zero
            }

            override fun onPostScroll(
                consumed: Offset,
                available: Offset,
                source: NestedScrollSource
            ): Offset {
                if (source != NestedScrollSource.UserInput || !visible) return Offset.Zero
                val dy = available.y
                // Unconsumed downward scroll (user is at top of scroll view and pulls down)
                if (dy > 0f) {
                    applyVerticalDrag(dy)
                    return Offset(0f, dy)
                }
                // Unconsumed upward scroll while not yet full page
                if (dy < 0f && expansion.value < 0.995f) {
                    applyVerticalDrag(dy)
                    return Offset(0f, dy)
                }
                return Offset.Zero
            }

            override suspend fun onPreFling(available: Velocity): Velocity {
                if (dismissDrag.value > 2f || (expansion.value > 0.005f && expansion.value < 0.995f)) {
                    settleDrag(available.y)
                    return available
                }
                return Velocity.Zero
            }

            override suspend fun onPostFling(consumed: Velocity, available: Velocity): Velocity {
                if (dismissDrag.value > 1f || (expansion.value > 0.005f && expansion.value < 0.995f) || (!isFullPage && available.y < -520f)) {
                    settleDrag(available.y)
                    return available
                }
                return Velocity.Zero
            }
        }
    }

    BoxWithConstraints(
        modifier = modifier
            .fillMaxSize()
            .zIndex(80f)
    ) {
        val topBarPad = systemBarsPadding.calculateTopPadding()
        val bottomBarPad = systemBarsPadding.calculateBottomPadding()
        val activeInsetDp = outerInsetDp * (1f - liveExpansion * 0.35f)
        val fullSheetHeight = (maxHeight - topBarPad - bottomBarPad - activeInsetDp * 2).coerceAtLeast(320.dp)
        val compactMaxHeight = (maxHeight * 0.72f).coerceAtLeast(320.dp)
        val currentMaxHeight = compactMaxHeight + (fullSheetHeight - compactMaxHeight) * liveExpansion
        val currentMinHeight = fullSheetHeight * liveExpansion
        val screenHeightPx = with(density) { maxHeight.toPx() }
        // When dismissed via downward swipe, never reverse upward into a top-bar trigger icon.
        val effectiveTriggerRect = if (dismissedBySwipe && (triggerRectInRoot == null || triggerRectInRoot.center.y < screenHeightPx * 0.65f)) {
            null
        } else {
            triggerRectInRoot
        }

        // 1. Interactive backdrop scrim
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    Color.Black.copy(
                        alpha = (scrimMaxAlpha * effectsProgress * (1f - dragFraction * 0.5f)).coerceIn(0f, 0.6f)
                    )
                )
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onDismissRequest
                )
        )

        // 2. Floating Concentric SeaGlass Sheet Container that morphs from triggerRectInRoot and expands to full page
        Box(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .padding(
                    start = activeInsetDp,
                    end = activeInsetDp,
                    top = topBarPad + activeInsetDp,
                    bottom = bottomBarPad + activeInsetDp
                )
                .nestedScroll(nestedScrollConnection)
                .pointerInput(visible, onDismissRequest) {
                    detectVerticalDragGestures(
                        onDragEnd = { settleDrag(0f) },
                        onDragCancel = { settleDrag(0f) },
                        onVerticalDrag = { change, dragAmount ->
                            change.consume()
                            applyVerticalDrag(dragAmount)
                        }
                    )
                }
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = {}
                )
        ) {
            SeaGlassMorphContainer(
                morphProgress = if (enableMorphAnimation) morphProgress else 1f,
                effectsProgress = if (enableMorphAnimation) effectsProgress else 1f,
                triggerRectInRoot = effectiveTriggerRect,
                stageState = stageState,
                material = material,
                concentricCorners = true,
                extraTranslationY = dismissDragPx,
                dragScale = 1f,
                contentPadding = PaddingValues(horizontal = 18.dp, vertical = 12.dp),
                triggerGhost = if (effectiveTriggerRect != null) triggerGhost else null,
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(
                        min = currentMinHeight,
                        max = currentMaxHeight
                    )
            ) {
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    // Top Centered iOS-Style Grabber Handle Pill (tap or drag to toggle full page)
                    Box(
                        contentAlignment = Alignment.Center,
                        modifier = Modifier
                            .align(Alignment.CenterHorizontally)
                            .clickable(
                                interactionSource = remember { MutableInteractionSource() },
                                indication = null,
                                onClick = {
                                    val next = !isFullPage
                                    isFullPage = next
                                    scope.launch {
                                        expansion.animateTo(
                                            if (next) 1f else 0f,
                                            spring(dampingRatio = 0.84f, stiffness = 520f)
                                        )
                                    }
                                }
                            )
                            .padding(horizontal = 24.dp, vertical = 4.dp)
                    ) {
                        Box(
                            modifier = Modifier
                                .size(width = 38.dp, height = 4.5.dp)
                                .clip(CircleShape)
                                .background(LocalContentColor.current.copy(alpha = 0.32f))
                        )
                    }

                    content()
                }
            }
        }
    }
}

/**
 * Concentric Device-Corner-Matched SeaGlass Sheet Surface (`SeaGlassFloatingSheetSurface`).
 * Can be used inside [SeaGlassFloatingSheet] or embedded directly as a native page card.
 */
@Composable
fun SeaGlassFloatingSheetSurface(
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass.copy(
        cornerRadiusDp = 42f,
        thicknessDp = 22f
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
                        contentColor.toArgb(),
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
    // Light ink means a dark sheet. Luminance is robust to tinted inks.
    val isDark = (0.2126f * contentColor.red + 0.7152f * contentColor.green + 0.0722f * contentColor.blue) > 0.5f
    val fillAlpha = if (isDark) 0.10f else 0.06f
    val borderAlpha = if (isDark) 0.14f else 0.10f

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
    uncheckedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    checkedTrackColor: Color? = null,
    uncheckedTrackColor: Color? = null
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
                    uncheckedMaterial = uncheckedMaterial,
                    checkedTrackColor = checkedTrackColor,
                    uncheckedTrackColor = uncheckedTrackColor
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
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
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

    val motion = LocalSeaGlassMotionScheme.current
    AnimatedContent(
        targetState = activeSubPage,
        transitionSpec = {
            (fadeIn(animationSpec = tween(200, easing = SeaGlassPhysicsSCurveEasing)) +
                scaleIn(
                    initialScale = 0.94f,
                    animationSpec = motion.subPageSpatial
                )).togetherWith(
                fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing)) +
                    scaleOut(targetScale = 0.96f, animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
            ).using(
                SizeTransform(clip = true) { _, _ -> motion.subPageSpatialSpec() }
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
                    uncheckedMaterial = dev.seaglass.core.SeaGlassPalette.RoseQuartzShell.primaryGlass.copy(
                        tintColorArgb = 0xFFF06E6A.toInt(),
                        tintDensity = 0.74f,
                        coarseness = 0.48f
                    )
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
