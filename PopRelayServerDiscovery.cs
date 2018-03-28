using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//	gr: this won't work for UWP/hololens
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

[RequireComponent(typeof(PopRelayServer))]
public class PopRelayServerDiscovery : MonoBehaviour {

	static string	BroadcastObserverString = "whereisobserverserver";
	static string	BroadcastGodString = "whoisserver";
	static int		BroadcastPort = (int)PopRelayClient.Role.BroadcastPort;

	UdpClient		Socket;
	IAsyncResult	PendingRecieve = null;	//	we need to explicitly cancel this (ie. stop the thread) when we want to close. Otherwise port stays bound

	PopRelayServer	_Server = null;
	PopRelayServer	Server { get { return (_Server==null) ? (_Server=GetComponent<PopRelayServer>()) : _Server; } }

	void OnEnable()
	{
		Server.GetGodAddress();
		var ListenPort = BroadcastPort;

		//	https://stackoverflow.com/a/759624/355753
		var ListenAddress = new IPEndPoint(IPAddress.Any, ListenPort);
		try
		{
			Socket = new UdpClient();
			Socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			Socket.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.
			Socket.Client.Bind(ListenAddress);
			/*
			Socket = new UdpClient(ListenPort);
			Socket.ExclusiveAddressUse = false;
			Socket.EnableBroadcast = true;
			*/
			StartListening();
		}
		catch
		{
			CloseSocket();
			throw;
		}
	}

	void CloseSocket()
	{
		if (Socket != null)
		{
			//	end recieve needs to not start another...
			/*
			var OldSocket
			if (PendingRecieve != null)
			{
				IPEndPoint EndPoint = null;
				byte[] ReceivedBytes = Socket.EndReceive(PendingRecieve, ref EndPoint);
				PendingRecieve = null;
			}
	*/
			Socket.Close();
			PendingRecieve = null;
			//Socket.Dispose();
			Socket = null;
		}

		if (PendingRecieve != null)
		{
			throw new System.Exception("Pending recieve still present. Socket may be stuck open.");
		}
	}

	void OnDisable()
	{
		CloseSocket();
	}

	void StartListening()
	{
		if ( Socket == null )
			throw new System.Exception("StartListening failed, no socket");

		PendingRecieve = Socket.BeginReceive(new AsyncCallback(Receive), null);
	}

	void Receive(System.IAsyncResult Result)
	{
		//	socket has been closed
		if (Socket == null)
		{
			Debug.LogWarning("Receive aborted, no more socket");
			return;
		}

		if ( Result != PendingRecieve )
			Debug.Log("Got different AsyncResult...");
		PendingRecieve = null;

		//	terminate this session to get the data
		IPEndPoint EndPoint = null;
		//EndPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
		byte[] ReceivedBytes = Socket.EndReceive(Result, ref EndPoint);

		try
		{
			var ReceivedString = Encoding.ASCII.GetString(ReceivedBytes);

			string ResponseString = null;
			if (ReceivedString == BroadcastObserverString)
			{
				ResponseString = Server.GetObserverAddress();
				if (ResponseString == null)
					throw new System.Exception("Not listening for observer");
			}
			else if (ReceivedString == BroadcastGodString)
			{
				ResponseString = Server.GetGodAddress();
				if (ResponseString == null)
					throw new System.Exception("Not listening for god");
			}
			else
			{
				throw new System.Exception("Received non-probe string [" + ReceivedString + "], expected [" + BroadcastObserverString + "/" + BroadcastGodString + "]");
			}

			var ResponseBytes = Encoding.ASCII.GetBytes(ResponseString);
			Socket.Send(ResponseBytes, ResponseString.Length, EndPoint);

		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}

		//	listen again
		StartListening();
	}


}
