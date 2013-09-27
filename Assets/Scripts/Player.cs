using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Player
{
	public int id;
	public UniMoveController move;
	public int score;
	public Color defaultColour;
	public PlayerState state;
	public bool isShieldActive;
	public bool isCharging;
	public int targetId = -1;
	public int attackerId = -1;
	public float attackerOrientation; //stores the orientation of an attacker, to check if a countermove is successful

	private GameManager gameMan;
	private float chargeAmount;
	private float shieldEnergy = 1f;

	//config constants
	private const float SHIELD_DURATION = 10f;
	private const float CHARGE_CAP = 4000f; //total rotation to charge an attack

	//blink control variables
	private bool isBlinking;
	private Color blinkColor;
	private Color altBlinkColor;
	private float startBlinkTime;
	private float stopBlinkTime;
	private float blinkFrequency;
	private bool isRainbowBlinking;
	private int currentRainbowColour;

	//vibration control variables
	private bool isVibrating;
	private float vibrationIntensity;
	private float startVibrationTime;
	private float stopVibrationTime;

	//accelerometer story
	private List<Vector3> accelerationStory;
	private const float ACC_STORY_LENGTH = 15;
	private bool detectStillPosition;
	private float STILL_TRESHOLD = 1f; //max different between accelerations in accStory to call it a 'still position'
	private float ORIENTATION_TRESHOLD = .5f; //max difference of position angle during counters

	//glow control
	private bool isGlowing;
	private float startGlowTime;
	private float glowPeriod = 2f;

	//colors
	public static Color RED = Color.red;
	public static Color PINK = Color.magenta;
	public static Color CYAN = Color.cyan;
	public static Color GREEN = Color.green;
	public static Color SHIELD = Color.white;
	public static Color BROKEN_SHIELD = new Color(.2f, .2f, .2f);
	public static Color BLACK = Color.black;
	public static List<Color> COLORS = new List<Color>{PINK, GREEN, RED, CYAN};
	public static Color[] RAINBOW_COLORS = new Color[]{Color.red, Color.yellow, Color.green, Color.blue, Color.magenta, Color.white};

	//states
	public enum PlayerState
	{
		IDLE,
		SHIELDING,
		CHARGING,
		ATTACKING,
		COUNTERING,
		SUCCESSFUL_ATTACK,
		HIT_COOLDOWN
	}

	public Player(int initId, UniMoveController initMove, GameManager gameManReference)
	{
		id = initId;
		move = initMove;
		gameMan = gameManReference;

		defaultColour = COLORS[id];
		state = PlayerState.IDLE;

		accelerationStory = new List<Vector3>();

		StartGlow();
	}

	//PLAYER ACTIONS
	public void SetShield(bool active)
	{
		isShieldActive = active;

		if(active)
		{
			if(shieldEnergy > 0f)
			{
				move.SetLED(SHIELD * shieldEnergy);
				StopGlowing();
				StopBlinking();
				state = PlayerState.SHIELDING;
			}
			else
			{
				StartBlinking(1f, .2f, BROKEN_SHIELD, defaultColour * .05f);
				isShieldActive = false;
			}
		}
		else
		{
			StartGlow();
			state = PlayerState.IDLE;
		}
	}

	public void StartChargingAttack(int intendedTargetId)
	{
		targetId = intendedTargetId;
		state = PlayerState.CHARGING;
		StopGlowing();
		//Debug.Log("target: " + intendedTargetId);
	}

	public void StopChargingAttack()
	{
		targetId = -1;
		chargeAmount = 0f;
		move.SetRumble(0f);
		StartGlow();
		state = PlayerState.IDLE;
		//Debug.Log("Stopped charging");
	}

	public void ChargeAttack(float amount)
	{
		if(chargeAmount < CHARGE_CAP)
		{
			//Debug.Log(chargeAmount);
			chargeAmount += amount;
			float multiplier = chargeAmount / CHARGE_CAP;
			move.SetLED(COLORS[targetId] * multiplier);
			move.SetRumble(multiplier * .3f);

			if(chargeAmount >= CHARGE_CAP)
			{
				chargeAmount = CHARGE_CAP;
				StartBlinking(10f, .5f, COLORS[targetId]);
				move.SetRumble(.3f);
			}
		}
		else
		{
			//Debug.Log("Limit reached");
		}
	}

	public void AboveForceLimit()
	{
		if(chargeAmount >= CHARGE_CAP)
		{
			//if the player charged enough
			//gameMan.LaunchAttack(id, targetId);
			state = PlayerState.ATTACKING;
			detectStillPosition = true;
			//as soon as the player is still, the attack is unleashed
		}
		else
		{
			//attack was broken
			gameMan.BreakAttack(id);
		}

		chargeAmount = 0f;
	}

	//when pressing the Move button during a counter time
	public void AttemptCounter()
	{
		detectStillPosition = true;
	}

	//forced on the player when somebody attacks him
	public void CounterTime(int initAttackerId, float orientation)
	{
		Debug.Log("Suffering attack from " + initAttackerId + " oriented " + orientation);
		StopChargingAttack();
		StopBlinking();
		StopGlowing();

		StartVibration(.5f, .5f); //warning vibration

		//at this point, the user is unable to do anything but counter
		//the gameMan will call SufferAttack to release him and award the attacker if he doesn't counter in time
		state = PlayerState.COUNTERING;
		attackerId = initAttackerId;
		attackerOrientation = orientation;
		move.SetLED(COLORS[attackerId]);
	}

	//stillness detected
	private void PlayerIsStill()
	{
		detectStillPosition = false;
		float yPos = move.Acceleration.y;

		if(state == PlayerState.ATTACKING)
		{
			//unleashing attack
			gameMan.UnleashAttack(id, targetId, yPos);
			Debug.Log("Attack Unleashed from " + id + " to " + targetId + " yPos: " + yPos);
		}
		if(state == PlayerState.COUNTERING)
		{
			//attempting defence
			if(Mathf.Abs(attackerOrientation - yPos) < ORIENTATION_TRESHOLD)
			{
				gameMan.CounteredAttack(attackerId, id);
			}
			else
			{
				gameMan.SuccessfulAttack(attackerId, id);
			}
			Debug.Log("Defending " + yPos);
		}
	}

	//called from a coroutine when the player deals a successful attack, or when he takes a hit
	public void BackToIdle()
	{
		isRainbowBlinking = false;

		StartGlow();
		state = PlayerState.IDLE;
	}

	//RESULT FUNCTIONS
	public void SuccessfulAttack(int targetId)
	{
		state = PlayerState.SUCCESSFUL_ATTACK;
		StartRainbowBlinking(3f);
		StartVibration(2f, 1f);
		state = PlayerState.IDLE;
	}

	public void SufferAttack(int attackerId)
	{
		state = PlayerState.HIT_COOLDOWN;
		StopGlowing();
		StartBlinking(2f, 1f, Color.black, Color.black);
	}

	public void AttackBlocked(int attackerId)
	{
		StartBlinking(2f, .1f, COLORS[attackerId], SHIELD);
		StartVibration(2f, 1f);
		state = PlayerState.IDLE;
	}

	public void CounteredAnotherPlayerAttack()
	{
		StartBlinking(2f, .1f, COLORS[attackerId], SHIELD); //TODO: change colours?
		StartVibration(2f, 1f);
		attackerId = -1;
		state = PlayerState.IDLE;
	}

	public void AttackGotCountered(int defenderId)
	{
		StartBlinking(2f, .1f, COLORS[defenderId], SHIELD); //TODO: change colours?
		StartVibration(2f, 1f);
		state = PlayerState.IDLE;
	}

	//UTILITY FUNCTIONS
	public void StartBlinking(float duration, float initBlinkFrequency, Color initColour)
	{
		StartBlinking(duration, initBlinkFrequency, initColour, BLACK);
	}

	public void StartBlinking(float duration, float initBlinkFrequency, Color initColour, Color initAltColor)
	{
		StopGlowing();

		blinkColor = initColour;
		startBlinkTime = Time.time;
		stopBlinkTime = startBlinkTime + duration;
		blinkFrequency = initBlinkFrequency;
		altBlinkColor = initAltColor;

		isBlinking = true;
	}

	public void StartRainbowBlinking(float duration)
	{
		StopGlowing();
		StopBlinking();

		startBlinkTime = Time.time;
		stopBlinkTime = startBlinkTime + duration;
		blinkFrequency = .1f;

		isRainbowBlinking = true;
		currentRainbowColour = 0;
	}

	public void StopBlinking()
	{
		isBlinking = false;
	}

	public void StartGlow()
	{
		move.SetLED(Color.black);
		startGlowTime = Time.time;
		isGlowing = true;
	}

	public void StopGlowing()
	{
		isGlowing = false;
	}

	public void StartVibration(float duration, float intensity)
	{
		vibrationIntensity = intensity;
		stopVibrationTime = Time.time + duration;
		move.SetRumble(intensity);
		
		isVibrating = true;
	}

	//returns a Vector3 averaged over the course of no: accStoryLength frames
	public Vector3 GetAveragedAcceleration()
	{
		Vector3 total = Vector3.zero;
		foreach(Vector3 acc in accelerationStory)
		{
			total += acc;
		}
		return total / accelerationStory.Count;
	}

	//returns the y component of the averaged acceleration
	public float GetAveragedYOrientation()
	{
		Vector3 acc = GetAveragedAcceleration();
		return acc.y;
	}
	
	public void Update()
	{
		if(isShieldActive)
		{
			if(shieldEnergy > 0f)
			{
				shieldEnergy -= Time.deltaTime / SHIELD_DURATION;
				move.SetLED(SHIELD * shieldEnergy);
			}
			if(shieldEnergy < 0f)
			{
				shieldEnergy = 0f;
				SetShield(false);
			}
		}
		else
		{
			//Shield recharging
			/*if(shieldEnergy < 1f)
			{
				shieldEnergy += Time.deltaTime / SHIELD_DURATION / 2f;
			}
			if(shieldEnergy > 1f)
			{
				shieldEnergy = 1f;
			}*/
		}

		//blinking routine
		if(isBlinking)
		{
			if(Time.time <= stopBlinkTime)
			{
				if((Time.time - startBlinkTime)%blinkFrequency*2f <= blinkFrequency)
				{
					move.SetLED(blinkColor);
				}
				else
				{
					move.SetLED(altBlinkColor);
				}
			}
			else
			{
				StopBlinking();
				StartGlow();
			}
		}

		//rainbow routine
		if(isRainbowBlinking)
		{
			if(Time.time <= stopBlinkTime)
			{
				if((Time.time - startBlinkTime)%blinkFrequency <= blinkFrequency)
				{
					move.SetLED(RAINBOW_COLORS[currentRainbowColour]);
					
					currentRainbowColour++;
					if(currentRainbowColour == RAINBOW_COLORS.Length)
					{
						currentRainbowColour = 0;
					}
				}
			}
			else
			{
				BackToIdle();
			}
		}

		//vibration routine
		if(isVibrating)
		{
			if(Time.time >= stopVibrationTime)
			{
				move.SetRumble(0f);
				isVibrating = false;
			}
		}

		//glowing routine
		if(isGlowing)
		{
			float multiplier = (Mathf.Abs(Mathf.Sin(Time.time*glowPeriod)) * .05f) + .01f;
			Color desiredColor = defaultColour * multiplier;
			move.SetLED(desiredColor);
		}

		//record last accelerations
		accelerationStory.Add(move.Acceleration);
		if(accelerationStory.Count > ACC_STORY_LENGTH) { accelerationStory.RemoveAt(0); } //trim the list

		//try to understand when the user is still (for counters/attacks)
		if(detectStillPosition)
		{
			bool isStill = false;
			for(int i = accelerationStory.Count-1; i > 1; i--)
			{
				if((accelerationStory[i] - accelerationStory[i-1]).sqrMagnitude > STILL_TRESHOLD)
				{
					Debug.LogWarning("Not still at pos: " + i + " and acc: " + (accelerationStory[i] - accelerationStory[i-1]).sqrMagnitude);
					isStill = false;
					break;
				}
				isStill = true;
			}

			if(isStill)
			{
				PlayerIsStill(); //will result in either an attack, or a counter (depending on the state)
			}
		}
	}
}
