using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InputManager : Manager
{
	private Dictionary<PSMoveButton, int> buttonColors = new Dictionary<PSMoveButton, int>();

	void Start()
	{
		buttonColors[PSMoveButton.Square] = 0;
		buttonColors[PSMoveButton.Triangle] = 1;
		buttonColors[PSMoveButton.Circle] = 2;
		buttonColors[PSMoveButton.Cross] = 3;
	}
	
	void Update()
	{
		int i = 0;
		foreach(UniMoveController mc in moveMan.moves)
		{
			if(mc.GetButtonDown(PSMoveButton.Trigger))
			{ gameMan.SetPlayerShield(i, true); }
			if(mc.GetButtonUp(PSMoveButton.Trigger))
			{ gameMan.SetPlayerShield(i, false); }

			if(mc.GetButtonDown(PSMoveButton.Move))
			{ gameMan.AttemptCountermove(i); }

			if(mc.GetButtonDown(PSMoveButton.Square))
			{ gameMan.StartCharge(i, buttonColors[PSMoveButton.Square]); }
			if(mc.GetButtonDown(PSMoveButton.Triangle))
			{ gameMan.StartCharge(i, buttonColors[PSMoveButton.Triangle]); }
			if(mc.GetButtonDown(PSMoveButton.Circle))
			{ gameMan.StartCharge(i, buttonColors[PSMoveButton.Circle]); }
			if(mc.GetButtonDown(PSMoveButton.Cross))
			{ gameMan.StartCharge(i, buttonColors[PSMoveButton.Cross]); }

			if(mc.GetButtonUp(PSMoveButton.Square))
			{ gameMan.StopCharge(i, buttonColors[PSMoveButton.Square]); }
			if(mc.GetButtonUp(PSMoveButton.Triangle))
			{ gameMan.StopCharge(i, buttonColors[PSMoveButton.Triangle]); }
			if(mc.GetButtonUp(PSMoveButton.Circle))
			{ gameMan.StopCharge(i, buttonColors[PSMoveButton.Circle]); }
			if(mc.GetButtonUp(PSMoveButton.Cross))
			{ gameMan.StopCharge(i, buttonColors[PSMoveButton.Cross]); }

			//read accelerometer values
			if(mc.Acceleration.sqrMagnitude > GameManager.FORCE_LIMIT)
			{
				gameMan.AboveForceLimit(i);
			}

			//read gyroscope values
			gameMan.RotationRegistered(i, mc.Gyro.sqrMagnitude);

			i++;
		}
	}
}
