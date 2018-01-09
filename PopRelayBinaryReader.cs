using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PopRelayBinaryReader : MonoBehaviour
{
	public PopRelayClient		_Client;
	public PopRelayClient		Client { get { return (_Client!=null)?_Client:GetComponent<PopRelayClient>(); } }

	public BasePacket			DataHeader;
	public TextAsset			Data;


	public void PopNextPacket()
	{
		//	make header
		var Json = JsonUtility.ToJson(DataHeader);
		var JsonBytes = System.Text.Encoding.UTF8.GetBytes(Json);

		//	append binary data
		var DataBytes = Data.bytes;

		//	make total packet
		var PacketBytes = new List<byte>();
		PacketBytes.AddRange (JsonBytes);
		PacketBytes.AddRange (DataBytes);
	
		var Packet = new PopMessageBinary (Data.bytes);
		Packet.Data = PacketBytes.ToArray();

		Client.OnMessageBinary.Invoke(Packet);
	}

	void Start()
	{
		PopNextPacket ();
	}
}
