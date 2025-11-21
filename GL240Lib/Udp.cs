using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace GL240Lib
{
	/// <summary>
	/// Parameter for UDP connections [[UDP接続用のパラメータ]]
	/// </summary>
	public class UdpMode
	{
		public enum enUdpMode { UdpMode_Nop, UpdMode_Search, UpdMode_Reset };
		private enUdpMode udpmode;

		public enUdpMode Udpmode
		{
			get { return udpmode; }
			set { udpmode = value; }
		}

		public UdpMode()
		{
			Udpmode = enUdpMode.UdpMode_Nop;
		}
	}

	/// <summary>
	/// Sub class(UDP) [[サブクラス(UDP)]]
	/// </summary>
	public class DevIoUdp : DevIoAbs
	{
		protected UdpClient client;
		protected string Host;
		protected int Port;
		System.Text.Encoding enc = System.Text.Encoding.GetEncoding("shift-jis");

		public DevIoUdp()
		{
			SetSendWaitTime(100);
		}

		override public String GetClassName()
		{
			return "DevIoUdp";
		}

		override public bool Open(Param param)
		{
			Connect(param.IpAddr.GetIpAddrStr(), (int)param.PortNumber);
			return true;
		}

		override public bool Close()
		{
			if (client != null) { client.Close(); }
			return true;
		}

		override public void SendBinary(byte[] bytes, int SendTimeWait = -1)
		{
			client.Send(bytes, bytes.Length, Host, Port);
			base.SendBinary(null, SendTimeWait);
		}

		override public int GetReadSize()
		{
			return client.Available;
		}

		override public byte[] ReadBinary(int bytecounts = -1, int ReadTimeout = -1)
		{
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(Host), Port);

			int TimeOutCounter = 10;
			bool TimeOutFlag = true;
			for (int i = 0; i < TimeOutCounter; i++)
			{
				System.Threading.Thread.Sleep(100);
				if (client.Available > 0)
				{
					TimeOutFlag = false;
					break;
				}
			}

			if (TimeOutFlag == false)
			{
				int counts = (bytecounts == -1) ? client.Available : bytecounts;
				byte[] bytes = new byte[counts];

				return client.Receive(ref endpoint);
			}
			return null;
		}

		override public void SendString(string text, int SendTimeWait = -1)
		{
			if (text.Length != 0)
			{
				byte[] bytes = enc.GetBytes(text);
				SendBinary(bytes, SendTimeWait);
			}
		}

		protected UdpSearch mUdpSearch = null;
		override public stDeviceList[] GetDeviceList(uint port = 8023, string Filter = "GL", int ctrl=0)
		{
			stDeviceList[] PortList=null;
			

			switch (ctrl)
			{
				case 0:
					{
						List<PacketSearch> List;

						List = SearchDeviceUdp((uint)port, Filter);	//Port (def=8023)

						int Loop = List.Count;
						PortList = new stDeviceList[Loop];
						char[] c = new char[1];
						c[0] = (char)0;

						for (int i = 0; i < Loop; i++)
						{
							IpAddr ipaddr = new IpAddr();
							ipaddr.ipAddr = List[i].ipaddress;

							PortList[i].DeviceName = List[i].hostname.TrimEnd(c);
							PortList[i].Com_Id = "";
							PortList[i].Usb_Id = 0;
							PortList[i].Udp_Id = ipaddr.GetIpAddrStr() + ":" + string.Format("{0}", port);
							PortList[i].Tcp_IpAdd = null;
							PortList[i].Udp_IpAdd = ipaddr;
							PortList[i].Port = (int)port;
							PortList[i].Udp_Header = List[i].Bytes;

							PortList[i].SelectName = "(" + PortList[i].Udp_Id + "):" + PortList[i].DeviceName;

						}
						break;
					}
				case 1:
					{
						mUdpSearch = SearchDeviceUdpStart((uint)port, Filter);	//Port (def=8023)
						PortList = null;
						break;
					}
				case 2:
					{
						List<PacketSearch> List;

						List = SearchDeviceUdpRepeat();

						int Loop = List.Count;
						PortList = new stDeviceList[Loop];
						char[] c = new char[1];
						c[0] = (char)0;

						for (int i = 0; i < Loop; i++)
						{
							IpAddr ipaddr = new IpAddr();
							ipaddr.ipAddr = List[i].ipaddress;

							PortList[i].DeviceName = List[i].hostname.TrimEnd(c);
							PortList[i].Com_Id = "";
							PortList[i].Usb_Id = 0;
							PortList[i].Udp_Id = ipaddr.GetIpAddrStr() + ":" + string.Format("{0}", port);
							PortList[i].Tcp_IpAdd = null;
							PortList[i].Udp_IpAdd = ipaddr;
							PortList[i].Port = (int)port;
							PortList[i].Udp_Header = List[i].Bytes;

							PortList[i].SelectName = "(" + PortList[i].Udp_Id + "):" + PortList[i].DeviceName;
						}
						break;
					}
				case 3:
					{
						SearchDeviceUdpSend();
						PortList = null;
						break;
					}
				case 4:
					{
						SearchDeviceUdpEnd();
						PortList = null;
						break;
					}
			}
			return PortList;
		}

		//---- private ----

		/// <summary>
		/// Udp Connect
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		private void Connect(string host, int port)
		{
			Host = host;
			Port = port;
			if (client != null)
			{
				client.Close();
			}
			client = new UdpClient();
		}

		override public string GetHostIpPort()
		{
			return Host + ":"+Port.ToString();
		}

		static private UdpSearch Udp = null;
		string mFilter = "";

		private UdpSearch SearchDeviceUdpStart(uint port, string Filter = "GL")
		{
			//List<PacketSearch> packetserach = new List<PacketSearch>();
			SearchDeviceUdpEnd();
			mFilter = Filter;
			Udp = new UdpSearch();
			var param = new Param();
			param.IpAddr.ipAddr = param.IpAddr.ConvertInt4ToInt(255, 255, 255, 255);
			//param.IpAddr.ipAddr = param.IpAddr.ConvertInt4ToInt(192, 168, 230, 0);

			param.PortNumber = port;
			param.UdpMode.Udpmode = UdpMode.enUdpMode.UpdMode_Search;

			if (Udp.Open(param) == false) return null;
			//SearchDeviceUdpSend();	//udp一発目でブロードキャストする場合はコメント外してください
			return Udp;
		}

		private void SearchDeviceUdpSend()
		{
			Udp.SendBinary(null);
		}

		private void SearchDeviceUdpEnd()
		{
			if (Udp != null)
			{
				Udp.Close();
				Udp = null;
			}
		}

		private List<PacketSearch> SearchDeviceUdpRepeat()
		{
			List<PacketSearch> packetserach = new List<PacketSearch>();

			byte[] bytes = Udp.ReadBinary();

			var arraybytes = Packet.Splitter(bytes);
			foreach (var buf in arraybytes)
			{
				var res = PacketSearch.GetAnalyzedPacket(buf);
				res.Bytes = buf;
				if (res.hostname != null)
				{
					if (mFilter.Length == 0)
					{
						packetserach.Add(res);
					}
					else
					{
						if (res.hostname.Substring(0, 2) == mFilter)
						{
							packetserach.Add(res);
						}
					}
				}
			}

			return packetserach;
		}

		/// <summary>
		/// I do a broadcast in a UDP protocol and get equipment information. [[UDPプロトコルにてブロードキャストを行い機器情報を入手する]]
		/// </summary>
		/// <param name="port">Port no. [[ポート番号]]</param>
		/// <returns>PacketSearch arrangement [[PacketSearch配列]]</returns>
		private List<PacketSearch> SearchDeviceUdp(uint port , string Filter="GL")
		{
			List<PacketSearch> packetserach = new List<PacketSearch>();

			var obj = new UdpSearch();
			var param = new Param();
			param.IpAddr.ipAddr = param.IpAddr.ConvertInt4ToInt(255, 255, 255, 255);
			param.PortNumber = port;
			param.UdpMode.Udpmode = UdpMode.enUdpMode.UpdMode_Search;

			if (obj.Open(param) == false) return packetserach;
			obj.SendBinary(null);
			System.Threading.Thread.Sleep(1000);
			byte[] bytes = obj.ReadBinary();
			obj.Close();

			var arraybytes = Packet.Splitter(bytes);
			foreach (var buf in arraybytes)
			{
				var res = PacketSearch.GetAnalyzedPacket(buf);
				res.Bytes = buf;
				if (res.hostname != null)
				{
					if (Filter.Length == 0)
					{
						packetserach.Add(res);
					}
					else
					{
						if (res.hostname.Substring(0, 2) == Filter)
						{
							packetserach.Add(res);
						}
					}
				}
			}

			return packetserach;
		}

	}

	/// <summary>
	/// UdpSearch Class [[Udp検索用のクラス]]
	/// </summary>
	public class UdpSearch : DevIoUdp
	{
		public override void SendBinary(byte[] bytes, int SendTimeWait = -1)
		{
			var index = 0;
			bytes = new byte[30];//256
			bytes[index++] = (byte)'G';
			bytes[index++] = (byte)'R';
			bytes[index++] = (byte)'A';
			bytes[index++] = (byte)'P';
			bytes[index++] = (byte)'H';
			bytes[index++] = (byte)'T';
			bytes[index++] = (byte)'E';
			bytes[index++] = (byte)'C';
			bytes[index++] = (byte)'-';
			bytes[index++] = (byte)'R';
			bytes[index++] = (byte)'D';
			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 0;

			bytes[index++] = 1;
			bytes[index++] = 2;
			bytes[index++] = 3;
			bytes[index++] = 4;

			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 1;

			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 0;
			bytes[index++] = 3;

			bytes[index++] = 0;

			base.SendBinary(bytes, SendTimeWait);
		}

		public override byte[] ReadBinary(int bytecounts = -1, int ReadTimeout = -1)
		{
			var result = new List<byte>();
			byte[] splitter = System.Text.Encoding.ASCII.GetBytes("@@@");

			var timeout = client.Client.ReceiveTimeout;
			client.Client.ReceiveTimeout = 200;
			bool broad = client.EnableBroadcast;

			for (int i = 0; i < 255; i++)
			{
				if (client.Available <= 0) break;
				byte[] rbytes = base.ReadBinary(bytecounts);
				if (rbytes == null) break;
				if (i != 0) result.AddRange(splitter);	// A splitter is inserted. [[スプリッタを挿入]]
				result.AddRange(rbytes);
				System.Threading.Thread.Sleep(10);
			}

			client.Client.ReceiveTimeout = timeout;

			return result.ToArray();
		}
	}

	/// <summary>
	/// Udp Packet Class
	/// </summary>
	public class Packet
	{
		public string gtec;
		public uint transid;
		public uint flag;
		public uint comandcode;

		public Encoding enc = Encoding.ASCII;

		public virtual void Analyze(byte[] bytes)
		{
			if (bytes.Length < 25) return;

			var enc = Encoding.GetEncoding("ASCII");
			gtec = enc.GetString(bytes, 0, 16);

			Array.Reverse(bytes, 16, 4);
			transid = BitConverter.ToUInt32(bytes, 16);

			Array.Reverse(bytes, 20, 4);
			flag = BitConverter.ToUInt32(bytes, 20);

			Array.Reverse(bytes, 24, 4);
			comandcode = BitConverter.ToUInt32(bytes, 24);
		}

		public static Packet GetAnalyzedPacket(byte[] bytes)
		{
			var packet = new Packet();
			packet.Analyze(bytes);
			return packet;
		}

		public static int GetFirstOccurance<T>(T[] byteArray, T byteToFind)
		{
			return Array.IndexOf(byteArray, byteToFind);
		}

		public static List<byte[]> Splitter(byte[] bytes)
		{
			List<byte[]> packet = new List<byte[]>();
			while (true)
			{
				char c = '@';
				int len = GetFirstOccurance(bytes, (byte)c);
				byte[] buf = null;
				if (len == -1)
				{
					buf = bytes;
					packet.Add(buf);
					break;
				}
				else
				{
					buf = new byte[len];
					Array.Copy(bytes, buf, len);
					packet.Add(buf);
					int length = bytes.Length;
					int remlength = length - (len + 3);
					buf = new byte[remlength];
					Array.Copy(bytes, len + 3, buf, 0, remlength);
					bytes = buf;
				}
			}

			return packet;
		}

	}

	/// <summary>
	/// UDP PacketSearch Class
	/// </summary>
	public class PacketSearch : Packet
	{
		public string modelname;
		public string firmversion;
		public string suffix;
		public string hostname;
		public uint ipaddress;
		public uint resetcounts;
		public uint tcpactive;
		public uint usbid;
		public uint maxch;
		public uint maxunit;
		public byte[] Bytes;

		public override void Analyze(byte[] bytes)
		{
			if (bytes.Length < 119) return;

			base.Analyze(bytes);
			modelname = enc.GetString(bytes, 28, 16);
			firmversion = enc.GetString(bytes, 44, 16);
			suffix = enc.GetString(bytes, 60, 16);
			hostname = enc.GetString(bytes, 76, 16);

			ipaddress = BitConverter.ToUInt32(bytes, 92);
			resetcounts = BitConverter.ToUInt32(bytes, 96);
			tcpactive = BitConverter.ToUInt32(bytes, 100);
			usbid = BitConverter.ToUInt32(bytes, 104);
			maxch = BitConverter.ToUInt32(bytes, 108);
			maxunit = BitConverter.ToUInt32(bytes, 112);
		}

		public new static PacketSearch GetAnalyzedPacket(byte[] bytes)
		{
			var packet = new PacketSearch();
			packet.Analyze(bytes);
			return packet;
		}
	}

}


