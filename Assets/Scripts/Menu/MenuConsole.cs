using System.Collections;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuConsole: TypingInterface
{
	int selected_scenario = -1;

	public override void Awake()
	{
		base.Awake();
		string name = PlayerPrefs.GetString("defaultName");
		if (name == "falken" || name == "joshua")
		{
			WriteOut("greetings professor falken");

		}
		else
		{
			WriteOptions();
		}

		Simulator.tutorialOverride = false;
	}

	void WriteOptions()
	{
		WriteOut("_______________________________________", false);
		WriteOut("select a simulation to load", false);
		WriteOut("");
		for(int i = 0; i < Simulator.scenarios.Count; i++) {
			Simulator.scenarios[i].completed = PlayerPrefs.GetInt(Simulator.scenarios[i].name, 0) == 1;
			if (Simulator.scenarios[i].completed) {
				WriteOut(Simulator.scenarios[i].name);
			}
			else if(i > 0){
				if (Simulator.scenarios[i - 1].completed) {
					WriteOut(Simulator.scenarios[i].name);
				}
			}
			else {
				WriteOut(Simulator.scenarios[i].name);
			}

		}
		WriteOut("");
		WriteOut("multiplayer");
		//WriteOut("");
		//WriteOut("type 'help' for more info", false);
		WriteOut("_______________________________________", false);
		WriteOut("");
	}

	public override void ProcessText(string message)
	{
		message = message.Replace("\u200B", "");

		if (message.Contains("quit", System.StringComparison.CurrentCultureIgnoreCase))
		{
			Application.Quit();
			return;
		}

		if (message.Contains("multiplayer", System.StringComparison.CurrentCultureIgnoreCase))
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized) {
				RelayDude.ins.StartMultiplayer();
			}


			WriteBracket();
			WriteOut("");
			WriteOut("launching multiplayer");
			WriteOut("");
			WriteOut("'host' to start a new game");
			WriteOut("'join joincode' to join a friends lobby");
			WriteOut("'shutdown' to disconnect");
			WriteOut("no public lobbies yet");
			WriteOut("");
			WriteBracket();
			//RelayDude.ins.StartMultiplayer();
			return;
		}
		if (message.Contains("host", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("hosting");
			RelayDude.ins.StartGame();
			return;
		}
		if (message.Contains("shutdown", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("shutting down multiplayer");
			NetworkManager.Singleton.Shutdown();
			return;
		}
		if (message.Contains("start", System.StringComparison.CurrentCultureIgnoreCase) && RelayDude.ins.hosting)
		{
			if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
			{
				LoadGame(true);
			}
			else
			{
				WriteOut("no opposing players, cannot start yet");
			}
			return;
		}
		if (message.Contains("join", System.StringComparison.CurrentCultureIgnoreCase))
		{
			int f = message.IndexOf(' ') + 1;
			string joincode = message[f..];
			Debug.Log("parsed this joincode = " + joincode);
			WriteOut("trying code..." + joincode.ToLower());
			RelayDude.ins.JoinGame(joincode);
			return;
		}
		if (message.Contains("hello joshua", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("greetings professor falken");
			WriteOut("how about a nice game of chess");
			return;
		}
		if (message.Contains("name =", System.StringComparison.CurrentCultureIgnoreCase)
		|| message.Contains("name=", System.StringComparison.CurrentCultureIgnoreCase))
		{
			int f = message.IndexOf('=') + 1;
			string name = message[f..];
			name = name.Replace(" ", "");
			if (MultiplayerVariables.ins != null)
			{
				MultiplayerVariables.ins.localName = name;
				ulong c = NetworkManager.Singleton.LocalClientId;
				MultiplayerVariables.ins.UpdateStateNameServerRPC(c, name);
			}
			PlayerPrefs.SetString("defaultName", name);
			WriteOut("name set to " + name);
			WriteOut("");
			if (name == "falken" || name == "joshua")
			{
				WriteOut("greetings professor falken");
				WriteOut("how about a nice game of chess");
			}
			return;
		}
		if (message.Contains("tears in rain", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("time to die");
			return;
		}
		if (message.Contains("repeat"))
		{
			if (RelayDude.ins.hosting)
			{
				WriteBracket();
				WriteOut("currently hosting a game");
				WriteOut("joincode = " + RelayDude.ins.joinCode.ToLower());
				WriteOut("number of players = " + NetworkManager.Singleton.ConnectedClientsList.Count);
				WriteOut("type 'start' to start game");
			}
			else if (NetworkManager.Singleton.IsConnectedClient)
			{
				WriteBracket();
				WriteOut("currently connected to a server");
				WriteOut("joincode = " + RelayDude.ins.joinCode.ToLower());
				WriteOut("waiting for host to begin...");
			}
			else
			{
				WriteOptions();
			}
			return;
		}
		if (message.Contains("list games"))
		{
			WriteOut("tic-tac-toe");
			WriteOut("falkens maze");
			WriteOut("black jack");
			WriteOut("gin rummy");
			WriteOut("hearts");
			WriteOut("bridge");
			WriteOut("checkers");
			WriteOut("chess");
			WriteOut("poker");
			WriteOut("fighter combat");
			WriteOut("guerrilla engagement");
			WriteOut("desert warfare");
			WriteOut("air-to-ground actions");
			WriteOut("theaterwide tactical warfare");
			WriteOut("biotoxic and chemical warfare");
			WriteOut("");
			WriteOut("global thermonuclear war");
			WriteOut("");
			return;
		}
		if (message.Contains("back"))
		{
			WriteOut("you are already in the root menu", false);
			return;
		}
		if (message.Contains("help"))
		{
			WriteOut("_______________________________________");
			WriteOut("");
			WriteOut("general commands");
			WriteOut("");
			WriteOut("'repeat' to see current options");
			//WriteOut("'back' to go back");
			WriteOut("'controls' for list of controls");
			WriteOut("'quit' to exit");
			WriteOut("'help' for commands");
			WriteOut("");
			WriteOut("_______________________________________");
			return;
		}
		if (message.Contains("controls"))
		{
			WriteOut("_______________________________________");
			WriteOut("");
			WriteOut("simulation controls");
			WriteOut("");
			WriteOut("arrow keys - menu navigation");
			WriteOut("spacebar or return - select");
			WriteOut("tab - back");
			WriteOut("w, a, s, d - pan camera"); 
			WriteOut("q, e - zoom camera");
			WriteOut("");
			WriteOut("_______________________________________");
			return;
		}

		//generalized scenario loading
		for (int i = 0; i < Simulator.scenarios.Count; i++)// Scenario sc in Simulator.scenarios)
		{
			Scenario sc = Simulator.scenarios[i];
			if (message.Contains(sc.name, System.StringComparison.CurrentCultureIgnoreCase))
			{
				if (message.Contains("load"))
				{
					Simulator.activeScenario = sc;
					Simulator.tutorialOverride = sc.tutorial > 0;
					LoadGame();
					return;
				}
				selected_scenario = i;
				WriteBracket();
				WriteOut("");
				WriteOut(sc.description);
				WriteOut("");
				WriteOut("'load' to load scenario");
				WriteOut("");
				WriteBracket();
				return;
			}

		}
		//tutorial reuses scenario a, so I have to put this here
		//if (message.Contains("tutorial", System.StringComparison.CurrentCultureIgnoreCase))
		//{
		//	if (message.Contains("load"))
		//	{
		//		Simulator.activeScenario = Simulator.scenarios[0];
		//		Simulator.tutorialOverride = true;
		//		LoadGame();
		//	}
		//	Simulator.activeScenario = Simulator.scenarios[0];
		//	Simulator.tutorialOverride = true;
		//	selected_scenario = 0;
		//	WriteBracket();
		//	WriteOut("");
		//	WriteOut("the tutorial scenario introduces the player to cities, growth, countries, and pre-emptive nuclear strikes");
		//	WriteOut("");
		//	WriteOut("'load' to load scenario");
		//	WriteOut("");
		//	WriteBracket();
		//	return;
		//}


		if (message.Contains("load") && selected_scenario != -1)
		{
			Simulator.activeScenario = Simulator.scenarios[selected_scenario];
			Simulator.tutorialOverride = Simulator.activeScenario.tutorial > 0;
			LoadGame();
			return;
		}
		if (message.Contains("resetprogress"))
		{
			PlayerPrefs.DeleteAll();
			ClearConsole();
			WriteOut("progress has been reset");
			WriteOptions();
			return;
		}
		WriteOut("unknown command");
		//selected_scenario = -1;
	}

	string GreenText(string message)
	{
		return "<color=\"green\">" + message + "</color>";
	}


	void LoadGame(bool online = false)
	{
		if (online)
		{

			NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
			return;
		}
		Destroy(NetworkManager.Singleton.gameObject);
		Destroy(MultiplayerVariables.ins);
		lockout = true;
		InvokeRepeating(nameof(Dot), 0, 0.01f);
		StartCoroutine(nameof(AsyncLoadScene), 1);
	}

	IEnumerator AsyncLoadScene(float delay = 1)
	{

		float startTime = Time.unscaledTime;

		AsyncOperation asyncLoad;
		asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
		asyncLoad.allowSceneActivation = false;
		//wait until the asynchronous scene fully loads
		while (!asyncLoad.isDone)
		{
			//scene has loaded as much as possible,
			// the last 10% can't be multi-threaded

			if (asyncLoad.progress >= 0.9f && Time.unscaledTime - startTime > delay)
			{
				asyncLoad.allowSceneActivation = true;
			}
			yield return null;
		}

		AsyncOperation asyncUnLoad;
		asyncUnLoad = SceneManager.UnloadSceneAsync("Menu");
		while (!asyncLoad.isDone)
		{
			Debug.Log("unloading " + asyncUnLoad.progress);
			yield return null;
		}
	}
}
