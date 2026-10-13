package dev.seaglass.compose

import android.graphics.Shader
import android.os.Build
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.SizeTransform
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
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsPressedAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.asPaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
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
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassShapeMorph
import dev.seaglass.core.backend.SeaGlassShaderProgramCache

/**
 * Single item inside an iOS-style [SeaGlassContextMenuCard] (`UIMenu` / `.contextMenu` convention:
 * label on the left, vector [glyph] on the right, optional subtitle, destructive styling, or
 * nested [submenuItems] that morph the menu in-place when tapped).
 */
@Immutable
data class SeaGlassContextMenuItem(
    val id: String,
    val title: String,
    val glyph: SeaGlassGlyph,
    val subtitle: String? = null,
    val isDestructive: Boolean = false,
    val startsNewSection: Boolean = false,
    val checked: Boolean = false,
    val submenuTitle: String? = null,
    val submenuItems: List<SeaGlassContextMenuItem> = emptyList()
)

/**
 * Proper iOS-Style Context Menu with Target Preview Lift, Quick-Reaction Pill Bar,
 * Right-Aligned Vector Glyphs, Section Separators, and In-Place Submenu Morphing
 * (`SeaGlassContextMenuOverlay`).
 *
 * Replicates the complete iOS `UIContextMenuInteraction` / `.contextMenu` experience:
 * 1. **Lifted Target Preview Card**: The pressed/selected card floats sharp and elevated (`1.03x`
 *    spring bounce) above the dimmed & blurred screen.
 * 2. **Floating Quick-Reaction / Quick-Action Pill**: A separate frosted capsule above the preview
 *    with 5 instant actions (`Pin`, `Heart`, `Bookmark`, `Share`, `Export`).
 * 3. **iOS-Convention Menu Card**:
 *    - Label on the left, crisp vector [SeaGlassIcon] on the right (`›` chevron for submenus).
 *    - Thick translucent section dividers separating standard actions from bottom destructive actions.
 *    - **In-Place Submenu Morphing + Temporal De-Aging**: Tapping any item with [SeaGlassContextMenuItem.submenuItems]
 *      morphs the menu card in-place into the nested submenu (while de-aging the glass during motion
 *      and re-crystallizing the sand-frosted finish at rest).
 */
@Composable
fun SeaGlassContextMenuOverlay(
    visible: Boolean,
    onDismissRequest: () -> Unit,
    onItemSelected: (SeaGlassContextMenuItem) -> Unit,
    modifier: Modifier = Modifier,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass.copy(
        cornerRadiusDp = 26f,
        transparency = 0.54f,
        coarseness = 0.44f,
        frostRadiusDp = 30f,
        thicknessDp = 20f,
        rimChalkiness = 0.88f
    ),
    previewTitle: String = "Pacific Surf & Sea Glass",
    previewSubtitle: String = "Sand-Frosted Continuum · 64% Clear · 1.54 IOR",
    previewMeta: String = "pinned · gemini-4-argon-eap · 199K tokens",
    items: List<SeaGlassContextMenuItem> = defaultIosContextMenuItems()
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current
    val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()

    val targetProgress = if (visible) 1f else 0f
    val rawSpatialProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = if (visible) motion.morphOpenSpatial else motion.morphCloseSpatial,
        label = "iosContextMenuSpatial"
    )
    val spatialProgress = SeaGlassShapeMorph.physicsSCurve(rawSpatialProgress, overshootGain = 0.45f)
    val effectsProgress = SeaGlassShapeMorph.sCurve(rawSpatialProgress)

    if (effectsProgress <= 0.005f && !visible) {
        return
    }

    val inFlightMalleability = SeaGlassShapeMorph.sCurveBell(rawSpatialProgress)
    val activeMenuMaterial = remember(material, inFlightMalleability) {
        material.deaged(inFlightMalleability)
    }
    val blurPx = ((1f - effectsProgress) * (1f - effectsProgress) * 14f * density.density).coerceAtLeast(0f)

    var selectedQuickReaction by remember { mutableStateOf("heart") }

    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .fillMaxSize()
            .zIndex(85f)
    ) {
        // 1. Dimmed & tap-to-dismiss background scrim
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = (0.32f * effectsProgress).coerceIn(0f, 0.55f)))
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onDismissRequest
                )
        )

        // 2. Centered iOS Context Menu Stack: Quick-Reaction Pill -> Lifted Target Preview -> Menu Card
        Column(
            horizontalAlignment = Alignment.End,
            verticalArrangement = Arrangement.spacedBy(10.dp),
            modifier = Modifier
                .fillMaxWidth()
                .padding(
                    start = 24.dp,
                    end = 24.dp,
                    top = systemBarsPadding.calculateTopPadding() + 16.dp,
                    bottom = systemBarsPadding.calculateBottomPadding() + 16.dp
                )
                .graphicsLayer {
                    alpha = effectsProgress.coerceIn(0f, 1f)
                    val s = 0.84f + 0.16f * spatialProgress
                    scaleX = s
                    scaleY = s
                    transformOrigin = TransformOrigin(0.85f, 0.35f)
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && blurPx > 0.6f) {
                        compositingStrategy = CompositingStrategy.Offscreen
                        renderEffect = SeaGlassShaderProgramCache
                            .obtainBlurRenderEffect(blurPx, Shader.TileMode.DECAL)
                            .asComposeRenderEffect()
                    } else {
                        renderEffect = null
                    }
                }
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = {}
                )
        ) {
            // 2A. Floating Quick-Reaction / Quick-Action Pill Bar
            SeaGlassSurface(
                stageState = stageState,
                material = activeMenuMaterial.copy(cornerRadiusDp = 26f, thicknessDp = 18f),
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
                modifier = Modifier.align(Alignment.Start)
            ) {
                val quickReactions = remember {
                    listOf(
                        "pin" to SeaGlassGlyph.Pin,
                        "heart" to SeaGlassGlyph.Heart,
                        "sparkles" to SeaGlassGlyph.Sparkles,
                        "bookmark" to SeaGlassGlyph.Bookmark,
                        "export" to SeaGlassGlyph.Export
                    )
                }
                Row(
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    quickReactions.forEach { (id, glyph) ->
                        val active = id == selectedQuickReaction
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .size(36.dp)
                                .clip(CircleShape)
                                .background(
                                    if (active) Color(LocalSeaGlassPalette.current.primaryGlass.tintColorArgb).copy(alpha = 0.38f)
                                    else LocalContentColor.current.copy(alpha = 0.08f)
                                )
                                .clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = { selectedQuickReaction = id }
                                )
                        ) {
                            SeaGlassIcon(
                                glyph = glyph,
                                modifier = Modifier.size(18.dp),
                                tint = LocalContentColor.current
                            )
                        }
                    }
                }
            }

            // 2B. Lifted Target Preview Card (Platter)
            SeaGlassSurface(
                stageState = stageState,
                material = LocalSeaGlassPalette.current.primaryGlass.copy(
                    cornerRadiusDp = 28f,
                    thicknessDp = 22f
                ).deaged(inFlightMalleability * 0.7f),
                contentPadding = PaddingValues(horizontal = 20.dp, vertical = 16.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .graphicsLayer {
                        val liftBounce = 1f + 0.025f * spatialProgress
                        scaleX = liftBounce
                        scaleY = liftBounce
                    }
            ) {
                Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = "LIFTED TARGET PREVIEW",
                            style = MaterialTheme.typography.labelSmall.copy(
                                fontWeight = FontWeight.ExtraBold,
                                letterSpacing = 1.1.sp,
                                fontSize = 10.sp
                            ),
                            color = LocalContentColor.current.copy(alpha = 0.74f)
                        )
                        SeaGlassIcon(
                            glyph = SeaGlassGlyph.Sparkles,
                            modifier = Modifier.size(16.dp),
                            tint = LocalContentColor.current
                        )
                    }
                    Text(
                        text = previewTitle,
                        style = MaterialTheme.typography.titleMedium.copy(
                            fontWeight = FontWeight.Black,
                            fontSize = 18.sp
                        ),
                        color = LocalContentColor.current
                    )
                    Text(
                        text = previewSubtitle,
                        style = MaterialTheme.typography.bodySmall.copy(
                            fontWeight = FontWeight.Medium,
                            fontSize = 12.5.sp
                        ),
                        color = LocalContentColor.current.copy(alpha = 0.86f)
                    )
                    Text(
                        text = previewMeta,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontFamily = FontFamily.Monospace,
                            fontSize = 11.sp
                        ),
                        color = LocalContentColor.current.copy(alpha = 0.68f)
                    )
                }
            }

            // 2C. iOS Context Menu Card with Right-Aligned Icons & In-Place Submenu Morphing
            SeaGlassContextMenuCard(
                items = items,
                stageState = stageState,
                material = activeMenuMaterial,
                onItemSelected = { selected ->
                    onItemSelected(selected)
                    if (selected.submenuItems.isEmpty()) {
                        onDismissRequest()
                    }
                }
            )
        }
    }
}

/**
 * Standalone iOS-Style Context Menu Card (`SeaGlassContextMenuCard`) with label-left / icon-right
 * rows, thick section dividers, and in-place submenu morphing.
 */
@Composable
fun SeaGlassContextMenuCard(
    items: List<SeaGlassContextMenuItem>,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass.copy(
        cornerRadiusDp = 24f,
        transparency = 0.54f,
        coarseness = 0.44f,
        frostRadiusDp = 28f,
        thicknessDp = 20f
    ),
    onItemSelected: (SeaGlassContextMenuItem) -> Unit = {}
) {
    val motion = LocalSeaGlassMotionScheme.current
    var activeSubmenuParent by remember { mutableStateOf<SeaGlassContextMenuItem?>(null) }

    val rawSubmenuMorph by animateFloatAsState(
        targetValue = if (activeSubmenuParent != null) 1f else 0f,
        animationSpec = motion.subPageSpatial,
        label = "contextSubmenuMorph"
    )
    val submenuMalleability = SeaGlassShapeMorph.sCurveBell(rawSubmenuMorph)
    val activeMaterial = remember(material, submenuMalleability) {
        material.deaged(submenuMalleability)
    }

    SeaGlassSurface(
        stageState = stageState,
        material = activeMaterial,
        contentPadding = PaddingValues(vertical = 6.dp),
        modifier = modifier.width(268.dp)
    ) {
        AnimatedContent(
            targetState = activeSubmenuParent,
            transitionSpec = {
                (fadeIn(animationSpec = tween(200, easing = SeaGlassPhysicsSCurveEasing)) +
                    scaleIn(
                        initialScale = 0.93f,
                        animationSpec = motion.subPageSpatial
                    )).togetherWith(
                    fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing)) +
                        scaleOut(targetScale = 0.95f, animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
                ).using(
                    SizeTransform(clip = true) { _, _ -> motion.subPageSpatialSpec() }
                )
            },
            label = "iosMenuSubmenuContent"
        ) { submenuParent ->
            val contentColor = LocalContentColor.current
            if (submenuParent == null) {
                Column(modifier = Modifier.fillMaxWidth()) {
                    items.forEachIndexed { index, item ->
                        if (item.startsNewSection && index > 0) {
                            // Thick iOS-style translucent section separator bar
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(7.dp)
                                    .background(contentColor.copy(alpha = 0.08f))
                            )
                        } else if (index > 0) {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(0.6.dp)
                                    .background(contentColor.copy(alpha = 0.11f))
                            )
                        }

                        IosContextMenuRow(
                            item = item,
                            onClick = {
                                if (item.submenuItems.isNotEmpty()) {
                                    activeSubmenuParent = item
                                } else {
                                    onItemSelected(item)
                                }
                            }
                        )
                    }
                }
            } else {
                Column(modifier = Modifier.fillMaxWidth()) {
                    // Collapsed parent header row (tap to morph back to root menu)
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable(
                                interactionSource = remember { MutableInteractionSource() },
                                indication = null,
                                onClick = { activeSubmenuParent = null }
                            )
                            .padding(horizontal = 16.dp, vertical = 11.dp)
                    ) {
                        SeaGlassIcon(
                            glyph = SeaGlassGlyph.Back,
                            modifier = Modifier.size(16.dp),
                            tint = contentColor.copy(alpha = 0.65f)
                        )
                        Text(
                            text = submenuParent.submenuTitle ?: submenuParent.title,
                            color = contentColor.copy(alpha = 0.68f),
                            style = MaterialTheme.typography.labelLarge.copy(
                                fontWeight = FontWeight.Bold,
                                fontSize = 13.sp
                            ),
                            modifier = Modifier.weight(1f)
                        )
                    }

                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(7.dp)
                            .background(contentColor.copy(alpha = 0.08f))
                    )

                    submenuParent.submenuItems.forEachIndexed { idx, subItem ->
                        if (idx > 0) {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(0.6.dp)
                                    .background(contentColor.copy(alpha = 0.11f))
                            )
                        }
                        IosContextMenuRow(
                            item = subItem,
                            onClick = {
                                onItemSelected(subItem)
                                activeSubmenuParent = null
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun IosContextMenuRow(
    item: SeaGlassContextMenuItem,
    onClick: () -> Unit
) {
    val contentColor = LocalContentColor.current
    val interactionSource = remember { MutableInteractionSource() }
    val isPressed by interactionSource.collectIsPressedAsState()
    val rowColor = if (item.isDestructive) Color(0xFFD92D20) else contentColor

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween,
        modifier = Modifier
            .fillMaxWidth()
            .background(
                if (isPressed) contentColor.copy(alpha = 0.10f) else Color.Transparent
            )
            .clickable(
                interactionSource = interactionSource,
                indication = null,
                onClick = onClick
            )
            .padding(horizontal = 16.dp, vertical = 12.dp)
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.weight(1f)
        ) {
            if (item.checked) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Check,
                    modifier = Modifier.size(15.dp),
                    tint = rowColor
                )
            }
            Column {
                Text(
                    text = item.title,
                    color = rowColor,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Medium,
                        fontSize = 15.sp
                    )
                )
                if (!item.subtitle.isNullOrBlank()) {
                    Text(
                        text = item.subtitle,
                        color = rowColor.copy(alpha = 0.60f),
                        maxLines = 1,
                        style = MaterialTheme.typography.labelSmall.copy(fontSize = 11.sp)
                    )
                }
            }
        }

        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(4.dp)
        ) {
            SeaGlassIcon(
                glyph = item.glyph,
                modifier = Modifier.size(18.dp),
                tint = rowColor.copy(alpha = if (item.isDestructive) 0.95f else 0.85f)
            )
            if (item.submenuItems.isNotEmpty()) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.ChevronRight,
                    modifier = Modifier.size(14.dp),
                    tint = rowColor.copy(alpha = 0.45f)
                )
            }
        }
    }
}

/**
 * iOS-Style Floating Action Sheet (`UIAlertController.Style.actionSheet`) with a frosted
 * action stack and a **physically separate bottom `"Cancel"` capsule** whose corners curve
 * concentrically with the physical device display corners (`SeaGlassActionSheet`).
 */
@Composable
fun SeaGlassActionSheet(
    visible: Boolean,
    title: String,
    message: String?,
    actions: List<SeaGlassContextMenuItem>,
    onActionSelected: (SeaGlassContextMenuItem) -> Unit,
    onDismissRequest: () -> Unit,
    modifier: Modifier = Modifier,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    cancelLabel: String = "Cancel",
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass.copy(
        cornerRadiusDp = 30f,
        transparency = 0.56f,
        coarseness = 0.44f,
        frostRadiusDp = 30f,
        thicknessDp = 20f
    )
) {
    val motion = LocalSeaGlassMotionScheme.current
    val systemBarsPadding = WindowInsets.systemBars.asPaddingValues()
    val targetProgress = if (visible) 1f else 0f
    val rawSpatialProgress by animateFloatAsState(
        targetValue = targetProgress,
        animationSpec = if (visible) motion.morphOpenSpatial else motion.morphCloseSpatial,
        label = "actionSheetSpatial"
    )
    val spatialProgress = SeaGlassShapeMorph.physicsSCurve(rawSpatialProgress, overshootGain = 0.40f)
    val effectsProgress = SeaGlassShapeMorph.sCurve(rawSpatialProgress)

    if (effectsProgress <= 0.005f && !visible) {
        return
    }

    val malleability = SeaGlassShapeMorph.sCurveBell(rawSpatialProgress)
    val activeMaterial = remember(material, malleability) {
        material.deaged(malleability)
    }

    Box(
        modifier = modifier
            .fillMaxSize()
            .zIndex(88f)
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = (0.26f * effectsProgress).coerceIn(0f, 0.5f)))
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onDismissRequest
                )
        )

        Column(
            verticalArrangement = Arrangement.spacedBy(10.dp),
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .fillMaxWidth()
                .padding(
                    start = 10.dp,
                    end = 10.dp,
                    bottom = systemBarsPadding.calculateBottomPadding() + 10.dp
                )
                .graphicsLayer {
                    alpha = effectsProgress.coerceIn(0f, 1f)
                    translationY = (1f - spatialProgress) * 220f
                    val s = 0.90f + 0.10f * spatialProgress
                    scaleX = s
                    scaleY = s
                }
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = {}
                )
        ) {
            // 1. Main Frosted Action Group
            SeaGlassSurface(
                stageState = stageState,
                material = activeMaterial,
                contentPadding = PaddingValues(vertical = 4.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                val contentColor = LocalContentColor.current
                Column(modifier = Modifier.fillMaxWidth()) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(3.dp),
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 20.dp, vertical = 12.dp)
                    ) {
                        Text(
                            text = title,
                            color = contentColor.copy(alpha = 0.65f),
                            textAlign = TextAlign.Center,
                            style = MaterialTheme.typography.labelLarge.copy(
                                fontWeight = FontWeight.Bold,
                                fontSize = 13.sp
                            )
                        )
                        if (!message.isNullOrBlank()) {
                            Text(
                                text = message,
                                color = contentColor.copy(alpha = 0.55f),
                                textAlign = TextAlign.Center,
                                style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp)
                            )
                        }
                    }

                    actions.forEach { action ->
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(0.6.dp)
                                .background(contentColor.copy(alpha = 0.12f))
                        )
                        val actionColor = if (action.isDestructive) Color(0xFFD92D20) else contentColor
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = {
                                        onActionSelected(action)
                                        onDismissRequest()
                                    }
                                )
                                .padding(vertical = 15.dp, horizontal = 20.dp)
                        ) {
                            Text(
                                text = action.title,
                                color = actionColor,
                                style = MaterialTheme.typography.titleMedium.copy(
                                    fontWeight = if (action.isDestructive) FontWeight.Bold else FontWeight.SemiBold,
                                    fontSize = 17.sp
                                )
                            )
                        }
                    }
                }
            }

            // 2. Detached Concentric Device-Corner-Matched "Cancel" Capsule
            SeaGlassSurface(
                stageState = stageState,
                material = LocalSeaGlassPalette.current.primaryGlass.copy(
                    cornerRadiusDp = 32f,
                    thicknessDp = 20f
                ).deaged(malleability),
                cornerAdaptationMode = SeaGlassCornerAdaptationMode.CONCENTRIC_PROXIMITY,
                cornerMask = SeaGlassCornerMask.BOTTOM_ONLY,
                cornerProximityThresholdDp = 64f,
                allowAsymmetricCorners = false,
                onClick = onDismissRequest,
                contentAlignment = Alignment.Center,
                contentPadding = PaddingValues(vertical = 16.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = cancelLabel,
                    color = LocalContentColor.current,
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 17.sp
                    )
                )
            }
        }
    }
}

/**
 * iOS Dynamic Island / Live Activity Expandable SeaGlass Capsule (`SeaGlassLiveActivityPill`).
 *
 * - **Collapsed**: Compact floating pill (`[✦ Live Render · 120Hz · 0.4ms]`).
 * - **Expanded**: Morphs with lead-lag spatial springs and temporal de-aging into a full
 *   Live Activity card with real-time waveform, progress bar, and quick action buttons.
 */
@Composable
fun SeaGlassLiveActivityPill(
    expanded: Boolean,
    onExpandedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState? = LocalSeaGlassStageState.current,
    modifier: Modifier = Modifier,
    title: String = "Shader Compile & UBO Sync",
    subtitle: String = "16×vec4 std140 · 0 Dirty Slots · 120 FPS",
    progress: Float = 0.82f,
    pillMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    expandedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    SeaGlassTwoElementMorph(
        expanded = expanded,
        onToggle = { onExpandedChange(!expanded) },
        stageState = stageState,
        collapsedMaterial = pillMaterial.copy(cornerRadiusDp = 24f, thicknessDp = 16f),
        expandedMaterial = expandedMaterial.copy(cornerRadiusDp = 32f, thicknessDp = 22f),
        morphMode = SeaGlassMorphMode.LIQUID_COALESCE,
        anchorAlignment = Alignment.TopCenter,
        modifier = modifier,
        collapsedContent = {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                modifier = Modifier.padding(horizontal = 18.dp, vertical = 10.dp)
            ) {
                Box(
                    modifier = Modifier
                        .size(8.dp)
                        .clip(CircleShape)
                        .background(Color(0xFF12B76A))
                )
                Text(
                    text = "LIVE ACTIVITY · 120 HZ",
                    maxLines = 1,
                    style = MaterialTheme.typography.labelMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 11.5.sp,
                        letterSpacing = 0.6.sp
                    ),
                    color = LocalContentColor.current
                )
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.AudioWave,
                    modifier = Modifier.size(16.dp),
                    tint = LocalContentColor.current
                )
            }
        },
        expandedContent = {
            Column(
                verticalArrangement = Arrangement.spacedBy(10.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 18.dp, vertical = 14.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Box(
                            contentAlignment = Alignment.Center,
                            modifier = Modifier
                                .size(32.dp)
                                .clip(CircleShape)
                                .background(LocalContentColor.current.copy(alpha = 0.12f))
                        ) {
                            SeaGlassIcon(
                                glyph = SeaGlassGlyph.Sparkles,
                                modifier = Modifier.size(16.dp),
                                tint = LocalContentColor.current
                            )
                        }
                        Column {
                            Text(
                                text = title,
                                style = MaterialTheme.typography.titleSmall.copy(
                                    fontWeight = FontWeight.Black,
                                    fontSize = 14.5.sp
                                ),
                                color = LocalContentColor.current
                            )
                            Text(
                                text = subtitle,
                                style = MaterialTheme.typography.labelSmall.copy(
                                    fontFamily = FontFamily.Monospace,
                                    fontSize = 10.5.sp
                                ),
                                color = LocalContentColor.current.copy(alpha = 0.72f)
                            )
                        }
                    }

                    SeaGlassBadge(
                        text = "${(progress * 100).toInt()}%",
                        stageState = stageState,
                        material = pillMaterial
                    )
                }

                SeaGlassLinearProgressIndicator(
                    progress = progress,
                    stageState = stageState,
                    trackMaterial = expandedMaterial,
                    fillMaterial = pillMaterial
                )
            }
        }
    )
}

/**
 * Default iOS-style context menu items with nested submenu morphing and destructive bottom section.
 */
fun defaultIosContextMenuItems(): List<SeaGlassContextMenuItem> = listOf(
    SeaGlassContextMenuItem(
        id = "pin",
        title = "Pin Session",
        glyph = SeaGlassGlyph.Pin
    ),
    SeaGlassContextMenuItem(
        id = "model_submenu",
        title = "Switch Model",
        subtitle = "gemini-4-argon-eap",
        glyph = SeaGlassGlyph.Sparkles,
        submenuTitle = "Select Model",
        submenuItems = listOf(
            SeaGlassContextMenuItem(
                id = "model_argon",
                title = "Gemini 4 Argon EAP",
                subtitle = "1M Context · High",
                glyph = SeaGlassGlyph.Sparkles,
                checked = true
            ),
            SeaGlassContextMenuItem(
                id = "model_sonnet",
                title = "Claude Sonnet 4.6",
                subtitle = "200K Context · Balanced",
                glyph = SeaGlassGlyph.Layers
            ),
            SeaGlassContextMenuItem(
                id = "model_gpt",
                title = "GPT-5.4 Pro",
                subtitle = "400K Context · Deep",
                glyph = SeaGlassGlyph.Tune
            )
        )
    ),
    SeaGlassContextMenuItem(
        id = "compact",
        title = "Compact Context",
        glyph = SeaGlassGlyph.Compact
    ),
    SeaGlassContextMenuItem(
        id = "fork",
        title = "Fork Thread",
        glyph = SeaGlassGlyph.Fork
    ),
    SeaGlassContextMenuItem(
        id = "export",
        title = "Export Transcript",
        glyph = SeaGlassGlyph.Export
    ),
    SeaGlassContextMenuItem(
        id = "delete",
        title = "Delete Session",
        glyph = SeaGlassGlyph.Close,
        isDestructive = true,
        startsNewSection = true
    )
)
