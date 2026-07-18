using System;
using System.Drawing;
using System.Net;
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
            AplicarTema(this); // Aplicar tema por defecto
            Log("SISTEMA", "Interfaz de usuario inicializada. Listo para operar.", LogLevel.Info);

            cmbDestino.Items.Add("(Todos)");
            cmbDestino.SelectedIndex = 0;
        }

        private void ConfigurarColumnasListView()
        {
            lstClientes.Columns.Clear();
            lstClientes.Columns.Add("Usuario", 120);
            lstClientes.Columns.Add("IP Cliente", 130);
            lstClientes.Columns.Add("Puerto", 80);
            lstClientes.Columns.Add("Hora Conexión", 120);
        }

        #region Servidor - Control de Interfaz
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            // Validar IP
            string ip = txtServerIp.Text.Trim();
            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Por favor, ingrese una dirección IP de escucha válida.", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar Puerto
            if (!int.TryParse(txtServerPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Por favor, ingrese un puerto válido (1 - 65535).", "Error de Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _server.Start(ip, port);
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
            txtServerIp.ReadOnly = isRunning;
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
                item.BackColor = _isDarkMode ? Color.FromArgb(30, 30, 36) : Color.White;
                item.ForeColor = _isDarkMode ? Color.FromArgb(229, 231, 235) : Color.FromArgb(31, 41, 55);

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
            Color backColor = _isDarkMode ? Color.FromArgb(30, 30, 36) : Color.FromArgb(248, 249, 250);
            Color panelColor = _isDarkMode ? Color.FromArgb(42, 42, 50) : Color.White;
            Color textColor = _isDarkMode ? Color.FromArgb(229, 231, 235) : Color.FromArgb(31, 41, 55);
            Color controlBack = _isDarkMode ? Color.FromArgb(30, 30, 36) : Color.White;
            Color buttonColor = _isDarkMode ? Color.FromArgb(59, 130, 246) : Color.FromArgb(37, 99, 235);
            Color buttonHover = _isDarkMode ? Color.FromArgb(37, 99, 235) : Color.FromArgb(29, 78, 216);

            this.BackColor = backColor;
            this.ForeColor = textColor;

            btnThemeToggle.Text = _isDarkMode ? "☀️ Modo Claro" : "🌙 Modo Oscuro";

            AplicarTemaRecursivo(parent, backColor, panelColor, textColor, controlBack, buttonColor, buttonHover);
        }

        private void AplicarTemaRecursivo(Control control, Color backColor, Color panelColor, Color textColor, Color controlBack, Color buttonColor, Color buttonHover)
        {
            foreach (Control child in control.Controls)
            {
                if (child is Panel && child.Name == "pnlHeader")
                {
                    child.BackColor = panelColor;
                    child.ForeColor = textColor;
                }
                else if (child is GroupBox gb)
                {
                    gb.BackColor = panelColor;
                    gb.ForeColor = textColor;
                }
                else if (child is TableLayoutPanel tlp)
                {
                    tlp.BackColor = Color.Transparent;
                    tlp.ForeColor = textColor;
                }
                else if (child is Button btn)
                {
                    if (btn == btnThemeToggle)
                    {
                        btn.BackColor = _isDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(229, 231, 235);
                        btn.ForeColor = textColor;
                        btn.FlatAppearance.BorderColor = _isDarkMode ? Color.FromArgb(82, 82, 91) : Color.FromArgb(209, 213, 219);
                    }
                    else
                    {
                        btn.BackColor = buttonColor;
                        btn.ForeColor = Color.White;
                        btn.FlatAppearance.MouseOverBackColor = buttonHover;
                        btn.FlatAppearance.BorderColor = _isDarkMode ? Color.FromArgb(59, 130, 246) : Color.FromArgb(37, 99, 235);
                    }
                }
                else if (child is TextBox tb)
                {
                    tb.BackColor = controlBack;
                    tb.ForeColor = textColor;
                }
                else if (child is ListView lv)
                {
                    lv.BackColor = controlBack;
                    lv.ForeColor = textColor;
                }
                else if (child is Label lbl)
                {
                    if (lbl.Name != "lblServerStatus")
                    {
                        lbl.ForeColor = textColor;
                    }
                }
                else if (child is RichTextBox rtb && rtb.Name == "rtxtLog")
                {
                    // La consola se mantiene siempre oscura para mejor visibilidad y contraste
                    rtb.BackColor = Color.FromArgb(24, 24, 27);
                    rtb.ForeColor = Color.FromArgb(228, 228, 231);
                }
                else if (child is RichTextBox rtb2 && rtb2.Name == "rtbChat")
                {
                    rtb2.BackColor = controlBack;
                    rtb2.ForeColor = textColor;
                }
                else if (child is ComboBox cmb)
                {
                    cmb.BackColor = controlBack;
                    cmb.ForeColor = textColor;
                }


                if (child.Controls.Count > 0)
                {
                    AplicarTemaRecursivo(child, backColor, panelColor, textColor, controlBack, buttonColor, buttonHover);
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

    }
}
