using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;

public class BusyBeaverHandler : MonoBehaviour {
	
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
	private const string alphabet = "QWERTYUIOPASDFGHJKLZXCVBNM", easyModeLetters = "ABCDEFGHIJKL";
	private int stageNo, position, stagesGeneratable, cStage = -1;
	private bool solved = false;

	public bool debugTape;

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
				Debug.LogFormat("[Busy Beaver #{0}]: There are insufficient non-ignored modules. The module will enter a special state for this.", _moduleId);
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
        for (int x = 0; x < Buttons.Length; x++)
        {
            int y = x;
			Buttons[x].OnInteract += delegate {
				kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[y].transform);
				kmAudio.PlaySoundAtTransform("tick", Buttons[y].transform);
				Buttons[y].AddInteractionPunch(0.1f);
				TogglePos(y);
				return false;
			};
        }
		submitBtn.OnInteract += delegate {
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submitBtn.transform);
			submitBtn.AddInteractionPunch(1f);
			ProcessSubmission();
			return false;
		};

		displayText.text = "";
		progressText.text = "";
		for (int y = 0; y < bitsRenderer.Length; y++)
		{
			bitsRenderer[y].material = disableState;
			colorblindText[y].text = "";
		}
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
				Debug.LogFormat("[Busy Beaver #{0}]: Correct tape submitted. Module passed.", _moduleId);
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
				kmAudio.PlaySoundAtTransform("strike", transform);
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
	bool IsProvidedConditionTrueEasy(char letter)
    {
		switch (letter)
		{// Note, positions are 0-indexed
			case 'A': return !correctStates[position];
			case 'B': return position <= 4;
			case 'C': return correctStates[stageNo % 10];
			case 'D': return correctStates[position] == correctStates[(position + 5) % 10];
			case 'E': return correctStates[Mod(position - 1, 10)] == correctStates[(position + 1) % 10];
			case 'F': return stageNo % 2 == 1;
			case 'G': return stageNo % 2 == 0;
			case 'H': return correctStates[Mod(position - 1, 10)] != correctStates[(position + 1) % 10];
			case 'I': return correctStates[position] != correctStates[(position + 5) % 10];
			case 'J': return correctStates[position];
			case 'K': return position > 4;
			case 'L': return correctStates[stageNo % 10];
		}
		return false;
    }
	bool IsProvidedConditionTrue(char letter)
    {
		switch (letter)
        {// Note, positions are 0-indexed

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
			case 'L': return (stageNo - (position + 1 + (correctStates[position] ? 1 : 0)) + 10) % 2 == 1;
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
			case 'Y': return (stageNo - (position + 1 + (correctStates[position] ? 1 : 0)) + 10) % 2 == 0;
			case 'Z': return correctStates[(stageNo + position + (correctStates[position] ? 6 : 5)) % 10];

		}
		return false;
    }

	void GenerateAllStages()
    {
		if (inSpecial)
			for (int x = 0; x < stagesGeneratable; x++)
			{
				assignLetters += alphabet.PickRandom();
				movementLetters += alphabet.PickRandom();
			}
		else
			for (int x = 0; x < stagesGeneratable; x++)
			{
                if (Rnd.value < 0.5f)
                {
                    char selectedLetter = easyModeLetters.PickRandom();
                    assignLetters += selectedLetter;
                    movementLetters += selectedLetter;
                }
                else
                {
                    assignLetters += easyModeLetters.PickRandom();
                    movementLetters += easyModeLetters.PickRandom();
                }
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
		Debug.LogFormat("[Busy Beaver #{0}]:-----------------------------------", _moduleId);
		for (int x = 0; x < Math.Min(assignLetters.Length, movementLetters.Length); x++)
        {
			stageNo++;
			Debug.LogFormat("[Busy Beaver #{0}]:-----------STAGE {1}-----------", _moduleId, stageNo);
			Debug.LogFormat("[Busy Beaver #{0}]: Characters displayed in this stage: \"{1}{2}\"", _moduleId, assignLetters[x], movementLetters[x]);
			bool stateModifer = inSpecial ? IsProvidedConditionTrue(assignLetters[x]) : IsProvidedConditionTrueEasy(assignLetters[x]), moveLeft = inSpecial ? IsProvidedConditionTrue(movementLetters[x]) : IsProvidedConditionTrueEasy(movementLetters[x]);
			Debug.LogFormat("[Busy Beaver #{0}]: The left character's condition returned {1}", _moduleId, stateModifer);
			Debug.LogFormat("[Busy Beaver #{0}]: The right character's condition returned {1}", _moduleId, moveLeft);
			correctStates[position] = stateModifer;
			Debug.LogFormat("[Busy Beaver #{0}]: Current Tape: {1}", _moduleId, correctStates.Select(a => a ? "1" : "0").Join(""));
			position = Mod(position + (moveLeft ? -1 : 1), 10);
			Debug.LogFormat("[Busy Beaver #{0}]: Current Pointer Position: {1}", _moduleId, position + 1);
			if (stageNo <= Math.Min(5, Math.Min(assignLetters.Length, movementLetters.Length) / 2) || debugTape)
			{// Check if 5 or more stages have not gone through or half as many stages did not went through already or this is being run in the Unity Editor
				displayStates.Add(correctStates.ToArray());
				displayPositions.Add(position);
            }
			Debug.LogFormat("[Busy Beaver #{0}]:-------------------------------", _moduleId);
		}
		Debug.LogFormat("[Busy Beaver #{0}]: Correct Tape to submit: {1}", _moduleId, correctStates.Select(a => a ? "1" : "0").Join(""));
	}
	void DisplayCurrentStage()
    {
		if (cStage > 0)
		{
			displayText.text = string.Format("{0} {1}", assignLetters[cStage - 1], movementLetters[cStage - 1]);
            progressText.text = inSpecial ? string.Format("STAGE {0}/{1}->", cStage.ToString("00"),stagesGeneratable) : string.Format("STAGE {0}",cStage.ToString("0000000"));
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
			if (cStage > 0 && !requestForceSolve)
			{
				StartCoroutine(AnimateBlendToNextStatesVisible(displayStates[cStage - 1], displayStates[cStage]));
				StartCoroutine(AnimateBlendToNextPos(displayPositions[cStage - 1], displayPositions[cStage]));
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
			if (cStage - 1 < displayStates.Count && !requestForceSolve)
            {
				StartCoroutine(AnimateDisableStates(displayStates[cStage - 1]));
				StartCoroutine(AnimateDisablePosition(displayPositions[cStage - 1]));
				kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.LightBuzzShort, transform);
			}
		}
	}
	int CountCorrect()
    {
		int output = 0;
		for (int x = 0; x < inputStates.Length; x++)
        {
			output += inputStates[x] == correctStates[x] ? 1 : 0;
        }
		return output;
    }
	IEnumerator AnimateBlendToNextStatesVisible(bool[] previousState, bool[] nextState)
    {
		bool[] canChange = new bool[Math.Min(previousState.Length, nextState.Length)];
        for (int x = 0; x < Math.Min(previousState.Length, nextState.Length); x++)
        {
			canChange[x] = previousState[x] != nextState[x];
        }
		if (!canChange.ToList().TrueForAll(a => !a))
		for (float x = 0; x <= 1f; x = Math.Min(x + 10 * Time.deltaTime, 1f))
        {
            for (int y = 0; y < canChange.Length; y++)
            {
				if (canChange[y] && bitsRenderer[y].material.HasProperty("_Blend"))
					bitsRenderer[y].material.SetFloat("_Blend", 1f - x);
            }
			if (x == 1f) break;
			yield return new WaitForSeconds(Time.deltaTime);
        }
		yield return null;
    }
	IEnumerator AnimateBlendToNextPos(int originalPos,int newPos)
    {
		if (originalPos == newPos) yield break;

		bool[] canChange = new bool[10];
		for (int x = 0; x < canChange.Length; x++)
		{
			canChange[x] = x == originalPos || x == newPos;
		}
		for (float x = 0; x <= 1f; x = Math.Min(x + 10 * Time.deltaTime, 1f))
		{
			for (int y = 0; y < positionRenderer.Length; y++)
			{
				if (canChange[y] && positionRenderer[y].material.HasProperty("_Blend"))
					positionRenderer[y].material.SetFloat("_Blend", 1f - x);
			}
			if (x == 1f) break;
			yield return new WaitForSeconds(Time.deltaTime);
		}
		yield return null;
    }
	float[] flickerTimings = { 0.1f, 0.5f, 0.1f, 0.2f, 0.1f };
	IEnumerator AnimateDisableStates(bool[] previousStates)
    {
        for (int x = 0; x < flickerTimings.Length; x++)
		{
			for (int y = 0; y < previousStates.Length; y++)
			{
				bitsRenderer[y].material = x % 2 == 0 ? disableState : previousStates[y] ? bitStates[1] : bitStates[0];
				colorblindText[y].text = x % 2 == 0 ? "" : previousStates[y] ? "1" : "0";
				colorblindText[y].color = previousStates[y] ? Color.black : Color.white;
			}
			yield return new WaitForSeconds(flickerTimings[x]);
		}
	}
	IEnumerator AnimateDisablePosition(int previousPosition)
	{
		for (int x = 0; x < flickerTimings.Length; x++)
		{
			for (int y = 0; y < positionRenderer.Length; y++)
			{
				positionRenderer[y].material = x % 2 == 0 ? disableState : y == previousPosition ? bitStates[1] : bitStates[0];
			}
			yield return new WaitForSeconds(flickerTimings[x]);
		}
	}
	IEnumerator HandleMercyReveal()
    {
		displayText.text = string.Format("{0} C", CountCorrect());
		for (int x = 0; x <= 4; x++)
		{
			displayText.color = x % 2 == 0 ? Color.white : Color.red;
			yield return new WaitForSeconds(0.2f);
		}
		for (int z = 0; z < 1 + movementLetters.Length; z++)
		{
			if (z > 0)
			{
				displayText.text = string.Format("{0} {1}", assignLetters[z - 1], movementLetters[z - 1]);
				progressText.text = string.Format("STAGE {0}", z.ToString("0000000"));
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
				if (z > 0)
                {
					StartCoroutine(AnimateBlendToNextPos(displayPositions[z - 1], displayPositions[z]));
					StartCoroutine(AnimateBlendToNextStatesVisible(displayStates[z - 1], displayStates[z]));
				}
				yield return new WaitForSeconds(3f);
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
				if (z - 1 < displayStates.Count)
                {
					StartCoroutine(AnimateDisablePosition(displayPositions[z - 1]));
					StartCoroutine(AnimateDisableStates(displayStates[z - 1]));
					kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.LightBuzzShort, transform);
				}
				yield return new WaitForSeconds(1.5f);
			}
		}
		while (displayText.text.Length > 0 || progressText.text.Length > 0)
		{
			if (!string.IsNullOrEmpty(displayText.text))
				displayText.text = displayText.text.Substring(0, displayText.text.Length - 1).Trim();
			if (!string.IsNullOrEmpty(progressText.text))
				progressText.text = progressText.text.Substring(0, progressText.text.Length - 1).Trim();
			yield return new WaitForSeconds(.05f);
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
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
			progressText.text = subText.Substring(x);
			yield return new WaitForSeconds(0.05f);
		}
		yield return null;
    }
	string[] possibleDisarmTexts = {
		"MODULE DONE",
		"SOLVED",
		"THERE WE GO",
		"WELL DONE",
		""
	};
	IEnumerator AnimateDisarmState()
	{
		string selectedText = possibleDisarmTexts.PickRandom();
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
			progressText.text = x % 2 == 1 ? "" : selectedText;
			yield return new WaitForSeconds(delayTimes[x]);
		}
	}
	IEnumerator AnimateFinaleState()
    {
		while (displayText.text.Length > 0 || progressText.text.Length > 0)
        {
			if (!string.IsNullOrEmpty(displayText.text))
				displayText.text = displayText.text.Substring(0, displayText.text.Length - 1).Trim();
			if (!string.IsNullOrEmpty(progressText.text))
				progressText.text = progressText.text.Substring(0, progressText.text.Length - 1).Trim();
			yield return new WaitForSeconds(.05f);
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
			kmAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
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
		private readonly string TwitchHelpMessage = "Submit the binary sequence using \"!{0} submit 1101001010\" (In this example, set the binary to 1101001010, then presses the submit button.) 'T'/'F' can be used for 1's and 0's instead. You may space out the binary digits in the command." +
		"To advance to the next stage, use \"!{0} advance/next\" (Only if there are insufficient unignored modules) You may append a number to specify how many stages to press next on.";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
    {
		if (!interactable)
        {
			yield return "sendtochaterror The module cannot be interacted right now. Wait a bit until you can interact with this.";
			yield break;
		}
        Match subCmd = Regex.Match(command, @"^\s*submit\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
			advCmd = Regex.Match(command,@"^\s*(advance|next)(\s\d+)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (subCmd.Success)
        {
			if (!inFinale)
			{
				yield return "sendtochaterror The module is not ready to submit. Advance all the stages first or solve enough modules to use this command.";
				yield break;
			}
			var Binary = command.Trim().Substring(7).Trim().ToLower().Replace('t','1').Replace('f', '0').Split();
			//Debug.Log(Binary.Join(", "));
			List<bool> binaryStates = new List<bool>();
			for (int x = 0; x < Binary.Length; x++)
			{
				foreach (char suggestedLetter in Binary[x])
					switch (suggestedLetter)
					{
						case '0':
							binaryStates.Add(false);
							break;
						case '1':
							binaryStates.Add(true);
							break;
						default:
							yield return string.Format("sendtochaterror I do not know what character \"{0}\" releates to in binary. Recheck your command.", Binary[x]);
							yield break;
					}
			}
			if (binaryStates.Count != 10)
			{
				yield return string.Format("sendtochaterror You provided {0} binary digits when I expected exactly 10. Recheck your command.",binaryStates.Count);
				yield break;
			}
            yield return null;  // acknowledge to TP that the command was valid

            for (int i = 0; i < binaryStates.Count; i++)
            {
				if (inputStates[i] != binaryStates[i])
				{
					Buttons[i].OnInteract();
					yield return new WaitForSeconds(.1f);
				}
            }
            submitBtn.OnInteract();
        }
		else if (advCmd.Success)
        {
			if (!inSpecial)
            {
				yield return "sendtochaterror The module is not in its special phase. This command is useless when there are enough non-ignored modules.";
				yield break;
			}
			if (inFinale)
            {
				yield return "sendtochaterror The module is awaiting submission. Pressing just \"SUBMIT\" would definitely strike you from here.";
				yield break;
			}
			string intereptedCommand = advCmd.Value.Substring(advCmd.Value.ToLower().StartsWith("next") ? 4 : 7);
			if (string.IsNullOrEmpty(intereptedCommand))
			{
				yield return null;
				submitBtn.OnInteract();
			}
			else
            {
				intereptedCommand = intereptedCommand.Trim();
				int repeatTimes;
				if (!int.TryParse(intereptedCommand, out repeatTimes) || repeatTimes <= 0)
                {
					yield return string.Format("sendtochaterror I do not know how to interact with a given button \"{0}\" times.", intereptedCommand);
					yield break;
				}
				for (int x = 0; x < repeatTimes; x++)
				{
					if (!interactable)
                    {
						yield return string.Format("sendtochaterror The module stopped allowing inputs to process after \"{0}\" presses.", x);
						yield break;
					}
					yield return null;
					submitBtn.OnInteract();
					yield return new WaitForSeconds(1f);
				}
			}
        }
    }
	IEnumerator TwitchHandleForcedSolve()
    {
		requestForceSolve = true;
		if (inSpecial && !inFinale)
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
