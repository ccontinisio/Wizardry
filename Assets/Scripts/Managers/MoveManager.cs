using UnityEngine;
using System;
using System.Collections.Generic;

public class MoveManager : Manager
{
	public List<UniMoveController> moves = new List<UniMoveController>();
	
	public void Init()
	{
		Time.maximumDeltaTime = 0.1f;
		int count = UniMoveController.GetNumConnected();
		Debug.Log(UniMoveController.GetNumConnected() + " controllers connected");
		
		for (int i = 0; i < count; i++) 
		{
			UniMoveController move = gameObject.AddComponent<UniMoveController>();	// It's a MonoBehaviour, so we can't just call a constructor
			
			// Remember to initialize!
			if (!move.Init(i)) 
			{	
				Destroy(move);	// If it failed to initialize, destroy and continue on
				continue;
			}
					
			// This example program only uses Bluetooth-connected controllers
			PSMoveConnectionType conn = move.ConnectionType;
			if (conn == PSMoveConnectionType.Unknown || conn == PSMoveConnectionType.USB) 
			{
				Destroy(move);
			}
			else 
			{
				moves.Add(move);
				
				move.OnControllerDisconnected += HandleControllerDisconnected;
			}
		}
	}

	void HandleControllerDisconnected (object sender, EventArgs e)
	{
		// TODO: Remove this disconnected controller from the list and maybe give an update to the player
	}
}
