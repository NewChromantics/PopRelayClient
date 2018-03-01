using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RepeatActionTimer
{
	[Range(1, 1000)]
	public int		ActionsPerSecond = 60;
	float			ActionDelaySecs		{ get { return 1.0f / ActionsPerSecond; 	}}
	float			LastActionTime = 0;
	float			SecsSinceLastAction { get { return Time.time - LastActionTime; } }


	public void		Update(System.Action Action)
	{
		if (SecsSinceLastAction < ActionDelaySecs)
			return;

		LastActionTime = Time.time;
		Action.Invoke();
	}

}



public class PopRelayCacheReader : MonoBehaviour
{
	public PopRelayClient		_Client;
	public PopRelayClient		Client { get { return (_Client!=null)?_Client:GameObject.FindObjectOfType<PopRelayClient>(); } }
	public TextAsset			Cache;
	public RepeatActionTimer	Timer;

	public bool					Loop = false;

	//	gr: realised a long time ago that the TextAsset reads from disk everytime bytes/string is accessed...
	//	so cache and eat data as we go
	byte[]						CacheData;
	int							CachePosition = 0;	//	mega expensive to rewrite the cache string, so search in-place

	//	returns null if we don't consider the following data binary
	int? GetBinaryLength(int StartPosition)
	{
		int NonBinaryCheckCount = 6;
		int? NextBrace = null;
		var AllWhitespace = true;

		//	gr: this needs to cope with prettier json
		var JsonOpening = new byte[] { (byte)'{', (byte)'"' };
			
		//	to make debugging easier, get results, THEN check
		var TestNext = new char[NonBinaryCheckCount];
		var ResultNext = new bool[NonBinaryCheckCount];
				
		for (int i = StartPosition; i < CacheData.Length-NonBinaryCheckCount; i++)
		{
			var OpeningMatch = true;
			for (int v = 0; v<JsonOpening.Length; v++)
				OpeningMatch = OpeningMatch && (CacheData[i+v] == JsonOpening[v] );

			//	if we just hit an opening match, AND it's all been whitespace,($NonBinaryCheckCount chars) then probably filler between json, so bail out
			//	if we check whitespace first, { will turn this chunk into "non whitespace"
			if (OpeningMatch && AllWhitespace)
				break;

			//AllWhitespace = AllWhitespace && PopX.Json.IsWhiteSpace((char)CacheData[i]);
			if (!PopX.Json.IsWhiteSpace((char)CacheData[i]))
				AllWhitespace = false;

			if (OpeningMatch)
			{
				bool IsValidAscii = true;

				for (int v = 0; IsValidAscii && v < NonBinaryCheckCount; v++)
				{
					//	if the next character is not valid json ascii, we're going to assume its more binary and just happens to be a brace
					var TestChar = (char)CacheData[i + v + 1];
					TestNext[v] = TestChar;
					ResultNext[v] = PopX.Json.IsValidJsonAscii(TestChar);
					IsValidAscii = IsValidAscii && ResultNext[v];
				}

				//	this isnt json, keep looking for it
				if (!IsValidAscii)
					continue;

				var DebugString = "Found next chunk of json: ";
				foreach (var Char in TestNext)
					DebugString += Char;
				//Debug.Log(DebugString);
				NextBrace = i;
				break;
			}
		}

		//	if all whitespace, ignore it
		if (AllWhitespace)
			return null;

		if (!NextBrace.HasValue)
			return null;

		var Length = NextBrace.Value - StartPosition;
		return Length;
	}

	public void PopNextPacket()
	{
		if (CacheData == null)
			CacheData = Cache.bytes;

		//	out of data, disable
		if ( CachePosition >= CacheData.Length )
		{
			if (Loop)
				CachePosition = 0;
			else
				this.enabled = false;
			return;
		}

		int JsonLength = 0;
		try
		{
			//	find next json chunk
			JsonLength = PopX.Json.GetJsonLength(CacheData,CachePosition);
		}
		catch(System.Exception e) {
			//	eof
			Debug.LogError ("Stopping parsing of " + this.name + " after exception: " + e.Message);
			//Debug.LogException(e);
			this.enabled = false;
			return;
		}

		//	look to see if there's some binary data afterwards
		var BinaryLength = GetBinaryLength(CachePosition + JsonLength);
		byte[] PacketData = null;

		if ( BinaryLength.HasValue )
		{
			PacketData = new byte[JsonLength + BinaryLength.Value];
			System.Array.Copy(CacheData, CachePosition, PacketData, 0, PacketData.Length);

			CachePosition += PacketData.Length;

			var BinaryPacket = new PopMessageBinary(PacketData);
			Client.OnMessageBinary.Invoke(BinaryPacket);
			return;
		}

		//	extract json string
		PacketData = new byte[JsonLength];
		System.Array.Copy(CacheData, CachePosition, PacketData, 0, PacketData.Length);

		var Json = System.Text.Encoding.UTF8.GetString(PacketData);
		//CacheData.Substring(CachePosition, JsonLength);
		CachePosition += JsonLength;

		//	turn it into a packet
		var Packet = new PopMessageText(Json);
		Client.OnMessageText.Invoke(Packet);
	}

	void Update()
	{
		Timer.Update(PopNextPacket);
	}
}
