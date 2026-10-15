using System;
using SeaGlass.Core;
using Xunit;

namespace SeaGlass.Tests.Core;

public sealed class CoreUpstreamParityTests
{
    [Fact]
    public void SCurve_Evaluates_Hermite_Properties()
    {
        Assert.Equal(0f, SeaGlassShapeMorph.SCurve(0f));
        Assert.Equal(1f, SeaGlassShapeMorph.SCurve(1f));
        Assert.Equal(0.5f, SeaGlassShapeMorph.SCurve(0.5f));

        // Clamping outside [0..1]
        Assert.Equal(0f, SeaGlassShapeMorph.SCurve(-0.5f));
        Assert.Equal(1f, SeaGlassShapeMorph.SCurve(1.5f));

        // Monotonic
        float prev = 0f;
        for (float t = 0.05f; t <= 1f; t += 0.05f)
        {
            float cur = SeaGlassShapeMorph.SCurve(t);
            Assert.True(cur >= prev, $"SCurve must be monotonically increasing: {cur} >= {prev}");
            prev = cur;
        }
    }

    [Fact]
    public void SmootherStep_Evaluates_Quintic_Properties()
    {
        Assert.Equal(0f, SeaGlassShapeMorph.SmootherStep(0f));
        Assert.Equal(1f, SeaGlassShapeMorph.SmootherStep(1f));
        Assert.Equal(0.5f, SeaGlassShapeMorph.SmootherStep(0.5f));

        // Clamping outside [0..1]
        Assert.Equal(0f, SeaGlassShapeMorph.SmootherStep(-1f));
        Assert.Equal(1f, SeaGlassShapeMorph.SmootherStep(2f));
    }

    [Fact]
    public void PhysicsSCurve_Satisfies_Reversal_Symmetry_And_Overshoot()
    {
        Assert.Equal(0f, SeaGlassShapeMorph.PhysicsSCurve(0f));
        Assert.Equal(1f, SeaGlassShapeMorph.PhysicsSCurve(1f));

        // Reversal symmetry: physicsSCurve(1 - u) == 1 - physicsSCurve(u)
        for (float u = 0.0f; u <= 1.0f; u += 0.1f)
        {
            float fwd = SeaGlassShapeMorph.PhysicsSCurve(u);
            float rev = SeaGlassShapeMorph.PhysicsSCurve(1f - u);
            Assert.True(MathF.Abs(fwd - (1f - rev)) < 1e-5f, $"Reversal symmetry failed at u={u}: {fwd} vs {1f - rev}");
        }

        // Outside [0..1], preserves damped spring overshoot
        float undershoot = SeaGlassShapeMorph.PhysicsSCurve(-0.25f, overshootGain: 0.52f);
        Assert.Equal(-0.25f * 0.52f, undershoot, precision: 5);

        float overshoot = SeaGlassShapeMorph.PhysicsSCurve(1.20f, overshootGain: 0.52f);
        Assert.Equal(1f + 0.20f * 0.52f, overshoot, precision: 5);
    }

    [Fact]
    public void SCurveWindow_And_SCurveLead_Shape_SubRanges()
    {
        // Window from 0.2 to 0.8
        Assert.Equal(0f, SeaGlassShapeMorph.SCurveWindow(0.2f, 0.2f, 0.8f));
        Assert.Equal(1f, SeaGlassShapeMorph.SCurveWindow(0.8f, 0.2f, 0.8f));
        Assert.Equal(0.5f, SeaGlassShapeMorph.SCurveWindow(0.5f, 0.2f, 0.8f));

        // Lead holds at 1.0 once progress >= until
        float lead = SeaGlassShapeMorph.SCurveLead(0.6f, until: 0.5f);
        Assert.True(lead >= 1f, $"Lead must hold at 1.0: {lead}");

        // Lead carries overshoot when progress > 1f
        float leadOvershoot = SeaGlassShapeMorph.SCurveLead(1.2f, until: 0.5f, overshootGain: 0.45f);
        Assert.True(leadOvershoot > 1.05f, $"Lead with spring overshoot must exceed 1.0: {leadOvershoot}");
    }

    [Fact]
    public void SCurveBell_Peaks_At_Midpoint()
    {
        Assert.Equal(0f, SeaGlassShapeMorph.SCurveBell(0f));
        Assert.Equal(0f, SeaGlassShapeMorph.SCurveBell(1f));
        Assert.Equal(1f, SeaGlassShapeMorph.SCurveBell(0.5f));

        float quarter = SeaGlassShapeMorph.SCurveBell(0.25f);
        float threeQuarter = SeaGlassShapeMorph.SCurveBell(0.75f);
        Assert.True(quarter > 0f && quarter < 1f);
        Assert.Equal(quarter, threeQuarter, precision: 5);
    }

    [Fact]
    public void VelvetGrainTexture_Generates_Seamless_Premultiplied_Tile()
    {
        Assert.Equal(128, SeaGlassVelvetGrainTexture.TileSize);

        byte[] pixels1 = SeaGlassVelvetGrainTexture.ObtainBgra32Pixels();
        Assert.NotNull(pixels1);
        Assert.Equal(128 * 128 * 4, pixels1.Length);

        // Cached instance check
        byte[] pixels2 = SeaGlassVelvetGrainTexture.ObtainBgra32Pixels();
        Assert.Same(pixels1, pixels2);

        int highlightCount = 0;
        int valleyCount = 0;

        for (int i = 0; i < pixels1.Length; i += 4)
        {
            byte b = pixels1[i + 0];
            byte g = pixels1[i + 1];
            byte r = pixels1[i + 2];
            byte a = pixels1[i + 3];

            // Premultiplied alpha invariant: color channels must never exceed alpha
            Assert.True(b <= a, $"Blue channel {b} exceeds alpha {a}");
            Assert.True(g <= a, $"Green channel {g} exceeds alpha {a}");
            Assert.True(r <= a, $"Red channel {r} exceeds alpha {a}");

            if (a > 0)
            {
                // In highlight (0xF6FAFD), B/G/R ratio is near 1.0 (pearl white)
                // In valley (0x0E2434), B > G > R
                if (b > r)
                {
                    valleyCount++;
                }
                else
                {
                    highlightCount++;
                }
            }
        }

        Assert.True(highlightCount > 0, "Texture must contain highlight micro-facets");
        Assert.True(valleyCount > 0, "Texture must contain subtle valley micro-facets");
    }
}
