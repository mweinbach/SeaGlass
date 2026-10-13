package dev.seaglass.compose

import android.graphics.RenderEffect as AndroidRenderEffect
import android.graphics.Shader
import android.os.Build
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
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
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.composed
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawWithContent
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.CompositingStrategy
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.graphics.asComposeRenderEffect
import androidx.compose.ui.graphics.drawscope.DrawScope
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.translate
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.layout.SubcomposeLayout
import androidx.compose.ui.layout.layout
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.onSizeChanged
import androidx.compose.ui.layout.positionInRoot
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.IntSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import dev.seaglass.core.SeaGlassColorMath
import dev.seaglass.core.SeaGlassCornerAdaptationConfig
import dev.seaglass.core.SeaGlassCornerAdaptationMode
import dev.seaglass.core.SeaGlassCornerAdaptationSolver
import dev.seaglass.core.SeaGlassCornerMask
import dev.seaglass.core.SeaGlassCornerRadiiPx
import dev.seaglass.core.SeaGlassDeviceHardwareProfile
import dev.seaglass.core.SeaGlassMaterial
import dev.seaglass.core.SeaGlassPalette
import dev.seaglass.core.SeaGlassSdfEngine
import dev.seaglass.core.SeaGlassShapeKind
import dev.seaglass.core.SeaGlassShapeMorph
import dev.seaglass.core.backend.SeaGlassShaderProgramCache
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.floor
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
    AudioWave,
    Pin,
    Unread,
    Mute,
    Compact,
    Fork,
    Export,
    ChevronRight,
    DotGrid,
    PhotoMountain,
    Microphone,
    BrandG,
    Mail,
    EditPencil,
    Speaker,
    PhoneHangup,
    Download,
    Comment,
    MoreVert
}

/**
 * Renders a crisp vector [SeaGlassGlyph] with an **Intaglio Etched-Into-Glass** dual bevel
 * (bottom-right frosted catch-light lip + top-left inner carved wall recess + deep tonal ink).
 */
@Composable
fun SeaGlassIcon(
    glyph: SeaGlassGlyph,
    modifier: Modifier = Modifier.size(20.dp),
    tint: Color = LocalContentColor.current
) {
    val insideEtched = LocalInsideSeaGlassEtched.current
    Canvas(modifier = modifier) {
        val w = size.width
        val h = size.height
        val sw = (min(w, h) * 0.105f).coerceAtLeast(1.8.dp.toPx())
        val stroke = Stroke(width = sw, cap = StrokeCap.Round, join = StrokeJoin.Round)
        if (!insideEtched) {
            val isLightInk = (tint.red * 0.299f + tint.green * 0.587f + tint.blue * 0.114f) > 0.55f
            val lipColor = if (isLightInk) {
                Color(0xFF040C18).copy(alpha = 0.56f * tint.alpha)
            } else {
                Color.White.copy(alpha = 0.68f * tint.alpha)
            }
            val lipOffset = if (isLightInk) {
                Offset(-0.28.dp.toPx(), -0.65.dp.toPx())
            } else {
                Offset(0.32.dp.toPx(), 0.78.dp.toPx())
            }

            // Pass 1: Bottom-right frosted etched lip catch-light
            translate(lipOffset.x, lipOffset.y) {
                drawSeaGlassGlyphGeometry(glyph, w, h, sw, stroke, lipColor)
            }
            // Pass 2: Primary engraved tonal ink
            drawSeaGlassGlyphGeometry(glyph, w, h, sw, stroke, tint.copy(alpha = (tint.alpha * 0.96f).coerceIn(0f, 1f)))
        } else {
            drawSeaGlassGlyphGeometry(glyph, w, h, sw, stroke, tint)
        }
    }
}

private fun DrawScope.drawSeaGlassGlyphGeometry(
    glyph: SeaGlassGlyph,
    w: Float,
    h: Float,
    sw: Float,
    stroke: Stroke,
    tint: Color
) {
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

            SeaGlassGlyph.Pin -> {
                drawLine(tint, Offset(w * 0.32f, h * 0.14f), Offset(w * 0.68f, h * 0.14f), sw, StrokeCap.Round)
                val body = Path().apply {
                    moveTo(w * 0.36f, h * 0.14f)
                    lineTo(w * 0.64f, h * 0.14f)
                    lineTo(w * 0.60f, h * 0.44f)
                    lineTo(w * 0.74f, h * 0.58f)
                    lineTo(w * 0.26f, h * 0.58f)
                    lineTo(w * 0.40f, h * 0.44f)
                    close()
                }
                drawPath(body, color = tint, style = stroke)
                drawLine(tint, Offset(w * 0.50f, h * 0.58f), Offset(w * 0.50f, h * 0.88f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Unread -> {
                val box = Path().apply {
                    moveTo(w * 0.14f, h * 0.38f)
                    lineTo(w * 0.50f, h * 0.14f)
                    lineTo(w * 0.86f, h * 0.38f)
                    lineTo(w * 0.86f, h * 0.82f)
                    lineTo(w * 0.14f, h * 0.82f)
                    close()
                }
                drawPath(box, color = tint, style = stroke)
                val flap = Path().apply {
                    moveTo(w * 0.14f, h * 0.38f)
                    lineTo(w * 0.50f, h * 0.62f)
                    lineTo(w * 0.86f, h * 0.38f)
                }
                drawPath(flap, color = tint, style = stroke)
            }

            SeaGlassGlyph.Mute -> {
                val p = Path().apply {
                    moveTo(w * 0.20f, h * 0.68f)
                    lineTo(w * 0.80f, h * 0.68f)
                    lineTo(w * 0.72f, h * 0.42f)
                    cubicTo(w * 0.72f, h * 0.20f, w * 0.28f, h * 0.20f, w * 0.28f, h * 0.42f)
                    close()
                }
                drawPath(p, color = tint, style = stroke)
                drawArc(
                    color = tint,
                    startAngle = 15f,
                    sweepAngle = 150f,
                    useCenter = false,
                    topLeft = Offset(w * 0.40f, h * 0.66f),
                    size = Size(w * 0.20f, h * 0.18f),
                    style = stroke
                )
            }

            SeaGlassGlyph.Compact -> {
                // Top downward arrow (stops above the center horizontal bar with a clean gap)
                drawLine(tint, Offset(w * 0.50f, h * 0.08f), Offset(w * 0.50f, h * 0.35f), sw, StrokeCap.Round)
                val topHead = Path().apply {
                    moveTo(w * 0.35f, h * 0.23f)
                    lineTo(w * 0.50f, h * 0.36f)
                    lineTo(w * 0.65f, h * 0.23f)
                }
                drawPath(topHead, color = tint, style = stroke)
                // Center horizontal bar
                drawLine(tint, Offset(w * 0.16f, h * 0.50f), Offset(w * 0.84f, h * 0.50f), sw, StrokeCap.Round)
                // Bottom upward arrow (stops below the center horizontal bar with a clean gap)
                drawLine(tint, Offset(w * 0.50f, h * 0.92f), Offset(w * 0.50f, h * 0.65f), sw, StrokeCap.Round)
                val botHead = Path().apply {
                    moveTo(w * 0.35f, h * 0.77f)
                    lineTo(w * 0.50f, h * 0.64f)
                    lineTo(w * 0.65f, h * 0.77f)
                }
                drawPath(botHead, color = tint, style = stroke)
            }

            SeaGlassGlyph.Fork -> {
                drawLine(tint, Offset(w * 0.50f, h * 0.86f), Offset(w * 0.50f, h * 0.52f), sw, StrokeCap.Round)
                val leftBranch = Path().apply {
                    moveTo(w * 0.50f, h * 0.54f)
                    cubicTo(w * 0.50f, h * 0.36f, w * 0.24f, h * 0.34f, w * 0.22f, h * 0.20f)
                }
                val rightBranch = Path().apply {
                    moveTo(w * 0.50f, h * 0.54f)
                    cubicTo(w * 0.50f, h * 0.36f, w * 0.76f, h * 0.34f, w * 0.78f, h * 0.20f)
                }
                drawPath(leftBranch, color = tint, style = stroke)
                drawPath(rightBranch, color = tint, style = stroke)
                val lHead = Path().apply {
                    moveTo(w * 0.16f, h * 0.34f)
                    lineTo(w * 0.22f, h * 0.18f)
                    lineTo(w * 0.38f, h * 0.22f)
                }
                val rHead = Path().apply {
                    moveTo(w * 0.62f, h * 0.22f)
                    lineTo(w * 0.78f, h * 0.18f)
                    lineTo(w * 0.84f, h * 0.34f)
                }
                drawPath(lHead, color = tint, style = stroke)
                drawPath(rHead, color = tint, style = stroke)
            }

            SeaGlassGlyph.Export -> {
                val tray = Path().apply {
                    moveTo(w * 0.34f, h * 0.40f)
                    lineTo(w * 0.20f, h * 0.40f)
                    lineTo(w * 0.20f, h * 0.84f)
                    lineTo(w * 0.80f, h * 0.84f)
                    lineTo(w * 0.80f, h * 0.40f)
                    lineTo(w * 0.66f, h * 0.40f)
                }
                drawPath(tray, color = tint, style = stroke)
                drawLine(tint, Offset(w * 0.50f, h * 0.62f), Offset(w * 0.50f, h * 0.14f), sw, StrokeCap.Round)
                val arrow = Path().apply {
                    moveTo(w * 0.34f, h * 0.28f)
                    lineTo(w * 0.50f, h * 0.12f)
                    lineTo(w * 0.66f, h * 0.28f)
                }
                drawPath(arrow, color = tint, style = stroke)
            }

            SeaGlassGlyph.ChevronRight -> {
                val p = Path().apply {
                    moveTo(w * 0.36f, h * 0.22f)
                    lineTo(w * 0.66f, h * 0.50f)
                    lineTo(w * 0.36f, h * 0.78f)
                }
                drawPath(p, color = tint, style = stroke)
            }

            SeaGlassGlyph.DotGrid -> {
                val dotR = (min(w, h) * 0.055f).coerceAtLeast(1.2f)
                for (row in -2..2) {
                    for (col in -2..2) {
                        if (abs(row) + abs(col) <= 3) {
                            drawCircle(
                                color = tint.copy(alpha = if (abs(row) + abs(col) <= 1) 0.85f else 0.45f),
                                radius = dotR,
                                center = Offset(w * (0.50f + col * 0.16f), h * (0.50f + row * 0.16f))
                            )
                        }
                    }
                }
            }

            SeaGlassGlyph.PhotoMountain -> {
                // Filled rounded square photo icon with cut-out mountain peaks (matches the "Photos" pill icon)
                val frame = Path().apply {
                    addRoundRect(
                        androidx.compose.ui.geometry.RoundRect(
                            left = w * 0.10f,
                            top = h * 0.10f,
                            right = w * 0.90f,
                            bottom = h * 0.90f,
                            cornerRadius = CornerRadius(w * 0.18f, h * 0.18f)
                        )
                    )
                }
                val mountains = Path().apply {
                    moveTo(w * 0.18f, h * 0.78f)
                    lineTo(w * 0.40f, h * 0.48f)
                    lineTo(w * 0.56f, h * 0.68f)
                    lineTo(w * 0.68f, h * 0.54f)
                    lineTo(w * 0.84f, h * 0.78f)
                    close()
                }
                val combined = Path.combine(androidx.compose.ui.graphics.PathOperation.Difference, frame, mountains)
                drawPath(combined, color = tint)
            }

            SeaGlassGlyph.Microphone -> {
                // Capsule microphone head + U-shaped cradle + base stem (matches "Ask Gemini" mic)
                val micHeadR = w * 0.14f
                drawRoundRect(
                    color = tint,
                    topLeft = Offset(w * 0.50f - micHeadR, h * 0.12f),
                    size = Size(micHeadR * 2f, h * 0.44f),
                    cornerRadius = CornerRadius(micHeadR, micHeadR),
                    style = stroke
                )
                drawArc(
                    color = tint,
                    startAngle = 0f,
                    sweepAngle = 180f,
                    useCenter = false,
                    topLeft = Offset(w * 0.24f, h * 0.26f),
                    size = Size(w * 0.52f, h * 0.46f),
                    style = stroke
                )
                drawLine(
                    color = tint,
                    start = Offset(w * 0.50f, h * 0.72f),
                    end = Offset(w * 0.50f, h * 0.88f),
                    strokeWidth = sw,
                    cap = StrokeCap.Round
                )
            }

            SeaGlassGlyph.BrandG -> {
                // Bold geometric Google-style "G" emblem matching the reference peach sea-glass pebble:
                // Clipped to the exact outer circular boundary so the horizontal crossbar and right arc
                // form a single seamless circular contour with zero protruding corner.
                val gStroke = (min(w, h) * 0.195f).coerceAtLeast(2.8.dp.toPx())
                val radius = min(w, h) * 0.35f
                val outerR = radius + gStroke * 0.5f
                val outerCircle = Path().apply {
                    addOval(
                        androidx.compose.ui.geometry.Rect(
                            left = w * 0.50f - outerR,
                            top = h * 0.50f - outerR,
                            right = w * 0.50f + outerR,
                            bottom = h * 0.50f + outerR
                        )
                    )
                }
                drawContext.canvas.save()
                drawContext.canvas.clipPath(outerCircle)
                drawArc(
                    color = tint,
                    startAngle = -10f,
                    sweepAngle = 322f,
                    useCenter = false,
                    topLeft = Offset(w * 0.50f - radius, h * 0.50f - radius),
                    size = Size(radius * 2f, radius * 2f),
                    style = Stroke(width = gStroke, cap = StrokeCap.Butt)
                )
                drawRect(
                    color = tint,
                    topLeft = Offset(w * 0.50f - gStroke * 0.08f, h * 0.50f - gStroke * 0.46f),
                    size = Size(outerR + gStroke * 0.2f, gStroke * 0.92f)
                )
                drawContext.canvas.restore()
            }

            SeaGlassGlyph.Mail -> {
                // Filled rounded envelope with cut-out V-flap matching the "Mail" pill icon in Reference Image 1
                val env = Path().apply {
                    addRoundRect(
                        androidx.compose.ui.geometry.RoundRect(
                            left = w * 0.08f,
                            top = h * 0.18f,
                            right = w * 0.92f,
                            bottom = h * 0.82f,
                            cornerRadius = CornerRadius(w * 0.12f, h * 0.12f)
                        )
                    )
                }
                val cutFlap = Path().apply {
                    moveTo(w * 0.06f, h * 0.22f)
                    lineTo(w * 0.50f, h * 0.54f)
                    lineTo(w * 0.94f, h * 0.22f)
                    lineTo(w * 0.94f, h * 0.32f)
                    lineTo(w * 0.50f, h * 0.64f)
                    lineTo(w * 0.06f, h * 0.32f)
                    close()
                }
                val combined = Path.combine(androidx.compose.ui.graphics.PathOperation.Difference, env, cutFlap)
                drawPath(combined, color = tint)
            }

            SeaGlassGlyph.EditPencil -> {
                // Diagonal pencil glyph matching the Compose button & square FAB in Reference Images 1 & 2
                val body = Path().apply {
                    moveTo(w * 0.16f, h * 0.84f)
                    lineTo(w * 0.20f, h * 0.62f)
                    lineTo(w * 0.68f, h * 0.14f)
                    lineTo(w * 0.86f, h * 0.32f)
                    lineTo(w * 0.38f, h * 0.80f)
                    close()
                }
                drawPath(body, color = tint, style = stroke)
                drawLine(
                    color = tint,
                    start = Offset(w * 0.56f, h * 0.26f),
                    end = Offset(w * 0.74f, h * 0.44f),
                    strokeWidth = sw * 0.95f,
                    cap = StrokeCap.Round
                )
            }

            SeaGlassGlyph.Speaker -> {
                // Filled speaker cone + two acoustic wave arcs matching the "Speaker" button in Reference Image 3
                val cone = Path().apply {
                    moveTo(w * 0.12f, h * 0.38f)
                    lineTo(w * 0.28f, h * 0.38f)
                    lineTo(w * 0.50f, h * 0.18f)
                    lineTo(w * 0.50f, h * 0.82f)
                    lineTo(w * 0.28f, h * 0.62f)
                    lineTo(w * 0.12f, h * 0.62f)
                    close()
                }
                drawPath(cone, color = tint)
                drawArc(
                    color = tint,
                    startAngle = -42f,
                    sweepAngle = 84f,
                    useCenter = false,
                    topLeft = Offset(w * 0.34f, h * 0.30f),
                    size = Size(w * 0.36f, h * 0.40f),
                    style = Stroke(width = sw, cap = StrokeCap.Round)
                )
                drawArc(
                    color = tint,
                    startAngle = -48f,
                    sweepAngle = 96f,
                    useCenter = false,
                    topLeft = Offset(w * 0.24f, h * 0.16f),
                    size = Size(w * 0.64f, h * 0.68f),
                    style = Stroke(width = sw, cap = StrokeCap.Round)
                )
            }

            SeaGlassGlyph.PhoneHangup -> {
                // Downward curved phone receiver matching the coral end-call pill in Reference Image 3
                val p = Path().apply {
                    moveTo(w * 0.08f, h * 0.56f)
                    cubicTo(w * 0.24f, h * 0.28f, w * 0.76f, h * 0.28f, w * 0.92f, h * 0.56f)
                    lineTo(w * 0.78f, h * 0.70f)
                    lineTo(w * 0.64f, h * 0.56f)
                    cubicTo(w * 0.56f, h * 0.48f, w * 0.44f, h * 0.48f, w * 0.36f, h * 0.56f)
                    lineTo(w * 0.22f, h * 0.70f)
                    close()
                }
                drawPath(p, color = tint)
            }

            SeaGlassGlyph.Download -> {
                drawLine(tint, Offset(w * 0.50f, h * 0.16f), Offset(w * 0.50f, h * 0.66f), sw, StrokeCap.Round)
                val head = Path().apply {
                    moveTo(w * 0.30f, h * 0.46f)
                    lineTo(w * 0.50f, h * 0.68f)
                    lineTo(w * 0.70f, h * 0.46f)
                }
                drawPath(head, color = tint, style = stroke)
                drawLine(tint, Offset(w * 0.22f, h * 0.82f), Offset(w * 0.78f, h * 0.82f), sw, StrokeCap.Round)
            }

            SeaGlassGlyph.Comment -> {
                val bubble = Path().apply {
                    moveTo(w * 0.16f, h * 0.20f)
                    lineTo(w * 0.84f, h * 0.20f)
                    lineTo(w * 0.84f, h * 0.68f)
                    lineTo(w * 0.38f, h * 0.68f)
                    lineTo(w * 0.16f, h * 0.86f)
                    close()
                }
                drawPath(bubble, color = tint, style = stroke)
                drawLine(tint, Offset(w * 0.30f, h * 0.38f), Offset(w * 0.70f, h * 0.38f), sw * 0.85f, StrokeCap.Round)
                drawLine(tint, Offset(w * 0.30f, h * 0.52f), Offset(w * 0.58f, h * 0.52f), sw * 0.85f, StrokeCap.Round)
            }

            SeaGlassGlyph.MoreVert -> {
                val r = (min(w, h) * 0.085f).coerceAtLeast(1.8f)
                drawCircle(tint, radius = r, center = Offset(w * 0.50f, h * 0.22f))
                drawCircle(tint, radius = r, center = Offset(w * 0.50f, h * 0.50f))
                drawCircle(tint, radius = r, center = Offset(w * 0.50f, h * 0.78f))
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

    val rawFocusProgress by animateFloatAsState(
        targetValue = if (isFocused) 1f else 0f,
        animationSpec = motion.fastEffects,
        label = "searchFocus"
    )
    val focusProgress = SeaGlassShapeMorph.sCurve(rawFocusProgress)

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
                val clearFrostTop = Color(
                    SeaGlassColorMath.computeFrostedRindColor(activeMaterial.tintColorArgb, activeMaterial.coarseness, 0.68f)
                ).copy(alpha = 0.78f)
                val clearFrostBot = Color(
                    SeaGlassColorMath.computeFrostedRindColor(activeMaterial.tintColorArgb, activeMaterial.coarseness, 0.52f)
                ).copy(alpha = 0.68f)
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(28.dp)
                        .clip(CircleShape)
                        .background(Brush.verticalGradient(listOf(clearFrostTop, clearFrostBot)))
                        .border(1.1.dp, Color.White.copy(alpha = 0.72f), CircleShape)
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
                val medFrostTop = Color(
                    SeaGlassColorMath.computeFrostedRindColor(activeMaterial.tintColorArgb, activeMaterial.coarseness, 0.68f)
                ).copy(alpha = 0.78f)
                val medFrostBot = Color(
                    SeaGlassColorMath.computeFrostedRindColor(activeMaterial.tintColorArgb, activeMaterial.coarseness, 0.52f)
                ).copy(alpha = 0.68f)
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(34.dp)
                        .clip(CircleShape)
                        .background(Brush.verticalGradient(listOf(medFrostTop, medFrostBot)))
                        .border(1.2.dp, Color.White.copy(alpha = 0.76f), CircleShape)
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = onTrailingClick
                        )
                ) {
                    SeaGlassIcon(
                        glyph = trailingGlyph,
                        modifier = Modifier.size(16.dp),
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

    val rawFocusProgress by animateFloatAsState(
        targetValue = if (isFocused) 1f else 0f,
        animationSpec = motion.fastEffects,
        label = "textFieldFocus"
    )
    val focusProgress = SeaGlassShapeMorph.sCurve(rawFocusProgress)
    val rawCornerProgress by animateFloatAsState(
        targetValue = if (isFocused) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "textFieldCorner"
    )
    val cornerRadius = 18f + 6f * SeaGlassShapeMorph.physicsSCurve(rawCornerProgress, overshootGain = 0.35f)

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
 * Tactile SeaGlass Toggle Switch featuring an inlaid, sand-frosted sea-glass capsule track
 * (which changes color based on its state—such as coral-red sea glass embedded in green sea glass
 * when off, morphing in Oklab space to the active sea-glass tint when on) paired with a
 * **crystal-clear 3D refractive glass lens circle** that transmits the track underneath and
 * reflects overhead light with a Fresnel meniscus rim, upper-left dome specular glint, and
 * focused lower-right internal caustic.
 */
@Composable
fun SeaGlassSwitch(
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    checkedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    uncheckedMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    checkedTrackColor: Color? = null,
    uncheckedTrackColor: Color? = null
) {
    val motion = LocalSeaGlassMotionScheme.current
    val density = LocalDensity.current

    val targetSwitchVal = if (checked) 1f else 0f
    val rawLeadThumb by animateFloatAsState(
        targetValue = targetSwitchVal,
        animationSpec = spring(dampingRatio = 0.66f, stiffness = 560f),
        label = "switchLeadThumb"
    )
    val rawTrailThumb by animateFloatAsState(
        targetValue = targetSwitchVal,
        animationSpec = spring(dampingRatio = 0.74f, stiffness = 320f),
        label = "switchTrailThumb"
    )
    val rawTintProgress by animateFloatAsState(
        targetValue = targetSwitchVal,
        animationSpec = motion.defaultEffects,
        label = "switchTintEffects"
    )
    val leadThumb = SeaGlassShapeMorph.physicsSCurve(rawLeadThumb, overshootGain = 0.45f)
    val trailThumb = SeaGlassShapeMorph.physicsSCurve(rawTrailThumb, overshootGain = 0.45f)
    val tintProgress = SeaGlassShapeMorph.sCurve(rawTintProgress)

    val leftThumbFrac = min(leadThumb, trailThumb).coerceIn(-0.04f, 1.04f)
    val rightThumbFrac = max(leadThumb, trailThumb).coerceIn(-0.04f, 1.04f)
    val stretchFrac = (rightThumbFrac - leftThumbFrac).coerceIn(0f, 0.75f)

    val trackWidthDp = 54.dp
    val trackHeightDp = 31.dp
    val baseThumbSizeDp = 25.dp
    val padPx = with(density) { 3.dp.toPx() }
    val travelPx = with(density) { (trackWidthDp - baseThumbSizeDp - 6.dp).toPx() }
    val thumbOffsetX = (padPx + travelPx * leftThumbFrac).roundToInt()
    val dynamicThumbWidthDp = baseThumbSizeDp + with(density) { (travelPx * stretchFrac).toDp() }

    val t = tintProgress.coerceIn(0f, 1f)
    val offTintArgb = uncheckedTrackColor?.toArgb() ?: uncheckedMaterial.tintColorArgb
    val onTintArgb = checkedTrackColor?.toArgb() ?: checkedMaterial.tintColorArgb
    val activeTrackTintArgb = remember(offTintArgb, onTintArgb, t) {
        SeaGlassColorMath.lerpOklab(offTintArgb, onTintArgb, t)
    }
    val activeCoarseness = uncheckedMaterial.coarseness +
        (checkedMaterial.coarseness - uncheckedMaterial.coarseness) * t
    val activeDensity = uncheckedMaterial.tintDensity +
        (checkedMaterial.tintDensity - uncheckedMaterial.tintDensity) * t

    val deepTopArgb = remember(activeTrackTintArgb, activeDensity) {
        SeaGlassColorMath.computeDeepVolumetricCoreColor(activeTrackTintArgb, (activeDensity + 0.15f).coerceAtMost(0.95f))
    }
    val midBodyArgb = remember(activeTrackTintArgb, activeCoarseness) {
        SeaGlassColorMath.computeFrostedRindColor(activeTrackTintArgb, activeCoarseness, 0.12f)
    }
    val causticBotArgb = remember(activeTrackTintArgb, activeCoarseness) {
        SeaGlassColorMath.computeSubsurfaceCausticColor(activeTrackTintArgb, activeCoarseness)
    }
    val frostedBevelArgb = remember(activeTrackTintArgb, activeCoarseness) {
        SeaGlassColorMath.computeFrostedRindColor(activeTrackTintArgb, activeCoarseness, 0.68f)
    }

    val trackPath = remember { android.graphics.Path() }
    val trackBevelPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.STROKE
            strokeCap = android.graphics.Paint.Cap.ROUND
            strokeJoin = android.graphics.Paint.Join.ROUND
        }
    }
    val trackSocketPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.STROKE
            strokeCap = android.graphics.Paint.Cap.ROUND
            strokeJoin = android.graphics.Paint.Join.ROUND
        }
    }
    val trackGrainPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.FILL
            isFilterBitmap = false
            xfermode = android.graphics.PorterDuffXfermode(android.graphics.PorterDuff.Mode.OVERLAY)
            shader = dev.seaglass.core.SeaGlassSandGrainTexture.obtainShader()
        }
    }
    val trackGrainMatrix = remember { android.graphics.Matrix() }

    val thumbPath = remember { android.graphics.Path() }
    val thumbShadowPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.FILL
        }
    }
    val thumbFresnelPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.STROKE
            strokeCap = android.graphics.Paint.Cap.ROUND
            strokeJoin = android.graphics.Paint.Join.ROUND
        }
    }
    val thumbRimPaint = remember {
        android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
            style = android.graphics.Paint.Style.STROKE
            strokeCap = android.graphics.Paint.Cap.ROUND
            strokeJoin = android.graphics.Paint.Join.ROUND
        }
    }

    Box(
        contentAlignment = Alignment.CenterStart,
        modifier = modifier
            .size(width = trackWidthDp, height = trackHeightDp)
            .drawWithContent {
                val w = size.width
                val h = size.height
                val r = min(w, h) * 0.5f
                trackPath.reset()
                trackPath.addRoundRect(0f, 0f, w, h, r, r, android.graphics.Path.Direction.CW)

                // 1. Embedded Sea-Glass Track Body (Volumetric Top -> Mid Pastel -> Lower Subsurface Caustic)
                drawRoundRect(
                    brush = Brush.verticalGradient(
                        0.00f to Color(deepTopArgb).copy(alpha = 0.90f),
                        0.52f to Color(midBodyArgb).copy(alpha = 0.88f),
                        1.00f to Color(causticBotArgb).copy(alpha = 0.94f)
                    ),
                    cornerRadius = CornerRadius(r, r)
                )

                val nCanvas = drawContext.canvas.nativeCanvas
                nCanvas.save()
                nCanvas.clipPath(trackPath)

                // 2. Embedded Sea-Glass Inner Bevel Cushion & Subsurface Caustic Rim
                val topBevelInt = (0x58 shl 24) or (frostedBevelArgb and 0x00FFFFFF)
                val midBevelInt = (0x78 shl 24) or (frostedBevelArgb and 0x00FFFFFF)
                val botBevelInt = (0xD8 shl 24) or (causticBotArgb and 0x00FFFFFF)
                trackBevelPaint.strokeWidth = 6.2f * this.density
                trackBevelPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(2.2f * this.density)
                trackBevelPaint.shader = android.graphics.LinearGradient(
                    w * 0.5f,
                    0f,
                    w * 0.5f,
                    h,
                    intArrayOf(topBevelInt, midBevelInt, botBevelInt),
                    floatArrayOf(0.0f, 0.45f, 1.0f),
                    Shader.TileMode.CLAMP
                )
                nCanvas.drawPath(trackPath, trackBevelPaint)

                // 3. Authentic Pinpoint Sea-Glass Sand-Pitting on the Embedded Track
                val grainScale = (this.density * 0.38f).coerceIn(0.78f, 1.22f)
                trackGrainMatrix.reset()
                trackGrainMatrix.setScale(grainScale, grainScale)
                trackGrainPaint.shader?.setLocalMatrix(trackGrainMatrix)
                trackGrainPaint.alpha = (activeCoarseness.coerceIn(0f, 1f) * 155f).toInt().coerceIn(0, 165)
                if (trackGrainPaint.alpha > 4) {
                    nCanvas.drawRect(0f, 0f, w, h, trackGrainPaint)
                }

                // 4. Inlaid Socket Bevel Rim (recessed top-left inner shadow + frosted bottom socket catch-light)
                trackSocketPaint.strokeWidth = 1.6f * this.density
                trackSocketPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(0.55f * this.density)
                trackSocketPaint.shader = android.graphics.LinearGradient(
                    w * 0.5f,
                    0f,
                    w * 0.5f,
                    h,
                    intArrayOf(0x48061420, 0x38FFFFFF, 0xB8FFFFFF.toInt()),
                    floatArrayOf(0.0f, 0.48f, 1.0f),
                    Shader.TileMode.CLAMP
                )
                nCanvas.drawPath(trackPath, trackSocketPaint)
                nCanvas.restore()

                this@drawWithContent.drawContent()
            }
            .clickable(
                interactionSource = remember { MutableInteractionSource() },
                indication = null,
                onClick = { onCheckedChange(!checked) }
            )
    ) {
        // 3D Clear-Tinted Sea-Glass Circle (Thumb) — "Clear like no color, but still some tint"
        // Modeled after the frosted clear-milk "Mail" inlay pill in Reference Image 2:
        // No intrinsic pigment of its own, yet picks up a soft ~26% Oklab tint from the underlying
        // sea-glass track, with a 360-degree pillowy frosted bevel, subsurface bottom caustic lift,
        // and fine sea-glass sand grain (zero glossy soap-bubble arcs!).
        val clearTintedTop = remember(activeTrackTintArgb, activeCoarseness) {
            Color(SeaGlassColorMath.computeFrostedRindColor(activeTrackTintArgb, activeCoarseness, 0.78f))
        }
        val clearTintedMid = remember(activeTrackTintArgb, activeCoarseness) {
            Color(SeaGlassColorMath.computeFrostedRindColor(activeTrackTintArgb, activeCoarseness, 0.66f))
        }
        val clearTintedBot = remember(causticBotArgb, activeCoarseness) {
            Color(SeaGlassColorMath.computeFrostedRindColor(causticBotArgb, activeCoarseness, 0.84f))
        }

        Box(
            modifier = Modifier
                .offset { IntOffset(thumbOffsetX, 0) }
                .size(width = dynamicThumbWidthDp, height = baseThumbSizeDp)
                .graphicsLayer {
                    scaleY = 1f - 0.10f * stretchFrac
                }
                .drawWithContent {
                    val w = size.width
                    val h = size.height
                    val r = min(w, h) * 0.5f
                    thumbPath.reset()
                    thumbPath.addRoundRect(0f, 0f, w, h, r, r, android.graphics.Path.Direction.CW)

                    val nCanvas = drawContext.canvas.nativeCanvas

                    // Pass 1: Soft grounding shadow strictly OUTSIDE the clear-tinted sea-glass circle
                    thumbShadowPaint.color = 0x36061420
                    thumbShadowPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(2.2f * this.density)
                    nCanvas.save()
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                        nCanvas.clipOutPath(thumbPath)
                    }
                    nCanvas.translate(0.2f * this.density, 1.5f * this.density)
                    nCanvas.drawPath(thumbPath, thumbShadowPaint)
                    nCanvas.restore()

                    // Pass 2: Clear-Tinted Frosted Sea-Glass Body (no color of its own, softly tinted by track)
                    nCanvas.save()
                    nCanvas.clipPath(thumbPath)

                    drawRoundRect(
                        brush = Brush.verticalGradient(
                            0.00f to clearTintedTop.copy(alpha = 0.62f),
                            0.48f to clearTintedMid.copy(alpha = 0.52f),
                            1.00f to clearTintedBot.copy(alpha = 0.74f)
                        ),
                        cornerRadius = CornerRadius(r, r)
                    )

                    // Soft subsurface caustic reflection along the lower third of the circle
                    drawRoundRect(
                        brush = Brush.radialGradient(
                            colors = listOf(
                                Color.White.copy(alpha = 0.46f),
                                clearTintedBot.copy(alpha = 0.28f),
                                Color.Transparent
                            ),
                            center = Offset(w * 0.54f, h * 0.78f),
                            radius = max(w, h) * 0.58f
                        ),
                        cornerRadius = CornerRadius(r, r)
                    )

                    // Pass 3: Pillowy 360-Degree Frosted Sea-Glass Bevel + Fine Sand-Grain Texture
                    thumbFresnelPaint.strokeWidth = 5.2f * this.density
                    thumbFresnelPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(2.0f * this.density)
                    thumbFresnelPaint.shader = android.graphics.LinearGradient(
                        w * 0.5f,
                        0f,
                        w * 0.5f,
                        h,
                        intArrayOf(
                            0x98FFFFFF.toInt(),
                            0x68FFFFFF,
                            0xDCFFFFFF.toInt()
                        ),
                        floatArrayOf(0.0f, 0.45f, 1.0f),
                        Shader.TileMode.CLAMP
                    )
                    nCanvas.drawPath(thumbPath, thumbFresnelPaint)

                    val grainScale = (this.density * 0.38f).coerceIn(0.78f, 1.22f)
                    trackGrainMatrix.reset()
                    trackGrainMatrix.setScale(grainScale, grainScale)
                    trackGrainPaint.shader?.setLocalMatrix(trackGrainMatrix)
                    trackGrainPaint.alpha = (activeCoarseness.coerceIn(0f, 1f) * 125f).toInt().coerceIn(0, 140)
                    if (trackGrainPaint.alpha > 4) {
                        nCanvas.drawRect(0f, 0f, w, h, trackGrainPaint)
                    }

                    // Soft tumbled outer rim lip
                    thumbRimPaint.strokeWidth = 1.2f * this.density
                    thumbRimPaint.maskFilter = SeaGlassShaderProgramCache.obtainBlurMaskFilter(0.45f * this.density)
                    thumbRimPaint.shader = android.graphics.LinearGradient(
                        w * 0.5f,
                        0f,
                        w * 0.5f,
                        h,
                        intArrayOf(
                            0x88FFFFFF.toInt(),
                            0x52FFFFFF,
                            0xCCFFFFFF.toInt()
                        ),
                        floatArrayOf(0.0f, 0.48f, 1.0f),
                        Shader.TileMode.CLAMP
                    )
                    nCanvas.drawPath(thumbPath, thumbRimPaint)
                    nCanvas.restore()
                }
        )
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
    val rawProgress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "checkboxMorph"
    )
    val rawTintProgress by animateFloatAsState(
        targetValue = if (checked) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "checkboxTint"
    )
    val progress = SeaGlassShapeMorph.physicsSCurve(rawProgress, overshootGain = 0.50f)
    val tintProgress = SeaGlassShapeMorph.sCurve(rawTintProgress)

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
    val rawProgress by animateFloatAsState(
        targetValue = if (selected) 1f else 0f,
        animationSpec = motion.fastSpatial,
        label = "radioSpatial"
    )
    val rawTintProgress by animateFloatAsState(
        targetValue = if (selected) 1f else 0f,
        animationSpec = motion.defaultEffects,
        label = "radioTint"
    )
    val progress = SeaGlassShapeMorph.physicsSCurve(rawProgress, overshootGain = 0.45f)
    val tintProgress = SeaGlassShapeMorph.sCurve(rawTintProgress)

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

    val targetSegFloat = selectedIndex.coerceIn(0, count - 1).toFloat()
    val rawLeadSegIndex by animateFloatAsState(
        targetValue = targetSegFloat,
        animationSpec = spring(dampingRatio = 0.72f, stiffness = 520f),
        label = "segmentedLeadEdge"
    )
    val rawTrailSegIndex by animateFloatAsState(
        targetValue = targetSegFloat,
        animationSpec = spring(dampingRatio = 0.76f, stiffness = 340f),
        label = "segmentedTrailEdge"
    )
    val leadSegBase = floor(rawLeadSegIndex)
    val leadSegIndex = leadSegBase + SeaGlassShapeMorph.physicsSCurve(rawLeadSegIndex - leadSegBase, overshootGain = 0.35f)
    val trailSegBase = floor(rawTrailSegIndex)
    val trailSegIndex = trailSegBase + SeaGlassShapeMorph.physicsSCurve(rawTrailSegIndex - trailSegBase, overshootGain = 0.35f)
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
        Color(indicatorMaterial.tintColorArgb).copy(alpha = 0.58f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(
                indicatorMaterial.tintColorArgb,
                indicatorMaterial.coarseness,
                0.66f
            )
        ).copy(alpha = 0.76f)
    }
    val pillBottomColor = if (isDarkBar) {
        Color(0xFF0A1E30).copy(alpha = 0.42f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(
                indicatorMaterial.tintColorArgb,
                indicatorMaterial.coarseness,
                0.50f
            )
        ).copy(alpha = 0.68f)
    }
    val pillRimColor = if (isDarkBar) {
        Color.White.copy(alpha = 0.36f)
    } else {
        Color(
            SeaGlassColorMath.computeFrostedRindColor(
                indicatorMaterial.tintColorArgb,
                indicatorMaterial.coarseness,
                0.86f
            )
        ).copy(alpha = 0.84f)
    }

    val segStretchMalleability = SeaGlassShapeMorph.sCurve(abs(leadSegIndex - trailSegIndex)) * 0.78f
    val activeSegMaterial = remember(containerMaterial, segStretchMalleability) {
        containerMaterial.copy(cornerRadiusDp = 23f, thicknessDp = 15f).deaged(segStretchMalleability)
    }

    ProvideSeaGlassLegibleContent(contentColor = contentColor) {
        Box(
            modifier = modifier
                .fillMaxWidth()
                .height(46.dp)
                .onSizeChanged { barWidthPx = it.width.toFloat().coerceAtLeast(1f) }
                .seaGlass(
                    stageState = stageState,
                    material = activeSegMaterial,
                    contentRole = SeaGlassContentRole.NAVIGATION_CHROME
                )
                .padding(4.dp)
        ) {
            val innerW = (barWidthPx - with(density) { 8.dp.toPx() }).coerceAtLeast(1f)
            val slotW = innerW / count
            val leftSeg = min(leadSegIndex, trailSegIndex)
            val rightSeg = max(leadSegIndex, trailSegIndex) + 1f
            val stretchSpan = abs(leadSegIndex - trailSegIndex).coerceIn(0f, 1.5f)
            val indicatorOffsetX = (leftSeg * slotW).roundToInt()
            val indicatorWidthDp = with(density) { ((rightSeg - leftSeg) * slotW).toDp() }

            Box(
                modifier = Modifier
                    .offset { IntOffset(indicatorOffsetX, 0) }
                    .width(indicatorWidthDp)
                    .fillMaxHeight()
                    .graphicsLayer {
                        scaleY = 1f - stretchSpan * 0.10f
                    }
                    .seaGlassBeveledInlay(
                        tintColorArgb = indicatorMaterial.tintColorArgb,
                        coarseness = indicatorMaterial.coarseness,
                        cornerRadiusDp = 19f
                    )
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

    val motion = LocalSeaGlassMotionScheme.current
    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = targetMaterial,
        contentPadding = PaddingValues(horizontal = 14.dp, vertical = 9.dp),
        modifier = modifier
    ) {
        AnimatedVisibility(
            visible = selected,
            enter = expandHorizontally(animationSpec = spring(dampingRatio = 0.80f, stiffness = 500f)) +
                fadeIn(animationSpec = tween(180, easing = SeaGlassPhysicsSCurveEasing)),
            exit = shrinkHorizontally(animationSpec = spring(dampingRatio = 0.88f, stiffness = 580f)) +
                fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
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
    val motion = LocalSeaGlassMotionScheme.current
    val rawExpand by animateFloatAsState(
        targetValue = if (expanded) 1f else 0f,
        animationSpec = if (expanded) motion.morphOpenSpatial else motion.morphCloseSpatial,
        label = "extendedFabMorph"
    )
    val expandEase = SeaGlassShapeMorph.physicsSCurve(rawExpand, overshootGain = 0.35f).coerceIn(0f, 1.08f)
    SeaGlassButton(
        onClick = onClick,
        stageState = stageState,
        material = material.copy(
            cornerRadiusDp = 20f + 8f * expandEase.coerceIn(0f, 1f),
            thicknessDp = 20f,
            rimChalkiness = 0.88f
        ),
        contentPadding = PaddingValues(
            horizontal = (16f + 4f * expandEase.coerceIn(0f, 1f)).dp,
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
            enter = expandHorizontally(animationSpec = spring(dampingRatio = 0.80f, stiffness = 500f)) +
                fadeIn(animationSpec = tween(180, easing = SeaGlassPhysicsSCurveEasing)),
            exit = shrinkHorizontally(animationSpec = spring(dampingRatio = 0.88f, stiffness = 580f)) +
                fadeOut(animationSpec = tween(140, easing = SeaGlassPhysicsSCurveEasing))
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

    val rawDetachedProgress by animateFloatAsState(
        targetValue = if (fabDetached) 1f else 0f,
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 400f),
        label = "toolbarFabGap"
    )
    val detachedProgress = SeaGlassShapeMorph.physicsSCurve(rawDetachedProgress, overshootGain = 0.45f)
    val gapDp = ((-6f) + 24f * detachedProgress).dp
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

    val toolbarGapMalleability = SeaGlassShapeMorph.sCurveBell(rawDetachedProgress)
    val activeToolbarMaterial = remember(barMaterial, toolbarGapMalleability) {
        barMaterial.deaged(toolbarGapMalleability)
    }

    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(heightDp)
            .onSizeChanged { totalWidthPx = it.width.toFloat().coerceAtLeast(240f) }
            .seaGlass(
                stageState = stageState,
                material = activeToolbarMaterial,
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

/**
 * Corner-Anchored Liquid Teardrop Context Menu Morph (`SeaGlassMorphingContextMenu`).
 *
 * Matches the iOS 26 Liquid Glass `(•••)` context menu morph:
 * - **Collapsed**: Circular `44.dp` `(•••)` icon button anchored at [anchorAlignment] (`Alignment.TopEnd`).
 * - **In-Flight**: Stretches out of (and sucks back into) the corner `(•••)` button via a 2-node
 *   `sminCubic` liquid teardrop neck while Gaussian-blurring and proportionally scaling the menu rows.
 * - **Expanded**: Full context menu card with a top 3-action header band (`Pin`, `Settle`, `Rename...`)
 *   and action list rows (`Files`, `Terminal`, `Git · main`, `Snooze`, `Regenerate Title`, `Delete...`).
 */
@Composable
fun SeaGlassMorphingContextMenu(
    expanded: Boolean,
    onExpandedChange: (Boolean) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    anchorAlignment: Alignment = Alignment.TopEnd,
    onActionSelected: (String) -> Unit = {},
    buttonMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    menuMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass
) {
    val quickHeaderActions = remember {
        listOf(
            SeaGlassGlyph.Bookmark to "Pin",
            SeaGlassGlyph.Check to "Settle",
            SeaGlassGlyph.Sparkles to "Rename…"
        )
    }
    val menuRows = remember {
        listOf(
            Triple(SeaGlassGlyph.Layers, "Files", ""),
            Triple(SeaGlassGlyph.Tune, "Terminal", ""),
            Triple(SeaGlassGlyph.Share, "Git", "main"),
            Triple(SeaGlassGlyph.Bell, "Snooze", "›"),
            Triple(SeaGlassGlyph.Sparkles, "Regenerate Title", ""),
            Triple(SeaGlassGlyph.Close, "Delete…", "destructive")
        )
    }

    SeaGlassTwoElementMorph(
        expanded = expanded,
        onToggle = { onExpandedChange(!expanded) },
        stageState = stageState,
        collapsedMaterial = buttonMaterial.copy(cornerRadiusDp = 22f, thicknessDp = 16f),
        expandedMaterial = menuMaterial.copy(cornerRadiusDp = 28f, thicknessDp = 20f),
        morphMode = SeaGlassMorphMode.LIQUID_COALESCE,
        anchorAlignment = anchorAlignment,
        reserveCollapsedLayoutSpace = true,
        modifier = modifier,
        collapsedContent = {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier.size(44.dp)
            ) {
                Row(horizontalArrangement = Arrangement.spacedBy(3.5.dp)) {
                    repeat(3) {
                        Box(
                            modifier = Modifier
                                .size(4.5.dp)
                                .clip(CircleShape)
                                .background(LocalContentColor.current)
                        )
                    }
                }
            }
        },
        expandedContent = {
            Column(
                modifier = Modifier
                    .width(276.dp)
                    .padding(10.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                // Top sky-tinted 3-action quick header strip (Pin / Settle / Rename...)
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(20.dp))
                        .background(
                            Brush.verticalGradient(
                                colors = listOf(
                                    Color(buttonMaterial.tintColorArgb).copy(alpha = 0.36f),
                                    Color(buttonMaterial.tintColorArgb).copy(alpha = 0.16f)
                                )
                            )
                        )
                        .padding(vertical = 10.dp, horizontal = 6.dp),
                    horizontalArrangement = Arrangement.SpaceEvenly,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    quickHeaderActions.forEach { (glyph, title) ->
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally,
                            verticalArrangement = Arrangement.spacedBy(4.dp),
                            modifier = Modifier
                                .clip(RoundedCornerShape(12.dp))
                                .clickable(
                                    interactionSource = remember { MutableInteractionSource() },
                                    indication = null,
                                    onClick = {
                                        onActionSelected(title)
                                        onExpandedChange(false)
                                    }
                                )
                                .padding(horizontal = 6.dp, vertical = 4.dp)
                        ) {
                            SeaGlassIcon(
                                glyph = glyph,
                                modifier = Modifier.size(18.dp),
                                tint = LocalContentColor.current
                            )
                            Text(
                                text = title,
                                maxLines = 1,
                                color = LocalContentColor.current,
                                style = MaterialTheme.typography.labelSmall.copy(
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 11.sp
                                )
                            )
                        }
                    }
                }

                // Menu Action List Rows
                menuRows.forEach { (glyph, label, meta) ->
                    val isDestructive = meta == "destructive"
                    val rowColor = if (isDestructive) Color(0xFFD92644) else LocalContentColor.current
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(12.dp),
                        modifier = Modifier
                            .fillMaxWidth()
                            .clip(RoundedCornerShape(14.dp))
                            .clickable(
                                interactionSource = remember { MutableInteractionSource() },
                                indication = null,
                                onClick = {
                                    onActionSelected(label)
                                    onExpandedChange(false)
                                }
                            )
                            .padding(horizontal = 12.dp, vertical = 9.dp)
                    ) {
                        SeaGlassIcon(
                            glyph = glyph,
                            modifier = Modifier.size(18.dp),
                            tint = rowColor
                        )
                        Text(
                            text = label,
                            color = rowColor,
                            modifier = Modifier.weight(1f),
                            style = MaterialTheme.typography.bodyMedium.copy(
                                fontWeight = FontWeight.SemiBold,
                                fontSize = 14.sp
                            )
                        )
                        if (meta.isNotEmpty() && !isDestructive) {
                            Text(
                                text = meta,
                                color = LocalContentColor.current.copy(alpha = 0.65f),
                                style = MaterialTheme.typography.labelSmall.copy(
                                    fontWeight = FontWeight.Bold,
                                    fontSize = 12.sp
                                )
                            )
                        }
                    }
                }
            }
        }
    )
}

/**
 * 3-State Cascaded Composer Model & Effort Picker Morph (`SeaGlassCascadedModelMorph`):
 * - **Stage 0 (Collapsed Composer Pill)**: `[📱 Gemini 4 Argon EAP · High ⌄]`
 * - **Stage 1 (Compact Effort Slider Popover)**: Morphs out of the pill into a floating popover
 *   with `[📱 Gemini 4 Argon EAP ›]`, bold effort level (`Low` / `Medium` / `High` / `Max`), and
 *   an indigo-violet-to-coral gradient slider with a white liquid-glass capsule thumb.
 * - **Stage 2 (Full Searchable Model List)**: Tapping the model header inside Stage 1 morphs the
 *   popover card itself through an oval liquid lens (with in-flight Gaussian content blur!) into
 *   the full searchable model list; tapping `‹` morphs back to Stage 1.
 */
@Composable
fun SeaGlassCascadedModelMorph(
    stage: Int,
    onStageChange: (Int) -> Unit,
    selectedModel: String,
    onSelectModel: (String) -> Unit,
    effortFraction: Float,
    onEffortChange: (Float) -> Unit,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    pillMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    popoverMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass
) {
    val density = LocalDensity.current
    val motion = LocalSeaGlassMotionScheme.current

    val spatialStage by animateFloatAsState(
        targetValue = stage.coerceIn(0, 2).toFloat(),
        animationSpec = spring(dampingRatio = 0.68f, stiffness = 360f),
        label = "cascadedSpatialStage"
    )
    val effectsStage by animateFloatAsState(
        targetValue = stage.coerceIn(0, 2).toFloat(),
        animationSpec = motion.defaultEffects,
        label = "cascadedEffectsStage"
    )

    val effortLabel = when {
        effortFraction < 0.25f -> "Low"
        effortFraction < 0.60f -> "Medium"
        effortFraction < 0.88f -> "High"
        else -> "Max"
    }
    var modelSearch by remember { mutableStateOf("") }
    val models = remember {
        listOf(
            "Gemini 4 Argon EAP" to "Google · 1M Context",
            "Claude Haiku 5.5" to "OpenCode Go",
            "Exo Free" to "OpenCode Zen",
            "Ling 3.1 Flash Free" to "OpenCode Zen",
            "GPT-6.1 Sol" to "OpenAI"
        )
    }

    val stage0Content: @Composable () -> Unit = {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp),
            modifier = Modifier
                .clickable(
                    interactionSource = remember { MutableInteractionSource() },
                    indication = null,
                    onClick = { onStageChange(1) }
                )
                .padding(horizontal = 14.dp, vertical = 9.dp)
        ) {
            SeaGlassIcon(
                glyph = SeaGlassGlyph.Layers,
                modifier = Modifier.size(15.dp),
                tint = LocalContentColor.current
            )
            Text(
                text = selectedModel,
                color = LocalContentColor.current,
                style = MaterialTheme.typography.labelMedium.copy(
                    fontWeight = FontWeight.ExtraBold,
                    fontSize = 12.sp
                )
            )
            Text(
                text = "$effortLabel ⌄",
                color = LocalContentColor.current.copy(alpha = 0.78f),
                style = MaterialTheme.typography.labelMedium.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 12.sp
                )
            )
        }
    }

    val stage1Content: @Composable () -> Unit = {
        Column(
            modifier = Modifier
                .width(292.dp)
                .padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    modifier = Modifier
                        .clip(RoundedCornerShape(14.dp))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onStageChange(2) }
                        )
                        .padding(horizontal = 8.dp, vertical = 4.dp)
                ) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.Layers,
                        modifier = Modifier.size(16.dp),
                        tint = LocalContentColor.current
                    )
                    Text(
                        text = "$selectedModel ›",
                        color = LocalContentColor.current,
                        style = MaterialTheme.typography.titleSmall.copy(
                            fontWeight = FontWeight.Black,
                            fontSize = 14.sp
                        )
                    )
                }

                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(28.dp)
                        .clip(CircleShape)
                        .background(LocalContentColor.current.copy(alpha = 0.12f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onStageChange(0) }
                        )
                ) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.Close,
                        modifier = Modifier.size(13.dp),
                        tint = LocalContentColor.current
                    )
                }
            }

            Text(
                text = effortLabel,
                color = LocalContentColor.current.copy(alpha = 0.85f),
                style = MaterialTheme.typography.labelLarge.copy(
                    fontWeight = FontWeight.Black,
                    fontSize = 14.sp
                )
            )

            Box(
                contentAlignment = Alignment.CenterStart,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(26.dp)
                    .clip(CircleShape)
                    .background(
                        Brush.horizontalGradient(
                            colors = listOf(
                                Color(0xFF4B44F6),
                                Color(0xFF9C3CF8),
                                Color(0xFFF25C78)
                            )
                        )
                    )
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = {
                            val next = if (effortFraction >= 0.9f) 0.2f else effortFraction + 0.35f
                            onEffortChange(next.coerceIn(0f, 1f))
                        }
                    )
                    .padding(horizontal = 4.dp)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    repeat(4) {
                        Box(
                            modifier = Modifier
                                .size(4.dp)
                                .clip(CircleShape)
                                .background(Color.White.copy(alpha = 0.72f))
                        )
                    }
                }
                Box(
                    modifier = Modifier
                        .fillMaxWidth(effortFraction.coerceIn(0.18f, 1f))
                ) {
                    Box(
                        modifier = Modifier
                            .align(Alignment.CenterEnd)
                            .size(width = 36.dp, height = 20.dp)
                            .clip(CircleShape)
                            .background(Color.White)
                    )
                }
            }
        }
    }

    val stage2Content: @Composable () -> Unit = {
        Column(
            modifier = Modifier
                .width(308.dp)
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .size(34.dp)
                        .clip(CircleShape)
                        .background(LocalContentColor.current.copy(alpha = 0.12f))
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onStageChange(1) }
                        )
                ) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.Back,
                        modifier = Modifier.size(16.dp),
                        tint = LocalContentColor.current
                    )
                }

                Column(modifier = Modifier.weight(1f)) {
                    Text(
                        text = selectedModel,
                        color = LocalContentColor.current,
                        style = MaterialTheme.typography.titleSmall.copy(
                            fontWeight = FontWeight.Black,
                            fontSize = 15.sp
                        )
                    )
                    Text(
                        text = "OpenCode 2 · Tap ‹ for Effort",
                        color = LocalContentColor.current.copy(alpha = 0.68f),
                        style = MaterialTheme.typography.labelSmall.copy(fontSize = 11.sp)
                    )
                }
            }

            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(16.dp))
                    .background(LocalContentColor.current.copy(alpha = 0.10f))
                    .padding(horizontal = 12.dp, vertical = 8.dp)
            ) {
                SeaGlassIcon(
                    glyph = SeaGlassGlyph.Search,
                    modifier = Modifier.size(15.dp),
                    tint = LocalContentColor.current.copy(alpha = 0.65f)
                )
                BasicTextField(
                    value = modelSearch,
                    onValueChange = { modelSearch = it },
                    singleLine = true,
                    textStyle = TextStyle(
                        color = LocalContentColor.current,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.SemiBold
                    ),
                    decorationBox = { inner ->
                        if (modelSearch.isEmpty()) {
                            Text(
                                text = "Search OpenCode 2 models…",
                                color = LocalContentColor.current.copy(alpha = 0.52f),
                                style = MaterialTheme.typography.bodySmall.copy(fontSize = 12.sp)
                            )
                        }
                        inner()
                    },
                    modifier = Modifier.fillMaxWidth()
                )
            }

            models.filter {
                modelSearch.isBlank() || it.first.contains(modelSearch, ignoreCase = true)
            }.forEach { (name, provider) ->
                val isSelected = name == selectedModel
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(14.dp))
                        .background(
                            if (isSelected) LocalContentColor.current.copy(alpha = 0.12f)
                            else Color.Transparent
                        )
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = {
                                onSelectModel(name)
                                onStageChange(1)
                            }
                        )
                        .padding(horizontal = 10.dp, vertical = 7.dp)
                ) {
                    SeaGlassIcon(
                        glyph = SeaGlassGlyph.Layers,
                        modifier = Modifier.size(16.dp),
                        tint = LocalContentColor.current.copy(alpha = 0.78f)
                    )
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = name,
                            color = LocalContentColor.current,
                            style = MaterialTheme.typography.bodySmall.copy(
                                fontWeight = if (isSelected) FontWeight.ExtraBold else FontWeight.SemiBold,
                                fontSize = 13.sp
                            )
                        )
                        Text(
                            text = provider,
                            color = LocalContentColor.current.copy(alpha = 0.62f),
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 10.sp)
                        )
                    }
                    if (isSelected) {
                        SeaGlassIcon(
                            glyph = SeaGlassGlyph.Check,
                            modifier = Modifier.size(15.dp),
                            tint = LocalContentColor.current
                        )
                    }
                }
            }
        }
    }

    val contentColor = remember(popoverMaterial.tintColorArgb, popoverMaterial.tintDensity) {
        Color(
            SeaGlassColorMath.optimalContentColorFor(
                popoverMaterial.tintColorArgb,
                popoverMaterial.tintDensity
            )
        )
    }

    SubcomposeLayout(modifier = modifier) {
        val unbounded = Constraints()
        val p0 = subcompose("stage0") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor, content = stage0Content)
        }.map { it.measure(unbounded) }
        val p1 = subcompose("stage1") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor, content = stage1Content)
        }.map { it.measure(unbounded) }
        val p2 = subcompose("stage2") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor, content = stage2Content)
        }.map { it.measure(unbounded) }

        val w0 = p0.maxOfOrNull { it.width }?.coerceAtLeast(48) ?: 200
        val h0 = p0.maxOfOrNull { it.height }?.coerceAtLeast(36) ?: 40
        val w1 = p1.maxOfOrNull { it.width }?.coerceAtLeast(48) ?: 292
        val h1 = p1.maxOfOrNull { it.height }?.coerceAtLeast(48) ?: 132
        val w2 = p2.maxOfOrNull { it.width }?.coerceAtLeast(48) ?: 308
        val h2 = p2.maxOfOrNull { it.height }?.coerceAtLeast(48) ?: 320

        val isUpperSegment = effectsStage > 1.0f || (stage == 2 && spatialStage > 0.85f)
        val rawLocalSpatial = if (isUpperSegment) (spatialStage - 1f) else spatialStage
        val rawLocalEffects = if (isUpperSegment) (effectsStage - 1f).coerceIn(0f, 1f) else effectsStage.coerceIn(0f, 1f)
        val localSpatial = SeaGlassShapeMorph.physicsSCurve(rawLocalSpatial, overshootGain = 0.45f)
        val localEffects = SeaGlassShapeMorph.sCurve(rawLocalEffects)

        val startW = if (isUpperSegment) w1 else w0
        val startH = if (isUpperSegment) h1 else h0
        val endW = if (isUpperSegment) w2 else w1
        val endH = if (isUpperSegment) h2 else h1

        val animatedWidthPx = (startW + (endW - startW) * localSpatial).roundToInt().coerceAtLeast(36)
        val animatedHeightPx = (startH + (endH - startH) * localSpatial).roundToInt().coerceAtLeast(32)
        val inFlightEnergy = SeaGlassShapeMorph.sCurveBell(rawLocalEffects)

        val activeMaterial = SeaGlassShapeMorph.computeDynamicFlexMaterial(
            startMaterial = if (isUpperSegment) popoverMaterial else pillMaterial,
            endMaterial = popoverMaterial,
            progress = localEffects,
            areaExpansionRatio = 1f + inFlightEnergy * 0.8f
        ).copy(
            cornerRadiusDp = (if (isUpperSegment) 28f else (20f + 8f * localEffects)) + 24f * inFlightEnergy,
            thicknessDp = 16f + 6f * localEffects + 4f * inFlightEnergy
        )

        val hostPlaceables = subcompose("cascaded_host") {
            ProvideSeaGlassLegibleContent(contentColor = contentColor) {
                Box(
                    contentAlignment = Alignment.TopStart,
                    modifier = Modifier
                        .size(
                            width = with(density) { animatedWidthPx.toDp() },
                            height = with(density) { animatedHeightPx.toDp() }
                        )
                        .graphicsLayer {
                            scaleX = 1f + 0.04f * inFlightEnergy
                            scaleY = 1f - 0.025f * inFlightEnergy
                        }
                        .seaGlass(
                            stageState = stageState,
                            material = activeMaterial,
                            touchIntensity = inFlightEnergy * 0.85f
                        )
                ) {
                    val alphaA = 1f - SeaGlassShapeMorph.sCurveWindow(rawLocalEffects, 0.0f, 0.62f)
                    val alphaB = SeaGlassShapeMorph.sCurveWindow(rawLocalEffects, 0.10f, 0.88f)
                    val blurAPx = (SeaGlassShapeMorph.sCurveWindow(rawLocalEffects, 0.0f, 0.72f) * 14f * density.density).coerceAtLeast(0f)
                    val defocusB = 1f - SeaGlassShapeMorph.sCurveWindow(rawLocalEffects, 0.14f, 0.96f)
                    val blurBPx = (defocusB * defocusB * 16f * density.density)
                        .coerceAtLeast(0f)

                    if (alphaA > 0.01f) {
                        val scaleXA = (animatedWidthPx.toFloat() / max(startW.toFloat(), 1f)).coerceIn(0.25f, 1.15f)
                        val scaleYA = (animatedHeightPx.toFloat() / max(startH.toFloat(), 1f)).coerceIn(0.25f, 1.15f)
                        Box(
                            modifier = Modifier
                                .size(
                                    width = with(density) { startW.toDp() },
                                    height = with(density) { startH.toDp() }
                                )
                                .graphicsLayer {
                                    alpha = alphaA
                                    transformOrigin = TransformOrigin(0f, 0f)
                                    scaleX = scaleXA
                                    scaleY = scaleYA
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && blurAPx > 0.6f) {
                                        compositingStrategy = CompositingStrategy.Offscreen
                                        renderEffect = SeaGlassShaderProgramCache
                                            .obtainBlurRenderEffect(blurAPx, Shader.TileMode.DECAL)
                                            .asComposeRenderEffect()
                                    } else {
                                        renderEffect = null
                                    }
                                }
                        ) {
                            if (isUpperSegment) stage1Content() else stage0Content()
                        }
                    }

                    if (alphaB > 0.01f) {
                        val scaleXB = (animatedWidthPx.toFloat() / max(endW.toFloat(), 1f)).coerceIn(0.22f, 1.08f)
                        val scaleYB = (animatedHeightPx.toFloat() / max(endH.toFloat(), 1f)).coerceIn(0.22f, 1.08f)
                        Box(
                            modifier = Modifier
                                .size(
                                    width = with(density) { endW.toDp() },
                                    height = with(density) { endH.toDp() }
                                )
                                .graphicsLayer {
                                    alpha = alphaB
                                    transformOrigin = TransformOrigin(0f, 0f)
                                    scaleX = scaleXB
                                    scaleY = scaleYB
                                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S && blurBPx > 0.6f) {
                                        compositingStrategy = CompositingStrategy.Offscreen
                                        renderEffect = SeaGlassShaderProgramCache
                                            .obtainBlurRenderEffect(blurBPx, Shader.TileMode.DECAL)
                                            .asComposeRenderEffect()
                                    } else {
                                        renderEffect = null
                                    }
                                }
                        ) {
                            if (isUpperSegment) stage2Content() else stage1Content()
                        }
                    }
                }
            }
        }.map {
            it.measure(Constraints.fixed(animatedWidthPx, animatedHeightPx))
        }

        layout(w0, h0) {
            hostPlaceables.forEach {
                it.placeRelative(0, 0, zIndex = if (spatialStage > 0.01f) 30f else 0f)
            }
        }
    }
}

/**
 * Circular Progress Ring $\leftrightarrow$ Context Window Popover Morph (`SeaGlassContextRingMorph`):
 * - **Collapsed**: Compact `38.dp` circular progress ring button in a composer bar.
 * - **In-Flight**: Scales the progress ring inside the expanding liquid droplet with Gaussian blur.
 * - **Expanded**: Floating `Context Window` popover card (`35% · 366K of 1.05M tokens · 683K left`).
 */
@Composable
fun SeaGlassContextRingMorph(
    expanded: Boolean,
    onExpandedChange: (Boolean) -> Unit,
    progress: Float,
    usedLabel: String,
    remainingLabel: String,
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    ringMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    popoverMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.elevatedGlass
) {
    val accentBlue = Color(0xFF0A7AFF)

    SeaGlassTwoElementMorph(
        expanded = expanded,
        onToggle = { onExpandedChange(!expanded) },
        stageState = stageState,
        collapsedMaterial = ringMaterial.copy(cornerRadiusDp = 19f, thicknessDp = 14f),
        expandedMaterial = popoverMaterial.copy(cornerRadiusDp = 26f, thicknessDp = 20f),
        morphMode = SeaGlassMorphMode.LIQUID_COALESCE,
        anchorAlignment = Alignment.TopCenter,
        reserveCollapsedLayoutSpace = true,
        modifier = modifier,
        collapsedContent = {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier.size(38.dp)
            ) {
                val trackColor = LocalContentColor.current.copy(alpha = 0.20f)
                Canvas(modifier = Modifier.size(20.dp)) {
                    val sw = 2.6.dp.toPx()
                    drawCircle(color = trackColor, style = Stroke(width = sw))
                    drawArc(
                        color = accentBlue,
                        startAngle = -90f,
                        sweepAngle = 360f * progress.coerceIn(0.05f, 1f),
                        useCenter = false,
                        style = Stroke(width = sw, cap = StrokeCap.Round)
                    )
                }
            }
        },
        expandedContent = {
            Column(
                modifier = Modifier
                    .width(236.dp)
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = "Context Window",
                    color = LocalContentColor.current,
                    style = MaterialTheme.typography.labelLarge.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 14.sp
                    )
                )
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Box(
                        contentAlignment = Alignment.Center,
                        modifier = Modifier.size(54.dp)
                    ) {
                        val trackColor = LocalContentColor.current.copy(alpha = 0.16f)
                        Canvas(modifier = Modifier.size(48.dp)) {
                            val sw = 5.dp.toPx()
                            drawCircle(color = trackColor, style = Stroke(width = sw))
                            drawArc(
                                color = accentBlue,
                                startAngle = -90f,
                                sweepAngle = 360f * progress.coerceIn(0.05f, 1f),
                                useCenter = false,
                                style = Stroke(width = sw, cap = StrokeCap.Round)
                            )
                        }
                        Text(
                            text = "${(progress * 100).roundToInt()}%",
                            color = LocalContentColor.current,
                            style = MaterialTheme.typography.labelSmall.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 11.sp
                            )
                        )
                    }
                    Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text(
                            text = usedLabel,
                            color = LocalContentColor.current,
                            style = MaterialTheme.typography.bodyMedium.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 14.sp
                            )
                        )
                        Text(
                            text = remainingLabel,
                            color = LocalContentColor.current.copy(alpha = 0.68f),
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 11.sp)
                        )
                    }
                }
            }
        }
    )
}

/**
 * Corner-Embedded SeaGlass Pod (`SeaGlassCornerEmbeddedPod`):
 *
 * Designed for elements embedded directly into or near the corners of a device screen or
 * page container. Uses [SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED] so the outer
 * corner(s) facing the device/page bezel match the concentric radius
 * $R_{\text{concentric}} = \max(r_{\min}, R_{\text{outer}} - \text{inset})$ while the inner
 * page-facing corner(s) maintain a tighter architectural radius.
 */
@Composable
fun SeaGlassCornerEmbeddedPod(
    stageState: SeaGlassStageState?,
    modifier: Modifier = Modifier,
    material: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    onClick: (() -> Unit)? = null,
    contentPadding: PaddingValues = PaddingValues(horizontal = 14.dp, vertical = 10.dp),
    contentAlignment: Alignment = Alignment.Center,
    cornerAdaptationMode: SeaGlassCornerAdaptationMode = SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED,
    cornerMask: SeaGlassCornerMask = SeaGlassCornerMask.ALL,
    cornerProximityThresholdDp: Float = 48f,
    content: @Composable BoxScope.() -> Unit
) {
    SeaGlassSurface(
        stageState = stageState,
        material = material,
        onClick = onClick,
        contentPadding = contentPadding,
        contentAlignment = contentAlignment,
        cornerAdaptationMode = cornerAdaptationMode,
        cornerMask = cornerMask,
        cornerProximityThresholdDp = cornerProximityThresholdDp,
        allowAsymmetricCorners = true,
        adaptToParentContainer = true,
        modifier = modifier,
        content = content
    )
}

/**
 * Interactive **Device & Page Corner Radius Adaptation Lab** (`SeaGlassAdaptiveCornerLabCard`):
 *
 * Demonstrates live hardware display corner detection (`WindowInsets.getRoundedCorner` ->
 * `Display.getRoundedCorner` -> `SystemUI dimen/rounded_corner_radius` -> `Build.MODEL` catalog)
 * and lets developers choose **where**, **when**, and **how close** (`proximityThresholdDp`)
 * to a device/page corner an element must be before its corners match concentrically.
 */
@Composable
fun SeaGlassAdaptiveCornerLabCard(
    stageState: SeaGlassStageState?,
    activeProfile: SeaGlassDeviceHardwareProfile,
    onSelectProfile: (SeaGlassDeviceHardwareProfile) -> Unit,
    modifier: Modifier = Modifier,
    surfaceMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.surfaceGlass,
    primaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.primaryGlass,
    tertiaryMaterial: SeaGlassMaterial = LocalSeaGlassPalette.current.tertiaryGlass
) {
    val density = LocalDensity.current
    val deviceCornerSpec = LocalSeaGlassDeviceCornerSpec.current

    // 0 = Flush Corner Embed (3dp inset), 1 = Concentric Padded (14dp inset), 2 = Page Interior (30dp inset)
    var insetPresetIndex by remember { mutableIntStateOf(0) }
    val targetInsetDp = when (insetPresetIndex) {
        0 -> 3f
        1 -> 14f
        else -> 30f
    }
    val animatedInsetDp by animateFloatAsState(
        targetValue = targetInsetDp,
        animationSpec = spring(dampingRatio = 0.64f, stiffness = 380f),
        label = "cornerLabInset"
    )

    // Developer controls for WHERE (Corner Mask) and HOW CLOSE (Proximity Threshold in dp)
    var maskIndex by remember { mutableIntStateOf(0) }
    val activeMask = when (maskIndex) {
        0 -> SeaGlassCornerMask.ALL
        1 -> SeaGlassCornerMask.BOTTOM_ONLY
        2 -> SeaGlassCornerMask.TOP_ONLY
        else -> SeaGlassCornerMask.NONE
    }
    val activeMode = if (maskIndex == 3) {
        SeaGlassCornerAdaptationMode.FIXED
    } else {
        SeaGlassCornerAdaptationMode.FLUSH_CORNER_EMBED
    }
    var proximityThresholdDp by remember { mutableFloatStateOf(36f) }

    val tlBezelDp = deviceCornerSpec.topLeftDp.roundToInt()
    val trBezelDp = deviceCornerSpec.topRightDp.roundToInt()
    val brBezelDp = deviceCornerSpec.bottomRightDp.roundToInt()
    val blBezelDp = deviceCornerSpec.bottomLeftDp.roundToInt()

    SeaGlassCard(
        stageState = stageState,
        material = surfaceMaterial,
        modifier = modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "ADAPTIVE DEVICE & PAGE CORNER OPTICS",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.ExtraBold,
                        letterSpacing = 1.2.sp
                    ),
                    color = LocalContentColor.current.copy(alpha = 0.76f)
                )
                Text(
                    text = "Where, When & Proximity Distance Control",
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Black)
                )
            }
            SeaGlassBadge(
                text = if (tlBezelDp == trBezelDp) "${tlBezelDp}dp Bezel" else "${tlBezelDp}/${trBezelDp}dp",
                stageState = stageState,
                material = tertiaryMaterial
            )
        }

        Text(
            text = "Source: ${deviceCornerSpec.source.label} · Bezel: TL ${tlBezelDp}dp · TR ${trBezelDp}dp · BR ${brBezelDp}dp · BL ${blBezelDp}dp · Match ≤ ${proximityThresholdDp.roundToInt()}dp",
            style = MaterialTheme.typography.labelSmall,
            color = LocalContentColor.current.copy(alpha = 0.84f)
        )

        // Device Hardware Bezel Profile Selector Row
        val featuredProfiles = remember {
            listOf(
                SeaGlassDeviceHardwareProfile.AUTO_DETECT,
                SeaGlassDeviceHardwareProfile.PIXEL_9_PRO,
                SeaGlassDeviceHardwareProfile.IPHONE_16_PRO,
                SeaGlassDeviceHardwareProfile.GALAXY_S25_ULTRA,
                SeaGlassDeviceHardwareProfile.FOLD_ASYMMETRIC
            )
        }
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            featuredProfiles.forEach { profile ->
                val selected = profile == activeProfile
                Box(
                    contentAlignment = Alignment.Center,
                    modifier = Modifier
                        .weight(1f)
                        .clip(RoundedCornerShape(14.dp))
                        .background(
                            if (selected) Color(primaryMaterial.tintColorArgb).copy(alpha = 0.42f)
                            else LocalContentColor.current.copy(alpha = 0.10f)
                        )
                        .border(
                            width = 1.dp,
                            color = if (selected) Color.White.copy(alpha = 0.72f) else Color.Transparent,
                            shape = RoundedCornerShape(14.dp)
                        )
                        .clickable(
                            interactionSource = remember { MutableInteractionSource() },
                            indication = null,
                            onClick = { onSelectProfile(profile) }
                        )
                        .padding(vertical = 8.dp, horizontal = 4.dp)
                ) {
                    Text(
                        text = profile.shortLabel,
                        maxLines = 1,
                        color = LocalContentColor.current,
                        style = MaterialTheme.typography.labelSmall.copy(
                            fontWeight = if (selected) FontWeight.ExtraBold else FontWeight.SemiBold,
                            fontSize = 10.sp
                        )
                    )
                }
            }
        }

        // Where / When Corner Mask Selector ("All", "Bottom", "Top", "Fixed")
        SeaGlassSegmentedButtonRow(
            options = listOf("All Corners", "Bottom Only", "Top Only", "Fixed"),
            selectedIndex = maskIndex,
            onSelect = { maskIndex = it },
            stageState = stageState,
            containerMaterial = surfaceMaterial,
            indicatorMaterial = tertiaryMaterial
        )

        // Proximity / Inset Mode Selector + Match Distance Presets
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            SeaGlassSegmentedButtonRow(
                options = listOf("Flush (3dp)", "Inset (14dp)", "Far (30dp)"),
                selectedIndex = insetPresetIndex,
                onSelect = { insetPresetIndex = it },
                stageState = stageState,
                containerMaterial = surfaceMaterial,
                indicatorMaterial = primaryMaterial,
                modifier = Modifier.weight(1f)
            )
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .height(46.dp)
                    .clip(RoundedCornerShape(18.dp))
                    .background(LocalContentColor.current.copy(alpha = 0.12f))
                    .clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null,
                        onClick = {
                            proximityThresholdDp = when {
                                proximityThresholdDp < 20f -> 36f
                                proximityThresholdDp < 50f -> 72f
                                else -> 14f
                            }
                        }
                    )
                    .padding(horizontal = 12.dp)
            ) {
                Text(
                    text = "≤${proximityThresholdDp.roundToInt()}dp ↻",
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontWeight = FontWeight.Black,
                        fontSize = 11.sp
                    ),
                    color = LocalContentColor.current
                )
            }
        }

        // Live Simulated Device / Page Bezel Frame with 4 Corner-Adaptive Glass Pods
        var frameSize by remember { mutableStateOf(IntSize(900, 520)) }
        var frameOriginInRoot by remember { mutableStateOf(Offset.Zero) }
        val bezelRadiiPx = deviceCornerSpec.toRadiiPx()

        val bezelShape = remember(tlBezelDp, trBezelDp, brBezelDp, blBezelDp) {
            RoundedCornerShape(
                topStart = tlBezelDp.dp,
                topEnd = trBezelDp.dp,
                bottomEnd = brBezelDp.dp,
                bottomStart = blBezelDp.dp
            )
        }

        val frameScope = remember(frameOriginInRoot, frameSize, bezelRadiiPx) {
            SeaGlassContainerCornerScope(
                originInRoot = frameOriginInRoot,
                widthPx = frameSize.width.toFloat().coerceAtLeast(1f),
                heightPx = frameSize.height.toFloat().coerceAtLeast(1f),
                cornerRadiiPx = bezelRadiiPx
            )
        }

        CompositionLocalProvider(LocalSeaGlassContainerCornerScope provides frameScope) {
            val requestedInsetPx = with(density) { animatedInsetDp.dp.toPx() }
            val solvedLayout = remember(frameSize, bezelRadiiPx, requestedInsetPx, density) {
                SeaGlassCornerAdaptationSolver.solveCornerPodLayoutPx(
                    containerWidthPx = frameSize.width.toFloat().coerceAtLeast(320f),
                    containerHeightPx = frameSize.height.toFloat().coerceAtLeast(210f),
                    containerCornersPx = bezelRadiiPx,
                    requestedInsetPx = requestedInsetPx,
                    minHorizontalGapPx = with(density) { 14.dp.toPx() },
                    minVerticalGapPx = with(density) { 14.dp.toPx() },
                    preferredPodWidthPx = with(density) { 148.dp.toPx() },
                    preferredTopPodHeightPx = with(density) { 78.dp.toPx() },
                    preferredBottomDockHeightPx = with(density) { 68.dp.toPx() }
                )
            }

            val effectiveInsetDp = with(density) { solvedLayout.effectiveInsetPx.toDp() }
            val podW = with(density) { solvedLayout.podWidthPx.toDp() }
            val podH = with(density) { solvedLayout.topPodHeightPx.toDp() }
            val dockH = with(density) { solvedLayout.bottomDockHeightPx.toDp() }
            val podWPx = solvedLayout.podWidthPx
            val podHPx = solvedLayout.topPodHeightPx
            val dockHPx = solvedLayout.bottomDockHeightPx
            val insetPx = solvedLayout.effectiveInsetPx
            val basePodCornerPx = with(density) { 18.dp.toPx() }
            val thresholdPx = with(density) { proximityThresholdDp.dp.toPx() }

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(206.dp)
                    .onGloballyPositioned { coords ->
                        frameOriginInRoot = coords.positionInRoot()
                        frameSize = coords.size
                    }
                    .clip(bezelShape)
                    .background(Color.Black.copy(alpha = 0.16f))
                    .border(2.dp, Color.White.copy(alpha = 0.68f), bezelShape)
                    .padding(effectiveInsetDp)
            ) {
                // Compute live solved radii for Top-Left, Top-Right, and Bottom Dock to display in labels
                val tlPodSolved = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                    elementLeftPx = insetPx,
                    elementTopPx = insetPx,
                    elementRightPx = insetPx + podWPx,
                    elementBottomPx = insetPx + podHPx,
                    containerWidthPx = frameSize.width.toFloat().coerceAtLeast(320f),
                    containerHeightPx = frameSize.height.toFloat().coerceAtLeast(206f),
                    containerCornersPx = bezelRadiiPx,
                    baseCornerRadiusPx = basePodCornerPx,
                    minCornerFloorPx = with(density) { 8.dp.toPx() },
                    influenceDistancePx = thresholdPx,
                    mode = activeMode,
                    cornerMask = activeMask
                )
                val trPodSolved = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                    elementLeftPx = (frameSize.width - insetPx - podWPx).coerceAtLeast(0f),
                    elementTopPx = insetPx,
                    elementRightPx = (frameSize.width - insetPx).coerceAtLeast(podWPx),
                    elementBottomPx = insetPx + podHPx,
                    containerWidthPx = frameSize.width.toFloat().coerceAtLeast(320f),
                    containerHeightPx = frameSize.height.toFloat().coerceAtLeast(206f),
                    containerCornersPx = bezelRadiiPx,
                    baseCornerRadiusPx = basePodCornerPx,
                    minCornerFloorPx = with(density) { 8.dp.toPx() },
                    influenceDistancePx = thresholdPx,
                    mode = activeMode,
                    cornerMask = activeMask
                )
                val dockSolved = SeaGlassCornerAdaptationSolver.solveElementCornerRadiiPx(
                    elementLeftPx = insetPx,
                    elementTopPx = (frameSize.height - insetPx - dockHPx).coerceAtLeast(0f),
                    elementRightPx = (frameSize.width - insetPx).coerceAtLeast(podWPx),
                    elementBottomPx = (frameSize.height - insetPx).coerceAtLeast(dockHPx),
                    containerWidthPx = frameSize.width.toFloat().coerceAtLeast(320f),
                    containerHeightPx = frameSize.height.toFloat().coerceAtLeast(206f),
                    containerCornersPx = bezelRadiiPx,
                    baseCornerRadiusPx = basePodCornerPx,
                    minCornerFloorPx = with(density) { 8.dp.toPx() },
                    influenceDistancePx = thresholdPx,
                    mode = activeMode,
                    cornerMask = activeMask
                )

                val tlOuterDp = (tlPodSolved.topLeftPx / density.density).roundToInt()
                val tlInnerDp = (tlPodSolved.bottomRightPx / density.density).roundToInt()
                val trOuterDp = (trPodSolved.topRightPx / density.density).roundToInt()
                val trInnerDp = (trPodSolved.bottomLeftPx / density.density).roundToInt()
                val blDockDp = (dockSolved.bottomLeftPx / density.density).roundToInt()
                val brDockDp = (dockSolved.bottomRightPx / density.density).roundToInt()

                // 1. Top-Left Corner-Embedded Pod
                SeaGlassCornerEmbeddedPod(
                    stageState = stageState,
                    material = primaryMaterial.copy(cornerRadiusDp = 18f, thicknessDp = 18f),
                    onClick = { insetPresetIndex = (insetPresetIndex + 1) % 3 },
                    contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
                    cornerAdaptationMode = activeMode,
                    cornerMask = activeMask,
                    cornerProximityThresholdDp = proximityThresholdDp,
                    modifier = Modifier
                        .align(Alignment.TopStart)
                        .size(width = podW, height = podH)
                ) {
                    Column(horizontalAlignment = Alignment.Start) {
                        Text(
                            text = "↖ TL Corner Pod",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelMedium.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 11.sp
                            )
                        )
                        Text(
                            text = "Outer ${tlOuterDp}dp · Inner ${tlInnerDp}dp",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 9.sp),
                            color = LocalContentColor.current.copy(alpha = 0.86f)
                        )
                    }
                }

                // 2. Top-Right Corner-Embedded Pod
                SeaGlassCornerEmbeddedPod(
                    stageState = stageState,
                    material = tertiaryMaterial.copy(cornerRadiusDp = 18f, thicknessDp = 18f),
                    onClick = { insetPresetIndex = (insetPresetIndex + 1) % 3 },
                    contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
                    cornerAdaptationMode = activeMode,
                    cornerMask = activeMask,
                    cornerProximityThresholdDp = proximityThresholdDp,
                    modifier = Modifier
                        .align(Alignment.TopEnd)
                        .size(width = podW, height = podH)
                ) {
                    Column(horizontalAlignment = Alignment.End) {
                        Text(
                            text = "TR Corner Pod ↗",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelMedium.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 11.sp
                            )
                        )
                        Text(
                            text = "Outer ${trOuterDp}dp · Inner ${trInnerDp}dp",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelSmall.copy(fontSize = 9.sp),
                            color = LocalContentColor.current.copy(alpha = 0.86f)
                        )
                    }
                }

                // 3. Bottom Full-Width Concentric Page Dock Bar
                SeaGlassCornerEmbeddedPod(
                    stageState = stageState,
                    material = primaryMaterial.copy(cornerRadiusDp = 22f, thicknessDp = 18f),
                    onClick = { insetPresetIndex = (insetPresetIndex + 1) % 3 },
                    contentPadding = PaddingValues(horizontal = 16.dp, vertical = 8.dp),
                    cornerAdaptationMode = activeMode,
                    cornerMask = activeMask,
                    cornerProximityThresholdDp = proximityThresholdDp,
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .fillMaxWidth()
                        .height(dockH)
                ) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text(
                            text = "↙ BL ${blDockDp}dp Concentric Page Dock",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelMedium.copy(
                                fontWeight = FontWeight.ExtraBold,
                                fontSize = 11.sp
                            )
                        )
                        Text(
                            text = "BR ${brDockDp}dp ↘",
                            maxLines = 1,
                            style = MaterialTheme.typography.labelMedium.copy(
                                fontWeight = FontWeight.Black,
                                fontSize = 11.sp
                            )
                        )
                    }
                }
            }
        }
    }
}


