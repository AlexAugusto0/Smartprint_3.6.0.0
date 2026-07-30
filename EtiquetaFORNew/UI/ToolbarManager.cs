using System;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew.UI
{
    public sealed class DesignerToolbar
    {
        public Panel Container { get; internal set; }
        public FlowLayoutPanel ActionPanel { get; internal set; }
        public Button UndoButton { get; internal set; }
        public Button RedoButton { get; internal set; }
        public Button ZoomOutButton { get; internal set; }
        public Button ZoomInButton { get; internal set; }
        public Label ZoomLabel { get; internal set; }
        public StatusStrip StatusStrip { get; internal set; }
    }

    public static class ToolbarManager
    {
        public static DesignerToolbar Create(
            Action newAction, Action saveAction, Action previewAction, Action undoAction,
            Action zoomOutAction, Action zoomInAction, Action closeAction)
        {
            var result = new DesignerToolbar();
            var container = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = ThemeManager.ToolbarBackground,
                Padding = new Padding(8, 5, 8, 0)
            };
            var commandRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = ThemeManager.ToolbarBackground,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = ThemeManager.ToolbarBackground,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            Add(actions, CreateButton("＋ Novo", 78, newAction));
            Add(actions, CreateButton("▣ Salvar", 82, saveAction, true));
            Add(actions, CreateButton("◉ Preview", 86, previewAction));
            result.UndoButton = CreateButton("↶", 38, undoAction);
            result.RedoButton = CreateButton("↷", 38, null);
            result.RedoButton.Enabled = false;
            Add(actions, result.UndoButton);
            Add(actions, result.RedoButton);
            result.ZoomOutButton = CreateButton("−", 38, zoomOutAction);
            result.ZoomLabel = new Label
            {
                Text = "100%",
                AutoSize = false,
                Size = new Size(54, 34),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = ThemeManager.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(2, 2, 2, 4)
            };
            result.ZoomInButton = CreateButton("+", 38, zoomInAction);
            var zoomPanel = CreateCenteredFlowPanel();
            Add(zoomPanel, result.ZoomOutButton);
            zoomPanel.Controls.Add(result.ZoomLabel);
            Add(zoomPanel, result.ZoomInButton);

            var closeButton = CreateButton("✕ Fechar", 90, closeAction);
            var closePanel = CreateCenteredFlowPanel(FlowDirection.RightToLeft);
            Add(closePanel, closeButton);
            commandRow.Controls.Add(actions, 0, 0);
            commandRow.Controls.Add(zoomPanel, 1, 0);
            commandRow.Controls.Add(closePanel, 2, 0);
            var statusStrip = new StatusStrip { Dock = DockStyle.Bottom, Height = 25 };
            container.Controls.Add(commandRow);
            container.Controls.Add(statusStrip);
            result.Container = container;
            result.ActionPanel = actions;
            result.StatusStrip = statusStrip;
            return result;
        }

        private static Button CreateButton(string text, int width, Action action, bool primary = false)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Margin = new Padding(2, 2, 2, 4),
                UseVisualStyleBackColor = false
            };
            ThemeManager.StyleActionButton(button, primary);
            if (action != null) button.Click += (s, e) => action();
            return button;
        }

        private static void Add(Control parent, Control child) { parent.Controls.Add(child); }

        private static FlowLayoutPanel CreateCenteredFlowPanel(
            FlowDirection direction = FlowDirection.LeftToRight)
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = direction,
                WrapContents = false,
                BackColor = ThemeManager.ToolbarBackground,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
        }

    }
}
