using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;

public class Busybeaver : MonoBehaviour {
	
	public KMAudio kmAudio;
    public KMBombModule Module;
    public KMBossModule Boss;
    public KMBombInfo Bomb;

	public TextMesh[] colorblindText;
	public TextMesh displayText, progressText;
	public KMSelectable[] Buttons;
	public KMSelectable submitBtn;
	public MeshRenderer[] bitsRenderer, positionRenderer;
	public Material[] bitStates = new Material[2];
	public Material disableState;
	private const string Alphabet = "QWERTYUIOPASDFGHJKLZXCVBNM";
	private int stageNo, position, stagesGeneratable, cStage = -1;
	private bool solved = false;

	private bool[] correctStates = new bool[10], inputStates = new bool[10];
	List<bool[]> displayStates = new List<bool[]>();
	List<int> displayPositions = new List<int>();
	private string assignLetters = "", movementLetters = "";
	private bool inFinale = false, inSpecial = false, hasStarted = false, interactable = true, requestForceSolve = false;

	private string[] ignoredModules = {
		"Busy Beaver",
		"OmegaForget",
		"14",
		"Bamboozling Time Keeper",
		"Brainf---",
		"Forget Enigma",
		"Forget Everything",
		"Forget It Not",
		"Forget Me Not",
		"Forget Me Later",
		"Forget Perspective",
		"Forget The Colors",
		"Forget Them All",
		"Forget This",
		"Forget Us Not",
		"Iconic",
		"Organization",
		"Purgatory",
		"RPS Judging",
		"Simon Forgets",
		"Simon's Stages",
		"Souvenir",
		"Tallordered Keys",
		"The Time Keeper",
		"The Troll",
		"The Twin",
		"The Very Annoying Button",
		"Timing Is Everything",
		"Turn The Key",
		"Ultimate Custom Night",
		"Übermodule"
	};

	static private int _moduleIdCounter = 1;
	private int _moduleId;

	IEnumerator currentMercyAnim;

	void Awake () {
		_moduleId = _moduleIdCounter++;
	    string[] ignoreRepo = Boss.GetIgnoredModules(Module, ignoredModules);
        if (ignoreRepo != null)
            ignoredModules = ignoreRepo;
	}
	// Use this for initialization
	void Start () {
		Module.OnActivate += delegate
		{
			/*
			if (!Application.isEditor)
            {
				stagesGeneratable = Bomb.GetSolvableModuleNames().Count(a => !ignoredModules.Contains(a)) - 1;
			}
			else
            {
				stagesGeneratable = 0;
            }*/
			stagesGeneratable = Bomb.GetSolvableModuleNames().Count(a => !ignoredModules.Contains(a)) - 1;
			if (stagesGeneratable <= 0)
			{
				Debug.LogFormat("[Busy Beaver #{0}]: There are no non-ignored modules. The module will enter a special state for this.", _moduleId);
				inSpecial = true;
				stagesGeneratable = 10;

			}
			else
			{
				Debug.LogFormat("[Busy Beaver #{0}]: Total stages generatable: {1} ({2} non-ignored modules detected.)", _moduleId, stagesGeneratable, Bomb.GetSolvableModuleNames().Count(a => !ignoredModules.Contains(a)));
				//StageGeneration();
			}
			GenerateAllStages();
			ProcessAllStages();
			hasStarted = true;
			if (inSpecial)
            {
				cStage = 0;
				DisplayCurrentStage();
            }

		};
		for (int x=0;x<Buttons.Length;x++)
        {
            int y = x;
			Buttons[x].OnInteract += delegate {
				kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[y].transform);
				kmAudio.PlaySoundAtTransform("tick", Buttons[y].transform);
				Buttons[y].AddInteractionPunch(0.5f);
				TogglePos(y);
				return false;
			};
        }
		submitBtn.OnInteract += delegate {
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitBtn.transform);
			submitBtn.AddInteractionPunch(0.1f);
			ProcessSubmission();
			return false;
		};

		displayText.text = "";
		progressText.text = "";
	}
	void TogglePos(int idx)
    {
		if (idx < 0 || idx >= 10) return;
		if (solved || !interactable) return;
		if (inFinale)
		{
			if (currentMercyAnim != null)
				StopCoroutine(currentMercyAnim);
			inputStates[idx] = !inputStates[idx];
			for (int x = 0; x < positionRenderer.Length ; x++)
			{
				bitsRenderer[x].material = inputStates[x] ? bitStates[1] : bitStates[0];
				colorblindText[x].text = inputStates[x] ? "1" : "0";
				colorblindText[x].color = inputStates[x] ? Color.black : Color.white;
				positionRenderer[x].material = disableState;
			}
			progressText.text = "SUBMIT";
			displayText.text = "";
		}
    }

	void ProcessSubmission()
    {
		if (solved || !interactable) return;
		if (inFinale)
        {
			if (correctStates.SequenceEqual(inputStates))
            {
				Debug.LogFormat("[Busy Beaver #{0}]: Correct tape submitted.", _moduleId);
				Module.HandlePass();
				StartCoroutine(AnimateDisarmState());
				solved = true;
            }
			else
            {
				Debug.LogFormat("[Busy Beaver #{0}]: Strike! You submitted the following tape: {1}", _moduleId, inputStates.Select(a => a ? "1" : "0").Join(""));
				Module.HandleStrike();
				if (currentMercyAnim != null)
					StopCoroutine(currentMercyAnim);
				currentMercyAnim = HandleMercyReveal();
				StartCoroutine(currentMercyAnim);
			}
        }
		else if (inSpecial)
        {
			cStage++;
			if (cStage > stagesGeneratable)
			{
				interactable = false;
				StartCoroutine(AnimateFinaleState());
			}
			else
			{
				timeLeft = 3f;
				DisplayCurrentStage();
			}
		}
    }

	bool IsProvidedConditionTrue(char letter)
    {
		switch (letter)
        {// Note, positions are 0-indexed however manual states it by 1st-10th positions

			case 'A': return !correctStates[position];
			case 'B': return position <= 4;
			case 'C': return correctStates[stageNo % 10];
			case 'D': return (position + 1) % 2 == 0;
            case 'E': return !correctStates[(stageNo + position + 1) % 10];
			case 'F': return correctStates[(position + 5) % 10];
			case 'G': return stageNo % 2 == 0;
			case 'H': return (stageNo + position + 1) % 2 == 1;
			case 'I': return !correctStates[(stageNo + position + (correctStates[position] ? 2 : 1)) % 10];
			case 'J': return (stageNo - (position + 1) + 10) % 2 == 1;
			case 'K': return ((correctStates[position] ? 1 : 0) + position + 1) % 2 == 0;
			case 'L': return (stageNo - (position + 1 + (correctStates[position] ? 1 : 0))) % 2 == 1;
			case 'M': return !correctStates[(stageNo + position + (correctStates[position] ? 6 : 5)) % 10];

			case 'N': return correctStates[position];
			case 'O': return position >= 5;
			case 'P': return !correctStates[stageNo % 10];
			case 'Q': return (position + 1) % 2 == 1;
			case 'R': return correctStates[(stageNo + position + 1) % 10];
			case 'S': return !correctStates[(position + 5) % 10];
			case 'T': return stageNo % 2 == 1;
			case 'U': return (stageNo + position + 1) % 2 == 0;
			case 'V': return correctStates[(stageNo + position + (correctStates[position] ? 2 : 1)) % 10];
			case 'W': return (stageNo - (position + 1) + 10) % 2 == 0;
			case 'X': return ((correctStates[position] ? 1 : 0) + position + 1) % 2 == 1;
			case 'Y': return (stageNo - (position + 1 + (correctStates[position] ? 1 : 0))) % 2 == 0;
			case 'Z': return correctStates[(stageNo + position + (correctStates[position] ? 6 : 5)) % 10];

		}
		return false;
    }

	void GenerateAllStages()
    {
        for (int x = 0; x < stagesGeneratable; x++)
        {
			assignLetters += Alphabet.PickRandom();
			movementLetters += Alphabet.PickRandom();
        }
    }
	void ProcessAllStages()
    {
		bool[] initialState = new bool[10];
		for (int y = 0; y < initialState.Length; y++)
        {
			initialState[y] = Rnd.value < 0.5f;
        }
        position = Rnd.Range(0, 10) % 10;
		correctStates = initialState.ToArray();

		displayStates.Add(initialState);
		displayPositions.Add(position);

		Debug.LogFormat("[Busy Beaver #{0}]:-----------INITIAL STATE-----------", _moduleId);
		Debug.LogFormat("[Busy Beaver #{0}]: Starting Tape: {1}", _moduleId, correctStates.Select(a => a ? "1" : "0").Join(""));
		Debug.LogFormat("[Busy Beaver #{0}]: Starting Pointer Position: {1}", _moduleId, position + 1);
		Debug.LogFormat("[Busy Beaver #{0}]:-----------------------------------", _moduleId, stageNo);
		for (int x = 0; x < Math.Min(assignLetters.Length, movementLetters.Length); x++)
        {
			stageNo++;
			Debug.LogFormat("[Busy Beaver #{0}]:-----------STAGE {1}-----------", _moduleId, stageNo);
			Debug.LogFormat("[Busy Beaver #{0}]: Characters displayed in this stage: \"{1}{2}\"", _moduleId, assignLetters[x], movementLetters[x]);
			bool stateModifer = IsProvidedConditionTrue(assignLetters[x]), movementModifer = IsProvidedConditionTrue(movementLetters[x]);
			Debug.LogFormat("[Busy Beaver #{0}]: The left character's condition returned {1}", _moduleId, stateModifer);
			correctStates[position] = stateModifer;
			Debug.LogFormat("[Busy Beaver #{0}]: Current Tape: {1}", _moduleId, correctStates.Select(a => a ? "1" : "0").Join(""));
			Debug.LogFormat("[Busy Beaver #{0}]: The right character's condition returned {1}", _moduleId, movementModifer);
			position = Mod(position + (movementModifer ? 1 : -1), 10);
			Debug.LogFormat("[Busy Beaver #{0}]: Current Pointer Position: {1}", _moduleId, position + 1);
			if (stageNo <= Math.Min(5, Math.Min(assignLetters.Length, movementLetters.Length) / 2) || Application.isEditor)
            {
				displayStates.Add(correctStates.ToArray());
				displayPositions.Add(position);
            }
			Debug.LogFormat("[Busy Beaver #{0}]:-------------------------------", _moduleId, stageNo);
		}
		Debug.LogFormat("[Busy Beaver #{0}]: Tape to submit: {1}", _moduleId, correctStates.Select(a => a ? "1" : "0").Join(""));
	}
	void DisplayCurrentStage()
    {
		if (cStage > 0)
		{
			displayText.text = string.Format("{0} {1}", assignLetters[cStage - 1], movementLetters[cStage - 1]);
            progressText.text = inSpecial ? string.Format("NEXT({0})", stagesGeneratable - cStage) : cStage.ToString("0000000");
		}
		else
        {
			displayText.text = "";
			progressText.text = inSpecial ? "START" : "INITIAL";
		}
		if (cStage < displayStates.Count)
		{
			for (int x = 0; x < bitsRenderer.Length; x++)
            {
				bitsRenderer[x].material = displayStates[cStage][x] ? bitStates[1] : bitStates[0];
				colorblindText[x].text = displayStates[cStage][x] ? "1" : "0";
				colorblindText[x].color = displayStates[cStage][x] ? Color.black : Color.white;
			}
			for (int x = 0; x < positionRenderer.Length; x++)
			{
				positionRenderer[x].material = displayPositions[cStage] == x ? bitStates[1] : bitStates[0];
			}
		}
		else
        {
			for (int x = 0; x < bitsRenderer.Length; x++)
			{
				bitsRenderer[x].material = disableState;
				colorblindText[x].text = "";
			}
			for (int x = 0; x < positionRenderer.Length; x++)
			{
				positionRenderer[x].material = disableState;
			}
		}
	}
	IEnumerator HandleMercyReveal()
    {
		for (int z = 0; z < 1 + movementLetters.Length; z++)
		{
			if (z > 0)
			{
				displayText.text = string.Format("{0} {1}", assignLetters[z - 1], movementLetters[z - 1]);
				progressText.text = z.ToString("0000000");
			}
			else
			{
				displayText.text = "";
				progressText.text = "INITIAL";
			}
			if (z < displayStates.Count)
			{
				for (int x = 0; x < bitsRenderer.Length; x++)
				{
					bitsRenderer[x].material = displayStates[z][x] ? bitStates[1] : bitStates[0];
					colorblindText[x].text = displayStates[z][x] ? "1" : "0";
					colorblindText[x].color = displayStates[z][x] ? Color.black : Color.white;
				}
				for (int x = 0; x < positionRenderer.Length; x++)
				{
					positionRenderer[x].material = displayPositions[z] == x ? bitStates[1] : bitStates[0];
				}
			}
			else
			{
				for (int x = 0; x < bitsRenderer.Length; x++)
				{
					bitsRenderer[x].material = disableState;
					colorblindText[x].text = "";
				}
				for (int x = 0; x < positionRenderer.Length; x++)
				{
					positionRenderer[x].material = disableState;
				}
			}
			yield return new WaitForSeconds(1f);
		}
		while (displayText.text.Length > 0 || progressText.text.Length > 0)
		{
			if (!string.IsNullOrEmpty(displayText.text))
				displayText.text = displayText.text.Substring(0, displayText.text.Length - 1);
			if (!string.IsNullOrEmpty(progressText.text))
				progressText.text = progressText.text.Substring(0, progressText.text.Length - 1);
			yield return new WaitForSeconds(.1f);
		}
		for (int x = 0; x < positionRenderer.Length / 2; x++)
		{
			bitsRenderer[x].material = inputStates[x] ? bitStates[1] : bitStates[0];
			colorblindText[x].text = inputStates[x] ? "1" : "0";
			colorblindText[x].color = inputStates[x] ? Color.black : Color.white;
			positionRenderer[x].material = disableState;

			bitsRenderer[9 - x].material = inputStates[9 - x] ? bitStates[1] : bitStates[0];
			colorblindText[9 - x].text = inputStates[9 - x] ? "1" : "0";
			colorblindText[9 - x].color = inputStates[9 - x] ? Color.black : Color.white;
			positionRenderer[9 - x].material = disableState;

			yield return new WaitForSeconds(0.05f);
		}
		string subText = "SUBMIT";
		for (int x = subText.Length - 1; x >= 0; x--)
		{
			progressText.text = subText.Substring(x);
			yield return new WaitForSeconds(0.05f);
		}
		yield return null;
    }

	IEnumerator AnimateDisarmState()
	{
		displayText.text = "";
		float[] delayTimes = { 0.8f, 0.1f, 0.6f, 0.1f, 0.4f, 0.1f, 0.2f, 0.1f, 0.1f, 0.1f };
		kmAudio.PlaySoundAtTransform("321107__nsstudios__robot-or-machine-destroy", transform);
		for (int x = 0; x < delayTimes.Length; x++)
		{
			for (int y = 0; y < positionRenderer.Length; y++)
			{
				bitsRenderer[y].material = x % 2 == 1 ? disableState : inputStates[y] ? bitStates[1] : bitStates[0];
				colorblindText[y].text = x % 2 == 1 ? "" : inputStates[y] ? "1" : "0";
				colorblindText[y].color = inputStates[y] ? Color.black : Color.white;
			}
			progressText.text = x % 2 == 1 ? "" : "SOLVED";
			yield return new WaitForSeconds(delayTimes[x]);
		}
	}
	IEnumerator AnimateFinaleState()
    {
		while (displayText.text.Length > 0 || progressText.text.Length > 0)
        {
			if (!string.IsNullOrEmpty(displayText.text))
				displayText.text = displayText.text.Substring(0, displayText.text.Length - 1);
			if (!string.IsNullOrEmpty(progressText.text))
				progressText.text = progressText.text.Substring(0, progressText.text.Length - 1);
			yield return new WaitForSeconds(.1f);
		}

		for (int x = 0; x < positionRenderer.Length / 2; x++)
		{
			bitsRenderer[x].material = inputStates[x] ? bitStates[1] : bitStates[0];
			colorblindText[x].text = inputStates[x] ? "1" : "0";
			colorblindText[x].color = inputStates[x] ? Color.black : Color.white;
			positionRenderer[x].material = disableState;

			bitsRenderer[9 - x].material = inputStates[9 - x] ? bitStates[1] : bitStates[0];
            colorblindText[9 - x].text = inputStates[9 - x] ? "1" : "0";
			colorblindText[9 - x].color = inputStates[9 - x] ? Color.black : Color.white;
			positionRenderer[9 - x].material = disableState;

			yield return new WaitForSeconds(0.05f);
		}
		string subText = "SUBMIT";
        for (int x = subText.Length - 1; x >= 0; x--)
        {
			progressText.text = subText.Substring(x);
			yield return new WaitForSeconds(0.05f);
		}
		inFinale = true;
		interactable = true;
    }

	// Update is called once per frame
	float timeLeft = 0f;
	void Update () {
		if (!inFinale && hasStarted && !inSpecial)
			if (cStage < Bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).Count() && !solved && timeLeft <= 0f)
			{
				cStage++;
				if (cStage > stagesGeneratable && !inFinale){
					Debug.LogFormat("[Busy Beaver #{0}]: Its time to input. Here we go.",_moduleId);
					
					StartCoroutine(AnimateFinaleState());
				}
				else
				{
					timeLeft = 3f;
					DisplayCurrentStage();
				}
			}
			else
			{
				timeLeft = requestForceSolve ? 0f : Mathf.Max(timeLeft - Time.deltaTime, 0);
			}
	}
	private int Mod(int num, int mod) {
        while (num < 0)
        {
			num += mod;
        }
		return num % mod;
	}
	#pragma warning disable 414
		private readonly string TwitchHelpMessage = "Submit the binary sequence using \"!{0} submit 1101001010\" (In this example, set the binary to 1101001010, then presses the submit button.) To advance to the next stage, use \"!{0} advance/next\" (Only if there are insufficient unignored modules)";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
    {
        Match subCmd = Regex.Match(command, @"^\s*submit\s+([01 ]){10}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			advCmd = Regex.Match(command,@"^\s*(advance|next)$");
        if (subCmd.Success)
        {    
            var Binary = subCmd.Groups[1].Value.ToList();
			Debug.Log(Binary.Join(", "));
            if(Binary.Count != 10)
				yield return "sendtochaterror Incorrect Syntax. Must be exactly 10 digits.";
            yield return null;  // acknowledge to TP that the command was valid

            for (int i = 0; i < 10; i++)
            {
                if(inputStates[i])
                    Buttons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            submitBtn.OnInteract();
        }
		else if (advCmd.Success)
        {
			if (!inSpecial)
            {
				yield return "sendtochaterror The module is not in its special phase. This command is not useful.";
				yield break;
			}
			yield return null;
			submitBtn.OnInteract();
        }
    }
	IEnumerator TwitchHandleForcedSolve()
    {
		requestForceSolve = true;
		if (inSpecial)
        {
			while (interactable)
            {
				submitBtn.OnInteract();
				yield return null;
			}
        }
		while (!inFinale) //Wait until submission time
            yield return true;
        for (int i = 0; i < 10; i++)
        {
			if(correctStates[i] != inputStates[i])
				Buttons[i].OnInteract();
			yield return null;
		}
		submitBtn.OnInteract();
		yield return true;
    }
	
}
