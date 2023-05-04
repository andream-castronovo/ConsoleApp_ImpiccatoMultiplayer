using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_ImpiccatoMultiplayer
{
    static class ProtocolliStringhe
    {
        public static string[] ListaProtocolli { get; } = new string[]
        {
            "END_GAME_LOST",
            "END_GAME_WON",
            "START",
            "WRONG"
        };

        public static string RimuoviProtocolli(string s)
        {
            string outp = s;
            foreach (var protocollo in ListaProtocolli)
            {
                if (outp.Contains("<WRONG:") && protocollo == "WRONG")
                {
                    int m = outp.IndexOf("<WRONG:");
                    outp = outp.Substring(0, m) + outp.Substring(m+"<WRONG:".Length+2); 
                }
                else
                    outp = outp.Replace($"<{protocollo}>", "");
            }
            return outp;
        }
    }
}
 