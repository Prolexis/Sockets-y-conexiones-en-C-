using System;

using System.Net.Sockets;

namespace SERVIDORES_SOCKETS
{
    /// <summary>
    /// Representa un cliente que se ha conectado al servidor.
    /// Contiene el socket TcpClient dedicado y metadatos de conexión.
    /// </summary>
    public class ClienteConectado
    {
        public TcpClient Socket { get; }
        public string IP { get; }
        public int Puerto { get; }
        public string Usuario { get; set; }
        public DateTime HoraConexion { get; }

        // Escritor dedicado a este cliente. Permite que OTRO hilo (el de un cliente distinto)
        // le reenvíe mensajes de chat sin tener que "adivinar" su stream.
        private readonly StreamWriter _writer;
        private readonly object _writeLock = new();

        public ClienteConectado(TcpClient socket, string ip, int puerto, string usuario, StreamWriter writer)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            IP = ip;
            Puerto = puerto;
            Usuario = usuario;
            HoraConexion = DateTime.Now;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <summary>
        /// Envía una línea de protocolo a este cliente de forma thread-safe.
        /// Devuelve false si falló (socket caído, etc.) sin lanzar excepción hacia arriba.
        /// </summary>
        public bool EnviarLinea(string linea)
        {
            lock (_writeLock)
            {
                try
                {
                    _writer.WriteLine(linea);
                    return true;
                }
                catch
                {
                    // El socket probablemente ya está caído; el bucle de lectura de ese
                    // cliente se encargará de detectarlo y limpiarlo (evita duplicar lógica aquí).
                    return false;
                }
            }
        }

        /// <summary>
        /// Cierra la conexión del socket de forma segura.
        /// </summary>
        public void Close()
        {
            try
            {
                Socket.Close();
            }
            catch
            {
                // Ignorar errores al cerrar el socket
            }
        }
    }
}
