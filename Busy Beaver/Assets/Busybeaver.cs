using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using KModkit;
using System.Text.RegularExpressions;
using Rnd = UnityEngine.Random;

public class Busybeaver : MonoBehaviour {
	
	public KMAudio Audio;
    public KMBombModule Module;
    public KMBossModule Boss;
    public KMBombInfo Bomb;
	
	private int[] I = {0,0,0,0,0,0,0,0,0,0};
	private int[] TapeShow = {0,0,0,0,0,0,0,0,0,0};
	private string DisplayTop;
	public TextMesh[] Text;
	public KMSelectable[] Buttons;
	private static string Alphabet = "QWERTYUIOPASDFGHJKLZXCVBNM";
	private int S;
	private int SubStage;
	private int P;
	private bool solved = false;
	private bool Submission = false;
	private int echo;
	
	private string[] ignoredModules = {"Busy Beaver","OmegaForget","14"," Bamboozling Time Keeper"," Brainf---"," Forget Enigma"," Forget Everything"," Forget It Not"," Forget Me Not"," Forget Me Later"," Forget Perspective"," Forget The Colors"," Forget Them All"," Forget This"," Forget Us Not"," Iconic"," Organization"," Purgatory"," RPS Judging"," Simon Forgets"," Simon's Stages"," Souvenir"," Tallordered Keys"," The Time Keeper"," The Troll"," The Twin"," The Very Annoying Button"," Timing Is Everything"," Turn The Key"," Ultimate Custom Night","Übermodule"};

	static private int _moduleIdCounter = 1;
	private int _moduleId;
	
	void Awake () {
		_moduleId = _moduleIdCounter++;
	    string[] ingore = Boss.GetIgnoredModules(Module, ignoredModules);
        if (ingore != null)
            ignoredModules = ingore;
		for (byte i = 0; i < Buttons.Length; i++)
        {
            KMSelectable btn = Buttons[i];
            btn.OnInteract += delegate
            {
                HandlePress(btn);
                return false;
            };
        }
	}
	void HandlePress(KMSelectable btn){
		if(solved)
			return;
		int X = Array.IndexOf(Buttons,btn);
		Buttons[X].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[X].transform);
		
		if (X!=10){
			TapeShow[X]=(TapeShow[X]+1)%2;
			Text[X+2].text = TapeShow[X].ToString();
		}
		else if (X==10&&Submission) {
			bool L = true;
			for(int i=0;i<10;i++)
				if (I[i]!=TapeShow[i]) L = false;
			if(L)
			Module.HandlePass();
			if(!L)
			Module.HandleStrike();
		}
		else{
		Module.HandleStrike();
        Debug.LogFormat("[Busy Beaver #{0}]: Too Early.", _moduleId);
		}

	}
	// Use this for initialization
	void Start () {
		if (!Application.isEditor)
            SubStage = Bomb.GetSolvableModuleNames().Where(a => !ignoredModules.Contains(a)).Count();
		else
			SubStage = 1;
		if(SubStage == 0){
        Debug.LogFormat("[Busy Beaver #{0}]: No avalible modules. Autosolving.", _moduleId);
			solved = true;
			Module.HandlePass();
		}
		else{
        Debug.LogFormat("[Busy Beaver #{0}]: On this bomb we will go through {1} stages.", _moduleId, SubStage);
		StageGeneration();
		}
	}
	void StageGeneration (){
		Debug.LogFormat("[Busy Beaver #{0}]:-----------STAGE {1}-----------",_moduleId,S);
		DisplayTop = String.Concat(Alphabet[Rnd.Range(0,26)],Alphabet[Rnd.Range(0,26)]);
		Text[0].text = DisplayTop;
		Text[1].text = S.ToString();
		Debug.LogFormat("[Busy Beaver #{0}]: The displayed characters are \"{1}\"",_moduleId,DisplayTop);

			switch (DisplayTop[0]){
				case 'A': if(I[P]              ==0) I[P]=1; else I[P]=0; break;
				case 'B': if((P+1)             <=5) I[P]=1; else I[P]=0; break;
				case 'C': if(I[Mod(S,10)]      ==1) I[P]=1; else I[P]=0; break;
				case 'D': if((P+1)%2           ==0) I[P]=1; else I[P]=0; break;
				case 'E': if(I[Mod(S+P+1,10)]  ==0) I[P]=1; else I[P]=0; break;
				case 'F': if(I[P+5]            ==1) I[P]=1; else I[P]=0; break;
				case 'G': if(S%2               ==0) I[P]=1; else I[P]=0; break;
				case 'H': if((S+P+1)%2         ==1) I[P]=1; else I[P]=0; break;
				case 'I': if(I[(S+P+2)%10]     ==0) I[P]=1; else I[P]=0; break;
				case 'J': if((S-P+11)%2        ==1) I[P]=1; else I[P]=0; break;
				case 'K': if((I[P]+P+1)%2      ==0) I[P]=1; else I[P]=0; break;
				case 'L': if((S-(P+I[P])+10)%2 ==1) I[P]=1; else I[P]=0; break;
				case 'M': if(I[(S+P+I[P]+5)%10]==0) I[P]=1; else I[P]=0; break;
				case 'N': if(I[P]              ==1) I[P]=1; else I[P]=0; break;
				case 'O': if((P+1)             >=6) I[P]=1; else I[P]=0; break;
				case 'P': if(I[Mod(S,10)]      ==0) I[P]=1; else I[P]=0; break;
				case 'Q': if((P+1)%2           ==1) I[P]=1; else I[P]=0; break;
				case 'R': if(I[Mod(S+P+1,10)]  ==1) I[P]=1; else I[P]=0; break;
				case 'S': if(I[P+5]            ==0) I[P]=1; else I[P]=0; break;
				case 'T': if(S%2               ==1) I[P]=1; else I[P]=0; break;
				case 'U': if((S+P+1)%2         ==0) I[P]=1; else I[P]=0; break;
				case 'V': if(I[(S+P+2)%10]     ==1) I[P]=1; else I[P]=0; break;
				case 'W': if((S-P+11)%2        ==0) I[P]=1; else I[P]=0; break;
				case 'X': if((I[P]+P+1)%2      ==1) I[P]=1; else I[P]=0; break;
				case 'Y': if((S-(P+I[P])+10)%2 ==0) I[P]=1; else I[P]=0; break;
				case 'Z': if(I[(S+P+I[P]+5)%10]==1) I[P]=1; else I[P]=0; break;
			}
			switch (DisplayTop[1]){
				case 'A': if(I[P]              ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'B': if((P+1)             <=5) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'C': if(I[Mod(S,10)]      ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'D': if((P+1)%2           ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'E': if(I[Mod(S+P+1,10)]  ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'F': if(I[P+5]            ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'G': if(S%2               ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'H': if((S+P+1)%2         ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'I': if(I[(S+P+2)%10]     ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'J': if((S-P+11)%2        ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'K': if((I[P]+P+1)%2      ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'L': if((S-(P+I[P])+10)%2 ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'M': if(I[(S+P+I[P]+5)%10]==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'N': if(I[P]              ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'O': if((P+1)             >=6) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'P': if(I[Mod(S,10)]      ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'Q': if((P+1)%2           ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'R': if(I[Mod(S+P+1,10)]  ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'S': if(I[P+5]            ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'T': if(S%2               ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'U': if((S+P+1)%2         ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'V': if(I[(S+P+2)%10]     ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'W': if((S-P+11)%2        ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'X': if((I[P]+P+1)%2      ==1) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'Y': if((S-(P+I[P])+10)%2 ==0) P=Mod(P-1,10); else P=(P+1)%10; break;
				case 'Z': if(I[(S+P+I[P]+5)%10]==1) P=Mod(P-1,10); else P=(P+1)%10; break;
			}
			Debug.LogFormat("[Busy Beaver #{0}]: Current tape is now {1}{2}{3}{4}{5}{6}{7}{8}{9}{10}",_moduleId,I[0],I[1],I[2],I[3],I[4],I[5],I[6],I[7],I[8],I[9]);
			Debug.LogFormat("[Busy Beaver #{0}]: We have moved to position {1}",_moduleId,P+1);
			}
	
	// Update is called once per frame
	void FixedUpdate () {

		 if (S < Bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).Count() && !solved && echo >= 300)
        {
            S++;
			if (S == SubStage && !Submission){
			Debug.LogFormat("[Busy Beaver #{0}]: Its time to input. Here we go.",_moduleId);
			Text[0].text = "--";
			Text[1].text = "SUB";
			Submission = true;
		}
			else{
            StageGeneration();
			echo = 0;
			}
        }
		else{
			echo++;
		}
	}
	private int Mod(int num, int mod) {
        while (true)
        {
            //modulation for negatives
            if (num < 0)
            {
                num += mod;
                continue;
            }

            //modulation for positives
            else if (num >= mod)
            {
                num -= mod;
                continue;
            }

            //once it reaches here, we know it's modulated and we can return it
            return num;
        }
	}
	#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit 1101001010 (Sets the binary to 1101001010, then submits.)";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*submit\s+([01 ])+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {    
            var Binary = m.Groups[1].Value.ToList();
			Debug.Log(Binary.Join(", "));
            if(Binary.Length != 10)
              yield return "sendtochaterror Incorrect Syntax. Must be exactly 10 digits.";
            yield return null;  // acknowledge to TP that the command was valid

            for (int i = 0; i < 10; i++)
            {
                if(Text[i+2].text!=Binary[i].ToString())
                    Buttons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            Buttons[10].OnInteract();
        }
    }
	IEnumerator TwitchHandleForcedSolve()
    {
		while (!Submission) //Wait until submission time
            yield return true;
		for(int i=0;i<10;i++){
		if(Text[i+2].text!=I[i].ToString())
			Buttons[i].OnInteract();
		yield return new WaitForSeconds(.1f);
		}
		Buttons[10].OnInteract();
    }
}
