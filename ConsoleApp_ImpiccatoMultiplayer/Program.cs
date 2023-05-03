using System;
using System.Collections.Generic;
using System.Threading;
using static ConsoleApp_PonteLevatoio.MyConsoleUtils;

namespace ConsoleApp_ImpiccatoMultiplayer
{
    class Program
    {
        static object _lockConsole = new object();
        static object _lockInput = new object();

        static List<Thread> _allThreads = new List<Thread>();

        static void Main(string[] args)
        {
            Console.WindowHeight = 50;
            Console.BufferHeight = 50;
            
            Console.WindowWidth = 100;
            Console.BufferWidth = 100;
            
            Console.CursorVisible = false;

            string daTrovare = "Liquirizia"; // Input esterno

            Thread t = new Thread(() => Gioco(daTrovare));

            MenuIniziale();


            foreach (Thread th in _allThreads)
                if (t.IsAlive)
                    t.Join();
        }



        static void MenuIniziale()
        {
            
            int yInizioMenu = Console.WindowHeight / 2 - 10;
            int xInizioMenu = Console.WindowWidth / 2;

            Scrivi(@" _   _  ___  _  _  _ _ ", x: xInizioMenu - 11, y: yInizioMenu);
            Scrivi(@"| \_/ || __|| \| || | |", x: xInizioMenu - 11, y: yInizioMenu + 1);
            Scrivi(@"| \_/ || _| | \\ || U |", x: xInizioMenu - 11, y: yInizioMenu + 2);
            Scrivi(@"|_| |_||___||_|\_||___|", x: xInizioMenu - 11, y: yInizioMenu + 3);


            Dictionary<char, string> possibilita =
                new Dictionary<char, string>()
                {
                    {'A',"Scegli una parola (fai da server)" },
                    {'B', "Indovina una parola (fai da client)"}
                };

            char selezioneAttuale = 'A';
            
            StampaScelte();

            bool esci = false;
            while (!esci)
            {
                ConsoleKey k = Console.ReadKey(true).Key;
            
                if (k == ConsoleKey.UpArrow)
                {
                    selezioneAttuale = (char)(selezioneAttuale - 1);
                    if (selezioneAttuale < 'A')
                        selezioneAttuale = (char) ('A' + possibilita.Count - 1);
                    StampaScelte();
                }
                else if (k == ConsoleKey.DownArrow)
                {
                    selezioneAttuale = (char)(selezioneAttuale + 1);
                    if (selezioneAttuale > 'A' + possibilita.Count - 1)
                        selezioneAttuale = 'A';
                    StampaScelte();
                }
                else if (k == ConsoleKey.Enter)
                {
                    Thread t = null;
                    if (selezioneAttuale == 'A')
                        t = new Thread(ScegliParola) { Name = "ScegliParola" };
                    else if (selezioneAttuale == 'B')
                        t = new Thread(IndovinaParola) { Name = "ScegliParola" };


                    _allThreads.Add(t);
                    t.Start();
                    esci = true;
                }

            }

            void StampaScelte()
            {
                int baseAdd = 5;
                foreach (KeyValuePair<char, string> kvp in possibilita)
                {
                    if (selezioneAttuale == kvp.Key)
                        Scrivi($"{kvp.Key}) {kvp.Value}",
                            x: xInizioMenu - 14,
                            y: yInizioMenu + baseAdd,
                            back: ConsoleColor.Gray,
                            fore: ConsoleColor.DarkGray);
                    else
                        Scrivi($"{kvp.Key}) {kvp.Value}",
                            x: xInizioMenu - 14,
                            y: yInizioMenu + baseAdd);
                    Scrivi(""); // Resetto i colori
                    baseAdd += 2;
                }
            }
        }

        #region Controlli e simili
        static void InputParola(out string s)
        {
            bool inOk = false;
            do
            {
                s = Console.ReadLine();
                inOk = ControllaParola(s);
                if (!inOk) // TODO da rivedere
                    Console.WriteLine("Riprova");
            } while (!inOk);
        }

        static bool ControllaParola(string s)
        {
            return true; // TODO scrivere CODICE
        }
        #endregion


        // SERVER
        static void ScegliParola()
        {
            Console.Clear();

            Server s = new Server("127.0.0.1",11000,1);
            s.Start();
            
            Scrivi("Attendo la connessione di un client... ");
            s.AspettaPlayer();

            Scrivi("Ho una connessione!\n");
            Scrivi("Ora devi scegliere la parola da far indovinare --> ");
            
            string parola;

            InputParola(out parola);

            Scrivi($"Parola scelta: {parola}\n");
            s.InviaMessaggioBroadcast($"{OttieniStringaBase(parola)}<START>");
            Scrivi("Ora il giocatore dovrà scrivere una lettera alla volta le cose. \n"); // TODO Rivedere le scritte


        }

        // CLIENT
        static void IndovinaParola()
        {
            Console.Clear();

            Scrivi("Mi connetto...");
            Client c = new Client("5.tcp.eu.ngrok.io", 19036);
            Scrivi("Connesso!");

            Scrivi("Attendi che l'host scelga la parola...");


            bool ok = false;
            do
            {
                while (c.MessaggiDalServer.Count == 0)
                    Thread.Sleep(300);

                if (c.MessaggiDalServer.Peek().Contains("<START>"))
                    ok = true;
                else
                    Scrivi(c.MessaggiDalServer.Dequeue());
            } while (!ok);

            Scrivi("Iniziamo\n\n");

            Gioco(c.MessaggiDalServer.Dequeue().Replace("<START>",""), c);

            
        }

        static void Gioco(string stringaBase, Client c)
        {
            const int TENTATIVI_TOTALI = 6;
            int tentativi = TENTATIVI_TOTALI;
            bool vittoria = false;

            string ricerca = stringaBase;

            do
            {
                Scrivi($"Stato ricerca: {ricerca}\n", lck: _lockConsole);
                Scrivi("Scrivi la lettera da cercare: ", lck: _lockConsole);

                string input;

                bool inOk;
                do
                {
                    lock (_lockInput)
                        input = Console.ReadLine().ToLower();
                    inOk = ControlloLettera(input);
                    if (!inOk)
                    {
                        Scrivi("L'input dev'essere una sola lettera (a-z, A-Z).\n", lck: _lockConsole, fore: ConsoleColor.Red);
                    }
                } while (!inOk);

                char inputConvertito = input[0];

                // TODO: Rivedere bene questa parte, ho sonno vado a dormire
                c.InviaAlServer(inputConvertito.ToString());
                c.AspettaRispostaServer();
                string risposta = c.MessaggiDalServer.Dequeue();

                if (daTrovare.Contains(input))
                {
                    Scrivi("Lettera trovata!\n", fore: ConsoleColor.Green, lck: _lockConsole);
                    for (int i = 0; i < daTrovare.Length; i++)
                    {
                        if (daTrovare[i] == inputConvertito)
                            ricerca = ricerca.Substring(0, i) + daTrovare[i] + ricerca.Substring(i + 1);
                    }

                    if (ricerca == daTrovare)
                    {
                        vittoria = true;
                        Scrivi($"Hai vinto con {TENTATIVI_TOTALI - tentativi} lettere sbagliate!\n", lck: _lockConsole);
                    }
                }
                else
                {
                    tentativi--;
                    Scrivi("Lettera non trovata!" + (tentativi != 0 ? $"Hai ancora {tentativi} tentativi!" : "") + "\n", lck: _lockConsole);

                    if (tentativi == 0)
                        Scrivi("Tentativi terminati. Mi spiace, hai perso.\n", lck: _lockConsole);
                }

            } while (!vittoria && tentativi > 0);
        }

        private static string OttieniStringaBase(string daTrovare)
        {
            string ricerca = "";
            foreach (char s in daTrovare)
                ricerca += "-";
            return ricerca;
        }

        static bool ControlloLettera(string s)
        {
            if (!(s.Length == 1))
                return false;

            char a = s[0];
            
            // In ASCII le lettere vanno da
            // A: 65 -> Z: 90
            // a: 97 -> z: 122
            if (
                (a >= 65 && a <= 90) // Se lettera maiuscola
                ||  // oppure
                (a >= 97 && a <= 122) // Se la lettera è minuscola
            )
                return true;

            return false;
        }
    }
}
