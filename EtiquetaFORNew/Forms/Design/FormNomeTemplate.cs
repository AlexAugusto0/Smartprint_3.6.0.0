using System;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public partial class FormNomeTemplate : Form
    {
        public string NomeTemplate { get; private set; }

        public FormNomeTemplate()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Salvar Template";
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblInstrucao = new Label
            {
                Text = "Digite um nome para o template:",
                Location = new Point(20, 20),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 10)
            };

            TextBox txtNome = new TextBox
            {
                Name = "txtNome",
                Location = new Point(20, 50),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10)
            };

            Button btnSalvar = new Button
            {
                Text = "Salvar",
                Location = new Point(190, 100),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnSalvar.FlatAppearance.BorderSize = 0;
            btnSalvar.Click += (s, e) =>
            {
                string nome = txtNome.Text.Trim();
                if (string.IsNullOrWhiteSpace(nome))
                {
                    MessageBox.Show("Digite um nome válido!", "Atenção",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                // Remove caracteres inválidos para nome de arquivo
                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                {
                    nome = nome.Replace(c.ToString(), "");
                }

                NomeTemplate = nome;
            };

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(280, 100),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblInstrucao, txtNome, btnSalvar, btnCancelar });
            this.AcceptButton = btnSalvar;
            this.CancelButton = btnCancelar;
        }
    }
}