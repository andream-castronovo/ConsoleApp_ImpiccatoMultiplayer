using System;

namespace ConsoleApp_PonteLevatoio
{
    static class MyConsoleUtils
    {
        // Programmato da Andrea Maria Castronovo - 4°I - Data consegna: 15/04/2023

        /// <summary>
        /// Metodo per scrittura in console aggiornato con utilizzo dei lock per sincronizzazione thread
        /// </summary>
        /// <param name="testo">Testo da stampare</param>
        /// <param name="lck">Lock da utilizzare per sincronizzare</param>
        /// <param name="x">Distanza dal bordo sinistro da cui stampare</param>
        /// <param name="y">Distanza dal bordo in alto da cui stampare</param>
        /// <param name="fore">Colore foreground, grigio di default</param>
        /// <param name="back">Colore background, nero di default</param>
        public static void Scrivi(string testo, object lck = null, int x = -1, int y = -1, ConsoleColor fore = ConsoleColor.Gray, ConsoleColor back = ConsoleColor.Black)
        {
            // Se x o y sono omesse, imposta la posizione del cursore
            if (x == -1)
                x = Console.CursorLeft;
            if (y == -1)
                y = Console.CursorTop;

            // Se coordinate maggiori della grandezza di buffer, non scrivere
            if (x >= Console.BufferWidth || y >= Console.BufferHeight)
                return;

            // Se il lock non è specificato, scrivi senza lock
            if (lck == null)
            {
                Console.ForegroundColor = fore;
                Console.BackgroundColor = back;
                Console.SetCursorPosition(x, y);
                Console.Write(testo);
            }
            else
                lock (lck)
                {
                    Console.ForegroundColor = fore;
                    Console.BackgroundColor = back;
                    Console.SetCursorPosition(x, y);
                    Console.Write(testo);
                }

            ResettaColori();
        }

        public static void ResettaColori()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }
}
