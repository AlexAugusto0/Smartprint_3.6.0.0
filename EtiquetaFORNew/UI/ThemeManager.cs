using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    /// <summary>Fonte única das cores e estilos da interface do SmartPrint Designer.</summary>
    public static class ThemeManager
    {
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
