using UnityEngine;
using System.Collections;

public class StateManager : Manager
{
	public string currentState;
	
	private void Start()
	{
		DontDestroyOnLoad(this);
		//ChangeState(TITLE);
	}
	
	private void Update()
	{
		
	}
	
	public void ChangeState(string newState)
	{
		switch(newState)
		{
		case TITLE:
			Application.LoadLevel(newState);
			break;
		}
		
		currentState = newState;
	}
	
	public const string TITLE = "TitleScreen";
}
