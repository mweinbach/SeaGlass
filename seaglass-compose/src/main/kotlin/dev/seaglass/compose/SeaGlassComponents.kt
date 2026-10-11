package dev.seaglass.compose

import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectTapGestures
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
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * Foundational SeaGlass container surface with Material 3 Expressive state morphing,
 * tactile press elasticity, and automatic Oklab contrast-safe foreground content color.
 */
@Composable
fun SeaGlassSurface(
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    onClick: (() -> Unit)? = null,
    contentPadding: PaddingValues = PaddingValues(0.dp),
    contentAlignment: Alignment = Alignment.TopStart,
    content: @Composable BoxScope.() -> Unit
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()

    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "surfacePress"
    )

    val effectiveMaterial by animateSeaGlassMaterialAsState(
        targetMaterial = material.pressed(pressFraction),
        motionScheme = motion
    )

    val scaleX = 1f + 0.025f * pressFraction
    val scaleY = 1f - 0.022f * pressFraction

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

    CompositionLocalProvider(LocalContentColor provides contentColor) {
        Box(
            contentAlignment = contentAlignment,
            modifier = modifier
                .graphicsLayer {
                    this.scaleX = scaleX
                    this.scaleY = scaleY
                }
                .then(clickableModifier)
                .seaGlass(
                    stageState = stageState,
                    material = effectiveMaterial
                )
                .padding(contentPadding),
            content = content
        )
    }
}

/**
 * Tactile tumbled sea-glass button that physically compresses on touch:
 * corners subtly morph, volumetric Beer-Lambert tint deepens, and the chalky
 * frosted rim brightens.
 */
@Composable
fun SeaGlassButton(
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    contentPadding: PaddingValues = PaddingValues(horizontal = 22.dp, vertical = 13.dp),
    content: @Composable RowScope.() -> Unit
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()

    val pressFraction by animateFloatAsState(
        targetValue = if (isPressed) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "buttonPress"
    )

    val animatedMaterial by animateSeaGlassMaterialAsState(
        targetMaterial = material.pressed(pressFraction),
        motionScheme = motion
    )

    val contentColor = remember(animatedMaterial.tintColorArgb, animatedMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = animatedMaterial.tintColorArgb,
                tintDensity = animatedMaterial.tintDensity
            )
        )
    }

    CompositionLocalProvider(LocalContentColor provides contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.Center,
            modifier = modifier
                .graphicsLayer {
                    scaleX = 1f + 0.045f * pressFraction
                    scaleY = 1f - 0.038f * pressFraction
                }
                .clickable(
                    interactionSource = interactionSource,
                    indication = null,
                    onClick = onClick
                )
                .seaGlass(
                    stageState = stageState,
                    material = animatedMaterial
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
        contentPadding = PaddingValues(horizontal = 18.dp, vertical = 10.dp),
        modifier = modifier
    ) {
        Text(
            text = label,
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
    content: @Composable ColumnScope.() -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material,
        onClick = onClick,
        contentPadding = PaddingValues(20.dp),
        modifier = modifier
    ) {
        Column(
            verticalArrangement = Arrangement.spacedBy(10.dp),
            content = content
        )
    }
}

/**
 * Liquid-Glass-inspired adaptive Material 3 Expressive navigation & action bar rendered in
 * frosted SeaGlass.
 *
 * Evaluates a multi-node coalescing SDF scene where:
 * - Node 0 is the main floating bar capsule
 * - Node 1 is the detachable / coalescing action FAB orb on the right (which merges into and
 *   splits out of the main bar via cubic `smin` as [actionExpanded] or selection state changes!)
 * - Inside the bar, an expressive primary sea-glass selection droplet glides with an under-damped
 *   spatial spring between items.
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
    onActionClick: (() -> Unit)? = null
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    val animatedIndex by animateFloatAsState(
        targetValue = selectedIndex.coerceIn(0, (items.size - 1).coerceAtLeast(0)).toFloat(),
        animationSpec = motion.defaultSpatial,
        label = "fluidBarSelection"
    )

    val actionSeparationDp by animateDpAsState(
        targetValue = if (actionDetached) 18.dp else 0.dp,
        animationSpec = spring(dampingRatio = 0.54f, stiffness = 300f),
        label = "actionOrbSeparation"
    )

    var totalWidthPx by remember { mutableFloatStateOf(0f) }
    var totalHeightPx by remember { mutableFloatStateOf(0f) }

    val barHeight = 64.dp
    val orbSize = 58.dp
    val barHeightPx = with(density) { barHeight.toPx() }
    val orbSizePx = with(density) { orbSize.toPx() }
    val gapPx = with(density) { actionSeparationDp.toPx() }

    val mainBarWidthPx = (totalWidthPx - orbSizePx - gapPx.coerceAtLeast(0f) - with(density) { 4.dp.toPx() })
        .coerceAtLeast(orbSizePx * 2f)

    // Construct the two coalescing SDF nodes: main capsule (Node 0) + action orb (Node 1)
    val coalescingNodes = remember(
        totalWidthPx,
        totalHeightPx,
        mainBarWidthPx,
        barHeightPx,
        orbSizePx,
        gapPx,
        barMaterial,
        indicatorMaterial
    ) {
        if (totalWidthPx <= 1f || totalHeightPx <= 1f) {
            emptyList()
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
                    thicknessPx = barHeightPx * 0.45f
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
                material = barMaterial,
                nodesOptional = coalescingNodes,
                smoothRadiusDp = 42f
            )
    ) {
        val mainBarWidthDp = with(density) { mainBarWidthPx.toDp() }
        val itemCount = items.size.coerceAtLeast(1)
        val innerPadding = 5.dp
        val trackWidthDp = (mainBarWidthDp - innerPadding * 2).coerceAtLeast(40.dp)
        val itemWidthDp = trackWidthDp / itemCount

        // Expressive sliding SeaGlass indicator pill inside the main bar with liquid velocity stretch
        Box(
            modifier = Modifier
                .width(mainBarWidthDp)
                .fillMaxHeight()
                .padding(innerPadding)
        ) {
            val indicatorOffsetX = with(density) {
                (itemWidthDp.toPx() * animatedIndex).roundToInt()
            }
            val motionStretch = kotlin.math.abs(selectedIndex - animatedIndex).coerceIn(0f, 1.2f)

            Box(
                modifier = Modifier
                    .offset { IntOffset(x = indicatorOffsetX, y = 0) }
                    .width(itemWidthDp)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleX = 1f + motionStretch * 0.34f
                        scaleY = 1f - motionStretch * 0.14f
                    }
                    .seaGlass(
                        stageState = stageState,
                        material = indicatorMaterial.copy(
                            cornerRadiusDp = 26f,
                            thicknessDp = 22f,
                            transparency = (indicatorMaterial.transparency + 0.12f).coerceAtMost(0.92f),
                            rimChalkiness = 0.95f
                        )
                    )
            )

            Row(
                modifier = Modifier.fillMaxSize(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                items.forEachIndexed { index, title ->
                    val selected = index == selectedIndex
                    val labelColor = if (selected) {
                        Color(
                            SeaGlassColorMath.optimalContentColorFor(
                                indicatorMaterial.tintColorArgb,
                                indicatorMaterial.tintDensity
                            )
                        )
                    } else {
                        Color(
                            SeaGlassColorMath.optimalContentColorFor(
                                barMaterial.tintColorArgb,
                                barMaterial.tintDensity
                            )
                        ).copy(alpha = 0.78f)
                    }

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
                        Text(
                            text = title,
                            color = labelColor,
                            style = MaterialTheme.typography.labelLarge.copy(
                                fontWeight = if (selected) FontWeight.Bold else FontWeight.SemiBold,
                                fontSize = 13.sp
                            )
                        )
                    }
                }
            }
        }

        // Coalescing Action Droplet Orb on the right
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
