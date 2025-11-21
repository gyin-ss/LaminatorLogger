using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL240DataLogger
{
    public class CommandHistory
    {
        private List<string> CommandHistories { get; set; } = new List<string>();
        private int HistoryIndex { get; set; } = -1;

        public void PushHistory(string command)
        {
            CommandHistories.Insert(0, command);
            ResetIndex();
        }

        public string NextHistory()
        {
            try
            {
                if (HistoryIndex == -1) HistoryIndex = 0;
                else if (HistoryIndex < CommandHistories.Count - 1) HistoryIndex++;
                else return "";
                var ret = CommandHistories[HistoryIndex];
                return ret;
            }
            catch { return ""; }
        }

        public string PrevHistory()
        {
            try
            {
                if (HistoryIndex == -1) HistoryIndex = 0;
                else if (HistoryIndex > 0) HistoryIndex--;
                else return "";
                var ret = CommandHistories[HistoryIndex];
                return ret;
            }
            catch { return ""; }
        }

        public void ResetIndex()
        {
            HistoryIndex = -1;
        }
    }
}
