using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerScript : NetworkBehaviour
{
    public double playerTimer;
    
    private GameScript gameScript;
    public Camera camera;
    private TextMesh textMesh;
    
    [SyncVar(hook = nameof(SyncLife))] 
    private int life = 100;
    
    [SyncVar(hook = nameof(SyncExp))] 
    private int exp = 0;
    
    // Encounter actions: (0) None, (1) Share, (2) Fight, (3) Flee, (4) Steal
    [SyncVar(hook = nameof(SyncAction))]
    private int encounterAction = 0;

    private int maxLife = 100;
    private int maxExp = 1000;
    
    public int[] gridPos = {0, 0};

    // How many moves do I still have this round? When all moves are used, player is set to waiting.
    public int availableMoves = 0;
    public int availableActions = 0;
    
    [SyncVar]
    public bool waiting = false;
    
    [SyncVar]
    public bool inExploration = true;
    
    [SyncVar]
    public bool inEncounter = false;

    private int harvestGain = 20;
    private int harvestAmount = 25;

    private int sowCost = 33;
    private int sowAmount = 25;
    
    private int convertCost = 50;
    private int convertAmount = 10;
    
    // UI elements!
    private Slider lifeSlider;
    private Slider expSlider;

    private GameObject encounterMessage;
    private TextMeshProUGUI encounterText;
    
    private double encounterMessageStart;
    private double encounterMessageDuration = 5;

    private List<GameObject> explorationUI;
    private List<GameObject> encounterUI;

    private PlayerControlScript playerControlScript;
    
    // Start is called before the first frame update
    void Start()
    {
        playerTimer = 0;
        
        // Get reference to Game Controller to access specific cells
        GameObject gameController = GameObject.Find("Game Controller");
        gameScript = gameController.GetComponent<GameScript>();

        // Get Reference to the Text Mesh.
        //textMesh = transform.GetComponentInChildren<TextMesh>();
        //textMesh.text = "" + life;

        gridPos = gameScript.ConvertToGridPos(transform.position);
        
        // Stuff reserved for the local player:
        if (!isLocalPlayer) return;
        
        lifeSlider = GameObject.Find("HealthBar").GetComponent<Slider>();
        expSlider = GameObject.Find("XP").GetComponent<Slider>();
        
        var gameOverlay = GameObject.Find("GameOverlayMenus");

        explorationUI = new List<GameObject>();
        encounterUI = new List<GameObject>();

        explorationUI.Add(gameOverlay.transform.Find("HarvestButton").gameObject);
        explorationUI.Add(gameOverlay.transform.Find("ConvertButton").gameObject);
        explorationUI.Add(gameOverlay.transform.Find("SowButton").gameObject);
        
        encounterUI.Add(gameOverlay.transform.Find("AttackButton").gameObject);
        encounterUI.Add(gameOverlay.transform.Find("FleeButton").gameObject);
        encounterUI.Add(gameOverlay.transform.Find("ShareButton").gameObject);
        encounterUI.Add(gameOverlay.transform.Find("StealButton").gameObject);
        encounterUI.Add(gameOverlay.transform.Find("EncounterWarning").gameObject);
        
        encounterMessage = gameOverlay.transform.Find("EncounterMessage").gameObject;
        encounterText = encounterMessage.transform.GetComponentInChildren<TextMeshProUGUI>();

        lifeSlider.value = life;
        expSlider.value = exp;

        playerControlScript = GetComponent<PlayerControlScript>();
        
        playerControlScript.enabled = true;
        camera.enabled = true;

        gameScript.UpdateVisibility(gridPos);
        
        AddPlayerToCell(gridPos);
    }

    // Update is called once per frame
    void Update()
    {
        playerTimer += Time.deltaTime;
        
        if (!isLocalPlayer) return;
        HandleMessages();
    }

    private void HandleMessages()
    {
        if (!encounterMessage.activeSelf) return;
        
        if (playerTimer - encounterMessageStart > encounterMessageDuration)
        {
            encounterMessage.SetActive(false);
        }
    }

    public void UpdatePos(int[] move)
    {
        // Remove 1 population from previous cell
        RemovePlayerFromCell(gridPos);

        // Update and restrict player's grid coordinates
        gridPos[0] += move[0];
        gridPos[1] += move[1];

        gridPos = gameScript.GetRealPos(gridPos);
        
        // Add population to the new cell and update player's visibility
        AddPlayerToCell(gridPos);
        gameScript.UpdateVisibility(gridPos, move);
        
        UpdateLife(-5);
    }

    public void HarvestCell()
    {
        // Only allow harvest if player is not at full health
        if (life >= maxLife) return;
        
        // Values subject to changing
        UpdateCellLife(gridPos, -harvestAmount);
        UpdateLife(+harvestGain);
    }
    
    public void SowCells()
    {
        // Only allow sow if player has 33 life to spare
        if (life < sowCost) return;
        
        var surrounding = gameScript.GetSurroundingPositions(gridPos);
        
        // Update life of all surrounding cells (exclude current cell)
        foreach (var pos in surrounding)
        {
            if (pos[0] != gridPos[0] || pos[1] != gridPos[1]) UpdateCellLife(pos, +sowAmount);
        }
        
        UpdateLife(-sowCost);
    }

    public void ConvertLife()
    {
        if (life <= convertCost) return;
        
        UpdateLife(-convertCost);
        UpdateExp(+convertAmount);
    }

    public void ToggleEncounter()
    {
        if (!isLocalPlayer) return;
        
        inEncounter = !inEncounter;
        inExploration = !inExploration;
        
        // Default: no action chosen
        encounterAction = 0;
        
        ToggleEncounterUI();
    }

    private void ToggleEncounterUI()
    {
        if (!isLocalPlayer) return;
        
        foreach (var elem in explorationUI)
        {
            elem.SetActive(!elem.activeSelf);
        }
        
        foreach (var elem in encounterUI)
        {
            elem.SetActive(!elem.activeSelf);
        }
    }

    public bool CanMove()
    {
        // Check available moves later
        return inExploration;
    }

    public void DisplayMessage(string message)
    {
        encounterMessage.SetActive(true);
        encounterText.text = message;

        if (!isLocalPlayer) return;
        encounterMessageStart = playerTimer;
    }

    // BTW: Z and X to mess with player's life!
    
    [Command]
    public void UpdateLife(int amount)
    {
        life += amount;
        
        if (life < 0) life = 0;
        else if (life > maxLife) life = maxLife;
    }

    [Command]
    public void UpdateExp(int amount)
    {
        exp += amount;
        
        if (exp < 0) exp = 0;
        else if (exp > maxExp) exp = maxExp;
        
        if (!isLocalPlayer) return;
        
        expSlider.value = exp;
    }
    
    [Command]
    public void UpdateAction(int action)
    {
        encounterAction = action;
    }
    
    public void UpdateMove(int[] move)
    {
        playerControlScript.Move(move);
    }
    
    private void SyncLife(int oldValue, int newValue)
    {
        life = newValue;
        //textMesh.text = "" + life;
        
        if (!isLocalPlayer) return;
        lifeSlider.value = life;
    }

    private void SyncExp(int oldValue, int newValue)
    {
        exp = newValue;

        if (!isLocalPlayer) return;
        expSlider.value = exp;
    }
    
    private void SyncAction(int oldValue, int newValue)
    {
        encounterAction = newValue;
    }

    [Command]
    public void UpdateCellLife(int[] gridPos, int amount)
    {
        UpdateCellLifeClient(gridPos, amount);
    }

    [ClientRpc]
    private void UpdateCellLifeClient(int[] gridPos, int amount)
    {
        gameScript.UpdateCellLife(gridPos, amount);
    }
    
    [Command]
    public void AddPlayerToCell(int[] gridPos)
    {
        AddPlayerToCellClient(gridPos);
    }

    [ClientRpc]
    private void AddPlayerToCellClient(int[] gridPos)
    {
        gameScript.AddPlayerToCell(gridPos, this);
    }
    
    [Command]
    public void RemovePlayerFromCell(int[] gridPos)
    {
        RemovePlayerFromCellClient(gridPos);
    }

    [ClientRpc]
    private void RemovePlayerFromCellClient(int[] gridPos)
    {
        gameScript.RemovePlayerFromCell(gridPos, this);
    }

    public int GetLife()
    {
        return life;
    }
    
    public int GetExp()
    {
        return exp;
    }
    
    public int GetEncounterAction()
    {
        return encounterAction;
    }
}