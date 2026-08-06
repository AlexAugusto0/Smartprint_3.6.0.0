using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    /// <summary>Fonte única das cores e estilos da interface do SmartPrint Designer.</summary>
    public static class ThemeManager
    {
        private const int PrimaryButtonCornerRadius = 12;

        public static Color PanelBackground => Color.White;
        //public static Color CanvasBackground => Color.FromArgb(244, 245, 247);

        public static Color CanvasBackground => Color.FromArgb(240, 235, 255);
        //public static Color WorkspaceBackground => Color.FromArgb(230, 231, 234);
        public static Color WorkspaceBackground => Color.FromArgb(240, 235, 255);
        public static Color SmartPrintOrange => Color.FromArgb(245, 124, 0);
        public static Color SmartPrintOrangeDark => Color.FromArgb(230, 103, 0);
        public static Color TextPrimary => Color.FromArgb(51, 51, 51);        
        public static Color TextSecondary => Color.FromArgb(100, 106, 115);
        public static Color HoverBackground => Color.FromArgb(255, 224, 178);
        public static Color Border => Color.FromArgb(218, 221, 226);
        public static Color HeaderBackground => Color.FromArgb(70, 73, 76);
        public static Color HeaderText => Color.White;
        public static Color ToolbarBackground => Color.White;
        public static Color StatusBackground => Color.FromArgb(248, 249, 250);
        public static Color Danger => Color.FromArgb(211, 47, 47);
        public static Color Disabled => Color.FromArgb(238, 239, 241);
        public static Color Shadow => Color.FromArgb(45, 0, 0, 0);

        public static Font HeaderFont => new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        public static Font SectionFont => new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        public static Font ButtonFont => new Font("Segoe UI", 9F, FontStyle.Regular);

        public static void StyleActionButton(Button button, bool primary = false)
        {
            Color normal = primary ? SmartPrintOrange : PanelBackground;
            Color hover = primary ? SmartPrintOrangeDark : HoverBackground;
            button.AutoSize = false;
            button.Height = 34;
            button.Padding = new Padding(8, 0, 8, 0);
            button.Font = ButtonFont;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? SmartPrintOrangeDark : Border;
            button.BackColor = normal;
            button.ForeColor = primary ? Color.White : TextPrimary;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.MouseEnter += (s, e) => { if (button.Enabled) button.BackColor = hover; };
            button.MouseLeave += (s, e) => button.BackColor = button.Enabled ? normal : Disabled;
            button.EnabledChanged += (s, e) => button.BackColor = button.Enabled ? normal : Disabled;
        }

        public static void StylePrimaryActionButton(Button button)
        {
            StyleActionButton(button, true);
            button.ForeColor = TextPrimary;
            EnableRoundedRendering(button, PrimaryButtonCornerRadius);
        }

        public static void EnableRoundedRendering(Button button, int logicalRadius)
        {
            ApplyRoundedCorners(button, logicalRadius);
            button.SizeChanged += (s, e) => ApplyRoundedCorners(button, logicalRadius);
            button.Paint += (s, e) => DrawAntiAliasedBorder(button, e.Graphics, logicalRadius);
        }

        public static void ApplyRoundedCorners(Button button, int logicalRadius)
        {
            if (button.Width <= 0 || button.Height <= 0)
                return;

            float dpiScale = button.DeviceDpi / 96F;
            float radius = System.Math.Max(1F, logicalRadius * dpiScale);
            RectangleF rectangle = button.ClientRectangle;

            using (GraphicsPath path = CreateRoundedPath(rectangle, radius))
            {
                Region previousRegion = button.Region;
                button.Region = new Region(path);
                previousRegion?.Dispose();
            }
        }

        private static void DrawAntiAliasedBorder(Button button, Graphics graphics, int logicalRadius)
        {
            int borderSize = button.FlatAppearance.BorderSize;
            if (borderSize <= 0 || button.Width <= borderSize || button.Height <= borderSize)
                return;

            float dpiScale = button.DeviceDpi / 96F;
            float radius = System.Math.Max(1F, logicalRadius * dpiScale);
            float inset = borderSize / 2F;
            RectangleF borderBounds = new RectangleF(
                inset,
                inset,
                button.ClientSize.Width - borderSize,
                button.ClientSize.Height - borderSize);

            GraphicsState state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (GraphicsPath path = CreateRoundedPath(borderBounds, radius - inset))
                using (var pen = new Pen(button.FlatAppearance.BorderColor, borderSize))
                {
                    pen.Alignment = PenAlignment.Center;
                    graphics.DrawPath(pen, path);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private static GraphicsPath CreateRoundedPath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float effectiveRadius = System.Math.Max(1F,
                System.Math.Min(radius, System.Math.Min(bounds.Width, bounds.Height) / 2F));
            float diameter = effectiveRadius * 2F;

            path.StartFigure();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void StyleToolCard(Button button, bool danger = false)
        {
            Color accent = danger ? Danger : SmartPrintOrange;
            button.Size = new Size(button.Width, 48);
            button.Padding = new Padding(12, 0, 8, 0);
            button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            button.BackColor = PanelBackground;
            button.ForeColor = danger ? Danger : TextPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = HoverBackground;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.UseVisualStyleBackColor = false;
            button.Paint += (s, e) =>
            {
                using (var brush = new SolidBrush(accent))
                    e.Graphics.FillRectangle(brush, 0, 0, 4, button.Height);
            };
        }

        public static void StyleInput(Control control)
        {
            control.Font = ButtonFont;
            control.ForeColor = TextPrimary;
            control.Margin = new Padding(4, 4, 4, 8);
        }
    }
}
