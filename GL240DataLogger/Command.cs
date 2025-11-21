
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GL240Lib;

namespace GL240DataLogger
{
    public class Command
    {
        internal virtual string Name { get; set; } = "";
        internal virtual string SendCommand { get; set; } = "";
        public virtual string Exec(Func<string, bool, string> func)
        {
            return func(SendCommand, true);
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Command> CommandList { get; set; } = new List<Command>()
        {
            new CommandIdn(),
            new CommandInfo(),
            new CommandOneData(),
            new CommandAmpInput(),
            new CommandSamp1sec(),
            new CommandSamp10sec(),
            new CommandSamp60sec(),
            new CommandMeasStart(),
            new CommandMeasStop(),
        };
    }

    public class CommandIdn : Command
    {
        internal override string Name { get; set; } = "*IDN?";
        internal override string SendCommand { get; set; } = "*IDN?";
    }
    public class CommandInfo : Command
    {
        internal override string Name { get; set; } = "Information";

        public override string Exec(Func<string, bool, string> func)
        {
            var readcommand = "";
            readcommand += func(":INFO:CH?", true);
            readcommand += func(":INFO:VER:MAIN?", true);
            readcommand += func(":INFO:VER:REVISION?", true);
            readcommand += func(":INFO:VER:PAT?", true);
            readcommand += func(":INFO:VER:SYS?", true);
            readcommand += func(":IF:MAC?", true);
            return readcommand;
        }
    }

    public class CommandAmpInput : Command
    {
        internal override string Name { get; set; } = ":AMP:CH<N>? (Amp Information)";
        public override string Exec(Func<string, bool, string> func)
        {
            var readcommand = "";
            for (var ch = 0; ch < 20; ch++)
            {
                readcommand += func($":AMP:CH{ch + 1}?", true);
            }
            return readcommand;
        }
    }

    public class CommandSamp1sec : Command
    {
        internal override string Name { get; set; } = ":DATA:SAMP 1S (Sampling 1 sec)";
        internal override string SendCommand { get; set; } = ":DATA:SAMP 1S;SAMP?";
    }

    public class CommandSamp10sec : Command
    {
        internal override string Name { get; set; } = ":DATA:SAMP 10S (Sampling 10 sec)";
        internal override string SendCommand { get; set; } = ":DATA:SAMP 10S;SAMP?";
    }

    public class CommandSamp60sec : Command
    {
        internal override string Name { get; set; } = ":DATA:SAMP 60S (Sampling 60 sec)";
        internal override string SendCommand { get; set; } = ":DATA:SAMP 60S;SAMP?";
    }

    public class CommandMeasStart : Command
    {
        internal override string Name { get; set; } = ":MEAS:START (Start Recording)";
        internal override string SendCommand { get; set; } = ":MEAS:START";

        public override string Exec(Func<string, bool, string> func)
        {
            var readcommand = func(SendCommand, true);
            int count = 0;
            while (true)
            {
                readcommand = func(":STAT:COND?", true);
                var sp = readcommand.Split(' ');
                if (sp.Length == 2)
                {
                    var stat = int.Parse(sp[1]);
                    if ((stat & 1) == 1) break;
                }

                Thread.Sleep(500);
                if (++count > 10) return "Recording start failure";
            }
            return "Recording started successfully";
        }
    }

    public class CommandMeasStop : Command
    {
        internal override string Name { get; set; } = ":MEAS:STOP (Stop recording)";
        internal override string SendCommand { get; set; } = ":MEAS:STOP";

        public override string Exec(Func<string, bool, string> func)
        {
            var readcommand = func(SendCommand, true);
            int count = 0;
            while (true)
            {
                readcommand = func(":STAT:COND?", true);
                var sp = readcommand.Split(' ');
                if (sp.Length == 2)
                {
                    var stat = int.Parse(sp[1]);
                    if ((stat & 1) == 0) break;
                }

                Thread.Sleep(500);
                if (++count > 10) return "Recording stop failure";
            }
            return "Recording stopped successfully";
        }
    }

    public class CommandOneData : Command
    {
        internal override string Name { get; set; } = ":MEAS:OUTP:ONE? (1 Data reception)";
        internal override string SendCommand { get; set; } = ":MEAS:OUTP:ONE?";

        bool[] selections = new bool[10] { false, false, false, false, false, false, false, false, false, false };

        public void SetChannelSelections(bool[] selected)
        {
            for (int i = 0; i < 10 && i < selections.Length; i++) selections[i] = selected[i];
        }

        public override string Exec(Func<string, bool, string> func)
        {
            var readcommand = func(SendCommand, true);
            var datas = readcommand.Split(',');

            // ch
            readcommand = func(":INFO:CH?", false);
            var sc = readcommand.Split(' ');
            if (sc.Length != 2) return readcommand;
            var maxch = int.Parse(sc[1]);

            var datastr = "";
            try
            {


                for (int ch = 0, index = 0, first = 0; ch < maxch; ch++)
                {
                    if (!selections[ch])
                        continue;

                    readcommand = func($":AMP:CH{ch + 1}?", false);
                    var sc2 = readcommand.Split(';');
                    var inp = "";
                    var input = Input.Factory((inp = sc2.First(x => x.Contains(":INP"))?.Split(' ')?[1]));
                    if (input == null) continue;
                    input.SetRange((inp.Contains("TEMP")) ? sc2.First(x => x.Contains("TEMPR"))?.Split(' ')?[1] : sc2.First(x => x.Contains("RANG"))?.Split(' ')?[1]);
                    datastr += ((first == 0) ? "" : ", ") + $"CH{ch + 1}:{input.Convert(datas[index++])}";
                    first = 1;
                }
            }
            catch (Exception ex)
            {
                datastr = "error: " + readcommand + "; " + ex.ToString(); // gyin added error handling
            }
            return datastr;
        }

        class Input
        {
            public static Input Factory(string input)
            {
                switch (input)
                {
                    case "DC": return new InputDc();
                    case "TEMP": return new InputTemp();
                    case "RH": return new InputRh();
                    default: return null;
                }
            }

            internal class AmpParam
            {
                internal double Div { get; set; } = 0.0;
                internal string Unit { get; set; } = "";
            };

            internal virtual string Range { get; set; }

            internal virtual AmpParam MyParam { get; set; }

            internal virtual Dictionary<string, AmpParam> RangeParamTable { get; set; } = new Dictionary<string, AmpParam>()
            {
                { "", new AmpParam() },
            };

            internal virtual string Data { get; set; }

            internal virtual void SetRange(string range)
            {
                Range = range;
                try
                {
                    MyParam = RangeParamTable[range];
                }
                catch { MyParam = new AmpParam(); }
            }

            internal virtual string Convert(string ad)
            {
                try
                {
                    Data = $"{double.Parse(ad) / MyParam?.Div} {MyParam?.Unit}";
                    return Data;
                }
                catch
                {
                    Data = "*******";
                    return Data;
                }
            }

        }

        class InputDc : Input
        {
            internal override Dictionary<string, AmpParam> RangeParamTable { get; set; } = new Dictionary<string, AmpParam>()
            {
                { "20MV",   new AmpParam() { Div = 1000.0, Unit = "mV" } },
                { "50MV",   new AmpParam() { Div = 400.0, Unit = "mV" } },
                { "100MV",  new AmpParam() { Div = 200.0, Unit = "mV" } },
                { "200MV",  new AmpParam() { Div = 100.0, Unit = "mV" } },
                { "500MV",  new AmpParam() { Div = 40.0, Unit = "mV" } },
                { "1V",     new AmpParam() { Div = 20000.0, Unit = "V" } },
                { "2V",     new AmpParam() { Div = 10000.0, Unit = "V" } },
                { "5V",     new AmpParam() { Div = 4000.0, Unit = "V" } },
                { "10V",    new AmpParam() { Div = 2000.0, Unit = "V" } },
                { "20V",    new AmpParam() { Div = 1000.0, Unit = "V" } },
                { "50V",    new AmpParam() { Div = 400.0, Unit = "V" } },
                { "100V",   new AmpParam() { Div = 200.0, Unit = "V" } },
                { "1-5V",   new AmpParam() { Div = 4000.0, Unit = "V" } },
            };
        }

        class InputTemp : Input
        {
            internal override Dictionary<string, AmpParam> RangeParamTable { get; set; } = new Dictionary<string, AmpParam>()
            {
                { "100",    new AmpParam() { Div = 200.0, Unit = "degC" } },
                { "500",    new AmpParam() { Div = 40.0, Unit = "degC" } },
                { "2000",   new AmpParam() { Div = 10.0, Unit = "degC" } },
            };
        }

        class InputRh : Input
        {
            internal override Dictionary<string, AmpParam> RangeParamTable { get; set; } = new Dictionary<string, AmpParam>()
            {
                { "NONE",     new AmpParam() { Div = 200.0, Unit = "%" } },
            };
        }
    }
}
