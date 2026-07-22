using System;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew.Forms
{
    /// <summary>
    /// Diálogo simples para solicitar o nome de um novo template
    /// </summary>
    public class FormNomeTemplate : Form
    {
        private TextBox txtNome;
        private Button btnOK;
        private Button btnCancelar;
        private Label lblTitulo;
        private Label lblInstrucao;

        public string NomeTemplate { get; private set; }

        public FormNomeTemplate()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Configuração do Form
            this.Text = "Nome do Template";
            this.Size = new Size(450, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Título
            lblTitulo = new Label
            {
                Text = "💾 Salvar Template",
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            this.Controls.Add(lblTitulo);

            // Instrução
            lblInstrucao = new Label
            {
                Text = "Digite um nome para identificar este template:",
                Location = new Point(20, 60),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(127, 140, 141)
            };
            this.Controls.Add(lblInstrucao);

            // TextBox para o nome
            txtNome = new TextBox
            {
                Location = new Point(20, 85),
                Size = new Size(390, 25),
                Font = new Font("Segoe UI", 10),
                MaxLength = 50
            };
            txtNome.KeyDown += TxtNome_KeyDown;
            this.Controls.Add(txtNome);

            // Botão OK
            btnOK = new Button
            {
                Text = "✓ Salvar",
                Location = new Point(200, 120),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            // Botão Cancelar
            btnCancelar = new Button
            {
                Text = "✕ Cancelar",
                Location = new Point(310, 120),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Click += BtnCancelar_Click;
            this.Controls.Add(btnCancelar);

            // Define botões padrão
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancelar;

            // Focus no TextBox ao abrir
            this.Load += (s, e) => txtNome.Focus();
        }

        private void TxtNome_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BtnOK_Click(sender, e);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string nome = txtNome.Text.Trim();

            // Validação
            if (string.IsNullOrWhiteSpace(nome))
            {
                MessageBox.Show(
                    "Por favor, digite um nome para o template.",
                    "Nome Obrigatório",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtNome.Focus();
                return;
            }

            // Validar caracteres inválidos para nome de arquivo
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            if (nome.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show(
                    "O nome contém caracteres inválidos.\n\n" +
                    "Evite usar: \\ / : * ? \" < > |",
                    "Nome Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtNome.Focus();
                return;
            }

            // Limite de tamanho
            if (nome.Length > 50)
            {
                MessageBox.Show(
                    "O nome do template deve ter no máximo 50 caracteres.",
                    "Nome Muito Longo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtNome.Focus();
                return;
            }

            // Tudo OK
            NomeTemplate = nome;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}