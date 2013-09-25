using UnityEngine;
using System.Collections;

public class Manager : MonoBehaviour
{
	protected StateManager stateMan;
	protected GameManager gameMan;
	protected MoveManager moveMan;

	protected void Awake ()
	{
		stateMan = (StateManager)GameObject.FindObjectOfType(typeof(StateManager));
		gameMan = (GameManager)GameObject.FindObjectOfType(typeof(GameManager));
		moveMan = (MoveManager)GameObject.FindObjectOfType(typeof(MoveManager));
	}
}
