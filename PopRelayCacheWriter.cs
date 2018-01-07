using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PopRelayCacheWriter : MonoBehaviour
{
	[InspectorButton("ClearCache")]
	public bool				_Clear_Cache;

	public PopRelayClient	_Client;
	public PopRelayClient	Client { get { return (_Client!=null)?_Client:GetComponent<PopRelayClient>(); } }
	public TextAsset		Cache;
#if UNITY_EDITOR
	public string			Filename { get { return AssetDatabase.GetAssetPath(Cache); } }
#else
	public string			Filename	{	get{	return null;	}}
#endif

	void OnEnable()
	{
		Client.OnMessageBinary.AddListener (WriteCache);
		Client.OnMessageText.AddListener (WriteCache);
	}

	void OnDisable()
	{
		Client.OnMessageBinary.RemoveListener (WriteCache);
		Client.OnMessageText.RemoveListener (WriteCache);
	}

	public void ClearCache()
	{
		System.IO.File.WriteAllText( Filename, "" );
	}

	void WriteCache(string Packet)
	{
		var OpeningBrace = '{';
		var ClosingBrace = '}';

		//	do some json verication 
		if (Packet[0] != OpeningBrace)
			throw new System.Exception("Expecting JSON opening brace for cache");
		if (Packet[Packet.Length-1] != ClosingBrace)
			throw new System.Exception("Expecting JSON closing brace for cache");
	
		System.IO.File.AppendAllText(Filename, Packet);

		//	for readability
		System.IO.File.AppendAllText(Filename, "\n\n\n");
	}

	public void WriteCache(PopMessageText Packet)
	{
		System.IO.File.AppendAllText(Filename, Packet.Data);
	}

	public void WriteCache(PopMessageBinary Packet)
	{
		//	see if we need to grab the data from the tail of the packet and re-encode it
		var JsonLength = PopX.Json.GetJsonLength(Packet.Data);

		var TailDataSize = Packet.Data.Length - JsonLength;
		Debug.Log("Binary packet has " + TailDataSize + " data");

		//	all json, just treat it as text
		if (TailDataSize == 0)
		{
			var DataString = System.Text.Encoding.UTF8.GetString(Packet.Data);
			WriteCache(DataString);
			return;
		}

		//	grab tail data, encode to base 64.
		//	update encoding and inject into json
		string Json;
		byte[] Data;
		PopRelayEncoding Encoding;
		PopRelayDecoder.DecodePacket( Packet, out Json, out Data, out Encoding );

		//	encode to base 64
		var Data64 = System.Convert.ToBase64String (Data);
		Encoding.Push(PopRelayEncoding.Type.Base64);
		PopX.Json.Replace (ref Json, "Encoding", Encoding.GetString());
		//Json = PopX.Json.Insert (Json, "Data", Data64);


	}

}
