using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEvent_JsonAndDataAndEncoding : UnityEvent <string,byte[],PopRelayEncoding> {}


[System.Serializable]
public class BasePacket
{
	public int			Timecode;
	public string		Encoding;
	public string		Data;
	public string		Stream;

	public PopRelayEncoding	GetEncoding()	
	{
		return new PopRelayEncoding (Encoding);
	}
};

//	make this serialisable!
public class PopRelayEncoding
{
	//	known encoding formats, but we allow arbtritry ones
	//	string enum - consider this instead (attribute associated with value) https://stackoverflow.com/a/8588476/355753
	public static class Type
	{
		public const string 
		Pcm8 = "Pcm8",
		Pcm16 = "Pcm16",
		Hex = "Hex",
		Base64 = "Base64",
		OggVorbis = "OggVorbis";
	};

	const char			Seperator = '/';	//	make sure this doesn't intefere with json or other encodings
	const string		SeperatorString = "/";
	static char[] 		SeperatorArray	{	get{	return new char[]{Seperator};}	}	//	get rid of this alloc!

	public int			Count			{	get{return Types.Count;}}
	List<string>		Types;

	public PopRelayEncoding(string Encoding)
	{
		Types = new List<string> (Encoding.Split (SeperatorArray));
	}

	public string		GetString()
	{
		return string.Join (SeperatorString, Types.ToArray ());
	}

	public void			Push(string Type)	
	{
		Types.Insert (0, Type);
	}

	public string		Peek()
	{
		return Types [0];
	}

	public string		FinalEncoding()
	{
		return Types [Types.Count - 1];
	}

	public string		Pop()
	{
		var Head = Types[0];
		Types.RemoveAt (0);
		return Head;
	}
}

[RequireComponent(typeof(PopRelayClient))]
public class PopRelayDecoder : MonoBehaviour {
		

	PopRelayClient	Client	{	get	{ return GetComponent<PopRelayClient> (); }}

	//	gr: maybe make this a list with different encodings to handle... or stream names?
	public UnityEvent_JsonAndDataAndEncoding	OnDecodedPacket;

	void OnEnable()
	{
		Client.OnMessageText.AddListener (HandlePacket);
		Client.OnMessageBinary.AddListener (HandlePacket);
	}

	void OnDisable()
	{
		Client.OnMessageText.RemoveListener (HandlePacket);
		Client.OnMessageBinary.RemoveListener (HandlePacket);
	}

	void HandlePacket(PopMessageText Packet)
	{
		string Json;
		byte[] Data;
		PopRelayEncoding Encoding;
		DecodePacket (Packet, out Json,out Data,out Encoding);
		OnDecodedPacket.Invoke (Json, Data, Encoding);
	}

	void HandlePacket(PopMessageBinary Packet)
	{
		string Json;
		byte[] Data;
		PopRelayEncoding Encoding;
		DecodePacket (Packet,out Json, out Data,out Encoding);
		OnDecodedPacket.Invoke (Json, Data, Encoding);
	}


	static byte HexCharToByte(char Char)
	{
		var b = 0;

		if (Char >= '0' && Char <= '9')
			b = Char - '0';
		if (Char >= 'a' && Char <= 'f')
			b = Char - 'a';
		if (Char >= 'A' && Char <= 'F')
			b = Char - 'A';

		if (b < 0 || b > 15)
			throw new System.Exception ("Hex char out of range: " +Char + "/" + b);

		return (byte)b;
	}

	static byte[] DecodeString(string SomeString)
	{
		//	gr: Not sure webapi string is UTF-8 or ascii!
		return System.Text.Encoding.UTF8.GetBytes(SomeString);
	}

	static byte[] DecodeBase64(string Base64)
	{
		return System.Convert.FromBase64String (Base64);
	}

	static byte[] DecodeHex(string Hex)
	{
		var OutData = new byte[Hex.Length / 2];
		for (var i = 0;	i < Hex.Length / 2;	i++) {
			var a = Hex [(i * 2) + 0];
			var b = Hex[(i * 2) + 1];
			var abyte = HexCharToByte (a);
			var bbyte = HexCharToByte (b);
			var abbyte = (abyte << 4) | (bbyte << 0);
			if (abbyte < 0 || abbyte > 255)
				throw new System.Exception ("Hex converted byte out of range: " + abbyte);
			OutData [i] = (byte)abbyte;
		}
		return OutData;
	}

	//	abstracted so this can be used on a thread
	public static void		DecodePacket(PopMessageText PacketMsg,out string Json,out byte[] Data,out PopRelayEncoding Encoding)
	{
		var Packet = PacketMsg.FromJson<BasePacket> ();

		//	if we've decoded from string to binary, it's here, otherwise in the packet
		byte[] DataBytes = null;

		//	peel off layers of encoding
		Encoding = Packet.GetEncoding();
		while ( Encoding.Count > 1 )
		{
			var HeadEncoding = Encoding.Pop ();

			if (HeadEncoding == "Base64") {
				DataBytes = DecodeBase64(Packet.Data);
				continue;
			}

			if (HeadEncoding == "Hex") {
				DataBytes = DecodeHex (Packet.Data);
				continue;
			}

			throw new System.Exception ("Don't know how to decode " + HeadEncoding);
		}

		//	if the data remaining is a string (in whatever encoding), turn it to bytes.
		//	todo: may want to pass on strings in future to reduce string->byte[]->string
		if (DataBytes == null) {
			DataBytes = DecodeString (Packet.Data);
		}

		Json = PacketMsg.Data;
		Data = DataBytes;
	}

	public static void		DecodePacket(PopMessageBinary Packet,out string Json,out byte[] Data,out PopRelayEncoding Encoding)
	{
		var JsonLength = PopX.Json.GetJsonLength (Packet.Data);

		var JsonBytes = new byte[JsonLength];
		System.Array.Copy (Packet.Data, JsonBytes, JsonBytes.Length);
		Json = System.Text.Encoding.UTF8.GetString(JsonBytes);
		var PacketMeta = JsonUtility.FromJson<BasePacket> (Json);
		Encoding = PacketMeta.GetEncoding ();
			
		Data = new byte[Packet.Data.Length - JsonBytes.Length];
		System.Array.Copy (Packet.Data, JsonBytes.Length, Data, 0, Data.Length);
	}



}
