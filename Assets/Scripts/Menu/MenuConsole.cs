using System.Collections;
using System.Drawing;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuConsole : TypingInterface
{
	int selected_scenario = -1;
	public static bool seenTitle = false;
	bool anykey;

	public override void Awake()
	{
		base.Awake();
		SFX.globalVolume = PlayerPrefs.GetFloat("volume", 5);
		string name = PlayerPrefs.GetString("defaultName");
		if (name == "falken" || name == "joshua")
		{
			WriteOut("greetings professor falken");

		}
		else if (!seenTitle) {
			ShowTitle();
		}
		else {
			WriteOptions();
		}
		Simulator.tutorialOverride = false;
	}
	float ttime;
	void ShowTitle() {
		WriteOut("");
		WriteOut("spacemann presents");
		WriteOut("");
		WriteOut("   __ ________  ____  __________  _  __");
		WriteOut("  / //_/  _/ / / __ \\/_  __/ __ \\/ |/ /");
		WriteOut(" / ,< _/ // /_/ /_/ / / / / /_/ /    / ");
		WriteOut("/_/|_/___/____|____/ /_/  \\____/_/|_/");
		WriteOut("  _________  _  ___________  ___ ");
		WriteOut(" / ___/ __ \\/ |/ /_  __/ _ \\/ _ |");
		WriteOut("/ /__/ /_/ /    / / / / , _/ __ |");
		WriteOut("\\___/\\____/_/|_/ /_/ /_/|_/_/ |_|");

		WriteOut("");
		WriteOut("press any key to start...");
		WriteOut("");
		lockout = true;
		anykey = true;
		ttime = Time.time;
	}
	public override void Update()
	{
		base.Update();
		if (lockout && anykey) {
			if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && Time.time - ttime > 0.5f) {
				lockout = false;
				anykey = false;
				WriteOptions();
				seenTitle = true;
			}
		}
	}
	bool isLastUnlocked(int scen)
	{
		if (scen + 1 <= Simulator.scenarios.Count) return true;
		if (Simulator.scenarios[scen + 1].completed) return false;
		return true;
	}
	void WriteOptions()
	{
		WriteOut("_______________________________________", false);
		WriteOut("enter a simulation title to load...", false);
		for (int i = 0; i < Simulator.scenarios.Count; i++)
		{
			Simulator.scenarios[i].completed = PlayerPrefs.GetInt(Simulator.scenarios[i].name, 0) == 1;
			if (Simulator.scenarios[i].completed)
			{
				if (isLastUnlocked(i))
				{
					WriteOut("<color=\"yellow\">" + Simulator.scenarios[i].name + "</color > ");
				}
				else
				{
					WriteOut(Simulator.scenarios[i].name);
				}
			}
			else if (i > 0)
			{
				if (Simulator.scenarios[i - 1].completed)
				{
					if (isLastUnlocked(i))
					{
						WriteOut("<color=\"yellow\">" + Simulator.scenarios[i].name + "</color > ");
					}
					else
					{
						WriteOut(Simulator.scenarios[i].name);
					}
				}
			}
			else
			{
				if (isLastUnlocked(i))
				{
					WriteOut("<color=\"yellow\">" + Simulator.scenarios[i].name + "</color > ");
				}
				else
				{
					WriteOut(Simulator.scenarios[i].name);
				}
			}

		}
		WriteOut("");
		WriteOut("multiplayer");
		//WriteOut("");
		//WriteOut("type 'help' for more info", false);
		WriteOut("_______________________________________", false);
		WriteOut("");
	}

	public override bool ProcessText(string message)
	{
		message = message.Replace("\u200B", "");
		if (base.ProcessText(message)) return true;
		if (message.Contains("quit") || message.Contains("exit"))
		{
			Application.Quit();
			return true;
		}
		if (message.Contains("multiplayer", System.StringComparison.CurrentCultureIgnoreCase))
		{
			if (UnityServices.State == ServicesInitializationState.Uninitialized)
			{
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
			return true;
		}
		if (message.Contains("host", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("hosting");
			RelayDude.ins.StartGame();
			return true;
		}
		if (message.Contains("shutdown", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("shutting down multiplayer");
			NetworkManager.Singleton.Shutdown();
			return true;
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
			return true;
		}
		if (message.Contains("join", System.StringComparison.CurrentCultureIgnoreCase))
		{
			int f = message.IndexOf(' ') + 1;
			string joincode = message[f..];
			Debug.Log("parsed this joincode = " + joincode);
			WriteOut("trying code..." + joincode.ToLower());
			RelayDude.ins.JoinGame(joincode);
			return true;
		}
		if (message.Contains("hello joshua", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("greetings professor falken");
			WriteOut("how about a nice game of chess");
			return true;
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
			return true;
		}
		if (message.Contains("tears in rain", System.StringComparison.CurrentCultureIgnoreCase))
		{
			WriteOut("time to die");
			return true;
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
			return true;
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
			return true;
		}
		if (message.Contains("back"))
		{
			ShowTitle();
			//WriteOut("you are already in the root menu", false);
			return true;
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
			return true;
		}
		if (message.Contains("controls"))
		{
			WriteOut("_______________________________________");
			WriteOut("");
			WriteOut("simulation controls");
			WriteOut("");
			WriteOut("arrow keys - menu navigation");
			WriteOut("spacebar or return true - select");
			WriteOut("tab - back");
			WriteOut("w, a, s, d - pan camera");
			WriteOut("q, e - zoom camera");
			WriteOut("");
			WriteOut("_______________________________________");
			return true;
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
					return true;
				}
				selected_scenario = i;
				WriteBracket();
				WriteOut("");
				WriteOut(sc.description);
				WriteOut("");
				WriteOut("'load' to load scenario");
				WriteOut("");
				WriteBracket();
				return true;
			}

		}

		if (message.Contains("load") && selected_scenario != -1)
		{
			Simulator.activeScenario = Simulator.scenarios[selected_scenario];
			Simulator.tutorialOverride = Simulator.activeScenario.tutorial > 0;
			LoadGame();
			return true;
		}
		if (message.Contains("resetprogress"))
		{
			PlayerPrefs.DeleteAll();
			ClearConsole();
			WriteOut("progress has been reset");
			WriteOptions();
			return true;
		}
		WriteOut("unknown command");
		return false;
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
/*
 __  __     __     __         ______     ______   ______     __   __    
/\ \/ /    /\ \   /\ \       /\  __ \   /\__  _\ /\  __ \   /\ "-.\ \   
\ \  _"-.  \ \ \  \ \ \____  \ \ \/\ \  \/_/\ \/ \ \ \/\ \  \ \ \-.  \  
 \ \_\ \_\  \ \_\  \ \_____\  \ \_____\    \ \_\  \ \_____\  \ \_\\"\_\ 
  \/_/\/_/   \/_/   \/_____/   \/_____/     \/_/   \/_____/   \/_/ \/_/ 

__ ________  ____  __________  _  __
/ //_/  _/ / / __ \/_  __/ __ \/ |/ /
/ ,< _/ // /_/ /_/ / / / / /_/ /    / 
/_/|_/___/____|____/ /_/  \____/_/|_/

______  ______  __   __  ______  ______  ______    
/\  ___\/\  __ \/\ "-.\ \/\__  _\/\  == \/\  __ \   
\ \ \___\ \ \/\ \ \ \-.  \/_/\ \/\ \  __<\ \  __ \  
\ \_____\ \_____\ \_\\"\_\ \ \_\ \ \_\ \_\ \_\ \_\ 
\/_____/\/_____/\/_/ \/_/  \/_/  \/_/ /_/\/_/\/_/
_____  ____    _  __ ______   ___    ___ 
/ ___/ / __ \  / |/ //_  __/  / _ \  / _ |
/ /__  / /_/ / /    /  / /    / , _/ / __ |
\___/  \____/ /_/|_/  /_/    /_/|_| /_/ |_|
_________  _  ___________  ___ 
/ ___/ __ \/ |/ /_  __/ _ \/ _ |
/ /__/ /_/ /    / / / / , _/ __ |
\___/\____/_/|_/ /_/ /_/|_/_/ |_|
		//WriteOut("  _____  ____    _  __ ______   ___    ___ ");
//WriteOut(" / ___/ / __ \\  / |/ //_  __/  / _ \\  / _ |");
//WriteOut("/ /__  / /_/ / /    /  / /    / , _/ / __ |");
//WriteOut("\\___/  \\____/ /_/|_/  /_/    /_/|_| /_/ |_|");

*/        /*
		   __  __     __     __         ______     ______   ______     __   __    
		  /\ \/ /    /\ \   /\ \       /\  __ \   /\__  _\ /\  __ \   /\ "-.\ \   
		  \ \  _"-.  \ \ \  \ \ \____  \ \ \/\ \  \/_/\ \/ \ \ \/\ \  \ \ \-.  \  
		   \ \_\ \_\  \ \_\  \ \_____\  \ \_____\    \ \_\  \ \_____\  \ \_\\"\_\ 
			\/_/\/_/   \/_/   \/_____/   \/_____/     \/_/   \/_____/   \/_/ \/_/ 

	 __ ________  ____  __________  _  __
	/ //_/  _/ / / __ \/_  __/ __ \/ |/ /
   / ,< _/ // /_/ /_/ / / / / /_/ /    / 
  /_/|_/___/____|____/ /_/  \____/_/|_/

   ______  ______  __   __  ______  ______  ______    
  /\  ___\/\  __ \/\ "-.\ \/\__  _\/\  == \/\  __ \   
  \ \ \___\ \ \/\ \ \ \-.  \/_/\ \/\ \  __<\ \  __ \  
   \ \_____\ \_____\ \_\\"\_\ \ \_\ \ \_\ \_\ \_\ \_\ 
	\/_____/\/_____/\/_/ \/_/  \/_/  \/_/ /_/\/_/\/_/
	_____  ____    _  __ ______   ___    ___ 
   / ___/ / __ \  / |/ //_  __/  / _ \  / _ |
  / /__  / /_/ / /    /  / /    / , _/ / __ |
  \___/  \____/ /_/|_/  /_/    /_/|_| /_/ |_|
	_________  _  ___________  ___ 
   / ___/ __ \/ |/ /_  __/ _ \/ _ |
  / /__/ /_/ /    / / / / , _/ __ |
  \___/\____/_/|_/ /_/ /_/|_/_/ |_|
				  //WriteOut("  _____  ____    _  __ ______   ___    ___ ");
		  //WriteOut(" / ___/ / __ \\  / |/ //_  __/  / _ \\  / _ |");
		  //WriteOut("/ /__  / /_/ / /    /  / /    / , _/ / __ |");
		  //WriteOut("\\___/  \\____/ /_/|_/  /_/    /_/|_| /_/ |_|");

		  */