using System;
using System.Drawing;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    public partial class FormNomeTemplate : Form
    {
        public string NomeTemplate { get; private set; }   
                  
        
        public FormNomeTemplate(
            string nomeInicial = null,
            string titulo = "Salvar Template",
            string instrucao = "Digite um nome para o template:",
            string textoBotao = "Salvar",
            Color? corBotao = null)
        {
            InitializeComponent(nomeInicial, titulo, instrucao, textoBotao, corBotao);
        }

        private void InitializeComponent(
            string nomeInicial,
            string titulo,
            string instrucao,
            string textoBotao,
            Color? corBotao)
        {
            this.Text = titulo;
            this.Size = new Size(400, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblInstrucao = new Label
            {
                Text = instrucao,
                Location = new Point(20, 20),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 10)
            };

            TextBox txtNome = new TextBox
            {
                Name = "txtNome",
                Location = new Point(20, 50),
                Size = new Size(340, 25),
                Font = new Font("Segoe UI", 10),
                Text = nomeInicial ?? ""
            };

            //Color corFundoBotao = corBotao ?? Color.FromArgb(237, 222, 31);
            Color corFundoBotao = corBotao ?? Color.FromArgb(209, 196, 27);

            Button btnSalvar = new Button
            {
                Text = textoBotao,
                Location = new Point(190, 100),
                Size = new Size(80, 30),
                //BackColor = Color.FromArgb(46, 204, 113),
                BackColor = corFundoBotao,
                ForeColor = Color.Black,
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

                if (string.IsNullOrWhiteSpace(nome))
                {
                    MessageBox.Show("Digite um nome válido!", "Atenção",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                NomeTemplate = nome;
            };

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(280, 100),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblInstrucao, txtNome, btnSalvar, btnCancelar });
            this.AcceptButton = btnSalvar;
            this.CancelButton = btnCancelar;
            txtNome.SelectAll();
            txtNome.Focus();
        }
    }
}