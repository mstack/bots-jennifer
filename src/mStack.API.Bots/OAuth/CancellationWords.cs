using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.OAuth
{
    public class CancellationWords
    {
        public static List<string> GetCancellationWords()
        {
            string cancellationWords = "QUIT,CANCEL,STOP,GO BACK,RESET,HELP";
            return cancellationWords.Split(',').ToList();
        }
    }
}
