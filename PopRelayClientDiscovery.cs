using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//	gr: this won't work for UWP/hololens
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;

[RequireComponent(typeof(PopRelayClient))]
public class PopRelayClientDiscovery : MonoBehaviour {

	public bool						AutoConnectOnDiscovery = true;
	public bool						DisableOnDiscovery = true;
	public UnityEvent_Hostname		OnDiscoveredHost;

	static string	BroadcastForObserverString = "whereisobserverserver";
	static string	BroadcastForGodString = "whoisserver";
	static int		BroadcastPort = (int)PopRelayClient.Role.BroadcastPort;

	string			BroadcastString	{	get{ return (RelayClient.ClientRole == PopRelayClient.Role.Observer) ? BroadcastForObserverString : BroadcastForGodString;	}}
	PopRelayClient	RelayClient { get { return GetComponent<PopRelayClient>(); } }
	UdpClient		Socket;
	IPEndPoint		EndPoint;

	[Range(0.5f,20.0f)]
	public float	BroadcastEveryXSeconds = 5;
	float			BroadcastCountdown = 0;
	public bool		DebugBroadcast = true;

	//	multithread, queue results :/
	List<string>	FoundHosts = null;

	void OnEnable()
	{
		Socket = new UdpClient();
		EndPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
		 Socket.BeginReceive(new AsyncCallback(Receive), null);
		FoundHosts = null;
	}


	void OnDisable()
	{
		Socket.Close();
	}

	void Update()
	{
		BroadcastCountdown -= Time.deltaTime;
		if (BroadcastCountdown <= 0) 
		{
			Broadcast ();
			BroadcastCountdown = BroadcastEveryXSeconds;
		}
	
		if (FoundHosts != null) {
			foreach (var Hostname in FoundHosts) {
				try
				{
					OnFoundHost(Hostname);
				}
				catch(System.Exception e) {
					Debug.LogException (e);
				}
			}
		}

	}

	void OnFoundHost(string Hostname)
	{
		OnDiscoveredHost.Invoke (Hostname);

		if ( AutoConnectOnDiscovery )
		{
			var prc = GetComponent<PopRelayClient>();
			prc.Connect(Hostname);
		}

		if ( DisableOnDiscovery )
			this.enabled = false;
	}


	void Broadcast() 
	{
		byte[] data = Encoding.ASCII.GetBytes(BroadcastString);
		if (DebugBroadcast)
			Debug.Log("Sent broadcast");
		Socket.Send(data, data.Length, EndPoint);
	}


	void Receive(System.IAsyncResult Result)
	{
		//	terminate this session to get the data
		byte[] received = Socket.EndReceive(Result, ref EndPoint);
		var Reply = Encoding.ASCII.GetString(received);

		//	have to queue this for the mono thread in order to do anything useful
		if ( FoundHosts == null )
			FoundHosts = new List<string> ();

		FoundHosts.Add (Reply);
	}


}
