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
                    try
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
                    catch { }
                }

                if (this.IsDisposed || !this.IsHandleCreated) return null;
                try
                {
                    if (this.InvokeRequired)
                    {
                        // Invoke SÍNCRONO (no BeginInvoke): el hilo de red debe esperar la decisión del usuario.
                        this.Invoke((Action)MostrarDialogo);
                    }
                    else
                    {
                        MostrarDialogo();
                    }
                }
                catch
                {
                    return null;
                }
                return resultado;
            };

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConfigurarColumnasListView();

            // Hacer el chat de solo lectura antes de aplicar el tema para que no restablezca el BackColor a gris/blanco
            rtbChat.ReadOnly = true;

            // Configurar el layout dinámico profesional que evita solapamientos y soporta DPI scaling
            ConfigurarLayoutChatProfesional();

            // Configurar el layout de la consola de logs flotante integrada
            ConfigurarLayoutLogModerno();

            // Configurar el pintado moderno estilo Windows 11 (tarjetas con bordes redondeados y separadores)
            ConfigurarPintadoModerno();

            // Sincronizar barra de título de Windows al arrancar
            SetTitleBarTheme(_isDarkMode);

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

        private void ConfigurarLayoutChatProfesional()
        {
            // Obtener el factor de escala DPI para ajustar las dimensiones proporcionalmente
            float scale = this.DeviceDpi / 96f;

            // Compactar tlpClientFields usando dimensiones escaladas para evitar desbordamiento y solapamiento
            tlpClientFields.Height = (int)(124 * scale);
            tlpClientFields.RowStyles.Clear();
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F * scale)); // Etiqueta IP
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F * scale)); // Inputs IP/Puerto
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F * scale)); // Etiqueta Usuario
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F * scale)); // Input Usuario & Conectar/Desconectar
            tlpClientFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F * scale)); // Botón PING (Latido)

            // Remover controles del GroupBox para agregarlos al contenedor dinámico
            gbCliente.Controls.Remove(rtbChat);
            gbCliente.Controls.Remove(cmbDestino);
            gbCliente.Controls.Remove(txtMensaje);
            gbCliente.Controls.Remove(btnEnviar);
            gbCliente.Controls.Remove(btnEnviarArchivo);
            gbCliente.Controls.Remove(lblDestino);
            gbCliente.Controls.Remove(lblMensaje);

            // Ocultar etiquetas fijas redundantes (las marcas de agua las reemplazan)
            lblDestino.Visible = false;
            lblMensaje.Visible = false;

            // NOTA: El GroupBox de Windows Forms ignora la propiedad Padding para acoplamientos (Dock).
            // Por lo tanto, aplicamos los márgenes laterales y el margen de seguridad inferior directamente a tlpChat.
            int marginSide = (int)(6 * scale);
            int marginBottom = (int)(16 * scale); // Aumentado a 16px para centrar los botones perfectamente y evitar recortes

            // Contenedor principal del chat (ocupa todo el espacio restante abajo del panel superior)
            TableLayoutPanel tlpChat = new()
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(marginSide, 0, marginSide, marginBottom) // Aplicado directamente en el contenedor del chat
            };
            tlpChat.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Fila del historial (flexible)
            tlpChat.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F * scale));  // Fila de herramientas de envío (fija y escalada)

            // Chat en la parte superior
            rtbChat.Dock = DockStyle.Fill;
            rtbChat.Margin = new Padding(0, 0, 0, (int)(5 * scale));
            tlpChat.Controls.Add(rtbChat, 0, 0);

            // Panel inferior horizontal
            TableLayoutPanel tlpInputs = new()
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                Margin = new Padding(0)
            };
            tlpInputs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Forzar a la fila a estirarse al 100% del alto (30px)
            tlpInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F)); // Selector Destino
            tlpInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F)); // Entrada de texto
            tlpInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Botón Enviar
            tlpInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Botón Archivo

            // Configurar los controles para que se expandan dinámicamente
            cmbDestino.Dock = DockStyle.Fill;
            cmbDestino.Margin = new Padding(0, 0, (int)(5 * scale), 0);

            txtMensaje.Dock = DockStyle.Fill;
            txtMensaje.Margin = new Padding(0, (int)(2 * scale), (int)(5 * scale), 0); // Ajuste vertical sutil para alineación de texto

            btnEnviar.Dock = DockStyle.Fill;
            btnEnviar.Margin = new Padding(0, 0, (int)(5 * scale), 0);
            btnEnviar.Text = "Enviar";

            btnEnviarArchivo.Dock = DockStyle.Fill;
            btnEnviarArchivo.Margin = new Padding(0, 0, 0, 0);
            btnEnviarArchivo.Text = "Archivo";

            tlpInputs.Controls.Add(cmbDestino, 0, 0);
            tlpInputs.Controls.Add(txtMensaje, 1, 0);
            tlpInputs.Controls.Add(btnEnviar, 2, 0);
            tlpInputs.Controls.Add(btnEnviarArchivo, 3, 0);

            tlpChat.Controls.Add(tlpInputs, 0, 1);

            // Agregar al GroupBox y enviar al fondo del Z-Order para que llene el espacio restante debajo de tlpClientFields (Dock=Top)
            gbCliente.Controls.Add(tlpChat);
            tlpChat.SendToBack();

            // Forzar recálculo inmediato del layout para aplicar el Padding y Z-Order, evitando recortes en los botones
            gbCliente.PerformLayout();
            this.PerformLayout();
        }

        private void ConfigurarLayoutLogModerno()
        {
            // Remover de gbLog
            gbLog.Controls.Remove(rtxtLog);

            float scale = this.DeviceDpi / 96f;

            // Crear panel contenedor con color de fondo del log para que se vea integrado como consola
            Panel pnlLog = new()
            {
                Name = "pnlLog", // Asignar nombre para identificarlo al aplicar el tema
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(9, 9, 11), // Fondo ultra negro
                Padding = new Padding((int)(8 * scale))
            };

            rtxtLog.Dock = DockStyle.Fill;
            rtxtLog.BorderStyle = BorderStyle.None; // Quitar borde para diseño plano
            pnlLog.Controls.Add(rtxtLog);

            // Crear un panel externo para dar margen respecto al GroupBox (para que flote dentro de la tarjeta)
            Panel pnlWrapper = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding((int)(12 * scale), (int)(22 * scale), (int)(12 * scale), (int)(12 * scale)) // Margen alrededor de la consola
            };
            pnlWrapper.Controls.Add(pnlLog);

            gbLog.Controls.Add(pnlWrapper);
            pnlWrapper.SendToBack();

            gbLog.PerformLayout();
        }

        private void ConfigurarColumnasListView()
        {
            float scale = this.DeviceDpi / 96f;
            lstClientes.Columns.Clear();
            lstClientes.Columns.Add("Usuario", (int)(110 * scale));
            lstClientes.Columns.Add("IP Cliente", (int)(120 * scale));
            lstClientes.Columns.Add("Puerto", (int)(65 * scale));
            lstClientes.Columns.Add("Hora Conexión", -2); // Estirar para llenar el espacio restante y evitar la columna vacía
            lstClientes.FullRowSelect = true;
            lstClientes.GridLines = true;

            // Registrar evento para recalcular el ancho de la última columna en redimensionamientos
            lstClientes.Resize += (s, e) =>
            {
                if (lstClientes.Columns.Count > 0)
                {
                    lstClientes.Columns[lstClientes.Columns.Count - 1].Width = -2;
                }
            };
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
                try
                {
                    this.BeginInvoke(new Action(() => SafeUpdateServerUI(isRunning)));
                }
                catch { }
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
                try
                {
                    this.BeginInvoke(new Action(SafeUpdateClientList));
                }
                catch { }
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
                try
                {
                    this.BeginInvoke(new Action(() => SafeUpdateClientUI(isConnected)));
                }
                catch { }
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
                try
                {
                    rtxtLog.BeginInvoke(new Action(() => Log(context, message, level)));
                }
                catch { }
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
                    // Color de texto adaptativo según el tema
                    color = _isDarkMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(51, 65, 85);
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
            // Desactivar repintado temporalmente para evitar parpadeos molestos
            SendMessageInt(this.Handle, WM_SETREDRAW, 0, 0);

            try
            {
                _isDarkMode = !_isDarkMode;
                SetTitleBarTheme(_isDarkMode); // Sincronizar barra de título de Windows
                
                // Actualizar colores del log existente en memoria mediante reemplazo rápido de RTF
                ActualizarColoresLogExistente();

                AplicarTema(this);

                // Actualizar la lista de clientes para re-pintar filas con el nuevo tema
                SafeUpdateClientList();
            }
            finally
            {
                // Volver a activar el repintado y forzar una única actualización visual limpia
                SendMessageInt(this.Handle, WM_SETREDRAW, 1, 0);
                this.Refresh(); // Forzar repintado completo de los bordes redondeados y gráficos
            }
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
                else if (child is Panel pnl && pnl.Name == "pnlLog")
                {
                    pnl.BackColor = _isDarkMode ? Color.FromArgb(9, 9, 11) : Color.FromArgb(248, 250, 252);
                }
                else if (child is RichTextBox rtb && rtb.Name == "rtxtLog")
                {
                    // Consola de comandos con fondo adaptivo y fuente monospace
                    rtb.BackColor = _isDarkMode ? Color.FromArgb(9, 9, 11) : Color.FromArgb(248, 250, 252);
                    rtb.ForeColor = _isDarkMode ? Color.FromArgb(228, 228, 231) : Color.FromArgb(51, 65, 85);
                    rtb.BorderStyle = BorderStyle.None; // Mantener diseño sin bordes
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
                try
                {
                    rtbChat.BeginInvoke(new Action(() => SafeAppendChat(remitente, contenido)));
                }
                catch { }
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
                try
                {
                    cmbDestino.BeginInvoke(new Action(() => SafeUpdateComboDestinatarios(usuarios)));
                }
                catch { }
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

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        private static extern int SendMessageInt(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int EM_SETCUEBANNER = 0x1501;
        private const int WM_SETREDRAW = 0x000B;

        private void ConfigurarPlaceholders()
        {
            SendMessage(txtClientUser.Handle, EM_SETCUEBANNER, 0, "Nombre del usuario...");
            SendMessage(txtMensaje.Handle, EM_SETCUEBANNER, 0, "Escribe un mensaje aquí...");
            SendMessage(txtClientIp.Handle, EM_SETCUEBANNER, 0, "IP del servidor...");
            SendMessage(txtClientPort.Handle, EM_SETCUEBANNER, 0, "Puerto...");
            SendMessage(txtServerPort.Handle, EM_SETCUEBANNER, 0, "Puerto...");
        }
        #endregion

        #region Windows 11 Estilos 2026 y GDI+ Custom Paint
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private void SetTitleBarTheme(bool darkMode)
        {
            try
            {
                int useDarkMode = darkMode ? 1 : 0;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                // Forzar refresco de la barra de título no-cliente
                this.Width++;
                this.Width--;
            }
            catch { }
        }

        private void ConfigurarPintadoModerno()
        {
            // Registrar los eventos de pintura personalizados para los GroupBoxes
            gbCliente.Paint += DibujarGroupBoxModerno;
            gbServidor.Paint += DibujarGroupBoxModerno;
            gbClientes.Paint += DibujarGroupBoxModerno;
            gbLog.Paint += DibujarGroupBoxModerno;

            // Registrar el evento de pintura del panel de cabecera
            pnlHeader.Paint += PnlHeader_Paint;
        }

        private void DibujarGroupBoxModerno(object? sender, PaintEventArgs e)
        {
            if (sender == null) return;
            GroupBox gb = (GroupBox)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Colores del tema actual
            Color borderColor = _isDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225); // Slate 700 vs Slate 300
            Color backColor = _isDarkMode ? Color.FromArgb(30, 41, 59) : Color.White; // Slate 800 vs White
            Color textColor = _isDarkMode ? Color.FromArgb(248, 250, 252) : Color.FromArgb(15, 23, 42);

            // Limpiar fondo con el color de la tarjeta
            using (SolidBrush brush = new(backColor))
            {
                g.FillRectangle(brush, gb.ClientRectangle);
            }

            // Dibujar borde redondeado
            float scale = this.DeviceDpi / 96f;
            int r = (int)(8 * scale); // Radio adaptado a DPI
            Rectangle rect = new(0, (int)(10 * scale), gb.Width - 1, gb.Height - (int)(11 * scale));
            
            using (System.Drawing.Drawing2D.GraphicsPath path = GetRoundedRectPath(rect, r))
            {
                using (Pen pen = new(borderColor, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // Dibujar título del GroupBox con tipografía premium
            if (!string.IsNullOrEmpty(gb.Text))
            {
                using (SolidBrush textBrush = new(textColor))
                {
                    using (Font font = new("Segoe UI Semibold", 9.75F, FontStyle.Bold))
                    {
                        SizeF textSize = g.MeasureString(gb.Text, font);
                        // Dibujar un pequeño rectángulo de fondo detrás del texto para que no cruce la línea
                        RectangleF textRect = new((int)(12 * scale), 0, textSize.Width + (int)(6 * scale), textSize.Height);
                        using (SolidBrush backBrush = new(backColor))
                        {
                            g.FillRectangle(backBrush, textRect);
                        }
                        g.DrawString(gb.Text, font, textBrush, (int)(15 * scale), 0);
                    }
                }
            }
        }

        private void PnlHeader_Paint(object? sender, PaintEventArgs e)
        {
            if (sender == null) return;
            Panel p = (Panel)sender;
            Color lineColor = _isDarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225); // Slate 700 / Slate 300
            using (Pen pen = new(lineColor, 1f))
            {
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        private void ActualizarColoresLogExistente()
        {
            try
            {
                string? rtf = rtxtLog.Rtf;
                if (rtf == null) return;

                // Reemplazar la definición de color del texto informativo en la tabla de colores del RTF
                if (_isDarkMode)
                {
                    // Cambiar de modo claro (51,65,85) a modo oscuro (228,228,231)
                    rtf = rtf.Replace(@"\red51\green65\blue85", @"\red228\green228\blue231");
                }
                else
                {
                    // Cambiar de modo oscuro (228,228,231) a modo claro (51,65,85)
                    rtf = rtf.Replace(@"\red228\green228\blue231", @"\red51\green65\blue85");
                }
                rtxtLog.Rtf = rtf;
            }
            catch { }
        }
        #endregion

    }
}
