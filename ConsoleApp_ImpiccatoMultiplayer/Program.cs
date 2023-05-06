using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
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

            MenuIniziale();
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
                        t = new Thread(IndovinaParola) { Name = "IndovinaParola" };


                    _allThreads.Add(t);
                    t.Start();
                    esci = true;
                }

            }
            
            foreach (Thread t in _allThreads)
            {
                if (t.IsAlive)
                    t.Join();
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


            string ip_porta = Console.ReadLine();

            Server s = new Server(ip_porta.Split(':')[0], int.Parse(ip_porta.Split(':')[1]), 1); // TODO: Controlli IP:PORTA
            s.Start();

            Scrivi("Attendo la connessione di un client... ");
            ServerClient c = s.AspettaUnPlayer();

            Scrivi("Ho una connessione!\n");
            Scrivi("Ora devi scegliere la parola da far indovinare --> ");
            string parola;
            InputParola(out parola);

            parola = parola.ToLower();

            Scrivi($"Parola scelta: {parola}\n");

            string ricerca = OttieniStringaBase(parola);
            s.InviaMessaggioA(ricerca + "<START><EOF>", c);

            bool gameOnGoing = true;
            int tentativi = 6;
            Scrivi("Ora il giocatore dovrà scrivere una lettera alla volta la parola.\n"); // TODO Rivedere le scritte

            do
            {
                string output;

                string input = s.AspettaRispostaClient(c);
                Scrivi("Il client ha scritto: " + input + "\n");

                char inputConvertito = input[0];
                if (parola.Contains(inputConvertito+""))
                {
                    Scrivi("Lettera trovata!\n", fore: ConsoleColor.Green, lck: _lockConsole);
                    for (int i = 0; i < parola.Length; i++)
                    {
                        if (parola[i] == inputConvertito)
                            ricerca = ricerca.Substring(0, i) + parola[i] + ricerca.Substring(i + 1);
                    }

                    output = ricerca + "<EOF>";

                    if (ricerca == parola)
                    {
                        gameOnGoing = false;
                        //Scrivi($"Hai vinto con {TENTATIVI_TOTALI - tentativi} lettere sbagliate!\n", lck: _lockConsole);
                        output = $"\"{ricerca}\": parola trovata! Hai vinto.<END_GAME_WON><EOF>";
                    }


                }
                else
                {
                    tentativi--;
                    output = $"Hai sbagliato! Tentativi rimasti: {tentativi}.<WRONG:{tentativi}><EOF>";

                    // Scrivi("Lettera non trovata!" + (tentativi != 0 ? $"Hai ancora {tentativi} tentativi!" : "") + "\n", lck: _lockConsole);

                    if (tentativi == 0)
                        output = $"Hai finito i tentativi. Hai perso. La parola era: {parola}.<END_GAME_LOST><EOF>";
                }

                s.InviaMessaggioA(output, s.Clients[0]);
                Scrivi("Invio al client: " + output + "\n");

            } while (gameOnGoing && tentativi > 0);

        }

        // CLIENT
        static void IndovinaParola()
        {
            Console.Clear();
            bool inOk = false;
            bool lan = false;
            do
            {
                Scrivi("LAN? (Y/N) -> ");
                string s = Console.ReadLine();

                lan = s.ToLower() == "y";
                inOk = s.ToLower() == "y" || s.ToLower() == "n";
                if (!inOk)
                    Scrivi("Input non valido, puoi scrivere solo \"y\" o \"n\"",fore: ConsoleColor.Red);

            } while (!inOk);

            Scrivi("A quale server vuoi connetterti? SERVER:PORTA -> ");
            string ip_port = Console.ReadLine(); // TODO Fare controlli IP:PORTA
            Client c;
            if (lan && !ip_port.Contains("localhost"))
                c = new Client(IPAddress.Parse("127.0.0.1"), int.Parse(ip_port.Split(':')[1]));
            else
                c = new Client(ip_port.Split(':')[0], int.Parse(ip_port.Split(':')[1]));

            Scrivi("Connesso!\n", fore: ConsoleColor.Green);

            Scrivi("Attendi che l'host scelga la parola..." + "\n");

            bool ok = false;
            do
            {
                c.AspettaRispostaServer();

                if (c.MessaggiDalServer.Peek().Contains("<START>"))
                    ok = true;
                else
                    Scrivi(c.MessaggiDalServer.Dequeue() + "\n");
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
                DisegnaImpiccato(tentativi, 80, 4);
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

                
                c.InviaAlServer(inputConvertito.ToString() + "<EOF>");
                Scrivi("Invio al server: " + inputConvertito.ToString() + "\n");
                c.AspettaRispostaServer();
                string risposta = c.MessaggiDalServer.Dequeue(); // TODO: Si potrebbe implementare un nuovo tag tipo <CHECK>
                                                                 // che viene inviato solo quando il server gli manda se la lettera
                                                                 // è giusta o sbagliata.


                if (risposta.Contains("<WRONG:"))
                {
                    tentativi = int.Parse(risposta.Split(new string[] { "<WRONG:" }, StringSplitOptions.None)[1][0].ToString());
                    DisegnaImpiccato(tentativi, 80, 4);
                }
                    
                else if (risposta.Contains("<END_GAME_WON>"))
                    vittoria = true;
                else if (risposta.Contains("<END_GAME_LOST>"))
                {
                    tentativi = 0;
                    DisegnaImpiccato(tentativi, 80, 4);
                }

                else
                    ricerca = risposta;

                Scrivi(ProtocolliStringhe.RimuoviProtocolli(risposta) + "\n");
                

            } while (!vittoria && tentativi > 0);
        }

        static void DisegnaImpiccato(int tentativiRimasti, int xBase, int yBase)
        {
            /*    ┌──┐
             *    │ \O/
             *    │  |
             *    │ / \
             *    │ 
             */
            // testa 3,1
            // corpo 3,2
            // braccio1 2,1
            // braccio2 4,1
            // gamba1 2,3
            // gamba2 4,3

            string[] standImpiccato = 
                {
                "┌──┐",    // 0,0
                "│",       // 0,1
                "│",       // 0,2
                "│",       // 0,3
                "│" };     // 0,4
            Dictionary<int,Tuple<int,string>> corpoImpiccato = new Dictionary<int, Tuple<int, string>>
            {
                { 0, new Tuple<int,string>(31,"O")},
                { 1, new Tuple<int,string>(32,"|")},
                { 2,new Tuple<int,string>(21,"\\") },
                { 3,new Tuple<int,string>(41,"/") },
                { 4,new Tuple<int,string>(23,"/") },
                { 5,new Tuple<int,string>(43,"\\") },
            };
            for (int i = 0; i<standImpiccato.Length; i++)
            {
                Scrivi(standImpiccato[i], x: xBase, y: yBase+i);
            }

            for (int i = 0; i < 6 - tentativiRimasti; i++)
            {
                Scrivi(corpoImpiccato[i].Item2, x: xBase + corpoImpiccato[i].Item1 / 10, y: yBase + corpoImpiccato[i].Item1 % 10);
            }

            Scrivi("\n");

            
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
