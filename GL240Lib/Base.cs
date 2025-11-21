using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GL240Lib
{
    //=====================================================
    // 　Communication mode designation cord [[通信方式指定コード]]
    //=====================================================
    public enum enIfType
    {
        IF_TCP,
        IF_USB,
        IF_COM,
        IF_UDP,
    }

    /// <summary>
    /// The structure to receive a list of the devices which connect with COM, UDP and USB. [[COMやUDPやUSBに繋がっているデバイスのリストを受け取る為の構造体です]]
    /// </summary>
    public struct stDeviceList
    {
        public bool ConnectFlag;    //true =  Effective false = is invalid for information on this stDeviceList. [[true = このstDeviceListの情報は有効である , false = 無効である]]
        public enIfType IfType;     //Connection method with a device (USB,COM,UDP,TCP) [[デバイスとの接続方式(USB,COM,UDP,TCP)]]
        public string SelectName;   //The name when choosing for ComboBox [[ComboBox等で選択する際の名称]]
        public string DeviceName;   //The name to which a device is peculiar [[デバイス固有の名称]]

        public int Usb_Id;          //The recognition ID which is at the time of a Usb connection [[Usb接続時の認識ID]]
        public string Com_Id;       //The recognition ID which is at the time of a Com connection [[Com接続時の認識ID]]
        public string Udp_Id;       //The recognition ID which is at the time of a Udp connection [[Udp接続時の認識ID]]

        public int Port;            //Port information (Tcp/Udp commonness) [[Port情報(Tcp/Udp共通)]]
        public IpAddr Tcp_IpAdd;    //Tcp information (IpAddress) [[Tcp情報(IpAddress)]]

        public IpAddr Udp_IpAdd;    //Udp information (IpAddress) [[Udp情報(IpAddress)]]
        public byte[] Udp_Header;   //Udp information (Header) [[Udp情報(Header)]]
    }

    //=====================================================
    // 　基本クラス群
    //=====================================================

    /// <summary>
    /// The parameter to connect with a device [[デバイスと接続する為のパラメータ]]
    /// </summary>
    /// 
    public class Param
    {
        //string usbCommand;
        uint portNumber;
        ComId comId = new ComId();
        UsbId usbId = new UsbId();
        IpAddr ipAddr = new IpAddr();
        private UdpMode udpMode = new UdpMode();

        public ComId ComId
        {
            get { return comId; }
            set { comId = value; }
        }

        public UsbId UsbId
        {
            get { return usbId; }
            set { usbId = value; }
        }

        public IpAddr IpAddr
        {
            get { return ipAddr; }
            set { ipAddr = value; }
        }

        public uint PortNumber
        {
            get { return portNumber; }
            set { portNumber = value; }
        }

        public UdpMode UdpMode
        {
            get { return udpMode; }
            set { udpMode = value; }
        }
    }

    /// <summary>
    /// Interface [[インターフェイス]]
    /// </summary>
    public interface DevIoIf
    {
        String GetClassName();
        bool isOpen();
        bool Open(Param param);
        bool Close();
        stDeviceList[] GetDeviceList(uint port = 8023, string Filter = "GL", int ctrl = 0);

        void SendCommand(string Command, int SendTimeWait = -1);
        string SendQuery(string Query, int BinarySize = 0, int SendTimeWait = -1, int ReadTimeout = -1);

        void SendBinary(byte[] bytes, int SendTimeWait = -1);
        byte[] ReadBinary(int ByteCounts = -1, int ReadTimeout = -1);
        int GetReadSize();
        void SendString(string text, int SendTimeWait = -1);
        string ReadString(int ReadTimeout = -1);
        string GetHostIpPort();

        void SetBinaryCrPoint(int CrPoint);
        int GetBinaryCrPoint();

        void Clean();
    }

    /// <summary>
    /// A class for debugging. [[デバッグ用のクラスです]]
    /// </summary>
    public class DebugSystem
    {
        public void DebugLog(string format, params object[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                format = format.Replace("{" + i.ToString() + "}", args[i].ToString());
            }
            Trace.WriteLine(format);
        }
    }

    /// <summary>
    /// Parent class [[親クラス]]
    /// </summary>
    public class DevIo
    {
        private DevIoAbs socket;
        private bool isOpenFlag = false;

        /// <summary>
        /// DevIo is created. [[DevIoを生成する]]
        /// </summary>
        /// <param name="type"> A connection method with a device is designated. [[デバイスとの接続方法を指定する]]</param>
        /// <returns>When succeeding, true is returned. hen failing, false is returned. [[成功すればtrueが返る、失敗すればfalseが返る]]</returns>
        public bool Create(enIfType type)
        {
            bool ret = true;
            if (type == enIfType.IF_USB)
            {
                socket = new DevIoUsb();
            }
            else if (type == enIfType.IF_TCP)
            {
                socket = new DevIoTcp();
            }
            else if (type == enIfType.IF_COM)
            {
                socket = new DevIoCom();
            }
            else if (type == enIfType.IF_UDP)
            {
                socket = new DevIoUdp();
            }
            else
            {
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// A device list is acquired. [[デバイスリストを取得する]]
        /// </summary>
        /// <param name="port"> When succeeding, true is returned. hen failing, false is returned. [[UDP接続時にはPortを指定してください]]</param>
        /// <returns></returns>
        public stDeviceList[] GetDeviceList(uint port = 8023, string Filter = "GL", int ctrl = 0)
        {
            return socket.GetDeviceList(port, Filter, ctrl);
        }

        /// <summary>
        /// DevIo, Open, or, it's returned. [[DevIoがOpenされているかを返す]]
        /// </summary>
        /// <returns> Open = true, Close = false </returns>
        public bool isOpen()
        {
            return isOpenFlag;
        }

        /// <summary>
        /// DevIo is opened. [[DevIoをOpenする]]
        /// </summary>
        /// <param name="param"> Parameter structure necessary to Open [[Openに必要なパラメータ構造体]]</param>
        /// <returns>If it succeeds, true is returned. If I fail, false is returned. [[成功したらtrueが返る 失敗したらfalseが返る]]</returns>
        public bool Open(Param param)
        {
            bool ret = false;
            if (socket != null)
            {
                ret = socket.Open(param);
                if (ret == true)
                {
                    isOpenFlag = true;
                }
            }
            return ret;
        }

        /// <summary>
        /// DevIo is Closed. [[DevIoをCloseする]]
        /// </summary>
        /// <returns>If it succeeds, true is returned. If I fail, false is returned. [[成功したらtrueが返る 失敗したらfalseが返る]]</returns>
        public bool Close()
        {
            isOpenFlag = false;
            if (socket != null)
            {
                return socket.Close();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Open, class name of DevIo is acquired (A connection method can be judged by class name.) [[OpenしたDevIoのクラス名を取得する(接続方法がクラス名で判断できます)]]
        /// </summary>
        /// <returns>class name [[クラス名]]</returns>
        public String GetClassName()
        {
            if (socket != null)
            {
                return socket.GetClassName();
            }
            else
            {
                return "";
            }
        }

        public String GetHostIpPort()
        {
            if (socket != null)
            {
                return socket.GetHostIpPort();
            }
            else
            {
                return "";
            }
        }

        public void SetBinaryCrPoint(int CrPoint)
        {
            socket.SetBinaryCrPoint(CrPoint);
        }

        public int GetBinaryCrPoint()
        {
            return socket.GetBinaryCrPoint();
        }

        public int GetReadSize()
        {
            return socket.GetReadSize();
        }

        /// <summary>
        /// A character string is sent. [[文字列を送信する]]
        /// </summary>
        /// <param name="text"> Sent character string [[送信する文字列]]</param>
        public void SendString(string text, int SendTimeWait = -1)
        {
            if (socket != null)
            {
                socket.SendString(text, SendTimeWait);
            }
        }

        /// <summary>
        /// Strings data is Receive. [[文字列を受信する]]
        /// </summary>
        /// <returns>rrangement of received Strings data [[受信した文字列データ]]</returns>
        public string ReadString(int ReadTimeout = -1)
        {
            if (socket != null)
            {
                return socket.ReadString(ReadTimeout);
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// binary is sent. [[バイナリを送信する]]
        /// </summary>
        /// <param name="bytes"> Sent binary data arrangement [[送信するバイナリデータ配列]]</param>
        public void SendBinary(byte[] bytes, int SendTimeWait = -1)
        {
            if (socket != null)
            {
                socket.SendBinary(bytes, SendTimeWait);
            }
        }

        /// <summary>
        /// binary data is Read. [[バイナリを受信する]]
        /// </summary>
        /// <param name="bytecounts"> The size in read binary [[受信するバイナリのバイト数]]</param>
        /// <returns> Arrangement of received binary data [[受信したbinaryデータの配列]]</returns>
        public byte[] ReadBinary(int ByteCounts = -1, int ReadTimeout = -1)
        {
            if (socket != null)
            {
                return socket.ReadBinary(ByteCounts, ReadTimeout);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// A character command is sent. [[文字列コマンドを送信する]]
        /// </summary>
        /// <param name="command"> Sent character command [[送信する文字列コマンド]]</param>
        public void SendCommand(string Command, int SendTimeWait = -1)
        {
            if (socket != null)
            {
                socket.SendCommand(Command.TrimEnd() + Environment.NewLine, SendTimeWait);
            }
        }

        public int GetSendWaitTime()
        {
            return socket.GetSendWaitTime();
        }


        /// <summary>
        /// A character Query is sent. [[文字列クエリを送信する]]
        /// </summary>
        /// <param name="send"> Sent character Query [[送信する文字列クエリ]]</param>
        /// <returns>Return value of a query. [[クエリの戻り値]]</returns>
        public string SendQuery(string Query, int BinarySize = 0, int SendTimeWait = -1, int ReadTimeout = -1)
        {
            string tmp;
            Query = Query.TrimEnd();

            if (Query.TrimEnd().Length != 0)
            {

                int size = Query.Length;
                tmp = Query.Substring(size - 1, 1);
                if (tmp == "?")
                {
                    tmp = socket.SendQuery(Query + Environment.NewLine, BinarySize, SendTimeWait, ReadTimeout);
                    return tmp;
                }
                else
                {
                    SendCommand(Query);
                    return "";
                }
            }
            else
            {
                return "";
            }
        }

        public byte[] SendReadData(string command, int headersize = 8, int statussize = 0, int checksumsize = 0, int timeout = -1)
        {
            return socket.SendReadData(command + Environment.NewLine, headersize, statussize, checksumsize, timeout);
        }

        public void Clean()
        {
            socket.Clean();
        }

        public static string CutStringSpace(string str)
        {
            string s = "";

            try
            {
                var index = str.IndexOf(" ");
                if (index < 0) return str;
                s = str.Substring(index + 1);
            }
            catch
            {
            }

            return s;
        }
    }


    /// <summary>
    /// Abs Super class [[アブソリュート　スーパークラス]]
    /// </summary>
    public class DevIoAbs : DevIoIf
    {
        public DebugSystem DebugSys;
        System.Text.Encoding enc = System.Text.Encoding.GetEncoding("shift-jis");  //gyin
        private int BinaryCrPoint = 16;
        private int mSendWaitTime = 0;

        virtual public bool Close()
        {
            return true;
        }

        virtual public bool isOpen()
        {
            return true;
        }

        virtual public bool Open(Param param)
        {
            Clean();
            return true;
        }

        virtual public String GetClassName()
        {
            return "DevIoAbs";
        }

        virtual public string GetHostIpPort()
        {
            return "";
        }

        virtual public stDeviceList[] GetDeviceList(uint port = 8023, string Filter = "GL", int ctrl = 0)
        {
            return null;
        }

        virtual public void SetBinaryCrPoint(int CrPoint)
        {
            BinaryCrPoint = CrPoint;
        }

        virtual public int GetBinaryCrPoint()
        {
            return BinaryCrPoint;
        }

        virtual public void SendString(string text, int SendTimeWait = -1)
        {
            text = text.TrimEnd() + Environment.NewLine;
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            SendBinary(bytes, SendTimeWait);
        }

        virtual public int GetReadSize()
        {
            return 0;
        }

        virtual public string ReadString(int ReadTimeout = -1)
        {
            try
            {
                var pre = ReadBinary(2, ReadTimeout);
                if (pre == null) return "";
                string header = enc.GetString(pre, 0, pre.Length);
                if (header == "#6")
                {
                    return ReadData(ReadTimeout);
                }
                else
                {
                    //Character string reception [[文字列受信]]
                    var bytes = ReadBinary(ReadTimeout);
                    return $"{header}{enc.GetString(bytes, 0, bytes.Length)}";
                }
            }
            catch { return ""; }
        }

        public string ReadData(int ReadTimeout = -1)
        {
            var list = new List<long>();

            try
            {
                //It was binary receiving, so the data size is acquired. [[バイナリのレシーブだったのでデータサイズを取得する]]
                var size = ReadBinary(6, ReadTimeout);
                if (size == null) return "";
                var dwBytes = int.Parse(System.Text.Encoding.ASCII.GetString(size));
                if (dwBytes == 0) return "";

                //Binary data for the data size is received. [[データサイズ分のバイナリデータを受信する]]
                var data = ReadBinary(dwBytes, ReadTimeout);
                if (data == null || data.Length == 0) return "";
                for (var i = 0; i < data.Length; i += 2)
                {
                    var l = (long)((short)((data[i] << 8) + data[i + 1]));
                    list.Add(l);
                }
            }
            catch { return ""; }

            var ret = "";
            list.ForEach(x => ret += $"{x},");
            return ret;
        }


        virtual public void SendBinary(byte[] bytes, int SendTimeWait = -1)
        {
            //読込処理をoverrideしてください

            //その後base.SendBinary()として以下を実行してください
            if (SendTimeWait == -1)
            {
                SendTimeWait = GetSendWaitTime();
            }
            System.Threading.Thread.Sleep(SendTimeWait);    //GL100の為のWait
            return;
        }

        virtual public byte[] ReadBinary(int ByteCounts = -1, int ReadTimeout = -1)
        {
            return null;
        }

        virtual public void SendCommand(string Command, int SendTimeWait = -1)
        {
            SendString(Command, SendTimeWait);
        }

        virtual public void SetSendWaitTime(int WaitTime = 0)
        {
            mSendWaitTime = WaitTime;
        }

        virtual public int GetSendWaitTime()
        {
            return mSendWaitTime;
        }

        virtual public string SendQuery(string Query, int BinarySize = 0, int SendTimeWait = -1, int ReadTimeout = -1)
        {
            try
            {
                SendCommand(Query, SendTimeWait);
                if (BinarySize == 0)
                {
                    return ReadString();
                }
                else
                {
                    byte[] Binary = ReadBinary(BinarySize, ReadTimeout);
                    if (Binary != null)
                    {
                        return Bin8ToStrEx(Binary, BinarySize);
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            catch { return ""; }
        }

        public byte[] SendReadData(string command, int headersize = 8, int statussize = 0, int checksumsize = 0, int timeout = -1)
        {
            SendCommand(command, timeout);
            int read = headersize;
            var buf = ReadBinary(read, timeout);
            if (buf == null) return null;

            var readlength = GetBinarySizeFromHeader(read, buf);
            if (readlength > 0)
            {
                if (statussize > 0)
                {
                    var stat = ReadBinary(statussize, timeout);
                    if (stat.Length != 2 || stat[0] != 0 || stat[1] != 0) return null;
                }

                var buf2 = ReadBinary(readlength);

                if (checksumsize > 0)
                {
                    var chksum = ReadBinary(checksumsize, timeout);
                    if (chksum.Length != 2 || chksum[0] != 0 || chksum[1] != 0) return null;
                }

                return buf2;
            }

            return null;
        }

        private int GetBinarySizeFromHeader(int headersize, byte[] header)
        {
            if (header.Length == headersize && header[0] == 0x23 && header[1] == 0x36)
            {
                try { return int.Parse(Encoding.ASCII.GetString(header.Skip(2).Take(6).ToArray())); } catch { }
            }

            return 0;
        }

        /// <summary>
        /// Binary data is converted to a text. [[バイナリデータをテキストに変換]]
        /// </summary>
        /// <param name="BinaryData"></param>
        /// <param name="BinarySize"></param>
        /// <param name="crPoint"></param>
        /// <returns></returns>
        static string Bin16ToStrEx(byte[] BinaryData, int BinarySize, int crPoint = 10, int BaseAddress = -1)
        {
            string str = "";
            int k = 0;
            int a = BaseAddress;
            string ss;
            //str += "\r\n";

            for (int i = 0; i < BinarySize; i += 2)
            {
                byte c1 = BinaryData[i];
                byte c2 = BinaryData[i + 1];
                short s = (short)((c1 << 8) + c2);

                if (k != 0)
                {
                    str += ", ";
                }
                else
                {
                    if (BaseAddress != -1)
                    {
                        ss = string.Format("{0:00000000}: ", a);
                        str += ss;
                    }
                }

                ss = string.Format("{0,6}", s);
                str += ss;

                a++;
                k++;
                if (k == crPoint)   //In cr, carriage return [[crで改行]]
                {
                    k = 0;
                    str += "\r\n";
                }
            }
            return str;
        }

        /// <summary>
        /// Binary data is converted to a text. [[バイナリデータをテキストに変換]]
        /// </summary>
        /// <param name="BinaryData"></param>
        /// <param name="BinarySize"></param>
        /// <param name="crPoint"></param>
        /// <returns></returns>
        static string Bin8ToStrEx(byte[] BinaryData, int BinarySize)
        {
            string str = "";
            int k = 0;
            string ss;
            //str += "\r\n";

            for (int i = 0; i < BinarySize; i++)
            {
                byte c1 = BinaryData[i];

                if (k != 0)
                {
                    str += ", ";
                }
                k++;
                ss = string.Format("{0}", c1);
                str += ss;
            }
            return str;
        }

        /// <summary>
        /// Create and retrieve buffer for data storage
        /// </summary>
        public byte[] GetBuffer(int size)
        {
            return new byte[size];
        }

        public virtual void Clean()
        {
            while (true)
            {
                var r = ReadBinary(ByteCounts: 1, ReadTimeout: 10);
                if (r == null || r.Length == 0) break;
            }
        }
    }
}
