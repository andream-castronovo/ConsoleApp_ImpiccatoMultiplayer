using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp_ImpiccatoMultiplayer
{
    public class Client
    {
        Socket _serverSocket;
        Thread _ascoltaServer;
        Queue<string> _messaggiDalServer = new Queue<string>();

        public Queue<string> MessaggiDalServer { get => _messaggiDalServer; }

        string _ipDestinatario;
        int _portaDestinatario;

        public Client(string ip, int port) 
        {
            _ipDestinatario = ip;
            _portaDestinatario = port;
            
            ApriConnessione();
        }

        void ApriConnessione()
        {
            //IPAddress ip = IPAddress.Parse(_ipDestinatario); // IP verso cui stabilire la connessione
            IPAddress ip = Dns.GetHostEntry(_ipDestinatario).AddressList[0];

            IPEndPoint remoteEndPoint = new IPEndPoint(ip, _portaDestinatario);

            // Avendo l'associazione IP-Porta, posso aprire la Socket
            _serverSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Connect(remoteEndPoint);

            _ascoltaServer = new Thread(AscoltaServer) { Name = "AscoltaServer" };
            _ascoltaServer.Start();

        }

        void AscoltaServer()
        {
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
                        bytesRicevuti = _serverSocket.Receive(bytes);
                    }
                    catch (SocketException)
                    {
                        if (_serverSocket != null)
                        {
                            _serverSocket.Shutdown(SocketShutdown.Both);
                            _serverSocket.Close();
                        }
                        return;
                    }

                    data += Encoding.UTF8.GetString(bytes, 0, bytesRicevuti);

                } while (!data.Contains("<EOF>"));

                _messaggiDalServer.Enqueue(data.Replace("<EOF>",""));
            }
        }

        public void AspettaRispostaServer()
        {
            while (_messaggiDalServer.Count == 0)
                Thread.Sleep(300);
        }

        public void InviaAlServer(string messaggio)
        {
            _serverSocket.Send(Encoding.UTF8.GetBytes(messaggio + "<EOF>"));
        }

        //string[] _protocolli =
        //{
        //    "<EOF>",
        //    "<START>",
        //};

        //bool CoseUsate(string d)
        //{
        //    foreach (string protocollo in _protocolli)
        //        if (d.Contains(protocollo))
        //            return true;
        //    return false;
        //}
    }
}
