using System.Runtime.InteropServices;
using System.Text;

namespace GL240Lib
{
    class UsbErrorCode
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INVALID_HANDLE = 6;
        public const int ERROR_NOT_ENOUGH_MEMORY = 8;
        public const int ERROR_HANDLE_EOF = 38;
        public const int ERROR_BUSY = 170;
        public const int ERROR_DEVICE_NOT_CONNECTED = 1167;
        public const int ERROR_NO_MORE_DEVICES = 1248;
        public const int ERROR_TIMEOUT = 1460;
    }

    /// <summary>
    /// The class where I'll can control USB using gtcusbr.dll. [[USBを制御するgtcusbr.dllが使えるようにするクラス]]
    /// </summary>
    public class UsbDll
    {
        /// <summary>
        /// Expansion error information is acquired. [[拡張エラー情報を取得]]
        /// </summary>
        /// <returns>Error Code</returns>
        [DllImport("gtcusbr.dll")]
        public static extern int GtcUSBr_GetLastError();

        /// <summary>
        /// Device Open [[デバイスオープン]]
        /// </summary>
        /// <returns>When succeeding, the handle of the device is returned. When failing, NULL is returned. [[成功時 デバイスのハンドル、失敗時 NULL]]</returns>
        [DllImport("gtcusbr.dll")]
        public static extern int GtcUSBr_OpenDevice();

        /// <summary>
        /// Device Close [[デバイスクローズ]]
        /// </summary>
        /// <param name="hDevice">device Handle [[デバイスハンドル]]</param>
        /// <returns>When succeeding, TRUE is returned.When failing, FALSE is returned. [[成功時 TRUE、失敗時 FALSE]]</returns>
        [DllImport("gtcusbr.dll")]
        public static extern int GtcUSBr_CloseDevice(int hDevice);

        /// <summary>
        /// Synchronous reading [[同期読み込み]]
        /// </summary>
        /// <returns></returns>
        [DllImport("gtcusbr.dll")]
        public static extern int GtcUSBr_ReadDevice(
            int hDevice,
            ref byte lpBuffer,
            int nNumberOfBytesToRead,
            ref int lpNumberOfBytesRead,
            int dwTimeOut
        );

        /// <summary>
        /// Synchronous writing [[同期書き込み]]
        /// </summary>
        /// <returns></returns>
        [DllImport("gtcusbr.dll")]
        public static extern int GtcUSBr_WriteDevice(
            int hDevice,
            ref byte lpBuffer,
            int nNumberOfBytesToWrite,
            ref int lpNumberOfBytesWrite,
            int dwTimeOut
        );
    }

    /// <summary>
    /// Parameter for Usb connections [[Usb接続用のパラメータ]]
    /// </summary>
    public class UsbId
    {
        public UsbId()
        {
            usbid = -1;
        }

        int usbid;

        public int usbId
        {
            get { return usbid; }
            set { usbid = value; }
        }
    }

    /// <summary>
    /// Sub class(USB) [[サブクラス(USB)]]
    /// </summary>
    public class DevIoUsb : DevIoAbs
    {
        int deviceHandle = -1;

        public DevIoUsb()
        {
            SetSendWaitTime(0);
        }

        override public String GetClassName()
        {
            return "DevIoUsb";
        }

        override public bool Open(Param param)
        {
            bool ret = false;
            int usbid = param.UsbId.usbId;
            const int Max = 10;

            var handlelist = new List<int>();

            try
            {
                Close();

                for (int i = 0; i < Max; i++)
                {
                    var handle = OpenDevice();
                    if (handle == -1) break;

                    deviceHandle = handle;
                    //base.Open(param);
                    if (CheckId(usbid, GetId()) == false)
                    {
                        handlelist.Add(handle);
                        deviceHandle = -1;
                        continue;
                    }
                    ret = true;
                    break;
                }

                handlelist.ForEach(x => Close(x));
            }
            catch { ret = false; }

            return ret;
        }

        override public bool Close()
        {
            if (deviceHandle != -1)
            {
                int err = Close(deviceHandle);
                deviceHandle = -1;
            }

            return true;
        }

        private int Close(int handle)
        {
            return UsbDll.GtcUSBr_CloseDevice(handle);
        }

        override public void SendBinary(byte[] bytes, int SendTimeWait = -1)
        {
            int read = 0;
            int timeout = 10000;
            try
            {
                UsbDll.GtcUSBr_WriteDevice(deviceHandle, ref bytes[0], bytes.Length, ref read, timeout);
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }
            base.SendBinary(null, SendTimeWait);
        }

        override public int GetReadSize()
        {
            return 0;
        }

        override public byte[] ReadBinary(int bytecounts = -1, int ReadTimeout = -1)
        {
            const int loopcounts = 100;

            List<byte> list = new List<byte>();

            for (int i = 0; i < loopcounts; i++)
            {
                var result = ReadDevice(ref list, bytecounts, ReadTimeout);
                if (result == 0 && bytecounts != -1 && bytecounts > list.Count) continue;
                break;
            }

            var buffer = list.ToArray<byte>();
            return buffer;
        }

        override public stDeviceList[] GetDeviceList(uint port = 0, string Filter = "", int ctrl = 0)
        {
            stDeviceList[] PortList, TmpList;
            DevIo[] DevIoTable;
            int DevIoTableMax = 10;
            DevIoTable = new DevIo[DevIoTableMax];


            //= new DevIoUsb;
            Param param = new Param();

            int Count = 0;
            string Tmp;
            TmpList = new stDeviceList[DevIoTableMax];
            for (int i = 0; i < DevIoTableMax; i++)
            {
                DevIoTable[i] = new DevIo();
                DevIoTable[i].Create(enIfType.IF_USB);

                param.UsbId.usbId = i;
                bool Flag = DevIoTable[i].Open(param);
                if (Flag == false) continue;

                Tmp = DevIoTable[i].SendQuery("*IDN?");

                if (Tmp.Length != 0)
                {
                    TmpList[i].DeviceName = Tmp;
                    TmpList[i].Com_Id = "";
                    TmpList[i].Usb_Id = i;
                    TmpList[i].Udp_Id = "";
                    TmpList[i].Tcp_IpAdd = null;
                    TmpList[i].Udp_IpAdd = null;
                    TmpList[i].Port = 0;
                    TmpList[i].Udp_Header = null;

                    Count++;
                }
            }

            PortList = new stDeviceList[Count];
            int c = 0;
            string Recv = "*IDN GRAPHTEC,";
            int RecvSize = Recv.Length;

            for (int i = 0; i < DevIoTableMax; i++)
            {
                if (TmpList[i].DeviceName != null)
                {
                    int Point = TmpList[i].DeviceName.IndexOf(Recv);
                    int Size = TmpList[i].DeviceName.Length;
                    if (Point >= 0)
                    {
                        PortList[c].DeviceName = TmpList[i].DeviceName.Substring(RecvSize, Size - RecvSize);
                        PortList[c].Com_Id = "";
                        PortList[c].Usb_Id = TmpList[i].Usb_Id;
                        PortList[c].Udp_Id = "";
                        PortList[c].Tcp_IpAdd = null;
                        PortList[c].Udp_IpAdd = null;
                        PortList[c].Port = 0;
                        PortList[c].Udp_Header = null;

                        Tmp = string.Format("(USB{0}):", i);
                        PortList[c].SelectName = Tmp + PortList[c].DeviceName;

                        c++;
                    }
                }
            }

            //All USB connections held for device list acquisition once are closed. [[一旦デバイスリスト取得の為に開いたUSB接続を全て閉じる]]
            for (int i = 0; i < DevIoTableMax; i++)
            {
                DevIoTable[i].Close();
            }

            return PortList;
        }

        //---- private ----

        private int OpenDevice()
        {
            int result = -1;

            try
            {
                result = UsbDll.GtcUSBr_OpenDevice();
                //DebugSys.DebugLog("GtcUSBr_OpenDevice({0})", result);
            }
            catch (System.Exception ex)
            {
                throw new Exception(ex.Message);
            }

            if (result == 0 || result == UsbErrorCode.ERROR_INVALID_HANDLE)
            {
                return -1;
            }

            return result;
        }

        private int GetId()
        {
            var text = ":IF:ID?" + Environment.NewLine;
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            SendBinary(bytes);

            bytes = ReadBinary();
            string str = System.Text.Encoding.GetEncoding("shift-jis").GetString(bytes, 0, bytes.Length);
            if (str == "" || str == null) return -1;
            str = str.Substring(7);
            int id = int.Parse(str);
            return id;
        }

        private bool CheckId(int srcid, int readid)
        {
            if (srcid == -1) return true;
            if (srcid == readid) return true;
            return false;
        }

        private int ReadDevice(ref List<byte> list, int bytecounts, int readtimeout = -1)
        {
            int readSize = 0;
            int timeout = (readtimeout == -1) ? 10 : readtimeout;
            int packet = (bytecounts == -1) ? 1000 : bytecounts;
            byte[] buf = new byte[packet];
            int result = 0;
            int i;
            for (i = 0; i < packet; i++)
            {
                try
                {
                    result = UsbDll.GtcUSBr_ReadDevice(deviceHandle, ref buf[i], 1, ref readSize, timeout);

                    if (result == 0 || readSize == 0)
                    {
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            readSize = i;

            Array.Resize(ref buf, readSize);
            list.AddRange(buf);

            if (bytecounts != -1) result = 0;

            return result;
        }

        private bool CheckReadBinaryError(int errcode)
        {
            if (errcode != 0) return false;

            // Error
            int lastError = UsbDll.GtcUSBr_GetLastError();
            if ((lastError != UsbErrorCode.ERROR_TIMEOUT) &&
                (lastError != UsbErrorCode.ERROR_SUCCESS))
            {
                // Error
                string message = string.Format("ReadString failed, Last Error: {0}", lastError);
                throw new Exception(message);
            }

            return true;
        }

        /*
                private bool ReadStringLoop(ref string buffer)
                {
                    const int loopcounts = 1;
                    byte c = 0; ;
                    int readSize = 0;
                    const int timeout = 1000;

                    for (int i = 0; i < loopcounts; i++)
                    {
                        int result = 0;
                        try
                        {
                            result = UsbDll.GtcUSBr_ReadDevice(deviceHandle, ref c, 1, ref readSize, timeout);
                        }
                        catch (System.Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                        if (result == 0)
                        {
                            // Error
                            int lastError = UsbDll.GtcUSBr_GetLastError();
                            if (lastError != UsbErrorCode.ERROR_TIMEOUT)
                            {
                                // Error besides the timeout [[タイムアウト以外のエラー]]
                                string message = string.Format("ReadString failed, Last Error: {0}", lastError);
                                throw new Exception(message);
                            }

                            // timeout
                            if (i == loopcounts - 1)
                            {
                                string message = string.Format("ReadString failed by timeout, Last Error: {0}", lastError);
                                throw new Exception(message);
                            }

                        }
                        else
                        {
                            // Normal end [[正常終了]]
                            break;
                        }
                    }

                    // Confirmation of character string termination [[文字列終端の確認]]
                    if (c == 0 || c == 10)
                    {
                        // NUL or LF
                        return false;
                    }
                    // It's added to the character string. [[文字列に追加]]
                    if (c != 13)
                    {
                        // CR ignores it. [[CRは無視]]
                        buffer += Encoding.ASCII.GetString(new byte[] { c });
                    }

                    return true;
                }
        */
    }

}


