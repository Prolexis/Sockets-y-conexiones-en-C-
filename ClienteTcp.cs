using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using System.Linq;

namespace SERVIDORES_SOCKETS
{
    public class ClienteTcp
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private bool _connected;
        private bool _connecting;

        public bool IsConnected => _connected;

        private string _usuarioPropio = "";
        public string UsuarioActual => _usuarioPropio;



        private const int FILE_CHUNK_SIZE = 32 * 1024;               // 32 KB por chunk
        private const long FILE_MAX_SIZE_BYTES = 50L * 1024 * 1024;  // 50 MB, ajustable

        // Protege escrituras concurrentes al mismo _writer (chat, ping, archivos pueden
        // dispararse casi simultáneamente desde distintos hilos/tareas).
        private readonly SemaphoreSlim _writeSemaphore = new(1, 1);

        private readonly object _transferenciasLock = new();
        private readonly Dictionary<string, TransferenciaEntrante> _transferenciasEntrantes = new();

        private class TransferenciaEntrante
        {
            public string FileId = "";
            public string NombreArchivo = "";
            public long TamañoTotal;
            public long BytesRecibidos;
            public string Remitente = "";
            public string RutaDestino = "";
            public FileStream? Stream;
        }


        public event Action<string, LogLevel>? OnLog;
        public event Action<bool>? OnConnectionStatusChanged;

        public event Action<string, string>? OnChatMessageReceived; // (remitente, contenido)


        public event Action<string, string, string, long>? OnFileIncomingStarted;   // (remitente, fileId, nombreArchivo, tamañoBytes)
        public event Action<string, long, long>? OnFileTransferProgress;           // (fileId, bytesRecibidos, tamañoTotal)
        public event Action<string, string, string>? OnFileTransferCompleted;      // (fileId, remitente, rutaGuardada)
        public event Action<string, string>? OnFileTransferError;                  // (fileId, mensaje)


        public event Action<List<string>>? OnUserListUpdated;


        /// <summary>
        /// Función que Form1 debe asignar para preguntarle al usuario dónde guardar un archivo
        /// entrante. Se invoca desde el hilo de recepción de red — la implementación en Form1
        /// DEBE usar Invoke (síncrono, no BeginInvoke) para mostrar el diálogo y devolver la ruta
        /// elegida, o null si el usuario cancela. Si no se asigna, se usa la carpeta por defecto.
        /// </summary>
        public Func<string, long, string?>? SolicitarRutaGuardado { get; set; }


        /// <summary>
        /// Se conecta asíncronamente al servidor en la IP y puerto especificados.
        /// </summary>
        public async Task ConnectAsync(string ip, int puerto, string usuario)
        {
            if (_connected || _connecting) return;

            _connecting = true;
            OnLog?.Invoke($"Intentando conectar a {ip}:{puerto} como '{usuario}'...", LogLevel.Info);

            try
            {
                _client = new TcpClient();
                // Establecer un timeout corto para la conexión
                var connectTask = _client.ConnectAsync(ip, puerto);
                var delayTask = Task.Delay(5000); // 5 segundos de timeout

                var completedTask = await Task.WhenAny(connectTask, delayTask);
                if (completedTask == delayTask)
                {
                    throw new TimeoutException("Se agotó el tiempo de espera para conectar con el servidor.");
                }

                // Si falló connectTask, lanzará la excepción correspondiente
                await connectTask;

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                // 1. Enviar identificación inmediatamente
                await _writer.WriteLineAsync($"IDENTIFY|{usuario}");

                // 2. Esperar confirmación del servidor
                string? response = await _reader.ReadLineAsync();
                if (response == null)
                {
                    throw new IOException("El servidor cerró la conexión prematuramente.");
                }

                if (response.StartsWith("OK|"))
                {
                    _connected = true;
                    _usuarioPropio = usuario;
                    OnLog?.Invoke("Conexión establecida correctamente con el servidor.", LogLevel.Success);
                    OnConnectionStatusChanged?.Invoke(true);

                    // Iniciar el hilo de recepción
                    _ = Task.Run(() => EscucharServidorAsync());
                }
                else if (response.StartsWith("ERROR|"))
                {
                    string errorMsg = response.Substring("ERROR|".Length);
                    OnLog?.Invoke($"El servidor rechazó la conexión: {errorMsg}", LogLevel.Error);
                    Disconnect();
                }
                else
                {
                    OnLog?.Invoke("Respuesta de protocolo inválida del servidor.", LogLevel.Error);
                    Disconnect();
                }
            }
            catch (SocketException ex)
            {
                OnLog?.Invoke($"Error de conexión: No se pudo establecer conexión con el servidor ({ex.Message}).", LogLevel.Error);
                Disconnect();
            }
            catch (TimeoutException ex)
            {
                OnLog?.Invoke($"Error: {ex.Message}", LogLevel.Error);
                Disconnect();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error inesperado al conectar: {ex.Message}", LogLevel.Error);
                Disconnect();
            }
            finally
            {
                _connecting = false;
            }
        }

        /// <summary>
        /// Hilo de escucha en segundo plano para recibir mensajes del servidor.
        /// </summary>
        private async Task EscucharServidorAsync()
        {
            try
            {
                while (_connected && _reader != null)
                {
                    string? linea = await _reader.ReadLineAsync();

                    // Lectura nula (0 bytes a nivel socket) indica desconexión del servidor
                    if (linea == null)
                    {
                        break;
                    }

                    // Por ahora sólo escuchamos respuestas del servidor
                    if (linea.Equals("PONG", StringComparison.OrdinalIgnoreCase))
                    {
                        OnLog?.Invoke("Servidor respondió PONG", LogLevel.Info);
                    }
                    else if (linea.StartsWith("MSG|", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] partes = linea.Split(new[] { '|' }, 3);
                        if (partes.Length == 3)
                        {
                            string remitente = partes[1];
                            string contenido = partes[2];
                            OnChatMessageReceived?.Invoke(remitente, contenido);
                        }
                    }
                    else if (linea.StartsWith("ERROR|", StringComparison.OrdinalIgnoreCase))
                    {
                        string err = linea.Substring("ERROR|".Length);
                        OnLog?.Invoke($"Servidor: {err}", LogLevel.Error);
                    }
                    else if (linea.Equals("SERVER_SHUTDOWN", StringComparison.OrdinalIgnoreCase))
                    {
                        OnLog?.Invoke("El servidor se está apagando.", LogLevel.Error);
                        break;
                    }
                    else if (linea.StartsWith("FILE_INFO|", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcesarFileInfo(linea);
                    }
                    else if (linea.StartsWith("FILE_CHUNK|", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcesarFileChunk(linea);
                    }
                    else if (linea.StartsWith("FILE_END|", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcesarFileEnd(linea);
                    }
                    else if (linea.StartsWith("USERLIST|", StringComparison.OrdinalIgnoreCase))
                    {
                        string payload = linea.Substring("USERLIST|".Length);
                        List<string> usuarios = string.IsNullOrEmpty(payload)
                            ? new List<string>()
                            : payload.Split(',').ToList();
                        OnUserListUpdated?.Invoke(usuarios);
                    }
                }
            }
            catch (IOException)
            {
                // Conexión interrumpida abruptamente
            }
            catch (ObjectDisposedException)
            {
                // Socket cerrado intencionalmente
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error en hilo de recepción de cliente: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                if (_connected)
                {
                    OnLog?.Invoke("Se ha perdido la conexión con el servidor.", LogLevel.Error);
                    Disconnect();
                }
            }
        }

        private void ProcesarFileInfo(string linea)
        {
            string[] p = linea.Split('|');
            if (p.Length < 5)
            {
                OnLog?.Invoke("FILE_INFO malformado recibido, ignorado.", LogLevel.Error);
                return;
            }
            string remitente = p[1];
            string fileId = p[2];
            string nombreArchivo = SanitizarNombreArchivo(p[3]);
            if (!long.TryParse(p[4], out long tamaño) || tamaño < 0)
            {
                OnLog?.Invoke("FILE_INFO con tamaño inválido, ignorado.", LogLevel.Error);
                return;
            }

            OnLog?.Invoke($"Recibiendo archivo '{nombreArchivo}' ({tamaño} bytes) de {remitente}...", LogLevel.Info);
            OnFileIncomingStarted?.Invoke(remitente, fileId, nombreArchivo, tamaño);

            // Preguntar a la UI dónde guardar (bloquea este hilo hasta que el usuario responda).
            string? rutaDestino = SolicitarRutaGuardado?.Invoke(nombreArchivo, tamaño);

            // Fallback: si no hay UI asignada, se conserva el comportamiento anterior (carpeta fija).
            if (SolicitarRutaGuardado == null)
            {
                string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchivosRecibidos");
                Directory.CreateDirectory(carpeta);
                rutaDestino = ObtenerRutaSinColision(carpeta, nombreArchivo);
            }

            if (rutaDestino == null)
            {
                // Usuario canceló el diálogo: registramos la transferencia SIN stream.
                // Los chunks que lleguen se descartan en silencio, sin romper el protocolo.
                lock (_transferenciasLock)
                {
                    _transferenciasEntrantes[fileId] = new TransferenciaEntrante
                    {
                        FileId = fileId,
                        NombreArchivo = nombreArchivo,
                        TamañoTotal = tamaño,
                        Remitente = remitente,
                        RutaDestino = "",
                        Stream = null
                    };
                }
                OnLog?.Invoke($"Archivo '{nombreArchivo}' de {remitente} rechazado (sin ubicación de guardado).", LogLevel.Info);
                return;
            }

            try
            {
                var transferencia = new TransferenciaEntrante
                {
                    FileId = fileId,
                    NombreArchivo = nombreArchivo,
                    TamañoTotal = tamaño,
                    Remitente = remitente,
                    RutaDestino = rutaDestino,
                    Stream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write, FileShare.None)
                };

                lock (_transferenciasLock)
                {
                    _transferenciasEntrantes[fileId] = transferencia;
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"No se pudo iniciar la recepción de '{nombreArchivo}': {ex.Message}", LogLevel.Error);
                OnFileTransferError?.Invoke(fileId, ex.Message);
            }
        }

        private void ProcesarFileChunk(string linea)
        {
            // FILE_CHUNK|<remitente>|<fileId>|<base64>
            int p1 = linea.IndexOf('|');
            int p2 = linea.IndexOf('|', p1 + 1);
            int p3 = linea.IndexOf('|', p2 + 1);
            if (p1 < 0 || p2 < 0 || p3 < 0) return;

            string fileId = linea.Substring(p2 + 1, p3 - p2 - 1);
            string base64 = linea.Substring(p3 + 1);

            TransferenciaEntrante? t;
            lock (_transferenciasLock)
            {
                _transferenciasEntrantes.TryGetValue(fileId, out t);
            }
            // Chunk "huérfano" (llegó tarde, o el FILE_INFO falló antes) -> se ignora sin tumbar el hilo.
            if (t == null || t.Stream == null) return;

            try
            {
                byte[] datos = Convert.FromBase64String(base64);
                t.Stream.Write(datos, 0, datos.Length);
                t.BytesRecibidos += datos.Length;
                OnFileTransferProgress?.Invoke(fileId, t.BytesRecibidos, t.TamañoTotal);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error al escribir chunk de '{t.NombreArchivo}': {ex.Message}", LogLevel.Error);
                AbortarTransferencia(fileId, t);
            }
        }

        private void ProcesarFileEnd(string linea)
        {
            // FILE_END|<remitente>|<fileId>
            string[] p = linea.Split('|');
            if (p.Length < 3) return;
            string fileId = p[2];

            TransferenciaEntrante? t;
            lock (_transferenciasLock)
            {
                _transferenciasEntrantes.Remove(fileId, out t);
            }
            if (t == null) return;
            if (t.Stream == null)
            {
                // Transferencia que el usuario rechazó al inicio: no hay nada que cerrar ni reportar como éxito.
                OnLog?.Invoke($"Archivo '{t.NombreArchivo}' de {t.Remitente} descartado (no aceptado).", LogLevel.Info);
                return;
            }

            try
            {
                t.Stream?.Flush();
                t.Stream?.Close();
                OnLog?.Invoke($"Archivo '{t.NombreArchivo}' recibido completo, guardado en: {t.RutaDestino}", LogLevel.Success);
                OnFileTransferCompleted?.Invoke(fileId, t.Remitente, t.RutaDestino);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error al finalizar archivo '{t.NombreArchivo}': {ex.Message}", LogLevel.Error);
                OnFileTransferError?.Invoke(fileId, ex.Message);
            }
        }

        private void AbortarTransferencia(string fileId, TransferenciaEntrante t)
        {
            lock (_transferenciasLock)
            {
                _transferenciasEntrantes.Remove(fileId);
            }
            try { t.Stream?.Close(); } catch { }
            try { if (File.Exists(t.RutaDestino)) File.Delete(t.RutaDestino); } catch { }
            OnFileTransferError?.Invoke(fileId, "La transferencia se interrumpió y el archivo parcial fue descartado.");
        }

        /// <summary>
        /// Evita path traversal (ej. un nombre "../../algo.exe") y caracteres inválidos en disco.
        /// </summary>
        private static string SanitizarNombreArchivo(string nombre)
        {
            string limpio = Path.GetFileName(nombre); // descarta cualquier ruta, solo deja el nombre
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                limpio = limpio.Replace(c, '_');
            }
            if (string.IsNullOrWhiteSpace(limpio)) limpio = "archivo_recibido";
            return limpio;
        }

        private static string ObtenerRutaSinColision(string carpeta, string nombreArchivo)
        {
            string ruta = Path.Combine(carpeta, nombreArchivo);
            if (!File.Exists(ruta)) return ruta;

            string nombreSinExt = Path.GetFileNameWithoutExtension(nombreArchivo);
            string ext = Path.GetExtension(nombreArchivo);
            int contador = 1;
            string nuevaRuta;
            do
            {
                nuevaRuta = Path.Combine(carpeta, $"{nombreSinExt} ({contador}){ext}");
                contador++;
            } while (File.Exists(nuevaRuta));
            return nuevaRuta;
        }

        /// <summary>
        /// Desconecta limpiamente el socket del cliente.
        /// </summary>
        public void Disconnect()
        {
            if (!_connected && _client == null) return;

            bool wasConnected = _connected;
            _connected = false;

            if (wasConnected && _writer != null)
            {
                try
                {
                    _writer.WriteLine("DISCONNECT");
                    _writer.Flush();
                }
                catch { }
            }

            try
            {
                _writer?.Close();
                _reader?.Close();
                _stream?.Close();
                _client?.Close();
            }
            catch
            {
                // Ignorar excepciones al cerrar los streams
            }
            finally
            {

                lock (_transferenciasLock)
                {
                    foreach (var t in _transferenciasEntrantes.Values)
                    {
                        try { t.Stream?.Close(); } catch { }
                    }
                    _transferenciasEntrantes.Clear();
                }

                _client = null;
                _stream = null;
                _reader = null;
                _writer = null;

                if (wasConnected)
                {
                    OnLog?.Invoke("Cliente desconectado.", LogLevel.Info);
                    OnConnectionStatusChanged?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// Envía un mensaje (ping o keepalive) en esta fase.
        /// </summary>
        public async Task EnviarPingAsync()
        {
            if (!_connected || _writer == null) return;
            try
            {
                await EscribirLineaAsync("PING");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error al enviar datos: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Envía un mensaje de chat. destino = "ALL" o vacío para difundir a todos,
        /// o el nombre de usuario destino para un mensaje privado.
        /// </summary>
        public async Task EnviarMensajeAsync(string destino, string contenido)
        {
            if (!_connected || _writer == null) return;
            if (string.IsNullOrWhiteSpace(contenido)) return;

            try
            {
                string destinoLimpio = string.IsNullOrWhiteSpace(destino) ? "ALL" : destino.Trim();
                // El protocolo es por líneas: evitamos que un salto de línea accidental rompa el parser.
                string contenidoLimpio = contenido.Replace("\r", " ").Replace("\n", " ");
                await EscribirLineaAsync($"MSG|{destinoLimpio}|{contenidoLimpio}");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error al enviar mensaje: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Helper de escritura thread-safe
        /// </summary>

        private async Task EscribirLineaAsync(string linea)
        {
            if (_writer == null) return;
            await _writeSemaphore.WaitAsync();
            try
            {
                await _writer.WriteLineAsync(linea);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        /// <summary>
        /// Envío de archivo
        /// </summary>

        public async Task EnviarArchivoAsync(string destino, string rutaArchivo)
        {
            if (!_connected || _writer == null)
            {
                OnLog?.Invoke("No se puede enviar archivo: no hay conexión activa.", LogLevel.Error);
                return;
            }
            if (!File.Exists(rutaArchivo))
            {
                OnLog?.Invoke($"El archivo '{rutaArchivo}' no existe o no es accesible.", LogLevel.Error);
                return;
            }

            var info = new FileInfo(rutaArchivo);
            if (info.Length > FILE_MAX_SIZE_BYTES)
            {
                OnLog?.Invoke($"El archivo supera el límite permitido ({FILE_MAX_SIZE_BYTES / (1024 * 1024)} MB).", LogLevel.Error);
                return;
            }

            string fileId = Guid.NewGuid().ToString("N");
            string destinoLimpio = string.IsNullOrWhiteSpace(destino) ? "ALL" : destino.Trim();
            string nombreArchivo = Path.GetFileName(rutaArchivo);

            try
            {
                await EscribirLineaAsync($"FILE_INFO|{destinoLimpio}|{fileId}|{nombreArchivo}|{info.Length}");

                byte[] buffer = new byte[FILE_CHUNK_SIZE];
                long bytesEnviados = 0;

                using FileStream fs = new(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.Read);
                int leidos;
                while ((leidos = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string base64 = Convert.ToBase64String(buffer, 0, leidos);
                    await EscribirLineaAsync($"FILE_CHUNK|{destinoLimpio}|{fileId}|{base64}");
                    bytesEnviados += leidos;
                    OnFileTransferProgress?.Invoke(fileId, bytesEnviados, info.Length);
                }

                await EscribirLineaAsync($"FILE_END|{destinoLimpio}|{fileId}");
                OnLog?.Invoke($"Archivo '{nombreArchivo}' enviado correctamente ({info.Length} bytes).", LogLevel.Success);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error al enviar archivo '{nombreArchivo}': {ex.Message}", LogLevel.Error);
            }
        }

    }
}
