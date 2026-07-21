namespace EtiquetaFORNew
{
    partial class FormSincronizacaoSoftcomShop
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
            this.btnSincronizarProdutos = new System.Windows.Forms.Button();
            this.btnBuscarNotaFiscal = new System.Windows.Forms.Button();
            this.btnBuscarVenda = new System.Windows.Forms.Button();
            this.lblUltimaSinc = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnFechar = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lblsincronizacao = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSincronizarProdutos
            // 
            this.btnSincronizarProdutos.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(143)))), ((int)(((byte)(0)))));
            this.btnSincronizarProdutos.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSincronizarProdutos.Location = new System.Drawing.Point(22, 57);
            this.btnSincronizarProdutos.Name = "btnSincronizarProdutos";
            this.btnSincronizarProdutos.Size = new System.Drawing.Size(420, 35);
            this.btnSincronizarProdutos.TabIndex = 0;
            this.btnSincronizarProdutos.Text = "Sincronizar Todos os Produtos";
            this.btnSincronizarProdutos.UseVisualStyleBackColor = false;
            this.btnSincronizarProdutos.Click += new System.EventHandler(this.btnSincronizarProdutos_Click);
            // 
            // btnBuscarNotaFiscal
            // 
            this.btnBuscarNotaFiscal.Enabled = false;
            this.btnBuscarNotaFiscal.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuscarNotaFiscal.Location = new System.Drawing.Point(22, 79);
            this.btnBuscarNotaFiscal.Name = "btnBuscarNotaFiscal";
            this.btnBuscarNotaFiscal.Size = new System.Drawing.Size(23, 35);
            this.btnBuscarNotaFiscal.TabIndex = 1;
            this.btnBuscarNotaFiscal.Text = "Buscar por Nota Fiscal";
            this.btnBuscarNotaFiscal.UseVisualStyleBackColor = true;
            this.btnBuscarNotaFiscal.Visible = false;
            this.btnBuscarNotaFiscal.Click += new System.EventHandler(this.btnBuscarNotaFiscal_Click);
            // 
            // btnBuscarVenda
            // 
            this.btnBuscarVenda.Enabled = false;
            this.btnBuscarVenda.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBuscarVenda.Location = new System.Drawing.Point(71, 79);
            this.btnBuscarVenda.Name = "btnBuscarVenda";
            this.btnBuscarVenda.Size = new System.Drawing.Size(23, 35);
            this.btnBuscarVenda.TabIndex = 2;
            this.btnBuscarVenda.Text = "Buscar por Venda";
            this.btnBuscarVenda.UseVisualStyleBackColor = true;
            this.btnBuscarVenda.Visible = false;
            this.btnBuscarVenda.Click += new System.EventHandler(this.btnBuscarVenda_Click);
            // 
            // lblUltimaSinc
            // 
            this.lblUltimaSinc.AutoSize = true;
            this.lblUltimaSinc.BackColor = System.Drawing.Color.White;
            this.lblUltimaSinc.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUltimaSinc.Location = new System.Drawing.Point(3, 11);
            this.lblUltimaSinc.Name = "lblUltimaSinc";
            this.lblUltimaSinc.Size = new System.Drawing.Size(166, 15);
            this.lblUltimaSinc.TabIndex = 1;
            this.lblUltimaSinc.Text = "Última sincronização: --/--/----";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(3, 5);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(43, 15);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Pronto";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(3, 25);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(454, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 3;
            // 
            // btnFechar
            // 
            this.btnFechar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFechar.Location = new System.Drawing.Point(365, 67);
            this.btnFechar.Name = "btnFechar";
            this.btnFechar.Size = new System.Drawing.Size(90, 30);
            this.btnFechar.TabIndex = 4;
            this.btnFechar.Text = "Fechar";
            this.btnFechar.UseVisualStyleBackColor = true;
            this.btnFechar.Click += new System.EventHandler(this.btnFechar_Click);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lblUltimaSinc);
            this.panel1.Location = new System.Drawing.Point(9, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(462, 38);
            this.panel1.TabIndex = 5;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.White;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.progressBar);
            this.panel2.Controls.Add(this.lblStatus);
            this.panel2.Controls.Add(this.btnFechar);
            this.panel2.Location = new System.Drawing.Point(12, 184);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(460, 102);
            this.panel2.TabIndex = 6;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.White;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.lblsincronizacao);
            this.panel3.Controls.Add(this.pictureBox1);
            this.panel3.Controls.Add(this.btnSincronizarProdutos);
            this.panel3.Controls.Add(this.btnBuscarNotaFiscal);
            this.panel3.Controls.Add(this.btnBuscarVenda);
            this.panel3.Location = new System.Drawing.Point(13, 59);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(457, 115);
            this.panel3.TabIndex = 7;
            // 
            // lblsincronizacao
            // 
            this.lblsincronizacao.AutoSize = true;
            this.lblsincronizacao.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblsincronizacao.Location = new System.Drawing.Point(99, 12);
            this.lblsincronizacao.Name = "lblsincronizacao";
            this.lblsincronizacao.Size = new System.Drawing.Size(256, 30);
            this.lblsincronizacao.TabIndex = 4;
            this.lblsincronizacao.Text = "Sincronização de dados";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::EtiquetaFORNew.Properties.Resources.Screenshot_1;
            this.pictureBox1.Location = new System.Drawing.Point(28, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(66, 48);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // FormSincronizacaoSoftcomShop
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(235)))), ((int)(((byte)(255)))));
            this.ClientSize = new System.Drawing.Size(484, 296);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSincronizacaoSoftcomShop";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Sincronização SoftcomShop";
            this.Load += new System.EventHandler(this.FormSincronizacaoSoftcomShop_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSincronizarProdutos;
        private System.Windows.Forms.Button btnBuscarNotaFiscal;
        private System.Windows.Forms.Button btnBuscarVenda;
        private System.Windows.Forms.Label lblUltimaSinc;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnFechar;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblsincronizacao;
    }
}
