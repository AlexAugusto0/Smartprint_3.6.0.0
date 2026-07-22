namespace EtiquetaFORNew
{
    partial class FormConfigEtiqueta
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelPrincipal = new System.Windows.Forms.Panel();
            this.panelDireita = new System.Windows.Forms.Panel();
            this.groupPreview = new System.Windows.Forms.GroupBox();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.panelEsquerda = new System.Windows.Forms.Panel();
            this.groupMargens = new System.Windows.Forms.GroupBox();
            this.lblMmMargemDir = new System.Windows.Forms.Label();
            this.numMargemDireita = new System.Windows.Forms.NumericUpDown();
            this.lblMargemDireita = new System.Windows.Forms.Label();
            this.lblMmMargemEsq = new System.Windows.Forms.Label();
            this.numMargemEsquerda = new System.Windows.Forms.NumericUpDown();
            this.lblMargemEsquerda = new System.Windows.Forms.Label();
            this.lblMmMargemInf = new System.Windows.Forms.Label();
            this.numMargemInferior = new System.Windows.Forms.NumericUpDown();
            this.lblMargemInferior = new System.Windows.Forms.Label();
            this.lblMmMargemSup = new System.Windows.Forms.Label();
            this.numMargemSuperior = new System.Windows.Forms.NumericUpDown();
            this.lblMargemSuperior = new System.Windows.Forms.Label();
            this.groupLayout = new System.Windows.Forms.GroupBox();
            this.lblMmEspacLin = new System.Windows.Forms.Label();
            this.numEspacamentoLinhas = new System.Windows.Forms.NumericUpDown();
            this.lblEspacamentoLinhas = new System.Windows.Forms.Label();
            this.lblMmEspacCol = new System.Windows.Forms.Label();
            this.numEspacamentoColunas = new System.Windows.Forms.NumericUpDown();
            this.lblEspacamentoColunas = new System.Windows.Forms.Label();
            this.numLinhas = new System.Windows.Forms.NumericUpDown();
            this.lblLinhas = new System.Windows.Forms.Label();
            this.numColunas = new System.Windows.Forms.NumericUpDown();
            this.lblColunas = new System.Windows.Forms.Label();
            this.groupDimensoes = new System.Windows.Forms.GroupBox();
            this.lblMmAltura = new System.Windows.Forms.Label();
            this.numAltura = new System.Windows.Forms.NumericUpDown();
            this.lblAltura = new System.Windows.Forms.Label();
            this.lblMmLargura = new System.Windows.Forms.Label();
            this.numLargura = new System.Windows.Forms.NumericUpDown();
            this.lblLargura = new System.Windows.Forms.Label();
            this.groupDadosEtiqueta = new System.Windows.Forms.GroupBox();
            this.cmbPapel = new System.Windows.Forms.ComboBox();
            this.lblPapel = new System.Windows.Forms.Label();
            this.cmbImpressora = new System.Windows.Forms.ComboBox();
            this.lblImpressora = new System.Windows.Forms.Label();
            this.txtNomeEtiqueta = new System.Windows.Forms.TextBox();
            this.lblNomeEtiqueta = new System.Windows.Forms.Label();
            this.panelBotoes = new System.Windows.Forms.Panel();
            this.btnTestarImpressao = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.panelPrincipal.SuspendLayout();
            this.panelDireita.SuspendLayout();
            this.groupPreview.SuspendLayout();
            this.panelEsquerda.SuspendLayout();
            this.groupMargens.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemDireita)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemEsquerda)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemInferior)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemSuperior)).BeginInit();
            this.groupLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEspacamentoLinhas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEspacamentoColunas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLinhas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numColunas)).BeginInit();
            this.groupDimensoes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAltura)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLargura)).BeginInit();
            this.groupDadosEtiqueta.SuspendLayout();
            this.panelBotoes.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelPrincipal
            // 
            this.panelPrincipal.Controls.Add(this.panelDireita);
            this.panelPrincipal.Controls.Add(this.panelEsquerda);
            this.panelPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPrincipal.Location = new System.Drawing.Point(0, 0);
            this.panelPrincipal.Name = "panelPrincipal";
            this.panelPrincipal.Size = new System.Drawing.Size(900, 600);
            this.panelPrincipal.TabIndex = 0;
            // 
            // panelDireita
            // 
            this.panelDireita.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.panelDireita.Controls.Add(this.groupPreview);
            this.panelDireita.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDireita.Location = new System.Drawing.Point(450, 0);
            this.panelDireita.Name = "panelDireita";
            this.panelDireita.Padding = new System.Windows.Forms.Padding(10);
            this.panelDireita.Size = new System.Drawing.Size(450, 600);
            this.panelDireita.TabIndex = 1;
            // 
            // groupPreview
            // 
            this.groupPreview.Controls.Add(this.panelPreview);
            this.groupPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupPreview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupPreview.Location = new System.Drawing.Point(10, 10);
            this.groupPreview.Name = "groupPreview";
            this.groupPreview.Size = new System.Drawing.Size(430, 580);
            this.groupPreview.TabIndex = 0;
            this.groupPreview.TabStop = false;
            this.groupPreview.Text = "👁️ Preview";
            // 
            // panelPreview
            // 
            this.panelPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelPreview.BackColor = System.Drawing.Color.White;
            this.panelPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPreview.Location = new System.Drawing.Point(10, 25);
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Size = new System.Drawing.Size(410, 545);
            this.panelPreview.TabIndex = 0;
            // 
            // panelEsquerda
            // 
            this.panelEsquerda.AutoScroll = true;
            this.panelEsquerda.BackColor = System.Drawing.Color.White;
            this.panelEsquerda.Controls.Add(this.groupMargens);
            this.panelEsquerda.Controls.Add(this.groupLayout);
            this.panelEsquerda.Controls.Add(this.groupDimensoes);
            this.panelEsquerda.Controls.Add(this.groupDadosEtiqueta);
            this.panelEsquerda.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelEsquerda.Location = new System.Drawing.Point(0, 0);
            this.panelEsquerda.Name = "panelEsquerda";
            this.panelEsquerda.Padding = new System.Windows.Forms.Padding(10);
            this.panelEsquerda.Size = new System.Drawing.Size(450, 600);
            this.panelEsquerda.TabIndex = 0;
            // 
            // groupMargens
            // 
            this.groupMargens.Controls.Add(this.lblMmMargemDir);
            this.groupMargens.Controls.Add(this.numMargemDireita);
            this.groupMargens.Controls.Add(this.lblMargemDireita);
            this.groupMargens.Controls.Add(this.lblMmMargemEsq);
            this.groupMargens.Controls.Add(this.numMargemEsquerda);
            this.groupMargens.Controls.Add(this.lblMargemEsquerda);
            this.groupMargens.Controls.Add(this.lblMmMargemInf);
            this.groupMargens.Controls.Add(this.numMargemInferior);
            this.groupMargens.Controls.Add(this.lblMargemInferior);
            this.groupMargens.Controls.Add(this.lblMmMargemSup);
            this.groupMargens.Controls.Add(this.numMargemSuperior);
            this.groupMargens.Controls.Add(this.lblMargemSuperior);
            this.groupMargens.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupMargens.Location = new System.Drawing.Point(10, 430);
            this.groupMargens.Name = "groupMargens";
            this.groupMargens.Size = new System.Drawing.Size(420, 150);
            this.groupMargens.TabIndex = 3;
            this.groupMargens.TabStop = false;
            this.groupMargens.Text = "4  ️⃣     Margens";
            this.groupMargens.Enter += new System.EventHandler(this.groupMargens_Enter);
            // 
            // lblMmMargemDir
            // 
            this.lblMmMargemDir.AutoSize = true;
            this.lblMmMargemDir.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmMargemDir.Location = new System.Drawing.Point(235, 123);
            this.lblMmMargemDir.Name = "lblMmMargemDir";
            this.lblMmMargemDir.Size = new System.Drawing.Size(29, 15);
            this.lblMmMargemDir.TabIndex = 11;
            this.lblMmMargemDir.Text = "mm";
            // 
            // numMargemDireita
            // 
            this.numMargemDireita.DecimalPlaces = 1;
            this.numMargemDireita.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numMargemDireita.Location = new System.Drawing.Point(150, 121);
            this.numMargemDireita.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numMargemDireita.Name = "numMargemDireita";
            this.numMargemDireita.Size = new System.Drawing.Size(80, 23);
            this.numMargemDireita.TabIndex = 10;
            this.numMargemDireita.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblMargemDireita
            // 
            this.lblMargemDireita.AutoSize = true;
            this.lblMargemDireita.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMargemDireita.Location = new System.Drawing.Point(15, 123);
            this.lblMargemDireita.Name = "lblMargemDireita";
            this.lblMargemDireita.Size = new System.Drawing.Size(92, 15);
            this.lblMargemDireita.TabIndex = 9;
            this.lblMargemDireita.Text = "Margem Direita:";
            // 
            // lblMmMargemEsq
            // 
            this.lblMmMargemEsq.AutoSize = true;
            this.lblMmMargemEsq.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmMargemEsq.Location = new System.Drawing.Point(235, 93);
            this.lblMmMargemEsq.Name = "lblMmMargemEsq";
            this.lblMmMargemEsq.Size = new System.Drawing.Size(29, 15);
            this.lblMmMargemEsq.TabIndex = 8;
            this.lblMmMargemEsq.Text = "mm";
            // 
            // numMargemEsquerda
            // 
            this.numMargemEsquerda.DecimalPlaces = 1;
            this.numMargemEsquerda.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numMargemEsquerda.Location = new System.Drawing.Point(150, 91);
            this.numMargemEsquerda.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numMargemEsquerda.Name = "numMargemEsquerda";
            this.numMargemEsquerda.Size = new System.Drawing.Size(80, 23);
            this.numMargemEsquerda.TabIndex = 7;
            this.numMargemEsquerda.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblMargemEsquerda
            // 
            this.lblMargemEsquerda.AutoSize = true;
            this.lblMargemEsquerda.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMargemEsquerda.Location = new System.Drawing.Point(15, 93);
            this.lblMargemEsquerda.Name = "lblMargemEsquerda";
            this.lblMargemEsquerda.Size = new System.Drawing.Size(106, 15);
            this.lblMargemEsquerda.TabIndex = 6;
            this.lblMargemEsquerda.Text = "Margem Esquerda:";
            // 
            // lblMmMargemInf
            // 
            this.lblMmMargemInf.AutoSize = true;
            this.lblMmMargemInf.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmMargemInf.Location = new System.Drawing.Point(235, 63);
            this.lblMmMargemInf.Name = "lblMmMargemInf";
            this.lblMmMargemInf.Size = new System.Drawing.Size(29, 15);
            this.lblMmMargemInf.TabIndex = 5;
            this.lblMmMargemInf.Text = "mm";
            // 
            // numMargemInferior
            // 
            this.numMargemInferior.DecimalPlaces = 1;
            this.numMargemInferior.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numMargemInferior.Location = new System.Drawing.Point(150, 61);
            this.numMargemInferior.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numMargemInferior.Name = "numMargemInferior";
            this.numMargemInferior.Size = new System.Drawing.Size(80, 23);
            this.numMargemInferior.TabIndex = 4;
            this.numMargemInferior.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblMargemInferior
            // 
            this.lblMargemInferior.AutoSize = true;
            this.lblMargemInferior.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMargemInferior.Location = new System.Drawing.Point(15, 63);
            this.lblMargemInferior.Name = "lblMargemInferior";
            this.lblMargemInferior.Size = new System.Drawing.Size(96, 15);
            this.lblMargemInferior.TabIndex = 3;
            this.lblMargemInferior.Text = "Margem Inferior:";
            // 
            // lblMmMargemSup
            // 
            this.lblMmMargemSup.AutoSize = true;
            this.lblMmMargemSup.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmMargemSup.Location = new System.Drawing.Point(235, 33);
            this.lblMmMargemSup.Name = "lblMmMargemSup";
            this.lblMmMargemSup.Size = new System.Drawing.Size(29, 15);
            this.lblMmMargemSup.TabIndex = 2;
            this.lblMmMargemSup.Text = "mm";
            // 
            // numMargemSuperior
            // 
            this.numMargemSuperior.DecimalPlaces = 1;
            this.numMargemSuperior.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numMargemSuperior.Location = new System.Drawing.Point(150, 31);
            this.numMargemSuperior.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numMargemSuperior.Name = "numMargemSuperior";
            this.numMargemSuperior.Size = new System.Drawing.Size(80, 23);
            this.numMargemSuperior.TabIndex = 1;
            this.numMargemSuperior.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblMargemSuperior
            // 
            this.lblMargemSuperior.AutoSize = true;
            this.lblMargemSuperior.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMargemSuperior.Location = new System.Drawing.Point(15, 33);
            this.lblMargemSuperior.Name = "lblMargemSuperior";
            this.lblMargemSuperior.Size = new System.Drawing.Size(102, 15);
            this.lblMargemSuperior.TabIndex = 0;
            this.lblMargemSuperior.Text = "Margem Superior:";
            // 
            // groupLayout
            // 
            this.groupLayout.Controls.Add(this.lblMmEspacLin);
            this.groupLayout.Controls.Add(this.numEspacamentoLinhas);
            this.groupLayout.Controls.Add(this.lblEspacamentoLinhas);
            this.groupLayout.Controls.Add(this.lblMmEspacCol);
            this.groupLayout.Controls.Add(this.numEspacamentoColunas);
            this.groupLayout.Controls.Add(this.lblEspacamentoColunas);
            this.groupLayout.Controls.Add(this.numLinhas);
            this.groupLayout.Controls.Add(this.lblLinhas);
            this.groupLayout.Controls.Add(this.numColunas);
            this.groupLayout.Controls.Add(this.lblColunas);
            this.groupLayout.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupLayout.Location = new System.Drawing.Point(10, 280);
            this.groupLayout.Name = "groupLayout";
            this.groupLayout.Size = new System.Drawing.Size(420, 144);
            this.groupLayout.TabIndex = 2;
            this.groupLayout.TabStop = false;
            this.groupLayout.Text = "3  ️⃣    Layout (Colunas e Linhas)";
            // 
            // lblMmEspacLin
            // 
            this.lblMmEspacLin.AutoSize = true;
            this.lblMmEspacLin.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmEspacLin.Location = new System.Drawing.Point(285, 125);
            this.lblMmEspacLin.Name = "lblMmEspacLin";
            this.lblMmEspacLin.Size = new System.Drawing.Size(29, 15);
            this.lblMmEspacLin.TabIndex = 9;
            this.lblMmEspacLin.Text = "mm";
            // 
            // numEspacamentoLinhas
            // 
            this.numEspacamentoLinhas.DecimalPlaces = 1;
            this.numEspacamentoLinhas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numEspacamentoLinhas.Location = new System.Drawing.Point(200, 115);
            this.numEspacamentoLinhas.Name = "numEspacamentoLinhas";
            this.numEspacamentoLinhas.Size = new System.Drawing.Size(80, 23);
            this.numEspacamentoLinhas.TabIndex = 8;
            this.numEspacamentoLinhas.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblEspacamentoLinhas
            // 
            this.lblEspacamentoLinhas.AutoSize = true;
            this.lblEspacamentoLinhas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblEspacamentoLinhas.Location = new System.Drawing.Point(15, 117);
            this.lblEspacamentoLinhas.Name = "lblEspacamentoLinhas";
            this.lblEspacamentoLinhas.Size = new System.Drawing.Size(148, 15);
            this.lblEspacamentoLinhas.TabIndex = 7;
            this.lblEspacamentoLinhas.Text = "Espaçamento Entre Linhas:";
            // 
            // lblMmEspacCol
            // 
            this.lblMmEspacCol.AutoSize = true;
            this.lblMmEspacCol.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmEspacCol.Location = new System.Drawing.Point(285, 95);
            this.lblMmEspacCol.Name = "lblMmEspacCol";
            this.lblMmEspacCol.Size = new System.Drawing.Size(29, 15);
            this.lblMmEspacCol.TabIndex = 6;
            this.lblMmEspacCol.Text = "mm";
            // 
            // numEspacamentoColunas
            // 
            this.numEspacamentoColunas.DecimalPlaces = 1;
            this.numEspacamentoColunas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numEspacamentoColunas.Location = new System.Drawing.Point(200, 85);
            this.numEspacamentoColunas.Name = "numEspacamentoColunas";
            this.numEspacamentoColunas.Size = new System.Drawing.Size(80, 23);
            this.numEspacamentoColunas.TabIndex = 5;
            this.numEspacamentoColunas.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblEspacamentoColunas
            // 
            this.lblEspacamentoColunas.AutoSize = true;
            this.lblEspacamentoColunas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblEspacamentoColunas.Location = new System.Drawing.Point(15, 87);
            this.lblEspacamentoColunas.Name = "lblEspacamentoColunas";
            this.lblEspacamentoColunas.Size = new System.Drawing.Size(157, 15);
            this.lblEspacamentoColunas.TabIndex = 4;
            this.lblEspacamentoColunas.Text = "Espaçamento Entre Colunas:";
            // 
            // numLinhas
            // 
            this.numLinhas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numLinhas.Location = new System.Drawing.Point(200, 55);
            this.numLinhas.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numLinhas.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLinhas.Name = "numLinhas";
            this.numLinhas.Size = new System.Drawing.Size(80, 23);
            this.numLinhas.TabIndex = 3;
            this.numLinhas.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLinhas.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblLinhas
            // 
            this.lblLinhas.AutoSize = true;
            this.lblLinhas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLinhas.Location = new System.Drawing.Point(15, 57);
            this.lblLinhas.Name = "lblLinhas";
            this.lblLinhas.Size = new System.Drawing.Size(107, 15);
            this.lblLinhas.TabIndex = 2;
            this.lblLinhas.Text = "Número de Linhas:";
            // 
            // numColunas
            // 
            this.numColunas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numColunas.Location = new System.Drawing.Point(200, 25);
            this.numColunas.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numColunas.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numColunas.Name = "numColunas";
            this.numColunas.Size = new System.Drawing.Size(80, 23);
            this.numColunas.TabIndex = 1;
            this.numColunas.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numColunas.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblColunas
            // 
            this.lblColunas.AutoSize = true;
            this.lblColunas.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblColunas.Location = new System.Drawing.Point(15, 27);
            this.lblColunas.Name = "lblColunas";
            this.lblColunas.Size = new System.Drawing.Size(116, 15);
            this.lblColunas.TabIndex = 0;
            this.lblColunas.Text = "Número de Colunas:";
            // 
            // groupDimensoes
            // 
            this.groupDimensoes.Controls.Add(this.lblMmAltura);
            this.groupDimensoes.Controls.Add(this.numAltura);
            this.groupDimensoes.Controls.Add(this.lblAltura);
            this.groupDimensoes.Controls.Add(this.lblMmLargura);
            this.groupDimensoes.Controls.Add(this.numLargura);
            this.groupDimensoes.Controls.Add(this.lblLargura);
            this.groupDimensoes.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupDimensoes.Location = new System.Drawing.Point(10, 170);
            this.groupDimensoes.Name = "groupDimensoes";
            this.groupDimensoes.Size = new System.Drawing.Size(420, 100);
            this.groupDimensoes.TabIndex = 1;
            this.groupDimensoes.TabStop = false;
            this.groupDimensoes.Text = "2  ️⃣     Dimensões da Etiqueta";
            this.groupDimensoes.Enter += new System.EventHandler(this.groupDimensoes_Enter);
            // 
            // lblMmAltura
            // 
            this.lblMmAltura.AutoSize = true;
            this.lblMmAltura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmAltura.Location = new System.Drawing.Point(235, 65);
            this.lblMmAltura.Name = "lblMmAltura";
            this.lblMmAltura.Size = new System.Drawing.Size(29, 15);
            this.lblMmAltura.TabIndex = 5;
            this.lblMmAltura.Text = "mm";
            // 
            // numAltura
            // 
            this.numAltura.DecimalPlaces = 1;
            this.numAltura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numAltura.Location = new System.Drawing.Point(150, 63);
            this.numAltura.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numAltura.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numAltura.Name = "numAltura";
            this.numAltura.Size = new System.Drawing.Size(80, 23);
            this.numAltura.TabIndex = 4;
            this.numAltura.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numAltura.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblAltura
            // 
            this.lblAltura.AutoSize = true;
            this.lblAltura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAltura.Location = new System.Drawing.Point(15, 65);
            this.lblAltura.Name = "lblAltura";
            this.lblAltura.Size = new System.Drawing.Size(88, 15);
            this.lblAltura.TabIndex = 3;
            this.lblAltura.Text = "Altura Etiqueta:";
            // 
            // lblMmLargura
            // 
            this.lblMmLargura.AutoSize = true;
            this.lblMmLargura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMmLargura.Location = new System.Drawing.Point(235, 35);
            this.lblMmLargura.Name = "lblMmLargura";
            this.lblMmLargura.Size = new System.Drawing.Size(29, 15);
            this.lblMmLargura.TabIndex = 2;
            this.lblMmLargura.Text = "mm";
            // 
            // numLargura
            // 
            this.numLargura.DecimalPlaces = 1;
            this.numLargura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numLargura.Location = new System.Drawing.Point(150, 33);
            this.numLargura.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numLargura.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numLargura.Name = "numLargura";
            this.numLargura.Size = new System.Drawing.Size(80, 23);
            this.numLargura.TabIndex = 1;
            this.numLargura.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLargura.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblLargura
            // 
            this.lblLargura.AutoSize = true;
            this.lblLargura.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblLargura.Location = new System.Drawing.Point(15, 35);
            this.lblLargura.Name = "lblLargura";
            this.lblLargura.Size = new System.Drawing.Size(96, 15);
            this.lblLargura.TabIndex = 0;
            this.lblLargura.Text = "Largura Etiqueta:";
            // 
            // groupDadosEtiqueta
            // 
            this.groupDadosEtiqueta.Controls.Add(this.cmbPapel);
            this.groupDadosEtiqueta.Controls.Add(this.lblPapel);
            this.groupDadosEtiqueta.Controls.Add(this.cmbImpressora);
            this.groupDadosEtiqueta.Controls.Add(this.lblImpressora);
            this.groupDadosEtiqueta.Controls.Add(this.txtNomeEtiqueta);
            this.groupDadosEtiqueta.Controls.Add(this.lblNomeEtiqueta);
            this.groupDadosEtiqueta.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.groupDadosEtiqueta.Location = new System.Drawing.Point(10, 10);
            this.groupDadosEtiqueta.Name = "groupDadosEtiqueta";
            this.groupDadosEtiqueta.Size = new System.Drawing.Size(420, 150);
            this.groupDadosEtiqueta.TabIndex = 0;
            this.groupDadosEtiqueta.TabStop = false;
            this.groupDadosEtiqueta.Text = "1  ️⃣    Dados da Etiqueta";
            // 
            // cmbPapel
            // 
            this.cmbPapel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPapel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbPapel.FormattingEnabled = true;
            this.cmbPapel.Location = new System.Drawing.Point(150, 107);
            this.cmbPapel.Name = "cmbPapel";
            this.cmbPapel.Size = new System.Drawing.Size(250, 23);
            this.cmbPapel.TabIndex = 5;
            // 
            // lblPapel
            // 
            this.lblPapel.AutoSize = true;
            this.lblPapel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPapel.Location = new System.Drawing.Point(15, 110);
            this.lblPapel.Name = "lblPapel";
            this.lblPapel.Size = new System.Drawing.Size(79, 15);
            this.lblPapel.TabIndex = 4;
            this.lblPapel.Text = "Papel Padrão:";
            // 
            // cmbImpressora
            // 
            this.cmbImpressora.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbImpressora.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbImpressora.FormattingEnabled = true;
            this.cmbImpressora.Location = new System.Drawing.Point(150, 77);
            this.cmbImpressora.Name = "cmbImpressora";
            this.cmbImpressora.Size = new System.Drawing.Size(250, 23);
            this.cmbImpressora.TabIndex = 3;
            // 
            // lblImpressora
            // 
            this.lblImpressora.AutoSize = true;
            this.lblImpressora.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblImpressora.Location = new System.Drawing.Point(15, 80);
            this.lblImpressora.Name = "lblImpressora";
            this.lblImpressora.Size = new System.Drawing.Size(108, 15);
            this.lblImpressora.TabIndex = 2;
            this.lblImpressora.Text = "Impressora Padrão:";
            // 
            // txtNomeEtiqueta
            // 
            this.txtNomeEtiqueta.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtNomeEtiqueta.Location = new System.Drawing.Point(15, 48);
            this.txtNomeEtiqueta.Name = "txtNomeEtiqueta";
            this.txtNomeEtiqueta.Size = new System.Drawing.Size(385, 23);
            this.txtNomeEtiqueta.TabIndex = 1;
            // 
            // lblNomeEtiqueta
            // 
            this.lblNomeEtiqueta.AutoSize = true;
            this.lblNomeEtiqueta.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblNomeEtiqueta.Location = new System.Drawing.Point(15, 30);
            this.lblNomeEtiqueta.Name = "lblNomeEtiqueta";
            this.lblNomeEtiqueta.Size = new System.Drawing.Size(105, 15);
            this.lblNomeEtiqueta.TabIndex = 0;
            this.lblNomeEtiqueta.Text = "Nome da Etiqueta:";
            // 
            // panelBotoes
            // 
            this.panelBotoes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.panelBotoes.Controls.Add(this.btnTestarImpressao);
            this.panelBotoes.Controls.Add(this.btnCancelar);
            this.panelBotoes.Controls.Add(this.btnOK);
            this.panelBotoes.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBotoes.Location = new System.Drawing.Point(0, 600);
            this.panelBotoes.Name = "panelBotoes";
            this.panelBotoes.Size = new System.Drawing.Size(900, 60);
            this.panelBotoes.TabIndex = 1;
            // 
            // btnTestarImpressao
            // 
            this.btnTestarImpressao.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnTestarImpressao.FlatAppearance.BorderSize = 0;
            this.btnTestarImpressao.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestarImpressao.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTestarImpressao.ForeColor = System.Drawing.Color.White;
            this.btnTestarImpressao.Location = new System.Drawing.Point(20, 15);
            this.btnTestarImpressao.Name = "btnTestarImpressao";
            this.btnTestarImpressao.Size = new System.Drawing.Size(160, 35);
            this.btnTestarImpressao.TabIndex = 2;
            this.btnTestarImpressao.Text = "🖨️ Testar Impressão";
            this.btnTestarImpressao.UseVisualStyleBackColor = false;
            this.btnTestarImpressao.Click += new System.EventHandler(this.btnTestarImpressao_Click);
            // 
            // btnCancelar
            // 
            this.btnCancelar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancelar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(149)))), ((int)(((byte)(165)))), ((int)(((byte)(166)))));
            this.btnCancelar.FlatAppearance.BorderSize = 0;
            this.btnCancelar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelar.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCancelar.ForeColor = System.Drawing.Color.White;
            this.btnCancelar.Location = new System.Drawing.Point(610, 15);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(130, 35);
            this.btnCancelar.TabIndex = 1;
            this.btnCancelar.Text = "✕ Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = false;
            this.btnCancelar.Click += new System.EventHandler(this.btnCancelar_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnOK.ForeColor = System.Drawing.Color.White;
            this.btnOK.Location = new System.Drawing.Point(750, 15);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(130, 35);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "✓ OK";
            this.btnOK.UseVisualStyleBackColor = false;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // FormConfigEtiqueta
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 660);
            this.Controls.Add(this.panelPrincipal);
            this.Controls.Add(this.panelBotoes);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConfigEtiqueta";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuração de Etiqueta";
            this.panelPrincipal.ResumeLayout(false);
            this.panelDireita.ResumeLayout(false);
            this.groupPreview.ResumeLayout(false);
            this.panelEsquerda.ResumeLayout(false);
            this.groupMargens.ResumeLayout(false);
            this.groupMargens.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemDireita)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemEsquerda)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemInferior)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMargemSuperior)).EndInit();
            this.groupLayout.ResumeLayout(false);
            this.groupLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEspacamentoLinhas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEspacamentoColunas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLinhas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numColunas)).EndInit();
            this.groupDimensoes.ResumeLayout(false);
            this.groupDimensoes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAltura)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLargura)).EndInit();
            this.groupDadosEtiqueta.ResumeLayout(false);
            this.groupDadosEtiqueta.PerformLayout();
            this.panelBotoes.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelPrincipal;
        private System.Windows.Forms.Panel panelEsquerda;
        private System.Windows.Forms.Panel panelDireita;
        private System.Windows.Forms.Panel panelBotoes;

        private System.Windows.Forms.GroupBox groupDadosEtiqueta;
        private System.Windows.Forms.Label lblNomeEtiqueta;
        private System.Windows.Forms.TextBox txtNomeEtiqueta;
        private System.Windows.Forms.Label lblImpressora;
        private System.Windows.Forms.ComboBox cmbImpressora;
        private System.Windows.Forms.Label lblPapel;
        private System.Windows.Forms.ComboBox cmbPapel;

        private System.Windows.Forms.GroupBox groupDimensoes;
        private System.Windows.Forms.Label lblLargura;
        private System.Windows.Forms.NumericUpDown numLargura;
        private System.Windows.Forms.Label lblMmLargura;
        private System.Windows.Forms.Label lblAltura;
        private System.Windows.Forms.NumericUpDown numAltura;
        private System.Windows.Forms.Label lblMmAltura;

        private System.Windows.Forms.GroupBox groupLayout;
        private System.Windows.Forms.Label lblColunas;
        private System.Windows.Forms.NumericUpDown numColunas;
        private System.Windows.Forms.Label lblLinhas;
        private System.Windows.Forms.NumericUpDown numLinhas;
        private System.Windows.Forms.Label lblEspacamentoColunas;
        private System.Windows.Forms.NumericUpDown numEspacamentoColunas;
        private System.Windows.Forms.Label lblMmEspacCol;
        private System.Windows.Forms.Label lblEspacamentoLinhas;
        private System.Windows.Forms.NumericUpDown numEspacamentoLinhas;
        private System.Windows.Forms.Label lblMmEspacLin;

        private System.Windows.Forms.GroupBox groupMargens;
        private System.Windows.Forms.Label lblMargemSuperior;
        private System.Windows.Forms.NumericUpDown numMargemSuperior;
        private System.Windows.Forms.Label lblMmMargemSup;
        private System.Windows.Forms.Label lblMargemInferior;
        private System.Windows.Forms.NumericUpDown numMargemInferior;
        private System.Windows.Forms.Label lblMmMargemInf;
        private System.Windows.Forms.Label lblMargemEsquerda;
        private System.Windows.Forms.NumericUpDown numMargemEsquerda;
        private System.Windows.Forms.Label lblMmMargemEsq;
        private System.Windows.Forms.Label lblMargemDireita;
        private System.Windows.Forms.NumericUpDown numMargemDireita;
        private System.Windows.Forms.Label lblMmMargemDir;

        private System.Windows.Forms.GroupBox groupPreview;
        private System.Windows.Forms.Panel panelPreview;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Button btnTestarImpressao;
    }
}