using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player
{
	public int id;
	public UniMoveController move;
	public int score;
	public Color defaultColour;
	public PlayerState state;
	public bool isCharging;
	public bool isShieldActive; //means the shield is up, so the player is invincible
	private bool isHoldingShield; //means the player is holding the shield button
	public int targetId = -1;
	public int attackerId = -1;
	public float attackerOrientation; //stores the orientation of an attacker, to check if a countermove is successful

	private GameManager gameMan;
	private float chargeAmount;
	private float shieldEnergy = 10f;
	private float shieldFadeTime;

	//config constants
	private const float SHIELD_DURATION = 2f;
	private const float CHARGE_CAP = 4000f; //total rotation to charge an attack

	//blink control variables
	private bool isBlinking;
	private Color blinkColor;
	private Color altBlinkColor;
	private float startBlinkTime;
	private float stopBlinkTime;
	private float blinkFrequency;

	//vibration control variables
	private bool isVibrating;
	private float vibrationIntensity;
	private float startVibrationTime;
	private float stopVibrationTime;

	//accelerometer story
	private List<Vector3> accelerationStory;
	private float accStoryLength = 15;

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

	//states
	public enum PlayerState
	{
		IDLE,
		SHIELDING,
		CHARGING,
		ATTACKING,
		COUNTERING
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
		if(active)
		{
			if(shieldEnergy > 0f)
			{
				state = PlayerState.SHIELDING;
				isShieldActive = active;
				isHoldingShield = true;

				shieldFadeTime = SHIELD_DURATION;
				StopGlowing();
				move.SetLED(SHIELD * Mathf.Clamp01(shieldEnergy));
			}
			else
			{
				StartBlinking(1f, .2f, BROKEN_SHIELD, defaultColour * .05f);
				isShieldActive = false;
			}
		}
		else
		{
			isHoldingShield = false;
		}
	}

	private void StopShielding()
	{
		Debug.Log("Shield ended");
		isShieldActive = false;
		isHoldingShield = false;
		state = PlayerState.IDLE;
		StartGlow();
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
			move.SetRumble(.1f + multiplier * .4f);

			if(chargeAmount >= CHARGE_CAP)
			{
				chargeAmount = CHARGE_CAP;
				StartBlinking(10f, .5f, COLORS[targetId]);
				move.SetRumble(1f);
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
			gameMan.LaunchAttack(id, targetId);
		}
		else
		{
			//attack was broken
			gameMan.BreakAttack(id);
		}

		chargeAmount = 0f;
	}

	public void UnleashAttack(int targetId)
	{
		Debug.Log("Attack Unleashed from " + id + " to " + targetId);
		state = PlayerState.ATTACKING;
	}

	public void AttemptCounter()
	{
		//TODO: insert check for orientation
		gameMan.CounteredAttack(attackerId, id);
	}

	public void CounterTime(int initAttackerId, float orientation)
	{
		Debug.Log("Suffering attack from " + initAttackerId + " oriented " + orientation);
		StopChargingAttack();
		StopBlinking();
		StopGlowing();

		//at this point, the user is unable to do anything but counter
		//the gameMan will call SufferAttack to release him and award the attacker if he doesn't counter in time
		state = PlayerState.COUNTERING;
		attackerId = initAttackerId;
		attackerOrientation = orientation;
		move.SetLED(COLORS[attackerId]);
	}

	//RESULT FUNCTIONS
	public void SuccessfulAttack(int pId)
	{
		StartBlinking(2f, .1f, COLORS[pId]);
		StartVibration(2f, 1f);
		state = PlayerState.IDLE;
	}

	public void SufferAttack(int attackerId)
	{
		StopGlowing();
		StartBlinking(2f, .1f, COLORS[attackerId]);
		StartVibration(2f, 1f);
		state = PlayerState.IDLE;
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
			//deplete shield
			shieldEnergy -= Time.deltaTime;

			if(isHoldingShield)
			{
				if(shieldEnergy < 0f)
				{
					StopShielding();
				}
			}
			else
			{
				shieldFadeTime -= Time.deltaTime;

				if(shieldFadeTime > 0f)
				{
					move.SetLED(SHIELD * Mathf.Clamp01(shieldFadeTime));
				}
				else
				{
					//stop shield
					StopShielding();
				}
			}
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
		if(accelerationStory.Count > accStoryLength) { accelerationStory.RemoveAt(0); } //trim the list
	}
}
