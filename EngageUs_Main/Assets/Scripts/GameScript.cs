using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mirror;
using UnityEngine;

public class GameScript : NetworkBehaviour
{
    /*
    Script to attach to the Game Overseer
    */

    public double gameTimer;
    
    private struct Encounter
    {
        // Start time of the encounter
        public double startTime;
        
        // Reference to both players
        public List<PlayerScript> playerReferences;
        public List<NetworkConnection> clientConnections;
    }

    private List<Encounter> currentEncounters;

    private GameObject[][] cells;
    private CellScript[][] cellScripts;
    
    public GameObject cellPrefab;
    
    public int mapScale = 1;
    public int mapSize = 13;

    private float encounterDuration = 10;

    // Start is called before the first frame update
    void Start()
    {
        currentEncounters = new List<Encounter>();

        gameTimer = 0;
        DrawGridMap();
    }

    // Update is called once per frame
    void Update()
    {
        gameTimer += Time.deltaTime;
        HandleEncounters();
    }
    
    // Set-up Grid Map
    private void DrawGridMap()
    {
        // Pre-allocate space for the cell objects and cell scripts (rows)
        cells = new GameObject[mapSize][];
        cellScripts = new CellScript[mapSize][];
        
        //cells = new SyncList<GameObject>[mapSize];
        //cellScripts = new SyncList<CellScript>[mapSize];
        
        // Iterate rows
        for (int i = 0; i < mapSize; i++)
        {
            // Pre-allocate space in each row (columns) for cells and cell scripts
            cells[i] = new GameObject[mapSize];
            cellScripts[i] = new CellScript[mapSize];
            
            //cells[i] = new SyncList<GameObject>();
            //cellScripts[i] = new SyncList<CellScript>();
            
            // Iterate columns
            for (int j = 0; j < mapSize; j++)
            {
                // Set the real position of the cell (based on cell-scale)
                Vector3 pos = new Vector3(i*mapScale,0,j*mapScale);
                
                // Instantiate the prefab into the world
                GameObject cell = Instantiate(cellPrefab, pos, Quaternion.identity);
                cells[i][j] = cell;
                //cells[i].Add(cell);
                
                // Get the script associated with each prefab, for later use
                CellScript cellScript = cells[i][j].GetComponent<CellScript>();
                cellScripts[i][j] = cellScript;
                //cellScripts[i].Add(cellScript);
                
                // Set the correct scale transform for the cell
                cellScripts[i][j].SetScale(mapScale);
            }
        }
    }
    
    // Server only function!
    public void StartEncounter(List<PlayerScript> playerReferences)
    {
        if (!NetworkServer.active) return;

        var encounter = new Encounter
        {
            startTime = gameTimer,
            playerReferences = playerReferences,
            
            clientConnections = new List<NetworkConnection>
            {
                playerReferences[0].gameObject.GetComponent<NetworkIdentity>().connectionToClient,
                playerReferences[1].gameObject.GetComponent<NetworkIdentity>().connectionToClient
            }
        };

        currentEncounters.Add(encounter);

        TogglePlayerEncounter(encounter.clientConnections[0], encounter.playerReferences[0].gameObject);
        TogglePlayerEncounter(encounter.clientConnections[1], encounter.playerReferences[1].gameObject);
        
        Debug.Log("ENCOUNTER!");
    }

    private void HandleEncounters()
    {
        if (!NetworkServer.active) return;
        
        for (var i = 0; i < currentEncounters.Count; i++)
        {
            // If time has run out, finish encounter
            if (gameTimer - currentEncounters[i].startTime > encounterDuration)
            {
                EndEncounter(i);
                Debug.Log("ENDING ENCOUNTER");
            }
        }
    }
    
    private void EndEncounter(int index)
    {
        if (!NetworkServer.active) return;
        
        var encounter = currentEncounters[index];
        
        DecideOutcome(encounter);
        
        TogglePlayerEncounter(encounter.clientConnections[0], encounter.playerReferences[0].gameObject);
        TogglePlayerEncounter(encounter.clientConnections[1], encounter.playerReferences[1].gameObject);

        Debug.Log("OUTCOME!");
        currentEncounters.Remove(encounter);
    }

    [TargetRpc]
    private void TogglePlayerEncounter(NetworkConnection target, GameObject player)
    {
        player.GetComponent<PlayerScript>().ToggleEncounter();
    }

    [TargetRpc]
    private void SendOutcome(NetworkConnection target, GameObject player, int lifeUpdate, int[] move, string message)
    {
        var script = player.GetComponent<PlayerScript>();
        
        script.UpdateLife(lifeUpdate);
        script.UpdateMove(move);
        script.DisplayMessage(message);
    }

    public void UpdateVisibility(int[] gridPos)
    {
        var surrounding = GetSurroundingPositions(gridPos);

        foreach (var pos in surrounding)
        {
            cellScripts[pos[0]][pos[1]].ToggleVisibility();
        }
    }

    public void UpdateVisibility(int[] newPos, int[] move)
    {
        int[] oldPos = GetRealPos(new int[] {newPos[0] - move[0], newPos[1] - move[1]});

        UpdateVisibility(oldPos);
        UpdateVisibility(newPos);
    }

    public int[][] GetSurroundingPositions(int[] gridPos)
    {
        var surrounding = new int[9][];

        for (var i = 0; i < 3; i++)
        {
            var z = gridPos[1] - 1 + i;
            var ind = i * 3;

            surrounding[ind] = GetRealPos(new int[] {gridPos[0] - 1, z});
            surrounding[ind + 1] = GetRealPos(new int[] {gridPos[0], z});
            surrounding[ind + 2] = GetRealPos(new int[] {gridPos[0] + 1, z});    
        }

        return surrounding;
    }

    public int[] GetRealPos(int[] gridPos)
    {
        gridPos[0] = gridPos[0] % 13;
        gridPos[1] = gridPos[1] % 13;
        
        if (gridPos[0] < 0) gridPos[0] = 12;
        if (gridPos[1] < 0) gridPos[1] = 12;
        
        return gridPos;
    }
    
    public int[] ConvertToGridPos(Vector3 pos)
    {
        int[] gridPos = new int[2];
        
        gridPos[0] = Mathf.RoundToInt(pos.x);
        gridPos[1] = Mathf.RoundToInt(pos.z);

        return GetRealPos(gridPos);
    }
    
    public void UpdateCellLife(int[] gridPos, int amount)
    {
        cellScripts[gridPos[0]][gridPos[1]].UpdateLife(amount);
    }

    public void AddPlayerToCell(int[] gridPos, PlayerScript script)
    {
        cellScripts[gridPos[0]][gridPos[1]].AddPlayer(script);
    }
    
    public void RemovePlayerFromCell(int[] gridPos, PlayerScript script)
    {
        cellScripts[gridPos[0]][gridPos[1]].RemovePlayer(script);
    }
    
    public int GetCellLife(int[] gridPos)
    {
        return cellScripts[gridPos[0]][gridPos[1]].GetLife();
    }

    public bool IsLockedCell(int[] gridPos)
    {
        return cellScripts[gridPos[0]][gridPos[1]].IsLocked();
    }

    private void DecideOutcome(Encounter encounter)
    {
        var actions = new int[2]
        {
            encounter.playerReferences[0].GetEncounterAction(),
            encounter.playerReferences[1].GetEncounterAction()
        };

        var lifeUpdates = new int[] {0, 0};
        var messages = new string[] {"", ""};
        
        var flee = new int[] {0, 0};
        
        var moves = new int[][]
        {
            new int[] {0, 0},
            new int[] {0, 0}
        };
        
        var lifeValues = new int[]
        {
            encounter.playerReferences[0].GetLife(),
            encounter.playerReferences[1].GetLife()
        };
        
        var expValues = new int[]
        {
            encounter.playerReferences[0].GetExp(),
            encounter.playerReferences[1].GetExp()
        };

        var randValues = new int[]
        {
            Random.Range(0, 100),
            Random.Range(0, 100)
        };

        switch ("" + actions[0] + actions[1])
        {
            // Nothing x Nothing (NULL)
            case ("00"):
                messages[0] = "Act quickly in encounters!\nYour opponent chose NOTHING, and nothing happened!";
                messages[1] = "Act quickly in encounters!\nYour opponent chose NOTHING, and nothing happened!";
                break;

            // Nothing x Fight (player 2 wins fight automatically)
            case ("01"):
                lifeUpdates[0] -= 50;
                messages[0] = "Act quickly in encounters!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                messages[1] = "FIGHT successful!\nYour opponent chose NOTHING, taking 50HP damage!";
                break;

            // Nothing x Flee (player 2 flees)
            case ("02"):
                flee[1] = 1;
                lifeUpdates[1] -= 25;
                messages[0] = "Act quickly in encounters!\nYour opponent chose to FLEE, and escaped!";
                messages[1] = "FLEE successful!\nYour opponent chose NOTHING, and you escaped!";
                break;
            
            // Nothing x Share (player 2 can't share)
            case ("03"):
                messages[0] = "Act quickly in encounters!\nYour opponent chose to SHARE, but nothing happened!";
                messages[1] = "SHARE failed!\nYour opponent chose NOTHING, and nothing happened!";
                break;
            
            // Nothing x Steal (player 2 steals with 100% chance)
            case ("04"):
                lifeUpdates[0] -= 25;
                lifeUpdates[1] += 25;
                messages[0] = "Act quickly in encounters!\nYour opponent chose to STEAL, stealing 25HP from you!";
                messages[1] = "STEAL successful!\nYour opponent chose NOTHING, and you stole 25HP from them!";
                break;
            
            // Fight x Nothing (player 1 wins fight automatically)
            case ("10"):
                lifeUpdates[1] -= 50;
                messages[0] = "FIGHT successful!\nYour opponent chose NOTHING, taking 50HP damage!";
                messages[1] = "Act quickly in encounters!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                break;

            // Fight x Fight (players fight)
            case ("11"):
                if (expValues[0] == expValues[1])
                { // Both players lose 25
                    lifeUpdates[0] -= 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "FIGHT draw!\nYour opponent chose to FIGHT, both taking 25HP damage!";
                    messages[1] = "FIGHT draw!\nYour opponent chose to FIGHT, both taking 25HP damage!";
                }
                else if (expValues[0] > expValues[1])
                { // Player 1 wins
                    lifeUpdates[1] -= 50;
                    messages[0] = "FIGHT successful!\nYour opponent chose to FIGHT, taking 50HP damage!";
                    messages[1] = "FIGHT failed!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                }
                else if (expValues[0] < expValues[1])
                { // Player 2 wins
                    lifeUpdates[0] -= 50;
                    messages[0] = "FIGHT failed!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                    messages[1] = "FIGHT successful!\nYour opponent chose to FIGHT, taking 50HP damage!";
                }

                break;
            
            // Fight x Flee (player 2 flees)
            case ("12"):
                flee[1] = 1;
                lifeUpdates[1] -= 25;
                messages[0] = "FIGHT failed!\nYour opponent chose to FLEE, and escaped!";
                messages[1] = "FLEE successful!\nYour opponent chose to FIGHT, but you escaped!";
                break;
            
            // Fight x Share (player 1 wins fight automatically)
            case ("13"):
                lifeUpdates[1] -= 50;
                messages[0] = "FIGHT successful!\nYour opponent chose to SHARE, taking 50HP damage!";
                messages[1] = "SHARE failed!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                break;
            
            // Fight x Steal (player 1 wins fight automatically)
            case ("14"):
                lifeUpdates[1] -= 50;
                messages[0] = "FIGHT successful!\nYour opponent chose to STEAL, taking 50HP damage!";
                messages[1] = "STEAL failed!\nYour opponent chose to FIGHT, spotting you and dealing you 50HP damage!";
                break;
            
            // Flee x Nothing (player 1 flees)
            case ("20"): 
                lifeUpdates[0] -= 25;
                messages[0] = "FLEE successful!\nYour opponent chose NOTHING, and you escaped!";
                messages[1] = "Act quickly in encounters!\nYour opponent chose to FLEE, and escaped!";
                break;
            
            // Flee x Fight (player 1 flees)
            case ("21"):
                flee[0] = 1;
                lifeUpdates[0] -= 25;
                messages[0] = "FLEE successful!\nYour opponent chose to FIGHT, but you escaped!";
                messages[1] = "FIGHT failed!\nYour opponent chose to FLEE, and escaped!";
                break;
                
            // Flee x Flee (both players flee)
            case ("22"):
                flee[0] = 1;
                flee[1] = 1;
                lifeUpdates[0] -= 25;
                lifeUpdates[1] -= 25;
                messages[0] = "FLEE successful!\nYour opponent chose to FLEE, and both escaped!";
                messages[1] = "FLEE successful!\nYour opponent chose to FLEE, and both escaped!";
                break;
            
            // Flee x Share (player 1 flees)
            case ("23"):
                flee[0] = 1;
                lifeUpdates[0] -= 25;
                messages[0] = "FLEE successful!\nYour opponent chose to SHARE, and you escaped!";
                messages[1] = "SHARE failed!\nYour opponent chose to FLEE, and escaped!";
                break;
            
            // Flee x Steal (player 1 flees and player 2 steals)
            case ("24"):
                if (randValues[1] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "FLEE successful!\nYour opponent chose to STEAL, and stole 25HP before you escaped!";
                    messages[1] = "STEAL successful!\nYour opponent chose to FLEE, but you stole 25HP before they escaped!";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    
                    messages[0] = "FLEE successful!\nYour opponent chose to STEAL, but you escaped!";
                    messages[1] = "STEAL failed!\nYour opponent chose to FLEE, and escaped! You lost 50HP!";
                }
                
                flee[0] = 1;
                lifeUpdates[0] -= 25;
                break;
            
            // Share x Nothing (player 1 can't share)
            case ("30"):
                messages[0] = "SHARE failed!\nYour opponent chose NOTHING, and nothing happened!";
                messages[1] = "Act quickly in encounters!\nYour opponent chose to SHARE, but nothing happened!";
                break;

            // Share x Fight (player 2 wins fight automatically)
            case ("31"):
                lifeUpdates[0] -= 50;
                messages[0] = "SHARE failed!\nYour opponent chose to FIGHT, dealing you 50HP damage!";
                messages[1] = "FIGHT successful!\nYour opponent chose to SHARE, taking 50HP damage!";
                break;
            
            // Share x Flee (player 2 flees)
            case ("32"):
                flee[1] = 1;
                lifeUpdates[1] -= 25;
                messages[0] = "SHARE failed!\nYour opponent chose to FLEE, and escaped!";
                messages[1] = "FLEE successful!\nYour opponent chose to SHARE, but you escaped!";
                break;
            
            // Share x Share (players share)
            case ("33"):
                lifeUpdates[0] += 50;
                lifeUpdates[1] += 50;
                messages[0] = "SHARE successful!\nYour opponent chose to SHARE, giving you 50HP!";
                messages[1] = "SHARE successful!\nYour opponent chose to SHARE, giving you 50HP!";
                break;
            
            // Share x Steal (player 2 steals)
            case ("34"):
                if (randValues[1] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "SHARE failed!\nYour opponent chose to STEAL, stealing 25HP from you!";
                    messages[1] = "STEAL successful!\nYour opponent chose to SHARE, and you stole 25HP from them!";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    messages[0] = "SHARE failed!\nYour opponent chose to STEAL, but nothing happened!";
                    messages[1] = "STEAL failed!\nYour opponent chose to SHARE, and nothing happened! You lost 50HP!";
                }
                break;
            
            // Steal x Nothing (player 1 steals with 100% chance)
            case ("40"):
                lifeUpdates[0] += 25;
                lifeUpdates[1] -= 25;
                messages[0] = "STEAL successful!\nYour opponent chose NOTHING, and you stole 25HP from them!";
                messages[1] = "Act quickly in encounters!\nYour opponent chose to STEAL, stealing 25HP from you!";
                break;
            
            // Steal x Fight (fight wins automatically)
            case ("41"):
                lifeUpdates[0] -= 50;
                messages[0] = "STEAL failed!\nYour opponent chose to FIGHT, spotting you and dealing you 50HP damage!";
                messages[1] = "FIGHT successful!\nYour opponent chose to STEAL, taking 50HP damage!";
                break;
            
            // Steal x Flee (player 1 steals and player 2 flees)
            case ("42"):
                if (randValues[0] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "STEAL successful!\nSTEAL successful!\nYour opponent chose to FLEE, but you stole 25HP before they escaped!";
                    messages[1] = "FLEE successful!\nYour opponent chose to STEAL, and stole 25HP before you escaped!";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    messages[0] = "STEAL failed!\nYour opponent chose to FLEE, and escaped! You lost 50HP!";
                    messages[1] = "FLEE successful!\nYour opponent chose to STEAL, but you escaped!";
                }
                
                flee[1] = 1;
                lifeUpdates[1] -= 25;
                break;
            
            // Steal x Share (player 1 steals)
            case ("43"):
                if (randValues[0] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "STEAL successful!\nYour opponent chose to SHARE, and you stole 25HP from them!";
                    messages[1] = "SHARE failed!\nYour opponent chose to STEAL, stealing 25HP from you!";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    messages[0] = "STEAL failed!\nYour opponent chose to SHARE, and nothing happened! You lost 50HP!";
                    messages[1] = "SHARE failed!\nYour opponent chose to STEAL, but nothing happened!";
                }
                
                break;
            
            // Steal x Steal (both players steal)
            case ("44"):
                if (randValues[0] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[0] = "STEAL successful!\n";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    messages[0] = "STEAL failed!\n";
                }
                
                if (randValues[1] > 25)
                { // Steal successful
                    lifeUpdates[0] += 25;
                    lifeUpdates[1] -= 25;
                    messages[1] = "STEAL successful!\n";
                }

                else
                { // Steal failed
                    lifeUpdates[0] -= 50;
                    messages[1] = "STEAL failed!\n";
                }
                
                messages[0] = messages[0] + "You opponent chose to STEAL! A confusing scuffle ensued!";
                messages[1] = messages[1] + "You opponent chose to STEAL! A confusing scuffle ensued!";
                
                break;
            
            // Impossible
            default:
                break;
        }

        if (flee[0] == 1)
        {
            if (randValues[0] < 50)
            {
                if (randValues[1] < 50) moves[0][0] = 1;
                else moves[0][0] = -1;
            }

            else
            {
                if (randValues[1] < 50) moves[0][1] = 1;
                else moves[0][1] = -1;
            }
        }
        
        if (flee[1] == 1)
        {
            if (randValues[0] < 50)
            {
                if (randValues[1] < 50) moves[1][0] = -1;
                else moves[0][0] = 1;
            }

            else
            {
                if (randValues[1] < 50) moves[1][1] = -1;
                else moves[0][1] = 1;
            }
        }
        
        SendOutcome(encounter.clientConnections[0], encounter.playerReferences[0].gameObject, lifeUpdates[0], moves[0], messages[0]);
        SendOutcome(encounter.clientConnections[1], encounter.playerReferences[1].gameObject, lifeUpdates[1], moves[1], messages[1]);
    }
}
