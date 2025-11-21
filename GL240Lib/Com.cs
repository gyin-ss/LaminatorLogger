using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO.Ports;

/// <summary>
/// Parameter structure of COM connection exclusive use [[COM接続専用のパラメータ構造体]]
/// </summary>
public struct stComParam
{
	public string portName;
	public Parity parity;
	public StopBits stopBits;
	public int baudRate;
	public int dataBits;
}

namespace GL240Lib
{
	/// <summary>
	/// Parameter for COM connections [[COM接続用のパラメータ]]
	/// </summary>
	public class ComId
	{

		public stComParam ComParam = new stComParam();

		public ComId()
		{
			ComParam.portName = "COM1";
			ComParam.parity = Parity.None;
			ComParam.stopBits = StopBits.One;
			ComParam.baudRate = 38400;
			ComParam.dataBits = 8;
		}
	}

	
	/// <summary>
	/// Sub class(COM) [[サブクラス(COM)]]
	/// </summary>
	public class DevIoCom : DevIoAbs
	{
		SerialPort serialport = new SerialPort();

		public DevIoCom()
		{
			SetSendWaitTime(300);
		}

		override public String GetClassName()
		{
			return "DevIoCom";
		}

		override public bool Open(Param param)
		{
			serialport.Parity = param.ComId.ComParam.parity;
			serialport.BaudRate = param.ComId.ComParam.baudRate;
			serialport.DataBits = param.ComId.ComParam.dataBits;
			serialport.StopBits = param.ComId.ComParam.stopBits;
			serialport.PortName = param.ComId.ComParam.portName;

			try
			{
				serialport.Close();
				serialport.Open();
			}
			catch
			{
				return false;
			}
			return true;
		}

		override public bool Close()
		{
			serialport.Close();
			return true;
		}

		override public void SendBinary(byte[] bytes, int SendTimeWait = -1)
		{
			try
			{
				serialport.Write(bytes, 0, bytes.GetLength(0));
			}
			catch (System.Exception ex)
			{
				throw new Exception(ex.Message);
			}
			base.SendBinary(null, SendTimeWait);
		}

		override public int GetReadSize()
		{
			return serialport.BytesToRead;
		}

		override public byte[] ReadBinary(int bytecounts = -1, int ReadTimeout = -1)
		{
			int Count=0;
			int a = 0;
			bool Flag=false;
			try
			{
				while (true)
				{
					if (a == serialport.BytesToRead)
					{
						Count++;
						System.Threading.Thread.Sleep(10);
						if (Count > 10)
						{
							if (a == 0)
							{
								Flag = true;
							}
							break;
						}
					}
					else
					{
						a = serialport.BytesToRead;
						Count = 0;
					}
				}
				if (Flag == false)
				{
					int counts = (bytecounts == -1) ? serialport.BytesToRead : bytecounts;
					byte[] bytes = new byte[counts];
					int r = 0;
					int offset = 0;
					while (1 == 1)
					{
						r = serialport.Read(bytes, offset, counts);
						offset += r;
						counts -= r;
						if (counts == 0)
						{
							break;
						}
					}

					return bytes;
				}
				else
				{
					return null;
				}
			}
			catch
			{
				return null;
			}
		}
/*
		override public stDeviceList[] GetDeviceList(uint port = 0, string Filter = "", int ctrl=0)
		{
			stDeviceList[] TmpList,ret;
			string[] PortList = SerialPort.GetPortNames();
			int Loop = PortList.Length;
			int Count = 0;
			string Tmp;

			DevIoCom TmpCom = new DevIoCom();
			Param param = new Param();

			TmpList = new stDeviceList[Loop];
			for (int i = 0; i < Loop; i++)
			{
				param.ComId.ComParam.portName = PortList[i];
				bool Flag = TmpCom.Open(param);
				if (Flag == false) continue;

				Tmp = TmpCom.SendQuery("*IDN?");
				TmpCom.Close();
				if (Tmp.Length != 0)
				{
					TmpList[Count].DeviceName = Tmp;
					TmpList[Count].Com_Id = PortList[i];
					TmpList[Count].Usb_Id = 0;
					TmpList[Count].Udp_Id = "";
					TmpList[Count].Port = 0;
					TmpList[Count].Tcp_IpAdd = null;
					TmpList[Count].Udp_IpAdd = null;
					TmpList[Count].Udp_Header = null;			

					Count++;
				}
			}

			ret = new stDeviceList[Count];
			int c = 0;
			string Recv="*IDN GRAPHTEC,";
			int RecvSize =Recv.Length;

			for (int i = 0; i < Count; i++)
			{
				if (TmpList[i].DeviceName != null)
				{
					TmpList[i].DeviceName = TmpList[i].DeviceName.TrimEnd();
					int Point = TmpList[i].DeviceName.IndexOf(Recv);
					int Size = TmpList[i].DeviceName.Length;
					if (Point>=0)
					{
						ret[c].DeviceName = TmpList[i].DeviceName.Substring(RecvSize, Size-RecvSize);
						ret[c].Com_Id = TmpList[i].Com_Id;
						ret[c].Usb_Id = 0;
						ret[c].Udp_Id = "";
						ret[c].Tcp_IpAdd = null;
						ret[c].Udp_IpAdd = null;
						ret[c].Port = 0;
						ret[c].Udp_Header = null;

						ret[c].SelectName = "("+ ret[c].Com_Id + "):" + ret[c].DeviceName;
						c++;
					}
				}
			}

			return ret;
		}

		//---- private ----
*/
	}
	

}
