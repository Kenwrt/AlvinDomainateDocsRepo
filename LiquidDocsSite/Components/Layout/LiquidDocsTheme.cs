namespace LiquidDocsSite.Components.Layout;

using MudBlazor;

public static class LiquidDocsTheme
{
    public static readonly MudTheme Light = new()
    {
        PaletteLight = new PaletteLight
        {
            // Brand
            Primary = "#253F59",          // Slate Navy Blue
            Secondary = "#0D1C2E",        // Deep Finance Navy
            Tertiary = "#223447",         // Deep Slate Blue

            // Layout surfaces
            Background = "#E6E6E6",       // Soft Light Gray (page backdrop)
            Surface = "#FFFFFF",          // Paper White (cards/papers)
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "#0D1C2E",

            // Text
            TextPrimary = "#1E1E1E",      // Slate Black
            TextSecondary = "#333333",    // Dark Charcoal Gray

            // Lines/dividers
            Divider = "#B0B0B0",          // Cool Medium Gray

            // States
            ActionDefault = "#253F59",
            ActionDisabled = "#B0B0B0",
            ActionDisabledBackground = "#E6E6E6",
        }
    };

    public static readonly MudTheme Dark = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#3E566E",          // Muted Steel Hover becomes a great dark-primary
            Secondary = "#B0B0B0",
            Tertiary = "#253F59",

            Background = "#1E1E1E",       // Slate Black
            Surface = "#223447",          // Deep Slate Blue as surface
            DrawerBackground = "#0D1C2E",
            AppbarBackground = "#0D1C2E",

            TextPrimary = "#FFFFFF",
            TextSecondary = "#E6E6E6",
            Divider = "#5C5C5C",

            ActionDefault = "#E6E6E6",
            ActionDisabled = "#5C5C5C",
            ActionDisabledBackground = "#333333",
        }
    };
}
