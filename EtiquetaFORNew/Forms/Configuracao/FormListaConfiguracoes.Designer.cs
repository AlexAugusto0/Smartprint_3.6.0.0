using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EtiquetaFORNew
{
    partial class FormListaConfiguracoes
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Carregar Configuração de Papel"; // ⭐ Título
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblInstrucao = new Label
            {
                Text = "Selecione uma configuração de papel para carregar:", // ⭐ Texto
                Location = new Point(20, 20),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            lstConfiguracoes = new ListBox
            {
                Name = "lstConfiguracoes", // ⭐ Nome do ListBox
                Location = new Point(20, 50),
                Size = new Size(440, 230),
                Font = new Font("Segoe UI", 10)
            };
            lstConfiguracoes.DoubleClick += (s, e) => CarregarSelecionado();

            // Caminho para o arquivo papeis_salvos.xml
            string caminhoListaPapeis = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EtiquetaFornew", "papeis_salvos.xml");

            Label lblInfo = new Label
            {
                Text = $"📁 Local: {Path.GetDirectoryName(caminhoListaPapeis)}", // ⭐ Adaptação do caminho
                Location = new Point(20, 290),
                Size = new Size(440, 20),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            Button btnAbrir = new Button
            {
                Text = "Abrir Pasta",
                Location = new Point(20, 320),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) =>
            {
                try
                {
                    // Abre a pasta onde o arquivo de configurações de papel está
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(caminhoListaPapeis));
                   
                    
                }
                catch { }
            };

            Button btnExcluir = new Button
            {
                Text = "Excluir",
                Location = new Point(130, 320),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExcluir.FlatAppearance.BorderSize = 0;
            btnExcluir.Click += BtnExcluir_Click;

            Button btnCarregar = new Button
            {
                Text = "Carregar",
                Location = new Point(260, 320),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnCarregar.FlatAppearance.BorderSize = 0;
            btnCarregar.Click += (s, e) => CarregarSelecionado();

            Button btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(370, 320),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] {
                lblInstrucao, lstConfiguracoes, lblInfo,
                btnAbrir, btnExcluir, btnCarregar, btnCancelar
            });
        }

        #endregion
    }
}