// SeaGlassSkipUI.swift
// Drop-in Swift / SkipUI wrappers for using SeaGlass on Android via Skip (https://skip.tools)
// while falling back cleanly to native SwiftUI materials on iOS / macOS.

import SwiftUI
#if SKIP
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import dev.seaglass.compose.SeaGlassSkipBridge
#endif

/// Root container that captures `background` into SeaGlass's GPU backdrop pyramid on Android
/// (via Skip) and provides `LocalSeaGlassStageState` to all descendant SeaGlass views.
public struct SeaGlassStageView<Background: View, Foreground: View>: View {
    private let paletteId: String
    private let deviceCornerProfileId: String
    private let background: Background
    private let foreground: Foreground

    public init(
        paletteId: String = "seafoam_mint",
        deviceCornerProfileId: String = "auto",
        @ViewBuilder background: () -> Background,
        @ViewBuilder foreground: () -> Foreground
    ) {
        self.paletteId = paletteId
        self.deviceCornerProfileId = deviceCornerProfileId
        self.background = background()
        self.foreground = foreground()
    }

    public var body: some View {
        #if SKIP
        ComposeView { ctx in
            SeaGlassSkipBridge.StageRoot(
                paletteId: paletteId,
                deviceCornerProfileId: deviceCornerProfileId,
                backgroundContent: {
                    background.Compose(ctx)
                },
                foregroundContent: {
                    foreground.Compose(ctx)
                }
            )
        }
        #else
        ZStack {
            background
            foreground
        }
        #endif
    }
}

/// Concentric device- and page-corner-adaptive SeaGlass surface for Swift / Skip apps.
public struct SeaGlassSurfaceView<Content: View>: View {
    private let role: String
    private let cornerRadiusDp: Float
    private let cornerModeId: String
    private let cornerMaskId: String
    private let proximityThresholdDp: Float
    private let allowAsymmetricCorners: Bool
    private let onTap: (() -> Void)?
    private let content: Content

    public init(
        role: String = "surface",
        cornerRadiusDp: Float = -1.0,
        cornerModeId: String = "concentric_proximity",
        cornerMaskId: String = "all",
        proximityThresholdDp: Float = 32.0,
        allowAsymmetricCorners: Bool = false,
        onTap: (() -> Void)? = nil,
        @ViewBuilder content: () -> Content
    ) {
        self.role = role
        self.cornerRadiusDp = cornerRadiusDp
        self.cornerModeId = cornerModeId
        self.cornerMaskId = cornerMaskId
        self.proximityThresholdDp = proximityThresholdDp
        self.allowAsymmetricCorners = allowAsymmetricCorners
        self.onTap = onTap
        self.content = content()
    }

    public var body: some View {
        #if SKIP
        ComposeView { ctx in
            SeaGlassSkipBridge.Surface(
                role: role,
                cornerRadiusDp: cornerRadiusDp,
                cornerModeId: cornerModeId,
                cornerMaskId: cornerMaskId,
                proximityThresholdDp: proximityThresholdDp,
                allowAsymmetricCorners: allowAsymmetricCorners,
                onClick: onTap
            ) {
                content.Compose(ctx)
            }
        }
        #else
        content
            .padding(.horizontal, 18)
            .padding(.vertical, 14)
            .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: CGFloat(cornerRadiusDp > 0 ? cornerRadiusDp : 28), style: .continuous))
        #endif
    }
}

/// Corner-embedded SeaGlass pod that curves its outer bezel-facing corner(s) concentrically
/// with the physical device or parent page container while keeping inner corners crisp.
public struct SeaGlassCornerPodView<Content: View>: View {
    private let role: String
    private let baseCornerRadiusDp: Float
    private let onTap: (() -> Void)?
    private let content: Content

    public init(
        role: String = "primary",
        baseCornerRadiusDp: Float = 18.0,
        onTap: (() -> Void)? = nil,
        @ViewBuilder content: () -> Content
    ) {
        self.role = role
        self.baseCornerRadiusDp = baseCornerRadiusDp
        self.onTap = onTap
        self.content = content()
    }

    public var body: some View {
        #if SKIP
        ComposeView { ctx in
            SeaGlassSkipBridge.CornerPod(
                role: role,
                baseCornerRadiusDp: baseCornerRadiusDp,
                onClick: onTap
            ) {
                content.Compose(ctx)
            }
        }
        #else
        content
            .padding(.horizontal, 14)
            .padding(.vertical, 10)
            .background(.thinMaterial, in: RoundedRectangle(cornerRadius: CGFloat(baseCornerRadiusDp), style: .continuous))
        #endif
    }
}
