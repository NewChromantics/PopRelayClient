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
	class ClientConnection : WebSocketService
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

	WebSocketServer Socket;


	[Header("Dictates which port we listen to")]
	public PopRelayClient.Role ListenRole = PopRelayClient.Role.God;

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

	void Listen()
	{
		//	already connected
		if (Socket != null)
			return;

		try
		{
			Socket = new WebSocketServer(Port);

			//	bind to messages here...
			WebSocketSharp.Func<ClientConnection> AllocService = () =>
			{
				return new ClientConnection(this);
			};
			Socket.AddWebSocketService<ClientConnection>(null, AllocService);

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
		switch (e.Type)
		{
			case Opcode.TEXT:
				{
					var Message = new PopMessageText(e.Data);
					Debug.Log("Client message " + Message.Data);
					return;
				}
			case Opcode.BINARY:
				{
					var Message = new PopMessageBinary(e.RawData);
					Debug.Log("Client message x" + Message.Data.Length + " bytes");
					return;
				}
			default:
				throw new System.Exception("Unhandled client message type " + e.Type);
		}
	}

	void OnConnected(ClientConnection Client)
	{
		Debug.Log("Client connected");
	}
}
