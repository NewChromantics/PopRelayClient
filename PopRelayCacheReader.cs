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

[RequireComponent(typeof(PopRelayClient))]
public class PopRelayCacheReader : MonoBehaviour
{
	public PopRelayClient		Client { get { return GetComponent<PopRelayClient>(); } }
	public TextAsset			Cache;
	public RepeatActionTimer	Timer;

	//	gr: realised a long time ago that the TextAsset reads from disk everytime bytes/string is accessed...
	//	so cache and eat data as we go
	string						CacheData;

	public void PopNextPacket()
	{
		if (CacheData == null)
			CacheData = Cache.text;

		//	find next json chunk
		//	todo: eat whitespace
		var JsonLength = PopX.Json.GetJsonLength(CacheData);
		var Json = CacheData.Substring(0, JsonLength);
		CacheData = CacheData.Substring(JsonLength);

		//	turn it into a packet
		var Packet = new PopMessageText(Json);
		Client.OnMessageText.Invoke(Packet);
	}

	void Update()
	{
		Timer.Update(PopNextPacket);
	}
}
