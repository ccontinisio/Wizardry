using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : Manager
{
	//score values
	public static int SUCCESSFUL_ATTACK = 100; //bonus for the attacker who deals damage
	public static int SUCCESSFUL_COUNTER = 60; //bonus for the defender who counters an attack successfully
	public static int SUCCESSFUL_BLOCK = 10; //bonus for the defender who uses the shield successfully

	public static int CANCEL_ATTACK = -10; //taken by the attacker who releases the button before unleashing
	public static int INTERRUPTED_ATTACK = -20; //taken by the attacker when anyone stops him physically
	public static int BLOCKED_ATTACK = -30; //taken by the attacker if the defender stops him with the shield
	public static int COUNTERED_ATTACK = -60; //taken by the attacker if the defender counters him
	public static int SUFFER_ATTACK = -60; //taken by a defender who fails to defend himself

	//configuration
	public static int WIN_SCORE = 1000;
	public static float FORCE_LIMIT = 2.5f;
	public static float MAX_ORIENTATION_DIFF = .3f;

	//private variables
	private List<Player> players;
	private float secondsToCounter = 2f;

	//inspector references
	public GameObject cylinder;

	private void Start()
	{
		moveMan.Init();

		//create players
		players = new List<Player>();
		int i = 0;
		foreach(UniMoveController mc in moveMan.moves)
		{
			players.Add(new Player(i, mc, this));
			i++;
		}
	}

	private void Update()
	{
		foreach(Player p in players)
		{
			p.Update();
			if(p.id == 0)
			{
				cylinder.transform.forward = p.GetAveragedAcceleration();
			}
		}
	}

	//Debug
	private void OnGUI()
	{
		float gutter = 10f;
		float tfWidth = 150f;
		float tfHeight = 20f;

		GUI.Label(new Rect(gutter, gutter, tfWidth, tfHeight), players[0].score.ToString());
		GUI.Label(new Rect(gutter, gutter + tfHeight, tfWidth, tfHeight), "State: " + players[0].state);

		GUI.Label(new Rect(Screen.width - tfWidth -gutter, gutter, tfWidth, tfHeight), players[1].score.ToString());
		GUI.Label(new Rect(Screen.width - tfWidth -gutter, gutter + tfHeight, tfWidth, tfHeight), "State: " + players[1].state);

		if(players.Count == 3)
			GUI.Label(new Rect(gutter, Screen.height - gutter - tfHeight, tfWidth, tfHeight), players[2].score.ToString());

		if(players.Count == 4)
			GUI.Label(new Rect(Screen.width -tfWidth -gutter, Screen.height - gutter - tfHeight, tfWidth, tfHeight), players[3].score.ToString());
	}

	//RESULT ACTIONS (usually assign points)
	public void LaunchAttack(int playerId, int targetPlayerId)
	{
		if(players[targetPlayerId].isShieldActive)
		{
			Debug.Log("Parry");
			//attack blocked by the shield
			players[playerId].score += BLOCKED_ATTACK;
			players[targetPlayerId].score += SUCCESSFUL_BLOCK;
			players[playerId].AttackBlocked(targetPlayerId);
		}
		else
		{
			if(players[targetPlayerId].state == Player.PlayerState.CHARGING)
			{
				//attack is automatically successful
				SuccessfulAttack(playerId, targetPlayerId);
			}
			else
			{
				//attack unleashed, the target has secondsToCounter second to reply
				players[playerId].UnleashAttack(targetPlayerId);
				players[targetPlayerId].CounterTime(playerId, players[playerId].GetAveragedYOrientation());
				StartCoroutine(StopCounterTime(playerId, targetPlayerId));

			}
		}
	}

	private IEnumerator StopCounterTime(int attackerId, int defenderId)
	{
		Debug.Log("Start coroutine");
		yield return new WaitForSeconds(secondsToCounter);

		Debug.Log("End coroutine " + players[defenderId].state);
		if(players[defenderId].state == Player.PlayerState.COUNTERING)
		{
			//player failed to counter in time
			SuccessfulAttack(attackerId, defenderId);
		}
	}

	//in case the target doesn't counter in time, or counters wrong
	public void SuccessfulAttack(int attackerId, int targetId)
	{
		players[attackerId].score += SUCCESSFUL_ATTACK;
		players[targetId].score += SUFFER_ATTACK;
		players[attackerId].SuccessfulAttack(targetId);
		players[targetId].SufferAttack(attackerId);
	}

	public void BreakAttack(int playerId)
	{
		players[playerId].score += INTERRUPTED_ATTACK;
		players[playerId].StopChargingAttack();
	}

	public void CounteredAttack(int attackerId, int targetId)
	{
		players[targetId].score += SUCCESSFUL_COUNTER;
		players[attackerId].score += COUNTERED_ATTACK;
		players[targetId].CounteredAnotherPlayerAttack();
		players[attackerId].AttackGotCountered(targetId);
	}

	//INPUT ACTIONS
	public void SetPlayerShield(int playerId, bool active)
	{
		if(players[playerId].state == Player.PlayerState.IDLE
			|| players[playerId].state == Player.PlayerState.SHIELDING)
		{
			players[playerId].SetShield(active);
		}
	}

	public void AttemptCountermove(int playerId)
	{
		if(players[playerId].state == Player.PlayerState.COUNTERING)
		{
			players[playerId].AttemptCounter();
		}
	}

	public void StartCharge(int playerId, int targetId)
	{
		//Debug.Log(playerId + " to " + targetId);
		if(playerId != targetId
		   && players[playerId].state != Player.PlayerState.CHARGING)
		{
			players[playerId].StartChargingAttack(targetId);
		}
	}

	//Release of a player button before unleashing an attack
	public void StopCharge(int playerId, int targetId)
	{
		if(players[playerId].state == Player.PlayerState.CHARGING)
		{
			//player cancelled the attack
			players[playerId].score += CANCEL_ATTACK;
			players[playerId].StopChargingAttack(); //FIXME: stops blinking and vibration after a successful attack
		}
	}

	public void AboveForceLimit(int playerId)
	{
		if(players[playerId].state == Player.PlayerState.CHARGING)
		{
			players[playerId].AboveForceLimit();
		}
	}

	public void RotationRegistered(int playerId, float rotation)
	{
		if(players[playerId].state == Player.PlayerState.CHARGING)
		{
			players[playerId].ChargeAttack(rotation);
		}
	}
}