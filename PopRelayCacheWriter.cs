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

	public PopRelayClient	Client { get { return GetComponent<PopRelayClient>(); } }
	public TextAsset		Cache;
#if UNITY_EDITOR
	public string			Filename { get { return AssetDatabase.GetAssetPath(Cache); } }
#else
	public string			Filename	{	get{	return null;	}}
#endif

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
		if (TailDataSize == 0)
		{
			var Json = System.Text.Encoding.UTF8.GetString(Packet.Data);
			WriteCache(Json);
			return;
		}

		//	grab tail data, encode to base 64.
		//	update encoding and inject into json

		//PopRelayDecoder.DecodePacket( Packet, Json, Data, out string Encoding)

	}

}
