using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

using UnityEngine.Events;




//	gr: this class is currently JUST a listener, not a relay like the proper Node.js PopRelayServer
//		consider renaming to... PopRelaySpy, PopRelayTrojan ?
public class PopRelayServer : MonoBehaviour
{
	public UnityEvent_JsonAndDataAndEncoding OnDecodedPacket;

	class ClientConnection : WebSocketBehavior
	{
		PopRelayServer Parent;
		public ClientConnection(PopRelayServer Parent)
		{
			this.Parent = Parent;
		}

		protected override void OnClose(CloseEventArgs e) { Parent.OnClose(this, e); }
		protected override void OnError(ErrorEventArgs e) { Parent.OnError(this, e); }
		protected override void OnMessage(MessageEventArgs e) { Parent.OnMessage(this, e); }
		protected override void OnOpen() { Parent.OnConnected(this); }
	};

	public string GetObserverAddress()
	{
		if (Socket == null)
			return null;
		if (ListenRole != PopRelayClient.Role.Observer)
			return null;

		var Address = Socket.Address.ToString();
		Address += ":";
		Address += Socket.Port;
		return Address;
	}

	public string GetGodAddress()
	{
		if (Socket == null)
			return null;
		if (ListenRole != PopRelayClient.Role.God2018)
			return null;

		var Address = Socket.Address.ToString();
		Address += ":";
		Address += Socket.Port;
		return Address;
	}

	WebSocketServer Socket;


	[Header("Dictates which port we listen to")]
	public PopRelayClient.Role ListenRole = PopRelayClient.Role.God2018;

	//	we just open the god-client port, as we probably only want to listen out for messages from "gods"
	public int Port
	{
		get
		{
			return (int)ListenRole;
		}
	}


	void OnEnable()
	{
		Listen();
	}

	void OnDisable()
	{
		Close();
	}

	void SocketDebug(WebSocketSharp.LogData Data, string Message)
	{
		switch (Data.Level)
		{
			default:
				Debug.Log(Message);
				break;

			case LogLevel.Warn:
				Debug.LogWarning(Message);
				break;

			case LogLevel.Error:
				Debug.LogError(Message);
				break;
		}
				
	}

	void Listen()
	{
		//	already connected
		if (Socket != null)
			return;

		try
		{
			//	we could use .any... but when we never know our address!
			var LocalAddress = PopX.Net.GetLocalAddress();
			Socket = new WebSocketServer(LocalAddress, Port);

			//	reuse address doesn't help.
			//	Socket.ReuseAddress = true;
			Socket.Log.Output = SocketDebug;

			//	bind to messages here...
			System.Func<ClientConnection> AllocService = () =>
			{
				return new ClientConnection(this);
			};
			//	code looks like this should throw with null, but it doesnt...
			Socket.AddWebSocketService<ClientConnection>("/", AllocService);


			Socket.Start();
			if (!Socket.IsListening)
				throw new System.Exception("Failed to start listening");

			Debug.Log("WebsocketServer(" + this.name + ") listening on " + Socket.Port);
		}
		catch
		{
			Close();
			throw;
		}

	}

	void Close()
	{
		if (Socket != null)
		{
			Socket.Stop();
			Debug.Log("WebsocketServer(" + this.name + ") stopped");
			Socket = null;
		}
	}

	void OnClose(ClientConnection Client, CloseEventArgs e)
	{
		Debug.Log("Client close " + e.Reason);
	}

	void OnError(ClientConnection Client, ErrorEventArgs e)
	{
		Debug.Log("Client error " + e.Message);
	}

	void OnMessage(ClientConnection Client, MessageEventArgs e)
	{
		if ( e.IsText )
		{
			var Message = new PopMessageText(e.Data);
			OnMessage(Message);
			//Debug.Log("Client message " + Message.Data);
			return;
		}
		else if ( e.IsBinary )
		{
			var Message = new PopMessageBinary(e.RawData);
			Debug.Log("Client message x" + Message.Data.Length + " bytes");
			return;
		}
		else
		{
			throw new System.Exception("Unhandled client message");
		}
	}

	void OnConnected(ClientConnection Client)
	{
		Debug.Log("Client connected");
	}

	void OnMessage(PopMessageText Packet)
	{
		string Json;
		byte[] Data;
		PopRelayEncoding Encoding;

		PopRelayDecoder.DecodePacket( Packet, out Json, out Data, out Encoding );

		OnDecodedPacket.Invoke(Json, Data, Encoding);
	}

}
