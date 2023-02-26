using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public partial class idExchange : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBossModule boss;
    public KMBombModule module;

    public KMSelectable[] cards;
    public KMSelectable[] portraitButtons;
    public KMSelectable displayButton;
    public KMSelectable crosshair;
    public Renderer[] mainPortraits;
    public TextMesh screenText;
    public Texture[] portraitTextures;
    public Texture[] newPortraitTextures;
    public GameObject[] tokenIcons;
    public GameObject hidable;
    public GameObject garnet;
    public GameObject logo;

    private role[] playerRoles = new role[13];
    private int[] playerScores = new int[13];
    private int lastConvict = -1;
    private bool[] tokens = new bool[13];
    private bool[] selectedTokens = new bool[13];
    private List<stageInfo> allStages = new List<stageInfo>();
    private int recoveryDisplay;
    private bool canRecover;

    private int stage;
    private bool solvable;
    private int stageCount;
    private int currentlySolved;
    private string[] ignoreList;
    private bool readyToAdvance = true;
    private const float waitTime = 3.5f; // change this as you wish

    private static readonly string[] letterRows = new string[13] { "AZ", "BY", "CX", "DW", "EV", "FU", "GT", "HS", "IR", "JQ", "KP", "LO", "MN" };
    private string[] playerNames = new string[13] { "Jungmoon", "Yeonseung", "Jinho", "Dongmin", "Kyunghoon", "Kyungran", "Yoohyun", "Junseok", "Sangmin", "Yohwan", "Yoonsun", "Hyunmin", "Junghyun" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    #region ModSettings
    idExchangeSettings Settings = new idExchangeSettings();
#pragma warning disable 414
    private static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
      new Dictionary<string, object>
      {
        { "Filename", "ID Exchange Settings.json"},
        { "Name", "ID Exchange" },
        { "Listings", new List<Dictionary<string, object>>
        {
          new Dictionary<string, object>
          {
            { "Key", "ClassicMode" },
            { "Text", "Enable classic mode and display the old icons"}
          }
        }}
      }
    };
#pragma warning restore 414

    private class idExchangeSettings
    {
        public bool ClassicMode = false;
    }
    #endregion

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        var modConfig = new modConfig<idExchangeSettings>("Id Exchange Settings");
        Settings = modConfig.read();

        foreach (KMSelectable card in cards)
            card.OnInteract += delegate () { PressCard(card); return false; };
        foreach (KMSelectable portrait in portraitButtons)
            portrait.OnInteract += delegate () { PressPortrait(portrait); return false; };
        displayButton.OnInteract += delegate () { PressDisplay(); return false; };
        crosshair.OnInteract += delegate () { PressCrosshair(); return false; };
        for (int i = 0; i < 13; i++)
        {
            portraitButtons[i].GetComponent<Renderer>().material.mainTexture = Settings.ClassicMode ? portraitTextures[i] : newPortraitTextures[i];
            portraitButtons[i].transform.localScale = Settings.ClassicMode ? new Vector3(.025f, .024423076923076922f, .025f) : new Vector3(.025f, .02994723f, .025f);
            tokenIcons[i].SetActive(false);
        }
        mainPortraits[0].transform.localScale = Settings.ClassicMode ? new Vector3(.05f, .048846153846153845f, .05f) : new Vector3(.05f, .05989446f, .05f);
        mainPortraits[1].transform.localScale = Settings.ClassicMode ? new Vector3(.05f, .048846153846153845f, .05f) : new Vector3(.05f, .05989446f, .05f);
        if (!Settings.ClassicMode)
            for (int i = 0; i < playerNames.Length; i++)
                playerNames[i] = "Player " + (i + 1);
    }

    private void Start()
    {
        if (ignoreList == null)
        {
            StartCoroutine(DisableStuff());
            ignoreList = boss.GetIgnoredModules("ID Exchange", new string[]
            {
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "Busy Beaver",
                "Concentration",
                "Duck Konundrum",
                "Don't Touch Anything",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Our Voices",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "ID Exchange",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForest",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "The Twin",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout"
            });
        }
        stageCount = bomb.GetSolvableModuleNames().Count() - bomb.GetSolvableModuleNames().Count(x => ignoreList.Contains(x));
        if (stageCount == 0)
        {
            Failsafe();
            return;
        }

        for (int i = 0; i < 13; i++)
            playerRoles[i] = role.unassigned;
        var sn = bomb.GetSerialNumber();
        var lastRow = 0;
        for (int i = 0; i < 6; i++)
        {
            var character = i == 0 && char.IsNumber(sn[i]) ? "ABCDEFGHIJ"[int.Parse(sn[i].ToString())] : sn[i];
            Debug.LogFormat("[ID Exchange #{0}] SN character {1}: {2}", moduleId, i + 1, character);
            var thisRow = char.IsNumber(character) ? lastRow : Array.IndexOf(letterRows, letterRows.First(x => x.Contains(character)));
            if (char.IsNumber(character))
                for (int j = 0; j < int.Parse(character.ToString()); j++)
                    thisRow = (thisRow + 1) % 13;
            while (playerRoles[thisRow] != role.unassigned)
                thisRow = (thisRow + 1) % 13;
            playerRoles[thisRow] = i == 5 ? role.convict : role.noble;
            Debug.LogFormat("[ID Exchange #{0}] Assigned {1} the role of {2}.", moduleId, playerNames[thisRow], playerRoles[thisRow]);
            lastRow = thisRow;
        }
        for (int i = 0; i < 13; i++)
            if (playerRoles[i] == role.unassigned)
                playerRoles[i] = role.commoner;
        Debug.LogFormat("[ID Exchange #{0}] Roles in order: {1}.", moduleId, playerRoles.Join(", "));
        GenerateStage();
    }

    private void GenerateStage()
    {
        readyToAdvance = false;
        StartCoroutine(StageTimer());
        Debug.LogFormat("[ID Exchange #{0}] Stage {1}:", moduleId, stage + 1);
        var playerA = rnd.Range(0, 13);
        var playerB = rnd.Range(0, 13);
        while (playerB == playerA)
            playerB = rnd.Range(0, 13);
        allStages.Add(new stageInfo(stage, playerA, playerB));
        var roleA = playerRoles[playerA];
        var roleB = playerRoles[playerB];
        Debug.LogFormat("[ID Exchange #{0}] {1} ({2}) swaps with {3} ({4}).", moduleId, playerNames[playerA], roleA, playerNames[playerB], roleB);
        var bothRoles = new role[] { roleA, roleB };
        if (bothRoles.Contains(role.commoner) && bothRoles.Contains(role.noble))
        {
            var noblePlayer = Array.IndexOf(bothRoles, role.noble);
            playerScores[noblePlayer == 0 ? playerA : playerB] += 1;
            Debug.LogFormat("[ID Exchange #{0}] {1} gets 1 point.", moduleId, playerNames[noblePlayer == 0 ? playerA : playerB]);
        }
        else if (bothRoles.Contains(role.convict) && bothRoles.Contains(role.commoner))
        {
            var convictPlayer = Array.IndexOf(bothRoles, role.convict);
            playerScores[convictPlayer == 0 ? playerA : playerB] += 2;
            Debug.LogFormat("[ID Exchange #{0}] {1} gets 2 points.", moduleId, playerNames[convictPlayer == 0 ? playerA : playerB]);
        }
        else
            Debug.LogFormat("[ID Exchange #{0}] No points are awarded.", moduleId);
        playerRoles[playerB] = roleA;
        playerRoles[playerA] = roleB;
        if (bothRoles.Contains(role.convict))
            lastConvict = Array.IndexOf(bothRoles, role.convict) == 0 ? playerA : playerB;
        mainPortraits[0].material.mainTexture = Settings.ClassicMode ? portraitTextures[playerA] : newPortraitTextures[playerA];
        mainPortraits[1].material.mainTexture = Settings.ClassicMode ? portraitTextures[playerB] : newPortraitTextures[playerB];
        screenText.text = ((stage + 1) % 1000).ToString("000");
    }

    private IEnumerator StageTimer()
    {
        var elapsed = 0f;
        while (elapsed < waitTime)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        readyToAdvance = true;
    }

    private void ReadyToSolve()
    {
        solvable = true;
        for (int i = 0; i < 2; i++)
        {
            mainPortraits[i].gameObject.SetActive(false);
            cards[i].gameObject.SetActive(false);
            logo.SetActive(false);
        }
        mainPortraits[0].material.mainTexture = Settings.ClassicMode ? portraitTextures[allStages[0].playerA] : newPortraitTextures[allStages[0].playerA];
        mainPortraits[1].material.mainTexture = Settings.ClassicMode ? portraitTextures[allStages[0].playerB] : newPortraitTextures[allStages[0].playerB];
        screenText.text = "???";
        hidable.SetActive(true);
        if (lastConvict != -1)
        {
            Debug.LogFormat("[ID Exchange #{0}] {1} gets 3 points for being the last player to get rid of the convict card.", moduleId, playerNames[lastConvict]);
            playerScores[lastConvict] += 3;
        }
        Debug.LogFormat("[ID Exchange #{0}] Submission phase entered:", moduleId);
        Debug.LogFormat("[ID Exchange #{0}] Scores in order: {1}", moduleId, playerScores.Join(", "));
        var validPlayers = Enumerable.Range(0, 13).Where(x => x != Array.IndexOf(playerRoles, role.convict)).ToArray();
        var winningScore = validPlayers.Select(x => playerScores[x]).Max();
        if (validPlayers.Select(x => playerScores[x]).Count(x => x == winningScore) == 12)
            Debug.LogFormat("[ID Exchange #{0}] Every non-convict player tied, and nobody gets any tokens of life.", moduleId);
        else
        {
            for (int i = 0; i < 13; i++)
            {
                if (playerScores[validPlayers[i]] == winningScore)
                {
                    Debug.LogFormat("[ID Exchange #{0}] {1} gets a token of life for having one of the highest scores.", moduleId, playerNames[validPlayers[i]]);
                    tokens[validPlayers[i]] = true;
                }
            }
        }
        Debug.LogFormat("[ID Exchange #{0}] {1} currently holds the convict card.", moduleId, playerNames[Array.IndexOf(playerRoles, role.convict)]);
    }

    private void Failsafe()
    {
        Debug.LogFormat("[ID Exchange #{0}] No non-ignored modules detected, automatically solving...", moduleId);
        garnet.SetActive(true);
        module.HandlePass();
        moduleSolved = true;
        for (int i = 0; i < 2; i++)
        {
            mainPortraits[i].gameObject.SetActive(false);
            cards[i].gameObject.SetActive(false);
            logo.SetActive(false);
        }
        base.gameObject.SetActive(false);
    }

    private void PressCard(KMSelectable card)
    {
        var ix = Array.IndexOf(cards, card);
        card.AddInteractionPunch(.25f);
        if (!solvable)
            return;
        audio.PlaySoundAtTransform("card" + rnd.Range(1, 6), card.transform);
        if (ix == 0)
            recoveryDisplay = recoveryDisplay == 0 ? 0 : recoveryDisplay - 1;
        else
            recoveryDisplay = recoveryDisplay == stageCount - 1 ? recoveryDisplay : recoveryDisplay + 1;
        var thisStage = allStages[recoveryDisplay];
        mainPortraits[0].material.mainTexture = Settings.ClassicMode ? portraitTextures[thisStage.playerA] : newPortraitTextures[thisStage.playerA];
        mainPortraits[1].material.mainTexture = Settings.ClassicMode ? portraitTextures[thisStage.playerB] : newPortraitTextures[thisStage.playerB];
        screenText.text = ((thisStage.stageNumber + 1) % 1000).ToString("000");
    }

    private void PressPortrait(KMSelectable portrait)
    {
        if (moduleSolved || !solvable)
            return;
        var ix = Array.IndexOf(portraitButtons, portrait);
        if (crosshair.gameObject.activeSelf)
        {
            portrait.AddInteractionPunch(.25f);
            selectedTokens[ix] = !selectedTokens[ix];
            tokenIcons[ix].SetActive(selectedTokens[ix]);
            audio.PlaySoundAtTransform("coin" + rnd.Range(1, 4), portrait.transform);
        }
        else
        {
            if (ix == Array.IndexOf(playerRoles, role.convict))
            {
                Debug.LogFormat("[ID Exchange #{0}] You correctly identified {1} as the convict. Module solved!", moduleId, playerNames[ix]);
                module.HandlePass();
                audio.PlaySoundAtTransform("solve", transform);
                hidable.SetActive(false);
                displayButton.gameObject.SetActive(false);
                garnet.SetActive(true);
            }
            else
            {
                Debug.LogFormat("[ID Exchange #{0}] You incorrectly identified {1} as the convict. Strike! Stage recovery unlocked.", moduleId, playerNames[ix]);
                module.HandleStrike();
                canRecover = true;
            }
        }
    }

    private void PressDisplay()
    {
        displayButton.AddInteractionPunch(.25f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, displayButton.transform);
        if (moduleSolved || !solvable)
            return;
        if (hidable.activeSelf)
        {
            if (!canRecover)
                return;
            hidable.SetActive(false);
            screenText.text = "001";
            for (int i = 0; i < 2; i++)
            {
                mainPortraits[i].gameObject.SetActive(true);
                cards[i].gameObject.SetActive(true);
            }
            logo.SetActive(true);
        }
        else
        {
            canRecover = false;
            for (int i = 0; i < 2; i++)
            {
                mainPortraits[i].gameObject.SetActive(false);
                cards[i].gameObject.SetActive(false);
            }
            logo.SetActive(false);
            hidable.SetActive(true);
        }
    }

    private void PressCrosshair()
    {
        if (moduleSolved || !solvable)
            return;
        crosshair.AddInteractionPunch(.25f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, crosshair.transform);
        if (selectedTokens.SequenceEqual(tokens))
        {
            Debug.LogFormat("[ID Exchange #{0}] You submitted the correct tokens. Now identify the convict, who is {1}.", moduleId, playerNames[Array.IndexOf(playerRoles, role.convict)]);
            crosshair.gameObject.SetActive(false);
            foreach (GameObject token in tokenIcons)
                token.SetActive(false);
        }
        else
        {
            Debug.LogFormat("[ID Exchange #{0}] You submitted the incorrect tokens. Strike! Stage recovery unlocked.", moduleId);
            module.HandleStrike();
            canRecover = true;
        }
    }

    private enum role
    {
        commoner,
        noble,
        convict,
        unassigned
    }

    private void Update()
    {
        if (solvable || moduleSolved || !readyToAdvance)
            return;
        currentlySolved = bomb.GetSolvedModuleNames().Where(x => !ignoreList.Contains(x)).Count();
        if (currentlySolved == stage || solvable)
            return;
        if (stage <= currentlySolved)
        {
            stage++;
            if (stage == stageCount)
                ReadyToSolve();
            else
                GenerateStage();
        }
    }

    private string GetLatestSolve(List<string> a, List<string> b)
    {
        var z = "";
        for (int i = 0; i < b.Count; i++)
            a.Remove(b.ElementAt(i));
        z = a.ElementAt(0);
        return z;
    }

    private class stageInfo
    {
        public int stageNumber { get; set; }
        public int playerA { get; set; }
        public int playerB { get; set; }

        public stageInfo(int s, int a, int b)
        {
            stageNumber = s;
            playerA = a;
            playerB = b;
        }
    }

    private IEnumerator DisableStuff()
    {
        yield return null;
        hidable.gameObject.SetActive(false);
        if (!moduleSolved)
            garnet.SetActive(false);
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} Jungmoon [Press Jungmoon's portrait, any name on the module can be used. When not in classic mode, players are named \"player x\" where x is their position in reading order.] | !{0} display [Press the stage counter] | !{0} crosshair [Press the crosshair] | !{0} <left/right> [Press that blank card] | !{0} stage <xx> [During stage recovery, press the blank cards until stage xx is displayed]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        var validStageNumbers = Enumerable.Range(1, stageCount).Select(x => x.ToString()).ToArray();
        if (input == "display")
        {
            yield return null;
            displayButton.OnInteract();
        }
        else if (input == "crosshair")
        {
            if (!crosshair.gameObject.activeSelf)
            {
                yield return "sendtochaterror You can't do that yet.";
                yield break;
            }
            yield return null;
            crosshair.OnInteract();
        }
        else if (input == "left")
        {
            yield return null;
            cards[0].OnInteract();
        }
        else if (input == "right")
        {
            yield return null;
            cards[1].OnInteract();
        }
        else if (playerNames.Select(x => x.ToLowerInvariant()).Contains(input))
        {
            if (!solvable)
            {
                yield return "sendtochaterror You can't do that yet.";
                yield break;
            }
            yield return null;
            var lowercaseNames = playerNames.Select(x => x.ToLowerInvariant()).ToArray();
            portraitButtons[Array.IndexOf(lowercaseNames, input)].OnInteract();
        }
        else if (input.StartsWith("stage "))
        {
            if (!validStageNumbers.Contains(input.Substring(6)))
            {
                yield return "sendtochaterror Not a valid stage number.";
                yield break;
            }
            var number = int.Parse(input.Substring(6));
            if (recoveryDisplay > number)
            {
                while (recoveryDisplay != number)
                {
                    yield return new WaitForSeconds(.1f);
                    cards[0].OnInteract();
                }
            }
            else if (recoveryDisplay < number)
            {
                while (recoveryDisplay != number)
                {
                    yield return new WaitForSeconds(.1f);
                    cards[1].OnInteract();
                }
            }
            else
            {
                yield return "sendtochaterror That stage is already displayed, stoopid.";
                yield break;
            }
        }
        else
            yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (!solvable)
        {
            yield return null;
            yield return true;
        }
        if (!crosshair.gameObject.activeSelf)
            goto submitConvict;
        for (int i = 0; i < 13; i++)
        {
            if (selectedTokens[i] != tokens[i])
            {
                yield return new WaitForSeconds(.1f);
                portraitButtons[i].OnInteract();
            }
        }
        yield return new WaitForSeconds(.1f);
        crosshair.OnInteract();
    submitConvict:
        yield return new WaitForSeconds(.1f);
        portraitButtons[Array.IndexOf(playerRoles, role.convict)].OnInteract();
    }
}
