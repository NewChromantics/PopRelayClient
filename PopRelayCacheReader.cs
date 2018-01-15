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
	public PopRelayClient		Client { get { return (_Client!=null)?_Client:GetComponent<PopRelayClient>(); } }
	public TextAsset			Cache;
	public RepeatActionTimer	Timer;

	//	gr: realised a long time ago that the TextAsset reads from disk everytime bytes/string is accessed...
	//	so cache and eat data as we go
	string						CacheData;
	int							CachePosition = 0;	//	mega expensive to rewrite the cache string, so search in-place

	public void PopNextPacket()
	{
		if (CacheData == null)
			CacheData = Cache.text;

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

		var Json = CacheData.Substring(CachePosition, JsonLength);
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
