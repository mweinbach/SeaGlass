# SeaGlass WinUI 3 Consumer Sample

A minimal, standalone Windows desktop application demonstrating how external consumers integrate **SeaGlass for Windows** with **zero project references** using only the official NuGet package (`SeaGlass.WinUI`).

---

## Architecture & Integration Highlights

1. **Pure NuGet Consumption**:
   References only `<PackageReference Include="SeaGlass.WinUI" Version="1.0.0" />`. No source or project dependencies.
2. **Single Theme Resource Dictionary**:
   Merged into `App.xaml` in a single line:
   ```xml
   <ResourceDictionary.MergedDictionaries>
       <controls:XamlControlsResources />
       <sg:SeaGlassThemeResources />
   </ResourceDictionary.MergedDictionaries>
   ```
3. **11 XAML Controls (`Views/XamlControlsPage.xaml`)**:
   Showcases `SeaGlassTopAppBar`, `SeaGlassCard`, `SeaGlassButton`, `SeaGlassPill`, `SeaGlassBadge`, `SeaGlassSwitch`, `SeaGlassCheckbox`, `SeaGlassLiquidSlider`, `SeaGlassLinearProgressIndicator`, `SeaGlassCircularProgressIndicator`, and `SeaGlassTextField`.
4. **100% Fluent C# Code-Only Page (`Views/FluentPage.cs`)**:
   Constructed entirely via C# object initialization and method chaining with zero XAML markup.
5. **Backdrop & Optical Depth Requirement (`SeaGlassStage`)**:
   Frosted sea glass is inherently translucent and volumetric rather than opaque plastic. In physical optics, surface micro-coarseness diffuses transmitted light while Beer-Lambert volumetric absorption shifts hue and chroma through material thickness. If rendered over a uniform solid white or black surface, glass lacks the spatial contrast needed for optical refraction, rim illumination, and sanding grain to be perceived by the human eye, appearing merely as flat pastel tinting. Hosting content inside `SeaGlassStage` with non-uniform backdrop imagery or multi-stop gradients provides the background luminance and edge variance essential for genuine sea-glass depth.

---

## Verified Packaging Modes

Both standard Windows App SDK packaging modes are verified and fully supported with identical drop-in semantics:

### 1. Unpackaged Desktop Mode (`WindowsPackageType=None`)
Standard unpackaged WinUI 3 desktop application requiring no package identity or developer mode installation:
```bash
dotnet build SeaGlass.ConsumerSample.csproj -c Release
```
In this mode, `SeaGlass.WinUI.pri` and `SeaGlass.WinUI/Themes/*.xbf` are staged to the output directory via the package's build targets, enabling MRT Core to resolve `ms-appx:///SeaGlass.WinUI/Themes/Generic.xaml`.

### 2. Packaged MSIX Mode (`WindowsPackageType=MSIX`)
Full Windows App SDK MSIX packaged application with single-project packaging:
```bash
dotnet build SeaGlass.ConsumerSample.csproj -c Release -p:WindowsPackageType=MSIX -r win-x64
```
In this mode, the standard WinUI class-library package layout (`lib/<tfm>/SeaGlass.WinUI.pri` and `lib/<tfm>/SeaGlass.WinUI/Themes/*.xbf`) merges directly into the consumer application's `resources.pri` during the build-time PRI indexing pipeline.

---

## ⚠️ Critical WinUI 3 Pitfall: Hosting Code-Only Pages

In WinUI 3 / Windows App SDK, calling `Frame.Navigate(typeof(PureCSharpPage))` invokes `ABI.Microsoft.UI.Xaml.Controls.INavigateMethods.Navigate`, which queries native `IXamlMetadataProvider`. When given a C#-only `Page` subclass without a XAML companion file, the native WinUI 3 runtime fails to resolve the XAML type metadata and crashes with exit code `0xC0000005` (Access Violation).

### Correct Pattern for Code-Only Pages:
Always assign the page instance directly to the host container's `Content` property:
```csharp
// ✅ Correct: Direct instance assignment avoids IXamlMetadataProvider lookup
ContentFrame.Content = new FluentPage();
```
Do **NOT** use `Frame.Navigate`:
```csharp
// ❌ Dangerous: Throws 0xC0000005 if FluentPage lacks XAML metadata registration
ContentFrame.Navigate(typeof(FluentPage));
```
