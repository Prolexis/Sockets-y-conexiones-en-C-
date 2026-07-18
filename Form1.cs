using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SERVIDORES_SOCKETS
{
    public partial class Form1 : Form
    {
        private readonly ServidorTcp _server = new();
        private readonly ClienteTcp _client = new();
        private bool _isDarkMode = true; // Iniciamos con el tema oscuro por defecto

        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            // Suscribir eventos del servidor
            _server.OnLog += (msg, level) => Log("SERVIDOR", msg, level);
            _server.OnClientConnected += (c) => SafeUpdateClientList();
            _server.OnClientDisconnected += (c) => SafeUpdateClientList();
            _server.OnStateChanged += (isRunning) => SafeUpdateServerUI(isRunning);

            // Suscribir eventos del cliente
            _client.OnLog += (msg, level) => Log("CLIENTE", msg, level);
            _client.OnConnectionStatusChanged += (isConnected) => SafeUpdateClientUI(isConnected);

            _client.OnChatMessageReceived += (remitente, contenido) => SafeAppendChat(remitente, contenido);

            _client.OnFileIncomingStarted += (remitente, fileId, nombre, tamaño) =>
                SafeAppendChat(remitente, $"📎 enviando archivo \"{nombre}\" ({tamaño / 1024} KB)...");
            _client.OnFileTransferCompleted += (fileId, remitente, ruta) =>
                SafeAppendChat(remitente, $"📎 archivo recibido y guardado en: {ruta}");
            _client.OnFileTransferError += (fileId, msg) =>
                Log("CLIENTE", $"Error de transferencia de archivo: {msg}", LogLevel.Error);

            _client.OnUserListUpdated += (usuarios) => SafeUpdateComboDestinatarios(usuarios);

            _client.SolicitarRutaGuardado = (nombreArchivo, tamaño) =>
            {
                string? resultado = null;

                void MostrarDialogo()
                {
                    using SaveFileDialog dlg = new()
                    {
                        FileName = nombreArchivo,
                        Title = $"Guardar archivo recibido de {tamaño / 1024} KB",
                        OverwritePrompt = true
                    };
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        resultado = dlg.FileName;
                    }
                }

                if (this.IsDisposed) return null;
                if (this.InvokeRequired)
                {
                    // Invoke SÍNCRONO (no BeginInvoke): el hilo de red debe esperar la decisión del usuario.
                    this.Invoke((Action)MostrarDialogo);
                }
                else
                {
                    MostrarDialogo();
                }
                return resultado;
            };

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConfigurarColumnasListView();

            // Hacer el chat de solo lectura antes de aplicar el tema para que no restablezca el BackColor a gris/blanco
            rtbChat.ReadOnly = true;

            // Rediseñar el layout de controles del chat para formato profesional (estilo Slack/Discord)
            rtbChat.Location = new Point(10, 185);
            rtbChat.Size = new Size(457, 92);

            lblDestino.Visible = false;
            lblMensaje.Visible = false;

            cmbDestino.Location = new Point(10, 285);
            cmbDestino.Size = new Size(100, 25);

            txtMensaje.Location = new Point(115, 285);
            txtMensaje.Size = new Size(195, 25);

            btnEnviar.Location = new Point(315, 285);
            btnEnviar.Size = new Size(70, 25);

            btnEnviarArchivo.Location = new Point(390, 285);
            btnEnviarArchivo.Size = new Size(77, 25);

            AplicarTema(this); // Aplicar tema por defecto
            Log("SISTEMA", "Interfaz de usuario inicializada. Listo para operar.", LogLevel.Info);

            cmbDestino.Items.Add("(Todos)");
            cmbDestino.SelectedIndex = 0;

            // Configurar placeholders/marcas de agua nativas
            ConfigurarPlaceholders();

            // Mostrar IPs locales informativas en el servidor y ponerlo en solo lectura
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ips = host.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString());
                txtServerIp.Text = string.Join(", ", ips);
            }
            catch (Exception)
            {
                txtServerIp.Text = "No detectadas";
            }
            txtServerIp.ReadOnly = true;
        }

        private void ConfigurarColumnasListView()
        {
            lstClientes.Columns.Clear();
            lstClientes.Columns.Add("Usuario", 120);
            lstClientes.Columns.Add("IP Cliente", 130);
            lstClientes.Columns.Add("Puerto", 80);
            lstClientes.Columns.Add("Hora Conexión", 120);
            lstClientes.FullRowSelect = true;
            lstClientes.GridLines = true;
        }

        #region Servidor - Control de Interfaz
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            // Validar Puerto
            if (!int.TryParse(txtServerPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Por favor, ingrese un puerto válido (1 - 65535).", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _server.Start(port);
            }
            catch (Exception)
            {
                // La excepción ya fue capturada y logueada internamente en el servidor
            }
        }

        private void btnStopServer_Click(object sender, EventArgs e)
        {
            _server.Stop();
        }

        private void SafeUpdateServerUI(bool isRunning)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SafeUpdateServerUI(isRunning)));
                return;
            }

            btnStartServer.Enabled = !isRunning;
            btnStopServer.Enabled = isRunning;
            txtServerIp.ReadOnly = true;
            txtServerPort.ReadOnly = isRunning;

            if (isRunning)
            {
                lblServerStatus.Text = "● ACTIVO";
                lblServerStatus.ForeColor = Color.FromArgb(16, 185, 129); // Emerald Green
            }
            else
            {
                lblServerStatus.Text = "● DETENIDO";
                lblServerStatus.ForeColor = Color.FromArgb(239, 68, 68); // Red Coral
                // Limpiar lista de clientes al apagar servidor
                lstClientes.Items.Clear();
            }
        }

        private void SafeUpdateClientList()
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(SafeUpdateClientList));
                return;
            }

            lstClientes.BeginUpdate();
            lstClientes.Items.Clear();

            var clientes = _server.GetClientes();
            foreach (var c in clientes)
            {
                var item = new ListViewItem(c.Usuario);
                item.SubItems.Add(c.IP);
                item.SubItems.Add(c.Puerto.ToString());
                item.SubItems.Add(c.HoraConexion.ToString("HH:mm:ss"));

                // Aplicar estilo de colores según tema para las filas de ListView
                item.BackColor = _isDarkMode ? Color.FromArgb(15, 23, 42) : Color.White;
                item.ForeColor = _isDarkMode ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42);

                lstClientes.Items.Add(item);
            }

            lstClientes.EndUpdate();
        }
        #endregion

        #region Cliente - Control de Interfaz
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            // Validar IP / Hostname
            string ip = txtClientIp.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Por favor, ingrese una dirección IP o nombre de host de servidor válido.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar Puerto
            if (!int.TryParse(txtClientPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Por favor, ingrese un puerto válido (1 - 65535).", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar Usuario
            string user = txtClientUser.Text.Trim();
            if (string.IsNullOrEmpty(user))
            {
                MessageBox.Show("Por favor, ingrese un nombre de usuario.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Bloquear botón de forma inmediata antes de la operación asíncrona para evitar clics dobles
            btnConnect.Enabled = false;

            try
            {
                await _client.ConnectAsync(ip, port, user);
            }
            catch (Exception ex)
            {
                Log("CLIENTE", $"Excepción no controlada al conectar: {ex.Message}", LogLevel.Error);
                btnConnect.Enabled = true;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _client.Disconnect();
        }

        private async void btnPing_Click(object sender, EventArgs e)
        {
            await _client.EnviarPingAsync();
        }

        private void SafeUpdateClientUI(bool isConnected)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SafeUpdateClientUI(isConnected)));
                return;
            }

            btnConnect.Enabled = !isConnected;
            btnDisconnect.Enabled = isConnected;
            btnPing.Enabled = isConnected;

            btnEnviar.Enabled = isConnected;

            btnEnviarArchivo.Enabled = isConnected;

            txtClientIp.ReadOnly = isConnected;
            txtClientPort.ReadOnly = isConnected;
            txtClientUser.ReadOnly = isConnected;
        }
        #endregion

        #region Consola Log y UI Tematizable
        /// <summary>
        /// Registra un evento en la consola (RichTextBox) de forma thread-safe y con colores según tipo.
        /// </summary>
        private void Log(string context, string message, LogLevel level)
        {
            if (this.IsDisposed || !rtxtLog.IsHandleCreated || rtxtLog.IsDisposed) return;

            if (rtxtLog.InvokeRequired)
            {
                rtxtLog.BeginInvoke(new Action(() => Log(context, message, level)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string textToAppend = $"[{timestamp}] [{context}] {message}\r\n";

            Color color;
            switch (level)
            {
                case LogLevel.Success:
                    color = Color.FromArgb(16, 185, 129); // Emerald Green
                    break;
                case LogLevel.Error:
                    color = Color.FromArgb(239, 68, 68); // Red Coral
                    break;
                case LogLevel.Info:
                default:
                    // Color de texto claro/consola
                    color = Color.FromArgb(228, 228, 231);
                    break;
            }

            rtxtLog.SelectionStart = rtxtLog.TextLength;
            rtxtLog.SelectionLength = 0;
            rtxtLog.SelectionColor = color;
            rtxtLog.AppendText(textToAppend);
            rtxtLog.SelectionColor = rtxtLog.ForeColor;

            // Limitar a un máximo de 1000 líneas para evitar consumo excesivo de memoria
            // Conservamos las últimas 800 líneas borrando las primeras 200 de forma limpia sin perder colores
            if (rtxtLog.Lines.Length > 1000)
            {
                int indexDeCorte = rtxtLog.GetFirstCharIndexFromLine(200);
                if (indexDeCorte > 0)
                {
                    rtxtLog.Select(0, indexDeCorte);
                    rtxtLog.SelectedText = "";
                }
            }

            rtxtLog.ScrollToCaret();
        }

        private void btnThemeToggle_Click(object sender, EventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            AplicarTema(this);

            // Actualizar la lista de clientes para re-pintar filas con el nuevo tema
            SafeUpdateClientList();
        }

        /// <summary>
        /// Aplica los colores del tema actual (claro/oscuro) de forma recursiva a todos los controles.
        /// </summary>
        private void AplicarTema(Control parent)
        {
            // Paleta de colores inspirada en interfaces web modernas (SaaS)
            Color backColor = _isDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(241, 245, 249); // Slate 900 vs Slate 100
            Color panelColor = _isDarkMode ? Color.FromArgb(30, 41, 59) : Color.White; // Slate 800 vs White
            Color textColor = _isDarkMode ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42); // Slate 50 vs Slate 900
            Color controlBack = _isDarkMode ? Color.FromArgb(15, 23, 42) : Color.White; // Fondo de inputs
            Color mutedTextColor = _isDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105); // Slate 400 vs Slate 600

            this.BackColor = backColor;
            this.ForeColor = textColor;

            btnThemeToggle.Text = _isDarkMode ? "☀️ Modo Claro" : "🌙 Modo Oscuro";

            AplicarTemaRecursivo(parent, backColor, panelColor, textColor, controlBack, mutedTextColor);
        }

        private void AplicarTemaRecursivo(Control control, Color backColor, Color panelColor, Color textColor, Color controlBack, Color mutedTextColor)
        {
            foreach (Control child in control.Controls)
            {
                // Unificar tipografía del sistema
                if (child.Font.Name != "Segoe UI" && child.Font.Name != "Segoe UI Semibold" && !(child is RichTextBox && child.Name == "rtxtLog"))
                {
                    child.Font = new Font("Segoe UI", child.Font.Size, child.Font.Style);
                }

                if (child is Panel && child.Name == "pnlHeader")
                {
                    child.BackColor = panelColor;
                    child.ForeColor = textColor;
                }
                else if (child is GroupBox gb)
                {
                    gb.BackColor = panelColor;
                    gb.ForeColor = textColor;
                    gb.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                }
                else if (child is TableLayoutPanel tlp)
                {
                    tlp.BackColor = Color.Transparent;
                    tlp.ForeColor = textColor;
                }
                else if (child is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                    btn.Cursor = Cursors.Hand;

                    if (btn == btnThemeToggle)
                    {
                        btn.BackColor = _isDarkMode ? Color.FromArgb(71, 85, 105) : Color.FromArgb(226, 232, 240);
                        btn.ForeColor = textColor;
                        btn.FlatAppearance.MouseOverBackColor = _isDarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(203, 213, 225);
                    }
                    else if (btn.Name == "btnDisconnect" || btn.Name == "btnStopServer")
                    {
                        // Botón destructivo / peligroso: Rojo elegante
                        btn.BackColor = Color.FromArgb(239, 68, 68); // Red 500
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38); // Red 600
                    }
                    else if (btn.Name == "btnPing")
                    {
                        // Botón secundario / neutral
                        btn.BackColor = _isDarkMode ? Color.FromArgb(71, 85, 105) : Color.FromArgb(226, 232, 240);
                        btn.ForeColor = _isDarkMode ? Color.White : Color.FromArgb(15, 23, 42);
                        btn.FlatAppearance.MouseOverBackColor = _isDarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(203, 213, 225);
                    }
                    else
                    {
                        // Botón primario: Índigo moderno
                        btn.BackColor = Color.FromArgb(79, 70, 229); // Indigo 600
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(67, 56, 202); // Indigo 700
                    }
                }
                else if (child is TextBox tb)
                {
                    tb.BackColor = controlBack;
                    tb.ForeColor = textColor;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular);
                }
                else if (child is ListView lv)
                {
                    lv.BackColor = controlBack;
                    lv.ForeColor = textColor;
                    lv.BorderStyle = BorderStyle.FixedSingle;
                    lv.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                }
                else if (child is Label lbl)
                {
                    if (lbl.Name == "lblServerStatus")
                    {
                        // Se actualiza de forma independiente con su propio color de estado
                    }
                    else if (lbl.Name == "lblTitle")
                    {
                        lbl.ForeColor = textColor;
                        lbl.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
                    }
                    else
                    {
                        lbl.ForeColor = mutedTextColor;
                        lbl.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                    }
                }
                else if (child is RichTextBox rtb && rtb.Name == "rtxtLog")
                {
                    // Consola de comandos con fondo oscuro profundo y fuente monospace
                    rtb.BackColor = Color.FromArgb(10, 15, 30);
                    rtb.ForeColor = Color.FromArgb(241, 245, 249);
                    rtb.BorderStyle = BorderStyle.FixedSingle;
                    rtb.Font = new Font("Consolas", 9F, FontStyle.Regular);
                }
                else if (child is RichTextBox rtb2 && rtb2.Name == "rtbChat")
                {
                    rtb2.BackColor = controlBack;
                    rtb2.ForeColor = textColor;
                    rtb2.BorderStyle = BorderStyle.FixedSingle;
                    rtb2.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                }
                else if (child is ComboBox cmb)
                {
                    cmb.BackColor = controlBack;
                    cmb.ForeColor = textColor;
                    cmb.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                }

                if (child.Controls.Count > 0)
                {
                    AplicarTemaRecursivo(child, backColor, panelColor, textColor, controlBack, mutedTextColor);
                }
            }
        }
        #endregion

        #region Chat de Texto

        private async void btnEnviar_Click(object sender, EventArgs e)
        {
            string contenido = txtMensaje.Text.Trim();
            if (string.IsNullOrEmpty(contenido)) return;

            string seleccion = cmbDestino.SelectedItem as string ?? "(Todos)";
            string destino = seleccion.Equals("(Todos)", StringComparison.OrdinalIgnoreCase) ? "" : seleccion;

            await _client.EnviarMensajeAsync(destino, contenido);
            txtMensaje.Clear();
            txtMensaje.Focus();
        }

        /// <summary>
        /// Agrega un mensaje de chat recibido al RichTextBox de chat, de forma thread-safe.
        /// </summary>
        private void SafeAppendChat(string remitente, string contenido)
        {
            if (this.IsDisposed || !rtbChat.IsHandleCreated || rtbChat.IsDisposed) return;
            if (rtbChat.InvokeRequired)
            {
                rtbChat.BeginInvoke(new Action(() => SafeAppendChat(remitente, contenido)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(96, 165, 250); // Azul para el remitente
            rtbChat.AppendText($"[{timestamp}] {remitente}: ");
            rtbChat.SelectionColor = rtbChat.ForeColor;
            rtbChat.AppendText($"{contenido}\r\n");
            rtbChat.ScrollToCaret();
        }

        #endregion

        private async void btnEnviarArchivo_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialogo = new()
            {
                Title = "Selecciona un archivo para enviar"
            };
            if (dialogo.ShowDialog() != DialogResult.OK) return;

            string seleccion = cmbDestino.SelectedItem as string ?? "(Todos)";
            string destino = seleccion.Equals("(Todos)", StringComparison.OrdinalIgnoreCase) ? "" : seleccion;
            btnEnviarArchivo.Enabled = false;
            try
            {
                await _client.EnviarArchivoAsync(destino, dialogo.FileName);
            }
            finally
            {
                btnEnviarArchivo.Enabled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cerrar sockets de servidor y cliente limpiamente
            _server.Stop();
            _client.Disconnect();
        }

        private void SafeUpdateComboDestinatarios(List<string> usuarios)
        {
            if (this.IsDisposed || !cmbDestino.IsHandleCreated) return;
            if (cmbDestino.InvokeRequired)
            {
                cmbDestino.BeginInvoke(new Action(() => SafeUpdateComboDestinatarios(usuarios)));
                return;
            }

            string seleccionPrevia = cmbDestino.SelectedItem as string ?? "(Todos)";

            cmbDestino.Items.Clear();
            cmbDestino.Items.Add("(Todos)");
            foreach (var u in usuarios)
            {
                // No te muestres a ti mismo como destinatario
                if (!u.Equals(_client.UsuarioActual, StringComparison.OrdinalIgnoreCase))
                {
                    cmbDestino.Items.Add(u);
                }
            }

            int idx = cmbDestino.Items.IndexOf(seleccionPrevia);
            cmbDestino.SelectedIndex = idx >= 0 ? idx : 0; // si el usuario elegido se desconectó, vuelve a "(Todos)"
        }

        #region Win32 Placeholders (Cue Banners)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        private void ConfigurarPlaceholders()
        {
            SendMessage(txtClientUser.Handle, EM_SETCUEBANNER, 0, "Nombre del usuario...");
            SendMessage(txtMensaje.Handle, EM_SETCUEBANNER, 0, "Escribe un mensaje aquí...");
            SendMessage(txtClientIp.Handle, EM_SETCUEBANNER, 0, "IP del servidor...");
            SendMessage(txtClientPort.Handle, EM_SETCUEBANNER, 0, "Puerto...");
            SendMessage(txtServerPort.Handle, EM_SETCUEBANNER, 0, "Puerto...");
        }
        #endregion

    }
}
