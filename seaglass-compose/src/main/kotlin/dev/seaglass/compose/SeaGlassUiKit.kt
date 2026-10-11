package dev.seaglass.compose

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.animation.expandHorizontally
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.shrinkHorizontally
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsFocusedAsState
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
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.material3.LocalContentColor
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
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
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
import dev.seaglass.core.SeaGlassShapeMorph
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt
import kotlin.math.sin

/**
 * Built-in crisp vector glyphs for SeaGlass UI components, toolbars, and navigation controls.
 */
enum class SeaGlassGlyph {
    Home,
    Search,
    Tune,
    Sparkles,
    Play,
    Pause,
    Next,
    Check,
    Close,
    Plus,
    Heart,
    Share,
    Bookmark,
    Bell,
    Settings,
    Back,
    Menu,
    Info,
    Layers,
    AudioWave
}

/**
 * Renders a crisp vector [SeaGlassGlyph] harmonized with [LocalContentColor].
 */
@Composable
fun SeaGlassIcon(
    glyph: SeaGlassGlyph,
    modifier: Modifier = Modifier.size(20.dp),
    tint: Color = LocalContentColor.current
) {
    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        val sw = (min(w, h) * 0.105f).coerceAtLeast(1.8.dp.toPx())
        val stroke = Stroke(width = sw, cap = StrokeCap.Round, join = StrokeJoin.Round)

        when (glyph) {
            SeaGlassGlyph.Home -> {
                val path = Path().apply {
                    moveTo(w * 0.50f, h * 0.12f)
                    lineTo(w * 0.88f, h * 0.45f)
                    lineTo(w * 0.78f, h * 0.45f)
                    lineTo(w * 0.78f, h * 0.86f)
                    lineTo(w * 0.22f, h * 0.86f)
                    lineTo(w * 0.22f, h * 0.45f)
                    lineTo(w * 0.12f, h * 0.45f)
                    close()
                }
                drawPath(path, color = tint, style = stroke)
            }

            SeaGlassGlyph.Search -> {
                drawCircle(
                    color = tint,
                    radius = w * 0.28f,
                    center = Offset(w * 0.42f, h * 0.42f),
                    style = stroke
                )
                drawLine(
                    color = tint,
                    start = Offset(w * 0.63f, h * 0.63f),
                    end = Offset(w * 0.88f, h * 0.88f),
                    strokeWidth = sw * 1.15f,
                    cap = StrokeCap.Round
                )
            }

            SeaGlassGlyph.Tune -> {
                drawLine(tint, Offset(w * 0.14f, h * 0.30f), Offset(w * 0.86f, h * 0.30f), sw, StrokeCap.Round)
                drawCircle(tint, radius = w * 0.11f, center = Offset(w * 0.36f, h * 0.30f))
                drawLine(tint, Offset(w * 0.14f, h * 0.70f), Offset(w * 0.86f, h * 0.70f), sw, StrokeCap.Round)
                drawCircle(tint, radius = w * 0.11f, center = Offset(w * 0.66f, h * 0.70f))
            }

            SeaGlassGlyph.Sparkles -> {
                val star = Path().apply {
                    moveTo(w * 0.50f, h * 0.10f)
                    quadraticTo(w * 0.52f, h * 0.48f, w * 0.90f, h * 0.50f)
                    quadraticTo(w * 0.52f, h * 0.52f, w * 0.50f, h * 0.90f)
                    quadraticTo(w * 0.48f, h * 0.52f, w * 0.10f, h * 0.50f)
                    quadraticTo(w * 0.48f, h * 0.48f, w * 0.50f, h * 0.10f)
                    close()
                }
                drawPath(star, color = tint)
            }

            SeaGlassGlyph.Play -> {
                val p = Path().apply {
                    moveTo(w * 0.24f, h * 0.16f)
                    lineTo(w * 0.84f, h * 0.50f)
                    lineTo(w * 0.24f, h * 0.84f)
                    close()
                }
                drawPath(p, color = tint)
            }

            SeaGlassGlyph.Pause -> {
                val r = CornerRadius(sw * 0.5f, sw * 0.5f)
                drawRoundRect(tint, Offset(w * 0.22f, h * 0.16f), Size(w * 0.20f, h * 0.68f), r)
                drawRoundRect(tint, Offset(w * 0.58f, h * 0.16f), Size(w * 0.20f, h * 0.68f), r)
            }

            SeaGlassGlyph.Next -> {
                val p1 = Path().apply {
                    moveTo(w * 0.12f, h * 0.20f)
                    lineTo(w * 0.52f, h * 0.50f)
                    lineTo(w * 0.12f, h * 0.80f)
                    close()
                }
                val p2 = Path().apply {
                    moveTo(w * 0.50f, h * 0.20f)
                    lineTo(w * 0.90f, h * 0.50f)
                    lineTo(w * 0.50f, h * 0.80f)
                    close()
                }
                drawPath(p1, color = tint)
                drawPath(p2, color = tint)
            }

            SeaGlassGlyph.Check -> {
                val p = Path().apply {
                    moveTo(w * 0.18f, h * 0.52f)
                    lineTo(w * 0.42f, h * 0.76f)
                    lineTo(w * 0.84f, h * 0.26f)
                }
                drawPath(p, color = tint, style = stroke)
            }

            SeaGlassGlyph.Close -> {
                drawLine(tint, Offset(w * 0.22f, h * 0.22f), Offset(w * 0.78f, h * 0.78f), sw, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.78f, h * 0.22f), Offset(w * 0.22f, h * 0.78f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Plus -> {
                drawLine(tint, Offset(w * 0.50f, h * 0.18f), Offset(w * 0.50f, h * 0.82f), sw, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.18f, h * 0.50f), Offset(w * 0.82f, h * 0.50f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Heart -> {
                val p = Path().apply {
                    moveTo(w * 0.50f, h * 0.84f)
                    cubicTo(w * 0.12f, h * 0.60f, w * 0.08f, h * 0.24f, w * 0.30f, h * 0.18f)
                    cubicTo(w * 0.42f, h * 0.15f, w * 0.50f, h * 0.26f, w * 0.50f, h * 0.32f)
                    cubicTo(w * 0.50f, h * 0.26f, w * 0.58f, h * 0.15f, w * 0.70f, h * 0.18f)
                    cubicTo(w * 0.92f, h * 0.24f, w * 0.88f, h * 0.60f, w * 0.50f, h * 0.84f)
                    close()
                }
                drawPath(p, color = tint, style = stroke)
            }

            SeaGlassGlyph.Share -> {
                drawCircle(tint, radius = w * 0.11f, center = Offset(w * 0.74f, h * 0.22f))
                drawCircle(tint, radius = w * 0.11f, center = Offset(w * 0.24f, h * 0.50f))
                drawCircle(tint, radius = w * 0.11f, center = Offset(w * 0.74f, h * 0.78f))
                drawLine(tint, Offset(w * 0.33f, h * 0.45f), Offset(w * 0.65f, h * 0.27f), sw * 0.9f, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.33f, h * 0.55f), Offset(w * 0.65f, h * 0.73f), sw * 0.9f, StrokeCap.Round)
            }

            SeaGlassGlyph.Bookmark -> {
                val p = Path().apply {
                    moveTo(w * 0.24f, h * 0.14f)
                    lineTo(w * 0.76f, h * 0.14f)
                    lineTo(w * 0.76f, h * 0.86f)
                    lineTo(w * 0.50f, h * 0.68f)
                    lineTo(w * 0.24f, h * 0.86f)
                    close()
                }
                drawPath(p, color = tint, style = stroke)
            }

            SeaGlassGlyph.Bell -> {
                val p = Path().apply {
                    moveTo(w * 0.22f, h * 0.68f)
                    lineTo(w * 0.78f, h * 0.68f)
                    lineTo(w * 0.72f, h * 0.42f)
                    cubicTo(w * 0.72f, h * 0.22f, w * 0.28f, h * 0.22f, w * 0.28f, h * 0.42f)
                    close()
                }
                drawPath(p, color = tint, style = stroke)
                drawCircle(tint, radius = w * 0.08f, center = Offset(w * 0.50f, h * 0.82f))
            }

            SeaGlassGlyph.Settings -> {
                drawCircle(tint, radius = w * 0.18f, center = Offset(w * 0.50f, h * 0.50f), style = stroke)
                for (i in 0 until 6) {
                    val a = i * (Math.PI / 3.0).toFloat()
                    drawLine(
                        color = tint,
                        start = Offset(w * 0.50f + cos(a) * w * 0.26f, h * 0.50f + sin(a) * h * 0.26f),
                        end = Offset(w * 0.50f + cos(a) * w * 0.39f, h * 0.50f + sin(a) * h * 0.39f),
                        strokeWidth = sw,
                        cap = StrokeCap.Round
                    )
                }
            }

            SeaGlassGlyph.Back -> {
                val p = Path().apply {
                    moveTo(w * 0.64f, h * 0.18f)
                    lineTo(w * 0.28f, h * 0.50f)
                    lineTo(w * 0.64f, h * 0.82f)
                }
                drawPath(p, color = tint, style = stroke)
            }

            SeaGlassGlyph.Menu -> {
                drawLine(tint, Offset(w * 0.18f, h * 0.28f), Offset(w * 0.82f, h * 0.28f), sw, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.18f, h * 0.50f), Offset(w * 0.82f, h * 0.50f), sw, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.18f, h * 0.72f), Offset(w * 0.62f, h * 0.72f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Info -> {
                drawCircle(tint, radius = w * 0.38f, center = Offset(w * 0.50f, h * 0.50f), style = stroke)
                drawCircle(tint, radius = sw * 0.6f, center = Offset(w * 0.50f, h * 0.32f))
                drawLine(tint, Offset(w * 0.50f, h * 0.46f), Offset(w * 0.50f, h * 0.70f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Layers -> {
                val top = Path().apply {
                    moveTo(w * 0.50f, h * 0.16f)
                    lineTo(w * 0.86f, h * 0.36f)
                    lineTo(w * 0.50f, h * 0.54f)
                    lineTo(w * 0.14f, h * 0.36f)
                    close()
                }
                drawPath(top, color = tint, style = stroke)
                val bottom = Path().apply {
                    moveTo(w * 0.16f, h * 0.60f)
                    lineTo(w * 0.50f, h * 0.78f)
                    lineTo(w * 0.84f, h * 0.60f)
                }
                drawPath(bottom, color = tint, style = stroke)
            }

            SeaGlassGlyph.AudioWave -> {
                val heights = floatArrayOf(0.32f, 0.68f, 0.48f, 0.80f, 0.38f)
                for (i in heights.indices) {
                    val x = w * (0.18f + i * 0.16f)
                    val half = h * heights[i] * 0.5f
                    drawLine(tint, Offset(x, h * 0.5f - half), Offset(x, h * 0.5f + half), sw, StrokeCap.Round)
                }
            }
        }
    }
}

/**
 * Floating Liquid SeaGlass Top App Bar with optional navigation icon, title/subtitle,
 * and trailing action buttons. Uses [SeaGlassContentRole.NAVIGATION_CHROME] for high-legibility
 * subsurface diffusion and outer meniscus edge refraction.
 */
@Composable
fun SeaGlassTopAppBar(
    title: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    navigationGlyph: SeaGlassGlyph? = SeaGlassGlyph.Back,
    onNavigationClick: (() -> Unit)? = null,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    actions: @Composable RowScope.() -> Unit = {}
) {
    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = material.tintColorArgb,
                tintDensity = material.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            modifier = modifier
                .fillMaxWidth()
                .height(60.dp)
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(
                        cornerRadiusDp = 30f,
                        thicknessDp = 18f,
                        rimChalkiness = 0.86f
                    ),
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(horizontal = 12.dp, vertical = 8.dp)
        ) {
            if (navigationGlyph != null && onNavigationClick != null) {
                SeaGlassIconButton(
                    glyph = navigationGlyph,
                    onClick = onNavigationClick,
                    stageState = stageState,
                    size = 40.dp,
                    material = LocalSeaGlassPalette.current.secondaryGlass
                )
            } else {
                Spacer(modifier = Modifier.width(4.dp))
            }

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = title,
                    color = contentColor,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 16.sp
                    )
                )
                if (!subtitle.isNullOrBlank()) {
                    Text(
                        text = subtitle,
                        color = contentColor.copy(alpha = 0.76f),
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = FontWeight.Medium,
                            fontSize = 11.sp
                        )
                    )
                }
            }

            Row(
                horizontalArrangement = Arrangement.spacedBy(6.dp),
                verticalAlignment = Alignment.CenterVertically,
                content = actions
            )
        }
    }
}

/**
 * Tactile circular/squircle/morphing SeaGlass icon button with 3D press tilt and HDR caustic bloom.
 */
@Composable
fun SeaGlassIconButton(
    glyph: SeaGlassGlyph,
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    selected: Boolean = false,
    size: Dp = 44.dp,
    material: SeaGlassMaterial = if (selected) {
        LocalSeaGlassPalette.current.primaryGlass
    } else {
        LocalSeaGlassPalette.current.surfaceGlass
    }
) {
    val cornerRadius = if (selected) 16f else (size.value * 0.5f)
    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = cornerRadius,
            thicknessDp = if (selected) 18f else 13f
        ),
        contentPadding = PaddingValues(0.dp),
        modifier = modifier.size(size)
    ) {
        SeaGlassIcon(
            glyph = glyph,
            modifier = Modifier.size(size * 0.46f),
            tint = LocalContentColor.current
        )
    }
}

/**
 * Interactive Liquid SeaGlass Search Bar with focus-reactive rim brightening, live text editing,
 * instant clear button, and optional trailing action button.
 */
@Composable
fun SeaGlassSearchBar(
    query: String,
    onQueryChange: (String) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    placeholder: String = "Search glass components, shaders, palettes…",
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    trailingGlyph: SeaGlassGlyph? = SeaGlassGlyph.Tune,
    onTrailingClick: (() -> Unit)? = null
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isFocused by interactionSource.collectIsFocusedAsState()

    val focusProgress by animateFloatAsState(
        targetValue = if (isFocused) 1f else 0f,
        animationSpec = motion.fastEffects,
        label = "searchFocus"
    )

    val activeMaterial = remember(material, focusProgress) {
        material.copy(
            cornerRadiusDp = 28f,
            thicknessDp = 16f + 4f * focusProgress,
            rimChalkiness = (material.rimChalkiness + 0.16f * focusProgress).coerceAtMost(0.98f),
            tintDensity = (material.tintDensity + 0.08f * focusProgress).coerceAtMost(0.92f)
        )
    }

    val contentColor = remember(activeMaterial.tintColorArgb, activeMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = activeMaterial.tintColorArgb,
                tintDensity = activeMaterial.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            modifier = modifier
                .fillMaxWidth()
                .height(52.dp)
                .seaGlass(
                    stageState = stageState,
                    material = activeMaterial,
                    touchIntensity = focusProgress * 0.35f,
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(horizontal = 16.dp)
        ) {
            SeaGlassIcon(
                glyph = SeaGlassGlyph.Search,
                modifier = Modifier.size(19.dp),
                tint = contentColor.copy(alpha = 0.85f)
            )

            Box(
                modifier = Modifier.weight(1f),
                contentAlignment = Alignment.CenterStart
            ) {
                if (query.isEmpty()) {
                    Text(
                        text = placeholder,
                        color = contentColor.copy(alpha = 0.62f),
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        style = MaterialTheme.typography.bodyMedium.copy(
                            fontWeight = FontWeight.Medium,
                            fontSize = 14.sp
                        )
                    )
                }
                BasicTextField(
                    value = query,
                    onValueChange = onQueryChange,
                    singleLine = true,
                    interactionSource = interactionSource,
                    cursorBrush = SolidColor(contentColor),
                    textStyle = TextStyle(
                        color = contentColor,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold
                    ),
                    modifier = Modifier.fillMaxWidth()
                )
            }

            if (query.isNotEmpty()) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(26.dp)
                        .clip(CircleShape)
                        .background(contentColor.copy(alpha = 0.14f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onQueryChange("") }
                        )
                ) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.Close,
                        modifier = Modifier.size(13.dp),
                        tint = contentColor
                    )
                }
            } else if (trailingGlyph != null && onTrailingClick != null) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(30.dp)
                        .clip(CircleShape)
                        .background(contentColor.copy(alpha = 0.12f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = onTrailingClick
                        )
                ) {
                    SeaGlassIcon(
                        glyph = trailingGlyph,
                        modifier = Modifier.size(15.dp),
                        tint = contentColor
                    )
                }
            }
        }
    }
}

/**
 * Frosted SeaGlass TextField with optional label, leading/trailing icons, and focus-reactive
 * corner & rim morphing.
 */
@Composable
fun SeaGlassTextField(
    value: String,
    onValueChange: (String) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    label: String? = null,
    placeholder: String = "",
    leadingGlyph: SeaGlassGlyph? = null,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val interactionSource = remember { MutableInteractionSource() }
    val isFocused by interactionSource.collectIsFocusedAsState()

    val focusProgress by animateFloatAsState(
        targetValue = if (isFocused) 1f else 0f,
        animationSpec = motion.fastEffects,
        label = "textFieldFocus"
    )
    val cornerRadius by animateFloatAsState(
        targetValue = if (isFocused) 24f else 18f,
        animationSpec = motion.fastSpatial,
        label = "textFieldCorner"
    )

    val fieldMaterial = remember(material, focusProgress, cornerRadius) {
        material.copy(
            cornerRadiusDp = cornerRadius,
            thicknessDp = 14f + 4f * focusProgress,
            rimChalkiness = (material.rimChalkiness + 0.18f * focusProgress).coerceAtMost(0.98f)
        )
    }

    val contentColor = remember(fieldMaterial.tintColorArgb, fieldMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = fieldMaterial.tintColorArgb,
                tintDensity = fieldMaterial.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Column(
            modifier = modifier
                .fillMaxWidth()
                .seaGlass(
                    stageState = stageState,
                    material = fieldMaterial,
                    touchIntensity = focusProgress * 0.30f,
                    contentRole = SeaGlassContentRole.TEXT_AND_ICONS
                )
                .padding(horizontal = 16.dp, vertical = 10.dp),
            verticalArrangement = Arrangement.spacedBy(2.dp)
        ) {
            if (!label.isNullOrBlank()) {
                Text(
                    text = label,
                    color = contentColor.copy(alpha = 0.72f),
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.Bold,
                        fontSize = 11.sp
                    )
                )
            }
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(10.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                if (leadingGlyph != null) {
                    SeaGlassIcon(
                        glyph = leadingGlyph,
                        modifier = Modifier.size(18.dp),
                        tint = contentColor.copy(alpha = 0.82f)
                    )
                }
                Box(
                    modifier = Modifier.weight(1f),
                    contentAlignment = Alignment.CenterStart
                ) {
                    if (value.isEmpty() && placeholder.isNotEmpty()) {
                        Text(
                            text = placeholder,
                            color = contentColor.copy(alpha = 0.56f),
                            style = MaterialTheme.typography.bodyMedium.copy(fontSize = 14.sp)
                        )
                    }
                    BasicTextField(
                        value = value,
                        onValueChange = onValueChange,
                        singleLine = true,
                        interactionSource = interactionSource,
                        cursorBrush = SolidColor(contentColor),
                        textStyle = TextStyle(
                            color = contentColor,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold
                        ),
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            }
        }
    }
}

/**
 * Tactile Liquid SeaGlass Toggle Switch whose thumb is a 3D water-drop lens that glides
 * across the frosted capsule track with velocity stretch and magnifies the track underneath.
 */
@Composable
fun SeaGlassSwitch(
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    checkedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    uncheckedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    val thumbProgress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "switchThumbSpatial"
    )
    val tintProgress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "switchTintEffects"
    )

    val trackMaterial = remember(checkedMaterial, uncheckedMaterial, tintProgress) {
        SeaGlassMaterial.lerp(uncheckedMaterial, checkedMaterial, tintProgress.coerceIn(0f, 1f)).copy(
            cornerRadiusDp = 18f,
            thicknessDp = 12f
        )
    }

    val motionStretch = abs((if (checked) 1f else 0f) - thumbProgress).coerceIn(0f, 1f)
    val trackWidthDp = 56.dp
    val trackHeightDp = 32.dp
    val thumbWidthDp = 26.dp
    val thumbHeightDp = 24.dp
    val padPx = with(density) { 3.dp.toPx() }
    val travelPx = with(density) { (trackWidthDp - thumbWidthDp - 6.dp).toPx() }
    val thumbOffsetX = (padPx + travelPx * thumbProgress).roundToInt()

    val contentColor = remember(trackMaterial.tintColorArgb, trackMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = trackMaterial.tintColorArgb,
                tintDensity = trackMaterial.tintDensity
            )
        )
    }

    Box(
        contentAlignment = Alignment.CenterStart,
        modifier = modifier
            .size(width = trackWidthDp, height = trackHeightDp)
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = { onCheckedChange(!checked) }
            )
            .seaGlass(
                stageState = stageState,
                material = trackMaterial,
                contentRole = SeaGlassContentRole.TEXT_AND_ICONS
            )
    ) {
        // 3D Water-Drop Lens Thumb
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset { IntOffset(thumbOffsetX, 0) }
                .size(width = thumbWidthDp, height = thumbHeightDp)
                .graphicsLayer {
                    scaleX = 1f + 0.28f * motionStretch
                    scaleY = 1f - 0.14f * motionStretch
                }
                .clip(RoundedCornerShape(14.dp))
                .background(
                    Brush.verticalGradient(
                        colors = listOf(
                            Color.White.copy(alpha = if (checked) 0.92f else 0.74f),
                            Color(checkedMaterial.tintColorArgb).copy(alpha = if (checked) 0.58f else 0.28f)
                        )
                    )
                )
                .border(1.dp, Color.White.copy(alpha = 0.82f), RoundedCornerShape(14.dp))
        ) {
            if (checked) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Check,
                    modifier = Modifier.size(13.dp),
                    tint = contentColor
                )
            }
        }
    }
}

/**
 * Material 3 Expressive SeaGlass Checkbox that morphs its contour between a rounded squircle
 * (unchecked) and an expressive 4-lobe Clover (`Cookie4`, checked) with an animated checkmark.
 */
@Composable
fun SeaGlassCheckbox(
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    checkedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    uncheckedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val progress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "checkboxMorph"
    )
    val tintProgress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "checkboxTint"
    )

    val mat = remember(checkedMaterial, uncheckedMaterial, tintProgress) {
        SeaGlassMaterial.lerp(uncheckedMaterial, checkedMaterial, tintProgress.coerceIn(0f, 1f)).copy(
            thicknessDp = 12f + 4f * tintProgress
        )
    }

    val contentColor = remember(mat.tintColorArgb, mat.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = mat.tintColorArgb,
                tintDensity = mat.tintDensity
            )
        )
    }

    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(28.dp)
            .graphicsLayer {
                val bounce = 1f + 0.14f * sin(progress.coerceIn(0f, 1f) * Math.PI.toFloat())
                scaleX = bounce
                scaleY = bounce
            }
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = { onCheckedChange(!checked) }
            )
            .seaGlass(
                stageState = stageState,
                material = mat,
                customContourPathBuilder = { wPx, hPx, outPath ->
                    SeaGlassShapeMorph.buildMorphedPolygonPath(
                        startKind = SeaGlassShapeKind.Squircle,
                        endKind = SeaGlassShapeKind.Cookie4,
                        progress = progress,
                        widthPx = wPx,
                        heightPx = hPx,
                        outPath = outPath
                    )
                }
            )
    ) {
        if (progress > 0.15f) {
            SeaGlassIcon(
                glyph = SeaGlassGlyph.Check,
                modifier = Modifier
                    .size(16.dp)
                    .graphicsLayer {
                        scaleX = progress.coerceIn(0f, 1.15f)
                        scaleY = progress.coerceIn(0f, 1.15f)
                        alpha = progress.coerceIn(0f, 1f)
                    },
                tint = contentColor
            )
        }
    }
}

/**
 * Frosted SeaGlass RadioButton with an animated inner droplet that blooms when selected.
 */
@Composable
fun SeaGlassRadioButton(
    selected: Boolean,
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    selectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    unselectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val progress by animateFloatAsState(
        targetValue = if (selected) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "radioSpatial"
    )
    val tintProgress by animateFloatAsState(
        targetValue = if (selected) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "radioTint"
    )

    val mat = remember(selectedMaterial, unselectedMaterial, tintProgress) {
        SeaGlassMaterial.lerp(unselectedMaterial, selectedMaterial, tintProgress.coerceIn(0f, 1f)).copy(
            cornerRadiusDp = 14f,
            thicknessDp = 12f + 4f * tintProgress
        )
    }

    val contentColor = remember(mat.tintColorArgb, mat.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = mat.tintColorArgb,
                tintDensity = mat.tintDensity
            )
        )
    }

    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(28.dp)
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = onClick
            )
            .seaGlass(
                stageState = stageState,
                material = mat
            )
    ) {
        if (progress > 0.05f) {
            Box(
                modifier = Modifier
                    .size(12.dp)
                    .graphicsLayer {
                        scaleX = progress.coerceAtLeast(0f)
                        scaleY = progress.coerceAtLeast(0f)
                    }
                    .clip(CircleShape)
                    .background(contentColor)
            )
        }
    }
}

/**
 * Multi-Segment Liquid SeaGlass Segmented Button Row with a gliding frosted lozenge indicator
 * that stretches elastically between segments.
 */
@Composable
fun SeaGlassSegmentedButtonRow(
    options: List<String>,
    selectedIndex: Int,
    onSelect: (Int) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    containerMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    indicatorMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current
    val count = options.size.coerceAtLeast(1)

    val animatedIndex by animateFloatAsState(
        targetValue = selectedIndex.coerceIn(0, count - 1).toFloat(),
        animationSpec = motion.defaultSpatial,
        label = "segmentedIndicator"
    )
    var barWidthPx by remember { mutableFloatStateOf(800f) }

    val contentColor = remember(containerMaterial.tintColorArgb, containerMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = containerMaterial.tintColorArgb,
                tintDensity = containerMaterial.tintDensity
            )
        )
    }

    val isDarkBar = remember(containerMaterial.tintColorArgb) {
        SeaGlassColorMath.argbToOklab(containerMaterial.tintColorArgb).l < 0.48f
    }
    val pillTopColor = if (isDarkBar) {
        Color(indicatorMaterial.tintColorArgb).copy(alpha = 0.52f)
    } else {
        Color(
            SeaGlassColorMath.lerpOklab(indicatorMaterial.tintColorArgb, 0xFFFFFFFF.toInt(), 0.44f)
        ).copy(alpha = 0.62f)
    }
    val pillBottomColor = if (isDarkBar) {
        Color(0xFF0A1E30).copy(alpha = 0.36f)
    } else {
        Color(indicatorMaterial.tintColorArgb).copy(alpha = 0.42f)
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            modifier = modifier
                .fillMaxWidth()
                .height(46.dp)
                .onSizeChanged { barWidthPx = it.width.toFloat().coerceAtLeast(1f) }
                .seaGlass(
                    stageState = stageState,
                    material = containerMaterial.copy(cornerRadiusDp = 23f, thicknessDp = 15f),
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(4.dp)
        ) {
            val innerW = (barWidthPx - with(density) { 8.dp.toPx() }).coerceAtLeast(1f)
            val slotW = innerW / count
            val slotWidthDp = with(density) { slotW.toDp() }
            val indicatorOffsetX = (animatedIndex * slotW).roundToInt()
            val motionStretch = abs(selectedIndex - animatedIndex).coerceIn(0f, 1.2f)

            Box(
                modifier = Modifier
                    .offset { IntOffset(indicatorOffsetX, 0) }
                    .width(slotWidthDp)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleX = 1f + motionStretch * 0.24f
                        scaleY = 1f - motionStretch * 0.10f
                    }
                    .clip(RoundedCornerShape(19.dp))
                    .background(Brush.verticalGradient(listOf(pillTopColor, pillBottomColor)))
                    .border(1.dp, Color.White.copy(alpha = 0.52f), RoundedCornerShape(19.dp))
            )

            Row(
                modifier = Modifier.fillMaxSize(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                options.forEachIndexed { index, label ->
                    val selected = index == selectedIndex
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
                            text = label,
                            color = if (selected) contentColor else contentColor.copy(alpha = 0.78f),
                            maxLines = 1,
                            style = MaterialTheme.typography.labelLarge.copy(
                                fontWeight = if (selected) FontWeight.ExtraBold else FontWeight.SemiBold,
                                fontSize = 13.sp
                            )
                        )
                    }
                }
            }
        }
    }
}

/**
 * Material 3 Expressive Filter Chip that smoothly expands to reveal a leading checkmark
 * when selected.
 */
@Composable
fun SeaGlassFilterChip(
    label: String,
    selected: Boolean,
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    leadingGlyph: SeaGlassGlyph? = null,
    selectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    unselectedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val targetMaterial = if (selected) {
        selectedMaterial.copy(cornerRadiusDp = 22f, thicknessDp = 16f)
    } else {
        unselectedMaterial.copy(cornerRadiusDp = 14f, thicknessDp = 10f)
    }

    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = targetMaterial,
        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 9.dp),
        modifier = modifier
    ) {
        AnimatedVisibility(
            visible = selected,
            enter = expandHorizontally() + fadeIn(),
            exit = shrinkHorizontally() + fadeOut()
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Check,
                    modifier = Modifier.size(15.dp),
                    tint = LocalContentColor.current
                )
                Spacer(modifier = Modifier.width(6.dp))
            }
        }
        if (!selected && leadingGlyph != null) {
            SeaGlassIcon(
                glyph = leadingGlyph,
                modifier = Modifier.size(15.dp),
                tint = LocalContentColor.current.copy(alpha = 0.85f)
            )
            Spacer(modifier = Modifier.width(6.dp))
        }
        Text(
            text = label,
            color = LocalContentColor.current,
            style = MaterialTheme.typography.labelMedium.copy(
                fontWeight = if (selected) FontWeight.ExtraBold else FontWeight.SemiBold
            )
        )
    }
}

/**
 * High-chroma SeaGlass Badge for notification counts or status callouts.
 */
@Composable
fun SeaGlassBadge(
    text: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass
) {
    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                tintArgb = material.tintColorArgb,
                tintDensity = material.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            contentAlignment = Alignment.Center,
            modifier = modifier
                .height(24.dp)
                .widthIn(min = 24.dp)
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(cornerRadiusDp = 12f, thicknessDp = 14f),
                    contentRole = SeaGlassContentRole.TEXT_AND_ICONS
                )
                .padding(horizontal = 8.dp)
        ) {
            Text(
                text = text,
                color = contentColor,
                style = MaterialTheme.typography.labelSmall.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 11.sp
                )
            )
        }
    }
}

/**
 * Expandable Liquid SeaGlass Floating Action Button (`SeaGlassExtendedFab`) that morphs
 * between a compact `56.dp` expressive icon orb and a wide pill with icon + label.
 */
@Composable
fun SeaGlassExtendedFab(
    text: String,
    glyph: SeaGlassGlyph,
    expanded: Boolean,
    onClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = if (expanded) 28f else 20f,
            thicknessDp = 20f,
            rimChalkiness = 0.88f
        ),
        contentPadding = PaddingValues(
            horizontal = if (expanded) 20.dp else 16.dp,
            vertical = 15.dp
        ),
        modifier = modifier.height(56.dp)
    ) {
        SeaGlassIcon(
            glyph = glyph,
            modifier = Modifier.size(22.dp),
            tint = LocalContentColor.current
        )
        AnimatedVisibility(
            visible = expanded,
            enter = expandHorizontally() + fadeIn(),
            exit = shrinkHorizontally() + fadeOut()
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Spacer(modifier = Modifier.width(10.dp))
                Text(
                    text = text,
                    color = LocalContentColor.current,
                    style = MaterialTheme.typography.labelLarge.copy(
                        fontWeight = FontWeight.ExtraBold,
                        fontSize = 14.sp
                    )
                )
            }
        }
    }
}

/**
 * Material 3 Expressive Floating Split Toolbar whose action pill and primary FAB orb coalesce
 * across a liquid `sminCubic` SDF bridge when docked and split apart when detached.
 */
@Composable
fun SeaGlassFloatingToolbar(
    actions: List<Pair<SeaGlassGlyph, String>>,
    selectedActionIndex: Int,
    onActionSelect: (Int) -> Unit,
    fabGlyph: SeaGlassGlyph,
    fabDetached: Boolean,
    onFabClick: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    barMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    fabMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current
    val heightDp = 58.dp
    val heightPx = with(density) { heightDp.toPx() }

    val gapDp by animateDpAsState(
        targetValue = if (fabDetached) 18.dp else (-6).dp,
        animationSpec = spring(dampingRatio = 0.62f, stiffness = 380f),
        label = "toolbarFabGap"
    )
    val gapPx = with(density) { gapDp.toPx() }
    var totalWidthPx by remember { mutableFloatStateOf(960f) }

    val mainBarWidthPx = (totalWidthPx - heightPx - max(0f, gapPx)).coerceAtLeast(heightPx * 2f)
    val fabLeftPx = (mainBarWidthPx + gapPx).coerceAtMost(totalWidthPx - heightPx)

    val sdfNodes = remember(mainBarWidthPx, heightPx, fabLeftPx, barMaterial, fabMaterial) {
        listOf(
            SeaGlassSdfEngine.SdfNode(
                centerX = mainBarWidthPx * 0.5f,
                centerY = heightPx * 0.5f,
                halfWidth = mainBarWidthPx * 0.5f,
                halfHeight = heightPx * 0.5f,
                uniformCornerRadius = heightPx * 0.5f,
                cornerSmoothing = 2.5f,
                tintColorArgb = barMaterial.tintColorArgb,
                thicknessPx = barMaterial.thicknessDp * density.density
            ),
            SeaGlassSdfEngine.SdfNode(
                centerX = fabLeftPx + heightPx * 0.5f,
                centerY = heightPx * 0.5f,
                halfWidth = heightPx * 0.5f,
                halfHeight = heightPx * 0.5f,
                uniformCornerRadius = heightPx * 0.5f,
                cornerSmoothing = 2.5f,
                tintColorArgb = fabMaterial.tintColorArgb,
                thicknessPx = fabMaterial.thicknessDp * density.density
            )
        )
    }

    val barContentColor = remember(barMaterial.tintColorArgb, barMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                barMaterial.tintColorArgb,
                barMaterial.tintDensity
            )
        )
    }
    val fabContentColor = remember(fabMaterial.tintColorArgb, fabMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                fabMaterial.tintColorArgb,
                fabMaterial.tintDensity
            )
        )
    }

    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(heightDp)
            .onSizeChanged { totalWidthPx = it.width.toFloat().coerceAtLeast(240f) }
            .seaGlass(
                stageState = stageState,
                material = barMaterial,
                nodesOptional = sdfNodes,
                smoothRadiusDp = 32f,
                contentRole = SeaGlassContentRole.NAVIGATION_CHROME
            )
    ) {
        ProvideSeaGlassLegibleContent(contentColor = barContentColor) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceEvenly,
                modifier = Modifier
                    .width(with(density) { mainBarWidthPx.toDp() })
                    .fillMaxHeight()
                    .padding(horizontal = 8.dp)
            ) {
                actions.forEachIndexed { index, (glyph, label) ->
                    val selected = index == selectedActionIndex
                    val itemAlpha = if (selected) 1f else 0.76f
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(5.dp),
                        modifier = Modifier
                            .clip(RoundedCornerShape(20.dp))
                            .background(
                                if (selected) barContentColor.copy(alpha = 0.14f) else Color.Transparent
                            )
                            .clickable(
                                interactionSource = remember { MutableInteractionSource() },
                                indication = null,
                                onClick = { onActionSelect(index) }
                            )
                            .padding(horizontal = 10.dp, vertical = 8.dp)
                    ) {
                        SeaGlassIcon(
                            glyph = glyph,
                            modifier = Modifier.size(18.dp),
                            tint = barContentColor.copy(alpha = itemAlpha)
                        )
                        if (selected) {
                            Text(
                                text = label,
                                color = barContentColor,
                                style = MaterialTheme.typography.labelMedium.copy(
                                    fontWeight = FontWeight.ExtraBold
                                )
                            )
                        }
                    }
                }
            }
        }

        // Coalescing Primary Action Orb
        Box(
            contentAlignment = Alignment.Center,
            modifier = Modifier
                .offset { IntOffset(fabLeftPx.roundToInt().coerceAtLeast(0), 0) }
                .size(heightDp)
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = onFabClick
                )
        ) {
            SeaGlassIcon(
                glyph = fabGlyph,
                modifier = Modifier.size(22.dp),
                tint = fabContentColor
            )
        }
    }
}

/**
 * Frosted SeaGlass Linear Progress Indicator with an Oklab gradient liquid fill bar.
 */
@Composable
fun SeaGlassLinearProgressIndicator(
    progress: Float,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    trackMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    fillMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass
) {
    val motion = LocalSeaGlassMotionScheme.current
    val animatedProgress by animateFloatAsState(
        targetValue = progress.coerceIn(0.04f, 1f),
        animationSpec = motion.defaultSpatial,
        label = "linearProgress"
    )

    val fillStart = Color(fillMaterial.tintColorArgb)
    val fillEnd = Color(
        SeaGlassColorMath.lerpOklab(fillMaterial.tintColorArgb, 0xFFFFFFFF.toInt(), 0.32f)
    )

    Box(
        contentAlignment = Alignment.CenterStart,
        modifier = modifier
            .fillMaxWidth()
            .height(14.dp)
            .seaGlass(
                stageState = stageState,
                material = trackMaterial.copy(cornerRadiusDp = 7f, thicknessDp = 10f),
                contentRole = SeaGlassContentRole.TEXT_AND_ICONS
            )
            .padding(2.dp)
    ) {
        Box(
            modifier = Modifier
                .fillMaxWidth(animatedProgress)
                .fillMaxHeight()
                .clip(CircleShape)
                .background(Brush.horizontalGradient(listOf(fillStart, fillEnd)))
                .border(0.8.dp, Color.White.copy(alpha = 0.72f), CircleShape)
        )
    }
}

/**
 * Frosted SeaGlass Circular Medallion Progress Indicator with animated arc sweep.
 */
@Composable
fun SeaGlassCircularProgressIndicator(
    progress: Float,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    size: Dp = 52.dp,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    accentColor: Color = Color(LocalSeaGlassPalette.current.primaryGlass.tintColorArgb)
) {
    val motion = LocalSeaGlassMotionScheme.current
    val animatedProgress by animateFloatAsState(
        targetValue = progress.coerceIn(0.05f, 1f),
        animationSpec = motion.defaultSpatial,
        label = "circularProgress"
    )
    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                material.tintColorArgb,
                material.tintDensity
            )
        )
    }

    Box(
        contentAlignment = Alignment.Center,
        modifier = modifier
            .size(size)
            .seaGlass(
                stageState = stageState,
                material = material.copy(
                    cornerRadiusDp = size.value * 0.5f,
                    thicknessDp = 15f
                ),
                contentRole = SeaGlassContentRole.TEXT_AND_ICONS
            )
    ) {
        Canvas(modifier = Modifier.size(size * 0.74f)) {
            val strokePx = 4.dp.toPx()
            drawCircle(
                color = contentColor.copy(alpha = 0.16f),
                style = Stroke(width = strokePx)
            )
            drawArc(
                color = accentColor,
                startAngle = -90f,
                sweepAngle = 360f * animatedProgress,
                useCenter = false,
                style = Stroke(width = strokePx, cap = StrokeCap.Round)
            )
        }
        Text(
            text = "${(animatedProgress * 100).roundToInt()}%",
            color = contentColor,
            style = MaterialTheme.typography.labelSmall.copy(
                fontWeight = FontWeight.Black,
                fontSize = 10.sp
            )
        )
    }
}

/**
 * Expressive Frosted SeaGlass List / Setting Row with leading icon, headline, supporting
 * subtitle, and trailing control slot.
 */
@Composable
fun SeaGlassListItem(
    headlineText: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    supportingText: String? = null,
    leadingGlyph: SeaGlassGlyph? = null,
    onClick: (() -> Unit)? = null,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    trailingContent: (@Composable RowScope.() -> Unit)? = null
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material.copy(cornerRadiusDp = 22f, thicknessDp = 14f),
        onClick = onClick,
        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
        modifier = modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            if (leadingGlyph != null) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(38.dp)
                        .clip(RoundedCornerShape(12.dp))
                        .background(LocalContentColor.current.copy(alpha = 0.12f))
                ) {
                    SeaGlassIcon(
                        glyph = leadingGlyph,
                        modifier = Modifier.size(20.dp),
                        tint = LocalContentColor.current
                    )
                }
            }

            Column(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(2.dp)
            ) {
                Text(
                    text = headlineText,
                    color = LocalContentColor.current,
                    style = MaterialTheme.typography.bodyMedium.copy(
                        fontWeight = FontWeight.Bold,
                        fontSize = 14.sp
                    )
                )
                if (!supportingText.isNullOrBlank()) {
                    Text(
                        text = supportingText,
                        color = LocalContentColor.current.copy(alpha = 0.78f),
                        style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp)
                    )
                }
            }

            if (trailingContent != null) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    content = trailingContent
                )
            }
        }
    }
}

/**
 * Floating Liquid SeaGlass Snackbar / Toast Banner with leading icon, message, and action button.
 */
@Composable
fun SeaGlassSnackbar(
    message: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    actionLabel: String? = null,
    onActionClick: (() -> Unit)? = null,
    leadingGlyph: SeaGlassGlyph = SeaGlassGlyph.Sparkles,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass
) {
    val contentColor = remember(material.tintColorArgb, material.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                material.tintColorArgb,
                material.tintDensity
            )
        )
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
            modifier = modifier
                .fillMaxWidth()
                .seaGlass(
                    stageState = stageState,
                    material = material.copy(cornerRadiusDp = 26f, thicknessDp = 18f),
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(horizontal = 16.dp, vertical = 12.dp)
        ) {
            SeaGlassIcon(
                glyph = leadingGlyph,
                modifier = Modifier.size(18.dp),
                tint = contentColor
            )
            Text(
                text = message,
                color = contentColor,
                modifier = Modifier.weight(1f),
                style = MaterialTheme.typography.bodySmall.copy(
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 13.sp
                )
            )
            if (actionLabel != null && onActionClick != null) {
                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(14.dp))
                        .background(contentColor.copy(alpha = 0.16f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = onActionClick
                        )
                        .padding(horizontal = 12.dp, vertical = 6.dp)
                ) {
                    Text(
                        text = actionLabel,
                        color = contentColor,
                        style = MaterialTheme.typography.labelMedium.copy(
                            fontWeight = FontWeight.ExtraBold
                        )
                    )
                }
            }
        }
    }
}

/**
 * Modal or Inline SeaGlass Dialog Card with header glyph, title, body text, and confirm/dismiss buttons.
 */
@Composable
fun SeaGlassDialogCard(
    title: String,
    body: String,
    confirmLabel: String,
    onConfirm: () -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    dismissLabel: String? = "Cancel",
    onDismiss: (() -> Unit)? = null,
    glyph: SeaGlassGlyph = SeaGlassGlyph.Sparkles,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass
) {
    SeaGlassCard(
        stageState = stageState,
        material = material.copy(cornerRadiusDp = 32f, thicknessDp = 22f, rimChalkiness = 0.88f),
        modifier = modifier.fillMaxWidth()
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(42.dp)
                    .clip(RoundedCornerShape(14.dp))
                    .background(LocalContentColor.current.copy(alpha = 0.14f))
            ) {
                SeaGlassIcon(
                    glyph = glyph,
                    modifier = Modifier.size(22.dp),
                    tint = LocalContentColor.current
                )
            }
            Text(
                text = title,
                color = LocalContentColor.current,
                style = MaterialTheme.typography.titleMedium.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 17.sp
                )
            )
        }

        Text(
            text = body,
            color = LocalContentColor.current.copy(alpha = 0.86f),
            style = MaterialTheme.typography.bodySmall.copy(
                fontSize = 13.sp,
                lineHeight = 18.sp
            )
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp, Alignment.End),
            verticalAlignment = Alignment.CenterVertically
        ) {
            if (dismissLabel != null && onDismiss != null) {
                SeaGlassButton(
                    onClick = onDismiss,
                    stageState = stageState,
                    material = LocalSeaGlassPalette.current.surfaceGlass.copy(cornerRadiusDp = 20f),
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 10.dp)
                ) {
                    Text(
                        text = dismissLabel,
                        style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.Bold)
                    )
                }
            }
            SeaGlassButton(
                onClick = onConfirm,
                stageState = stageState,
                material = LocalSeaGlassPalette.current.primaryGlass.copy(cornerRadiusDp = 20f),
                contentPadding = PaddingValues(horizontal = 18.dp, vertical = 10.dp)
            ) {
                Text(
                    text = confirmLabel,
                    style = MaterialTheme.typography.labelLarge.copy(fontWeight = FontWeight.ExtraBold)
                )
            }
        }
    }
}

/**
 * Frosted SeaGlass Bottom Sheet surface with a centered drag-handle pill and structured sheet content.
 */
@Composable
fun SeaGlassBottomSheetSurface(
    title: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    subtitle: String? = null,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass,
    content: @Composable ColumnScope.() -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material.copy(cornerRadiusDp = 34f, thicknessDp = 22f, rimChalkiness = 0.88f),
        contentPadding = PaddingValues(horizontal = 20.dp, vertical = 16.dp),
        modifier = modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.fillMaxWidth(),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            // Centered Frosted Drag Handle Pill
            Box(
                modifier = Modifier
                    .align(Alignment.CenterHorizontally)
                    .size(width = 40.dp, height = 4.5.dp)
                    .clip(CircleShape)
                    .background(LocalContentColor.current.copy(alpha = 0.28f))
            )

            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(
                    text = title,
                    color = LocalContentColor.current,
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 16.sp
                    )
                )
                if (!subtitle.isNullOrBlank()) {
                    Text(
                        text = subtitle,
                        color = LocalContentColor.current.copy(alpha = 0.78f),
                        style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp)
                    )
                }
            }

            content()
        }
    }
}
