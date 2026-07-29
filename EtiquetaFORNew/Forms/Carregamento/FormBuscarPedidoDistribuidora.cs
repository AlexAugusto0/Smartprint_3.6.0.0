using System;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public sealed class FormBuscarPedidoDistribuidora : Form
    {
        private readonly TextBox _txtNumeroPedido;

        public string NumeroPedido
        {
            get { return (_txtNumeroPedido.Text ?? string.Empty).Trim(); }
        }

        public FormBuscarPedidoDistribuidora()
        {
            Text = "NF-e / Volumes";
            ClientSize = new Size(360, 125);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var lblNumeroPedido = new Label
            {
                AutoSize = true,
                Left = 16,
                Top = 17,
                Text = "Número do Pedido"
            };

            _txtNumeroPedido = new TextBox
            {
                Left = 16,
                Top = 39,
                Width = 328
            };

            var btnBuscar = new Button
            {
                Text = "Buscar",
                Left = 188,
                Top = 78,
                Width = 75
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Left = 269,
                Top = 78,
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            btnBuscar.Click += BtnBuscar_Click;

            Controls.Add(lblNumeroPedido);
            Controls.Add(_txtNumeroPedido);
            Controls.Add(btnBuscar);
            Controls.Add(btnCancelar);

            AcceptButton = btnBuscar;
            CancelButton = btnCancelar;
        }

        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NumeroPedido))
            {
                MessageBox.Show(
                    this,
                    "Informe o Número do Pedido.",
                    "Atenção",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _txtNumeroPedido.Focus();
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
