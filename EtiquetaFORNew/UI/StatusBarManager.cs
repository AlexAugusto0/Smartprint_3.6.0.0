using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    public sealed class StatusBarManager
    {
        private readonly ToolStripStatusLabel documentLabel;
        private readonly ToolStripStatusLabel zoomLabel;
        private readonly ToolStripStatusLabel cursorLabel;
        private readonly ToolStripStatusLabel elementLabel;
        private readonly ToolStripStatusLabel detailsLabel;

        public StatusBarManager(StatusStrip statusStrip)
        {
            statusStrip.BackColor = ThemeManager.StatusBackground;
            statusStrip.ForeColor = ThemeManager.TextPrimary;
            statusStrip.SizingGrip = false;
            statusStrip.Padding = new Padding(8, 0, 8, 0);
            documentLabel = CreateLabel("Etiqueta: --");
            zoomLabel = CreateLabel("Zoom: 100%");
            cursorLabel = CreateLabel("Cursor: X:0 Y:0");
            elementLabel = CreateLabel("Elemento: Nenhum");
            detailsLabel = CreateLabel(string.Empty);
            detailsLabel.Spring = true;
            detailsLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(documentLabel);
            statusStrip.Items.Add(CreateSeparator());
            statusStrip.Items.Add(zoomLabel);
            statusStrip.Items.Add(CreateSeparator());
            statusStrip.Items.Add(cursorLabel);
            statusStrip.Items.Add(CreateSeparator());
            statusStrip.Items.Add(elementLabel);
            statusStrip.Items.Add(CreateSeparator());
            statusStrip.Items.Add(detailsLabel);
            statusStrip.Items.Add(CreateLabel("Ctrl+S Salvar"));
        }

        public void SetDocument(float width, float height)
        {
            documentLabel.Text = string.Format("Etiqueta: {0:0.#} x {1:0.#} mm", width, height);
        }

        public void SetZoom(float zoom)
        {
            zoomLabel.Text = string.Format("Zoom: {0:0}%", zoom * 100F);
        }

        public void SetCursor(float x, float y, bool inside)
        {
            cursorLabel.Text = inside
                ? string.Format("Cursor: X:{0:0.#} Y:{1:0.#}", x, y)
                : "Cursor: --";
        }

        public void SetSelection(string type, Rectangle bounds, int count)
        {
            if (count <= 0)
            {
                elementLabel.Text = "Elemento: Nenhum";
                detailsLabel.Text = string.Empty;
                return;
            }
            elementLabel.Text = count > 1 ? string.Format("Elementos: {0}", count) : "Elemento: " + type;
            detailsLabel.Text = count > 1
                ? "Seleção múltipla"
                : string.Format("Posição: {0},{1}   Tamanho: {2} x {3}",
                    bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private static ToolStripStatusLabel CreateLabel(string text)
        {
            return new ToolStripStatusLabel(text)
            {
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = ThemeManager.TextSecondary,
                Margin = new Padding(4, 2, 4, 2)
            };
        }

        private static ToolStripStatusLabel CreateSeparator()
        {
            return new ToolStripStatusLabel("|")
            {
                ForeColor = ThemeManager.Border,
                Margin = new Padding(1, 2, 1, 2)
            };
        }
    }
}
