using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
//using System.Net;
//using System.Runtime.InteropServices;

namespace GL240Lib
{
	/// <summary>
	/// Parameter for TCP connections [[TCP接続用のパラメータ]]
	/// </summary>
	public class IpAddr
	{
		private uint ipaddr;

		/// <summary>
		//Constructor in a IPApp class [[IPAppクラスのコンストラクタ]]
		/// </summary>
		public IpAddr()
		{
			ipAddr = ConvertInt4ToInt(192, 168, 0, 1);
		}

		/// <summary>
		/// 4 of numerical value of the IP address is changed to the uint form. //4つに分かれたIPAddをひとつのIPAddに変換する]]
		/// </summary>
		public IpAddr(uint a, uint b, uint c, uint d)
		{
			ipAddr = ConvertInt4ToInt(a, b, c, d);
		}

		/// <summary>
		/// IpAdd.の構造体
		/// </summary>
		public uint ipAddr
		{
			get { return ipaddr; }
			set { ipaddr = value; }
		}

		/// <summary>
		/// 4 of numerical value of the IP address is changed to the uint form. [[IPアドレスの4つの数値をuint形式に変換]]
		/// </summary>
		/// <param name="adr1">111.***.***.***</param>
		/// <param name="adr2">***.222.***.***</param>
		/// <param name="adr3">***.***.333.***</param>
		/// <param name="adr4">***.***.***.444</param>
		/// <returns></returns>
		public uint ConvertInt4ToInt(uint adr1, uint adr2, uint adr3, uint adr4)
		{
			uint adr = 0;

			adr = (adr1 << 0) + (adr2 << 8) + (adr3 << 16) + (adr4 << 24);

			return adr;
		}

		/// <summary>
		/// IPAdd of a PC is acquired. [[PCのIPAddを取得する]]
		/// </summary>
		/// <returns></returns>
		public string GetIpAddrStr()
		{
			var ipaddr = new System.Net.IPAddress(ipAddr);
			return ipaddr.ToString();
		}
	}

	/// <summary>
	/// Sub class(TCP) [[サブクラス(Tcp)]]
	/// </summary>
	public class DevIoTcp : DevIoAbs
	{
		TcpClient client;
		NetworkStream stream;
		System.Text.Encoding enc = System.Text.Encoding.GetEncoding("shift-jis");

		public DevIoTcp()
		{
			SetSendWaitTime(0);
		}

		override public String GetClassName()
		{
			return "DevIoTcp";
		}

		override public bool Open(Param param)
		{
			var ret = Connect(param.IpAddr.GetIpAddrStr(), (int)param.PortNumber);
			base.Open(param);
			return ret;
		}

		override public bool Close()
		{
			if (client != null) { client.Close(); }
			if (stream != null) { stream.Close(); }
			return true;
		}

		override public void SendBinary(byte[] bytes,int SendTimeWait=-1)
		{
			stream.WriteTimeout = 1000;
			stream.Write(bytes, 0, bytes.Length);
			base.SendBinary(null, SendTimeWait);
		}

		override public int GetReadSize()
		{
			return client.Available;
		}

		override public byte[] ReadBinary(int bytecounts = -1, int ReadTimeout = -1)
		{
			if (bytecounts == -1) return ReadBinaryAll();

			byte[] bytes = GetBuffer(bytecounts);

			var readsize = bytecounts;
			var readcount = 0;
			stream.ReadTimeout = (ReadTimeout == -1) ? 1000 : ReadTimeout;

			do
			{
				try
				{
					var ret = stream.Read(bytes, readcount, readsize);
					if (ret == 0) return null;
					readsize -= ret;
					readcount += ret;
				}
				catch
				{
					return null;
				}

			} while (readsize > 0);

			return bytes;
		}

		private byte[] ReadBinaryAll(int ReadTimeout = -1)
		{
			var tlist = new List<byte>();

			while (true)
			{
				try
				{
					var b = ReadBinary(1, ReadTimeout);
					if (b == null) goto End;
					tlist.Add(b[0]);

					if (b[0] == 0x0a || b[0] == 0x00)
					{
						goto End;
					}
				}
				catch
				{
					goto End;
				}
			}
		End:
			return tlist.ToArray();
		}

		override public void SendString(string text, int SendTimeWait=-1)
		{
			byte[] bytes = enc.GetBytes(text);
			SendBinary(bytes, SendTimeWait);
		}

		//---- private ----

		/// <summary>
		/// Tcp Connect
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		private bool Connect(string host, int port)
		{
			try
			{
				client = new TcpClient(host, port);
				stream = client.GetStream();
				return true;
			}
			catch
			{
				client = null;
				stream = null;
				return false;
			}
		}

	}
}