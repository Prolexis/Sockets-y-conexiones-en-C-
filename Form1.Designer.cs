using System.Windows.Forms;

namespace SERVIDORES_SOCKETS
{
    partial class Form1
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
            tlpMain = new TableLayoutPanel();
            pnlHeader = new Panel();
            lblTitle = new Label();
            btnThemeToggle = new Button();
            tlpWorkspace = new TableLayoutPanel();
            gbCliente = new GroupBox();
            btnEnviarArchivo = new Button();
            lblMensaje = new Label();
            lblDestino = new Label();
            btnEnviar = new Button();
            txtMensaje = new TextBox();
            rtbChat = new RichTextBox();
            tlpClientFields = new TableLayoutPanel();
            lblClientIp = new Label();
            txtClientIp = new TextBox();
            lblClientPort = new Label();
            txtClientPort = new TextBox();
            lblClientUser = new Label();
            txtClientUser = new TextBox();
            btnConnect = new Button();
            btnDisconnect = new Button();
            btnPing = new Button();
            tlpRight = new TableLayoutPanel();
            gbServidor = new GroupBox();
            tlpServerFields = new TableLayoutPanel();
            lblServerIp = new Label();
            txtServerIp = new TextBox();
            lblServerPort = new Label();
            txtServerPort = new TextBox();
            btnStartServer = new Button();
            btnStopServer = new Button();
            lblServerStatusLabel = new Label();
            lblServerStatus = new Label();
            gbClientes = new GroupBox();
            lstClientes = new ListView();
            gbLog = new GroupBox();
            rtxtLog = new RichTextBox();
            cmbDestino = new ComboBox();
            tlpMain.SuspendLayout();
            pnlHeader.SuspendLayout();
            tlpWorkspace.SuspendLayout();
            gbCliente.SuspendLayout();
            tlpClientFields.SuspendLayout();
            tlpRight.SuspendLayout();
            gbServidor.SuspendLayout();
            tlpServerFields.SuspendLayout();
            gbClientes.SuspendLayout();
            gbLog.SuspendLayout();
            SuspendLayout();
            // 
            // tlpMain
            // 
            tlpMain.ColumnCount = 1;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMain.Controls.Add(pnlHeader, 0, 0);
            tlpMain.Controls.Add(tlpWorkspace, 0, 1);
            tlpMain.Controls.Add(gbLog, 0, 2);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Location = new Point(0, 0);
            tlpMain.Margin = new Padding(0);
            tlpMain.Name = "tlpMain";
            tlpMain.RowCount = 3;
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpMain.Size = new Size(984, 661);
            tlpMain.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnThemeToggle);
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(984, 60);
            pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            lblTitle.Location = new Point(15, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(467, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "SISTEMA DE COMUNICACIÓN TCP (SOCKETS)";
            // 
            // btnThemeToggle
            // 
            btnThemeToggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnThemeToggle.Cursor = Cursors.Hand;
            btnThemeToggle.FlatStyle = FlatStyle.Flat;
            btnThemeToggle.Font = new Font("Segoe UI", 9F);
            btnThemeToggle.Location = new Point(854, 13);
            btnThemeToggle.Name = "btnThemeToggle";
            btnThemeToggle.Size = new Size(115, 34);
            btnThemeToggle.TabIndex = 1;
            btnThemeToggle.Text = "🌙 Modo Oscuro";
            btnThemeToggle.UseVisualStyleBackColor = true;
            btnThemeToggle.Click += btnThemeToggle_Click;
            // 
            // tlpWorkspace
            // 
            tlpWorkspace.ColumnCount = 2;
            tlpWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpWorkspace.Controls.Add(gbCliente, 0, 0);
            tlpWorkspace.Controls.Add(tlpRight, 1, 0);
            tlpWorkspace.Dock = DockStyle.Fill;
            tlpWorkspace.Location = new Point(0, 60);
            tlpWorkspace.Margin = new Padding(0);
            tlpWorkspace.Name = "tlpWorkspace";
            tlpWorkspace.RowCount = 1;
            tlpWorkspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpWorkspace.Size = new Size(984, 330);
            tlpWorkspace.TabIndex = 1;
            // 
            // gbCliente
            // 
            gbCliente.Controls.Add(cmbDestino);
            gbCliente.Controls.Add(btnEnviarArchivo);
            gbCliente.Controls.Add(lblMensaje);
            gbCliente.Controls.Add(lblDestino);
            gbCliente.Controls.Add(btnEnviar);
            gbCliente.Controls.Add(txtMensaje);
            gbCliente.Controls.Add(rtbChat);
            gbCliente.Controls.Add(tlpClientFields);
            gbCliente.Dock = DockStyle.Fill;
            gbCliente.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            gbCliente.Location = new Point(10, 5);
            gbCliente.Margin = new Padding(10, 5, 5, 5);
            gbCliente.Name = "gbCliente";
            gbCliente.Size = new Size(477, 320);
            gbCliente.TabIndex = 1;
            gbCliente.TabStop = false;
            gbCliente.Text = "CONFIGURACIÓN CLIENTE (TcpClient)";
            // 
            // btnEnviarArchivo
            // 
            btnEnviarArchivo.Location = new Point(396, 212);
            btnEnviarArchivo.Name = "btnEnviarArchivo";
            btnEnviarArchivo.Size = new Size(75, 51);
            btnEnviarArchivo.TabIndex = 7;
            btnEnviarArchivo.Text = "Enviar archivos";
            btnEnviarArchivo.UseVisualStyleBackColor = true;
            btnEnviarArchivo.Click += btnEnviarArchivo_Click;
            // 
            // lblMensaje
            // 
            lblMensaje.AutoSize = true;
            lblMensaje.Location = new Point(257, 267);
            lblMensaje.Name = "lblMensaje";
            lblMensaje.Size = new Size(64, 19);
            lblMensaje.TabIndex = 6;
            lblMensaje.Text = "Mensaje:";
            // 
            // lblDestino
            // 
            lblDestino.AutoSize = true;
            lblDestino.Location = new Point(257, 201);
            lblDestino.Name = "lblDestino";
            lblDestino.Size = new Size(60, 19);
            lblDestino.TabIndex = 5;
            lblDestino.Text = "Destino:";
            // 
            // btnEnviar
            // 
            btnEnviar.Location = new Point(396, 289);
            btnEnviar.Name = "btnEnviar";
            btnEnviar.Size = new Size(75, 23);
            btnEnviar.TabIndex = 4;
            btnEnviar.Text = "Enviar";
            btnEnviar.UseVisualStyleBackColor = true;
            btnEnviar.Click += btnEnviar_Click;
            // 
            // txtMensaje
            // 
            txtMensaje.Location = new Point(257, 289);
            txtMensaje.Name = "txtMensaje";
            txtMensaje.Size = new Size(121, 25);
            txtMensaje.TabIndex = 3;
            // 
            // rtbChat
            // 
            rtbChat.Location = new Point(6, 212);
            rtbChat.Name = "rtbChat";
            rtbChat.Size = new Size(229, 102);
            rtbChat.TabIndex = 1;
            rtbChat.Text = "";
            // 
            // tlpClientFields
            // 
            tlpClientFields.ColumnCount = 3;
            tlpClientFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpClientFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpClientFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpClientFields.Controls.Add(lblClientIp, 0, 0);
            tlpClientFields.Controls.Add(txtClientIp, 0, 1);
            tlpClientFields.Controls.Add(lblClientPort, 1, 0);
            tlpClientFields.Controls.Add(txtClientPort, 1, 1);
            tlpClientFields.Controls.Add(lblClientUser, 0, 2);
            tlpClientFields.Controls.Add(txtClientUser, 0, 3);
            tlpClientFields.Controls.Add(btnConnect, 1, 3);
            tlpClientFields.Controls.Add(btnDisconnect, 2, 3);
            tlpClientFields.Controls.Add(btnPing, 1, 4);
            tlpClientFields.Dock = DockStyle.Top;
            tlpClientFields.Location = new Point(3, 21);
            tlpClientFields.Name = "tlpClientFields";
            tlpClientFields.RowCount = 5;
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tlpClientFields.Size = new Size(471, 177);
            tlpClientFields.TabIndex = 0;
            // 
            // lblClientIp
            // 
            lblClientIp.AutoSize = true;
            lblClientIp.Dock = DockStyle.Bottom;
            lblClientIp.Font = new Font("Segoe UI", 9F);
            lblClientIp.Location = new Point(3, 10);
            lblClientIp.Name = "lblClientIp";
            lblClientIp.Size = new Size(229, 15);
            lblClientIp.TabIndex = 0;
            lblClientIp.Text = "IP Servidor:";
            // 
            // txtClientIp
            // 
            txtClientIp.BorderStyle = BorderStyle.FixedSingle;
            txtClientIp.Dock = DockStyle.Fill;
            txtClientIp.Font = new Font("Segoe UI", 9.75F);
            txtClientIp.Location = new Point(3, 28);
            txtClientIp.Name = "txtClientIp";
            txtClientIp.Size = new Size(229, 25);
            txtClientIp.TabIndex = 1;
            txtClientIp.Text = "127.0.0.1";
            // 
            // lblClientPort
            // 
            lblClientPort.AutoSize = true;
            tlpClientFields.SetColumnSpan(lblClientPort, 2);
            lblClientPort.Dock = DockStyle.Bottom;
            lblClientPort.Font = new Font("Segoe UI", 9F);
            lblClientPort.Location = new Point(238, 10);
            lblClientPort.Name = "lblClientPort";
            lblClientPort.Size = new Size(230, 15);
            lblClientPort.TabIndex = 2;
            lblClientPort.Text = "Puerto:";
            // 
            // txtClientPort
            // 
            txtClientPort.BorderStyle = BorderStyle.FixedSingle;
            tlpClientFields.SetColumnSpan(txtClientPort, 2);
            txtClientPort.Dock = DockStyle.Fill;
            txtClientPort.Font = new Font("Segoe UI", 9.75F);
            txtClientPort.Location = new Point(238, 28);
            txtClientPort.Name = "txtClientPort";
            txtClientPort.Size = new Size(230, 25);
            txtClientPort.TabIndex = 3;
            txtClientPort.Text = "8080";
            // 
            // lblClientUser
            // 
            lblClientUser.AutoSize = true;
            lblClientUser.Dock = DockStyle.Bottom;
            lblClientUser.Font = new Font("Segoe UI", 9F);
            lblClientUser.Location = new Point(3, 70);
            lblClientUser.Name = "lblClientUser";
            lblClientUser.Size = new Size(229, 15);
            lblClientUser.TabIndex = 4;
            lblClientUser.Text = "Nombre Usuario:";
            // 
            // txtClientUser
            // 
            txtClientUser.BorderStyle = BorderStyle.FixedSingle;
            txtClientUser.Dock = DockStyle.Fill;
            txtClientUser.Font = new Font("Segoe UI", 9.75F);
            txtClientUser.Location = new Point(3, 88);
            txtClientUser.Name = "txtClientUser";
            txtClientUser.Size = new Size(229, 25);
            txtClientUser.TabIndex = 5;
            txtClientUser.Text = "Cliente1";
            // 
            // btnConnect
            // 
            btnConnect.Cursor = Cursors.Hand;
            btnConnect.Dock = DockStyle.Fill;
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.Font = new Font("Segoe UI", 9F);
            btnConnect.Location = new Point(238, 88);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(111, 34);
            btnConnect.TabIndex = 6;
            btnConnect.Text = "Conectar";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Cursor = Cursors.Hand;
            btnDisconnect.Dock = DockStyle.Fill;
            btnDisconnect.Enabled = false;
            btnDisconnect.FlatStyle = FlatStyle.Flat;
            btnDisconnect.Font = new Font("Segoe UI", 9F);
            btnDisconnect.Location = new Point(355, 88);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(113, 34);
            btnDisconnect.TabIndex = 7;
            btnDisconnect.Text = "Desconectar";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // btnPing
            // 
            tlpClientFields.SetColumnSpan(btnPing, 2);
            btnPing.Cursor = Cursors.Hand;
            btnPing.Dock = DockStyle.Fill;
            btnPing.Enabled = false;
            btnPing.FlatStyle = FlatStyle.Flat;
            btnPing.Font = new Font("Segoe UI", 9F);
            btnPing.Location = new Point(238, 128);
            btnPing.Name = "btnPing";
            btnPing.Size = new Size(230, 46);
            btnPing.TabIndex = 8;
            btnPing.Text = "Enviar PING (Probar Latido)";
            btnPing.UseVisualStyleBackColor = true;
            btnPing.Click += btnPing_Click;
            // 
            // tlpRight
            // 
            tlpRight.ColumnCount = 1;
            tlpRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpRight.Controls.Add(gbServidor, 0, 0);
            tlpRight.Controls.Add(gbClientes, 0, 1);
            tlpRight.Dock = DockStyle.Fill;
            tlpRight.Location = new Point(492, 0);
            tlpRight.Margin = new Padding(0);
            tlpRight.Name = "tlpRight";
            tlpRight.RowCount = 2;
            tlpRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            tlpRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpRight.Size = new Size(492, 330);
            tlpRight.TabIndex = 1;
            // 
            // gbServidor
            // 
            gbServidor.Controls.Add(tlpServerFields);
            gbServidor.Dock = DockStyle.Fill;
            gbServidor.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            gbServidor.Location = new Point(5, 5);
            gbServidor.Margin = new Padding(5, 5, 10, 5);
            gbServidor.Name = "gbServidor";
            gbServidor.Size = new Size(477, 100);
            gbServidor.TabIndex = 0;
            gbServidor.TabStop = false;
            gbServidor.Text = "CONFIGURACIÓN SERVIDOR (TcpListener)";
            // 
            // tlpServerFields
            // 
            tlpServerFields.ColumnCount = 4;
            tlpServerFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpServerFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tlpServerFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22.5F));
            tlpServerFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22.5F));
            tlpServerFields.Controls.Add(lblServerIp, 0, 0);
            tlpServerFields.Controls.Add(txtServerIp, 0, 1);
            tlpServerFields.Controls.Add(lblServerPort, 1, 0);
            tlpServerFields.Controls.Add(txtServerPort, 1, 1);
            tlpServerFields.Controls.Add(btnStartServer, 2, 1);
            tlpServerFields.Controls.Add(btnStopServer, 3, 1);
            tlpServerFields.Controls.Add(lblServerStatusLabel, 2, 0);
            tlpServerFields.Controls.Add(lblServerStatus, 3, 0);
            tlpServerFields.Dock = DockStyle.Fill;
            tlpServerFields.Location = new Point(3, 21);
            tlpServerFields.Name = "tlpServerFields";
            tlpServerFields.RowCount = 2;
            tlpServerFields.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            tlpServerFields.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            tlpServerFields.Size = new Size(471, 76);
            tlpServerFields.TabIndex = 0;
            // 
            // lblServerIp
            // 
            lblServerIp.AutoSize = true;
            lblServerIp.Dock = DockStyle.Bottom;
            lblServerIp.Font = new Font("Segoe UI", 9F);
            lblServerIp.Location = new Point(3, 19);
            lblServerIp.Name = "lblServerIp";
            lblServerIp.Size = new Size(158, 15);
            lblServerIp.TabIndex = 0;
            lblServerIp.Text = "IP Escucha:";
            // 
            // txtServerIp
            // 
            txtServerIp.BorderStyle = BorderStyle.FixedSingle;
            txtServerIp.Dock = DockStyle.Fill;
            txtServerIp.Font = new Font("Segoe UI", 9.75F);
            txtServerIp.Location = new Point(3, 37);
            txtServerIp.Name = "txtServerIp";
            txtServerIp.Size = new Size(158, 25);
            txtServerIp.TabIndex = 1;
            txtServerIp.Text = "127.0.0.1";
            // 
            // lblServerPort
            // 
            lblServerPort.AutoSize = true;
            lblServerPort.Dock = DockStyle.Bottom;
            lblServerPort.Font = new Font("Segoe UI", 9F);
            lblServerPort.Location = new Point(167, 19);
            lblServerPort.Name = "lblServerPort";
            lblServerPort.Size = new Size(88, 15);
            lblServerPort.TabIndex = 2;
            lblServerPort.Text = "Puerto:";
            // 
            // txtServerPort
            // 
            txtServerPort.BorderStyle = BorderStyle.FixedSingle;
            txtServerPort.Dock = DockStyle.Fill;
            txtServerPort.Font = new Font("Segoe UI", 9.75F);
            txtServerPort.Location = new Point(167, 37);
            txtServerPort.Name = "txtServerPort";
            txtServerPort.Size = new Size(88, 25);
            txtServerPort.TabIndex = 3;
            txtServerPort.Text = "8080";
            // 
            // btnStartServer
            // 
            btnStartServer.Cursor = Cursors.Hand;
            btnStartServer.Dock = DockStyle.Fill;
            btnStartServer.FlatStyle = FlatStyle.Flat;
            btnStartServer.Font = new Font("Segoe UI", 9F);
            btnStartServer.Location = new Point(261, 37);
            btnStartServer.Name = "btnStartServer";
            btnStartServer.Size = new Size(99, 36);
            btnStartServer.TabIndex = 4;
            btnStartServer.Text = "Iniciar";
            btnStartServer.UseVisualStyleBackColor = true;
            btnStartServer.Click += btnStartServer_Click;
            // 
            // btnStopServer
            // 
            btnStopServer.Cursor = Cursors.Hand;
            btnStopServer.Dock = DockStyle.Fill;
            btnStopServer.Enabled = false;
            btnStopServer.FlatStyle = FlatStyle.Flat;
            btnStopServer.Font = new Font("Segoe UI", 9F);
            btnStopServer.Location = new Point(366, 37);
            btnStopServer.Name = "btnStopServer";
            btnStopServer.Size = new Size(102, 36);
            btnStopServer.TabIndex = 5;
            btnStopServer.Text = "Detener";
            btnStopServer.UseVisualStyleBackColor = true;
            btnStopServer.Click += btnStopServer_Click;
            // 
            // lblServerStatusLabel
            // 
            lblServerStatusLabel.AutoSize = true;
            lblServerStatusLabel.Dock = DockStyle.Bottom;
            lblServerStatusLabel.Font = new Font("Segoe UI", 9F);
            lblServerStatusLabel.Location = new Point(261, 19);
            lblServerStatusLabel.Name = "lblServerStatusLabel";
            lblServerStatusLabel.Size = new Size(99, 15);
            lblServerStatusLabel.TabIndex = 6;
            lblServerStatusLabel.Text = "Estado:";
            lblServerStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblServerStatus
            // 
            lblServerStatus.AutoSize = true;
            lblServerStatus.Dock = DockStyle.Bottom;
            lblServerStatus.Font = new Font("Segoe UI Black", 9.75F, FontStyle.Bold);
            lblServerStatus.ForeColor = Color.Red;
            lblServerStatus.Location = new Point(366, 17);
            lblServerStatus.Name = "lblServerStatus";
            lblServerStatus.Size = new Size(102, 17);
            lblServerStatus.TabIndex = 7;
            lblServerStatus.Text = "● DETENIDO";
            lblServerStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // gbClientes
            // 
            gbClientes.Controls.Add(lstClientes);
            gbClientes.Dock = DockStyle.Fill;
            gbClientes.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            gbClientes.Location = new Point(5, 115);
            gbClientes.Margin = new Padding(5, 5, 10, 5);
            gbClientes.Name = "gbClientes";
            gbClientes.Size = new Size(477, 210);
            gbClientes.TabIndex = 1;
            gbClientes.TabStop = false;
            gbClientes.Text = "CLIENTES CONECTADOS (Lista Thread-Safe)";
            // 
            // lstClientes
            // 
            lstClientes.BorderStyle = BorderStyle.FixedSingle;
            lstClientes.Dock = DockStyle.Fill;
            lstClientes.Font = new Font("Segoe UI", 9F);
            lstClientes.FullRowSelect = true;
            lstClientes.GridLines = true;
            lstClientes.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstClientes.Location = new Point(3, 21);
            lstClientes.MultiSelect = false;
            lstClientes.Name = "lstClientes";
            lstClientes.Size = new Size(471, 186);
            lstClientes.TabIndex = 0;
            lstClientes.UseCompatibleStateImageBehavior = false;
            lstClientes.View = View.Details;
            // 
            // gbLog
            // 
            gbLog.Controls.Add(rtxtLog);
            gbLog.Dock = DockStyle.Fill;
            gbLog.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            gbLog.Location = new Point(10, 395);
            gbLog.Margin = new Padding(10, 5, 10, 10);
            gbLog.Name = "gbLog";
            gbLog.Size = new Size(964, 256);
            gbLog.TabIndex = 2;
            gbLog.TabStop = false;
            gbLog.Text = "LOG DE EVENTOS Y ERRORES DEL SISTEMA (Consola en tiempo real)";
            // 
            // rtxtLog
            // 
            rtxtLog.BackColor = Color.FromArgb(24, 24, 27);
            rtxtLog.BorderStyle = BorderStyle.None;
            rtxtLog.Dock = DockStyle.Fill;
            rtxtLog.Font = new Font("Consolas", 9.75F);
            rtxtLog.ForeColor = Color.FromArgb(228, 228, 231);
            rtxtLog.Location = new Point(3, 21);
            rtxtLog.Name = "rtxtLog";
            rtxtLog.ReadOnly = true;
            rtxtLog.Size = new Size(958, 232);
            rtxtLog.TabIndex = 0;
            rtxtLog.Text = "";
            // 
            // cmbDestino
            // 
            cmbDestino.FormattingEnabled = true;
            cmbDestino.Location = new Point(257, 223);
            cmbDestino.Name = "cmbDestino";
            cmbDestino.Size = new Size(121, 25);
            cmbDestino.TabIndex = 8;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 661);
            Controls.Add(tlpMain);
            Font = new Font("Segoe UI", 9.75F);
            MinimumSize = new Size(950, 650);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Control de Conexión Sockets - TCP";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            tlpMain.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            tlpWorkspace.ResumeLayout(false);
            gbCliente.ResumeLayout(false);
            gbCliente.PerformLayout();
            tlpClientFields.ResumeLayout(false);
            tlpClientFields.PerformLayout();
            tlpRight.ResumeLayout(false);
            gbServidor.ResumeLayout(false);
            tlpServerFields.ResumeLayout(false);
            tlpServerFields.PerformLayout();
            gbClientes.ResumeLayout(false);
            gbLog.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private TableLayoutPanel tlpMain;
        private Panel pnlHeader;
        private Label lblTitle;
        private Button btnThemeToggle;
        private TableLayoutPanel tlpWorkspace;
        private TableLayoutPanel tlpRight;
        private GroupBox gbServidor;
        private TableLayoutPanel tlpServerFields;
        private Label lblServerIp;
        private TextBox txtServerIp;
        private Label lblServerPort;
        private TextBox txtServerPort;
        private Button btnStartServer;
        private Button btnStopServer;
        private Label lblServerStatusLabel;
        private Label lblServerStatus;
        private GroupBox gbClientes;
        private ListView lstClientes;
        private GroupBox gbCliente;
        private TableLayoutPanel tlpClientFields;
        private Label lblClientIp;
        private TextBox txtClientIp;
        private Label lblClientPort;
        private TextBox txtClientPort;
        private Label lblClientUser;
        private TextBox txtClientUser;
        private Button btnConnect;
        private Button btnDisconnect;
        private Button btnPing;
        private GroupBox gbLog;
        private RichTextBox rtxtLog;
        private TextBox txtMensaje;
        private RichTextBox rtbChat;
        private Label lblMensaje;
        private Label lblDestino;
        private Button btnEnviar;
        private Button btnEnviarArchivo;
        private ComboBox cmbDestino;
    }
}
