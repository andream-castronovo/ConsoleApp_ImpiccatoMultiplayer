using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp_ImpiccatoMultiplayer
{
    public class Server
    {
        Socket _listener;
        
        List<Thread> _allThreads = new List<Thread>();
        List<ServerClient> _clients = new List<ServerClient>();

        public List<ServerClient> Clients { get; set; }

        public int ClientConnessi { get => _clients.Count; }

        string _ip;
        int _port;
        int _maxQueue;
        SemaphoreSlim _limit;

        public Server(string ip, int port, int maxQueue=10, int limit=-1) 
        {
            _ip = ip; // TODO: Controllare IP
            _port = port; // TODO: Controllare porta
            _maxQueue = maxQueue; // TODO: Controllare coda massima (non negativa)
            
            if (limit > 0)
                _limit = new SemaphoreSlim(limit); 
        }

        public void Start()
        {
            IPAddress ip = IPAddress.Parse(_ip); // IP per stabilire la connessione


            IPEndPoint localEndPoint = new IPEndPoint(ip, _port); // Questo è il collegamento tra l'IP che i client useranno per
                                                                  // connettersi al server e la porta.

            // Avendo l'associazione IP-Porta, posso aprire la Socket
            _listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            _listener.Bind(localEndPoint); // Colleghiamo alla Socket la combinazione IP-Porta in cui il server ascolterà
            _listener.Listen(_maxQueue);

            Thread t = new Thread(Ascolto)
            {
                Name = "ServerAscolto",
                IsBackground = true,
            };

            t.Start();
            _allThreads.Add(t);
        }

        void Ascolto()
        {
            while (true)
            {
                ServerClient c = new ServerClient();

                if (_limit != null)
                    _limit.Wait();

                c.Socket = _listener.Accept();

                Thread t = new Thread(() => RiceviMessaggi(c))
                {
                    IsBackground = true,
                    Name = "Gestisci " + c.Socket.RemoteEndPoint.ToString()
                };

                t.Start();

                c.Thread = t;

                _clients.Add(c);
            }
        }

        public void AspettaPlayer()
        {
            while (_clients.Count == 0)
                Thread.Sleep(300);
        }

        void RiceviMessaggi(ServerClient client)
        {
            Socket handler = client.Socket;
            while (true)
            {
                byte[] bytes;
                string data = "";

                do
                {
                    bytes = new byte[4096];
                    int bytesRicevuti;

                    try
                    {
                        bytesRicevuti = handler.Receive(bytes);
                    }
                    catch (SocketException)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        
                        _limit.Release();
                        _clients.Remove(client);
                        return;
                    }

                    data += Encoding.UTF8.GetString(bytes);

                } while (!data.Contains("<EOF>"));
                
                client.Dati.Enqueue(data);

            }
        }

        public void InviaMessaggioA(string message, ServerClient toWho)
        {
            toWho.Socket.Send(
                Encoding.UTF8.GetBytes(message + "<EOF>")
            );
        }

        public void InviaMessaggioBroadcast(string message)
        {
            foreach (ServerClient c in _clients)
            {
                c.Socket.Send(Encoding.UTF8.GetBytes(message + "<EOF>"));
            }
        }



    }

    public class ServerClient
    {
        public Socket Socket { get; set; }
        public Thread Thread { get; set; }
        public Queue<string> Dati { get; set; }
    }
}
