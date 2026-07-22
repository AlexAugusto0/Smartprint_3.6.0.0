namespace EtiquetaFORNew.Forms
{
    partial class FormMenuConfiguracao
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.btnCarregar = new System.Windows.Forms.Button();
            this.btnNovaConfig = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.btnCarregar);
            this.panel1.Controls.Add(this.btnNovaConfig);
            this.panel1.Location = new System.Drawing.Point(12, 11);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(387, 109);
            this.panel1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(37, 34);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(327, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Deseja Criar uma Nova configuração ou Carregar uma Já existente?";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(263, 62);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(94, 32);
            this.button2.TabIndex = 2;
            this.button2.Text = "Fechar";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // btnCarregar
            // 
            this.btnCarregar.Location = new System.Drawing.Point(146, 61);
            this.btnCarregar.Name = "btnCarregar";
            this.btnCarregar.Size = new System.Drawing.Size(94, 32);
            this.btnCarregar.TabIndex = 1;
            this.btnCarregar.Text = "Carregar";
            this.btnCarregar.UseVisualStyleBackColor = true;
            this.btnCarregar.Click += new System.EventHandler(this.btnCarregar_Click);
            // 
            // btnNovaConfig
            // 
            this.btnNovaConfig.Location = new System.Drawing.Point(21, 61);
            this.btnNovaConfig.Name = "btnNovaConfig";
            this.btnNovaConfig.Size = new System.Drawing.Size(94, 32);
            this.btnNovaConfig.TabIndex = 0;
            this.btnNovaConfig.Text = "Novo";
            this.btnNovaConfig.UseVisualStyleBackColor = true;
            this.btnNovaConfig.Click += new System.EventHandler(this.btnNovaConfig_Click);
            // 
            // FormMenuConfiguracao
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(196)))), ((int)(((byte)(15)))));
            this.ClientSize = new System.Drawing.Size(410, 131);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormMenuConfiguracao";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormMenuConfiguracao";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnCarregar;
        private System.Windows.Forms.Button btnNovaConfig;
    }
}