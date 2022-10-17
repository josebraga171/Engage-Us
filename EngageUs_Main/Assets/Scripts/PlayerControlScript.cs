using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerControlScript : NetworkBehaviour
{
    private PlayerScript playerScript;
    private GameScript gameScript;
    
    private int life;
    private int exp;

    private Vector3 currentCoords;
    private Vector3 destinationCoords;
    private float distFraction = 0;
    private int[] cellPos;
    private float speed = 10f;
     
    //state stuff
    private bool isMoving = false;

    private GameObject harvestButton;
    private GameObject sowButton;
    private GameObject convertButton;
    
    private GameObject attackButton;
    private GameObject fleeButton;
    private GameObject shareButton;
    private GameObject stealButton;

    // Start is called before the first frame update
    void Start()
    {
        playerScript = GetComponent<PlayerScript>();
        
        var gameController = GameObject.Find("Game Controller");
        gameScript = gameController.GetComponent<GameScript>();
        
        var gameOverlay = GameObject.Find("GameOverlayMenus").gameObject;

        harvestButton = gameOverlay.transform.Find("HarvestButton").gameObject;
        harvestButton.GetComponent<Button>().onClick.AddListener(HarvestAction);

        sowButton = gameOverlay.transform.Find("SowButton").gameObject;
        sowButton.GetComponent<Button>().onClick.AddListener(SowAction);
        
        convertButton = gameOverlay.transform.Find("ConvertButton").gameObject;
        convertButton.GetComponent<Button>().onClick.AddListener(ConvertAction);
        
        attackButton = gameOverlay.transform.Find("AttackButton").gameObject;
        attackButton.GetComponent<Button>().onClick.AddListener(AttackAction);

        fleeButton = gameOverlay.transform.Find("FleeButton").gameObject;
        fleeButton.GetComponent<Button>().onClick.AddListener(FleeAction);
        
        shareButton = gameOverlay.transform.Find("ShareButton").gameObject;
        shareButton.GetComponent<Button>().onClick.AddListener(ShareAction);
        
        stealButton = gameOverlay.transform.Find("StealButton").gameObject;
        stealButton.GetComponent<Button>().onClick.AddListener(StealAction);
        
        currentCoords = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleActions();
        HandleDebugInput();
    }

    private void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.H) && !isMoving)
        {
            playerScript.HarvestCell();
        }
        
        else if (Input.GetKeyDown(KeyCode.S) && !isMoving)
        {
            playerScript.SowCells();
        }
        
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerScript.UpdateAction(1);
        }
        
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerScript.UpdateAction(2);
        }
        
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerScript.UpdateAction(3);
        }
        
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            playerScript.UpdateAction(4);
        }
    }

    private void HarvestAction()
    {
        playerScript.HarvestCell();
    }
    
    private void SowAction()
    {
        playerScript.SowCells();
    }
    
    private void ConvertAction()
    {
        playerScript.ConvertLife();
    }
    
    private void AttackAction()
    {
        playerScript.UpdateAction(1);
    }
    
    private void FleeAction()
    {
        playerScript.UpdateAction(2);
    }
    
    private void ShareAction()
    {
        playerScript.UpdateAction(3);
    }
    
    private void StealAction()
    {
        playerScript.UpdateAction(4);
    }
    
    private void HandleMovement()
    {
        if (playerScript.CanMove())
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                Move(new int[] {1, 0});
            }
    
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Move(new int[] {0, 1});
            }
    
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                Move(new int[] {-1, 0});
            }
    
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Move(new int[] {0, -1});
            }    
        }

        if (isMoving)
        {
            if (destinationCoords.x < 0)
            {
                if (distFraction < 0.5)
                {
                    distFraction += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(currentCoords, destinationCoords, distFraction);
                }
                else
                {
                    destinationCoords.x = 12;
                    currentCoords.x = 13f;
                    playerScript.transform.Translate(13f,0,0);
                    
                    if (distFraction < 1)
                    {
                        distFraction += Time.deltaTime * speed;
                        transform.position = Vector3.Lerp(currentCoords, destinationCoords, distFraction);
                    }
                    else
                    {
                        currentCoords.x = 12f;
                        distFraction = 0; 
                        isMoving = false; 
                    }
                }
            }
            
            else if (destinationCoords.z < 0)
            {
                if (distFraction < 0.5)
                {
                    distFraction += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(currentCoords, destinationCoords,distFraction);
                }
                
                else
                {
                    destinationCoords.z = 12;
                    currentCoords.z = 13f;
                    playerScript.transform.Translate(0,0,13f);
                    
                    if (distFraction < 1)
                    {
                        distFraction += Time.deltaTime * speed;
                        transform.position = Vector3.Lerp(currentCoords, destinationCoords,distFraction);
                    }
                    
                    else
                    {
                        currentCoords.z = 12f;
                        distFraction = 0; 
                        isMoving = false; 
                    }
                }
            }
            
            else if (destinationCoords.x > 12)
            {
                if (distFraction < 0.5)
                {
                    distFraction += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(currentCoords, destinationCoords, distFraction);
                }
                
                else
                {
                    destinationCoords.x = 0;
                    currentCoords.x = -1f;
                    playerScript.transform.Translate(-1f,0,0);
                    if (distFraction < 1)
                    {
                        distFraction += Time.deltaTime * speed;
                        transform.position = Vector3.Lerp(currentCoords, destinationCoords, distFraction);
                    }
                    
                    else
                    {
                        currentCoords.x = 0;
                        distFraction = 0; 
                        isMoving = false; 
                    }
                }
            }
            else if (destinationCoords.z > 12)
            {
                if (distFraction < 0.5)
                {
                    distFraction += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(currentCoords, destinationCoords,distFraction);
                }
                
                else
                {
                    destinationCoords.z = 0;
                    currentCoords.z = -1f;
                    playerScript.transform.Translate(0,0,-1f);
                    
                    if (distFraction < 1)
                    {
                        distFraction += Time.deltaTime * speed;
                        transform.position = Vector3.Lerp(currentCoords, destinationCoords,distFraction);
                    }
                    
                    else
                    {
                        currentCoords.z = 0;
                        distFraction = 0; 
                        isMoving = false; 
                    }
                }
            }
            
            else
            {
                if (distFraction < 1)
                {
                    distFraction += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(currentCoords, destinationCoords,distFraction);
                }
                
                else
                {
                    distFraction = 0; 
                    isMoving = false;
                }
            }
        }
    }

    public void Move(int[] move)
    {
        if (isMoving) return;
        
        Debug.Log("MOVE: " + move[0] + move[1]);
        
        switch ("" + move[0] + move[1])
        {
            case "10":
                currentCoords = transform.position;
                destinationCoords = currentCoords + Vector3.right;

                if (!gameScript.IsLockedCell(gameScript.ConvertToGridPos(destinationCoords)))
                {
                    playerScript.UpdatePos(new int[] {1, 0});
                    isMoving = true;
                }
                
                break;
            
            case "01":
                currentCoords = transform.position;
                destinationCoords = currentCoords + Vector3.forward;

                if (!gameScript.IsLockedCell(gameScript.ConvertToGridPos(destinationCoords)))
                {
                    playerScript.UpdatePos(new int[] {0, 1});
                    isMoving = true;    
                }
                
                break;
            
            case "-10":
                currentCoords = transform.position;
                destinationCoords = currentCoords + Vector3.left;

                if (!gameScript.IsLockedCell(gameScript.ConvertToGridPos(destinationCoords))) 
                {
                    playerScript.UpdatePos(new int[] {-1, 0});
                    isMoving = true;
                }
                
                break;

            case "0-1":
                currentCoords = transform.position;
                destinationCoords = currentCoords + Vector3.back;

                if (!gameScript.IsLockedCell(gameScript.ConvertToGridPos(destinationCoords)))
                {
                    playerScript.UpdatePos(new int[] {0, -1});
                    isMoving = true;
                }
                
                break;
            
            default:

                break;
        }
    }

    private void HandleDebugInput()
    {
        // DEBUG Life: X to increase, Z to decrease players life by 10
        if (Input.GetKeyDown(KeyCode.X))
        {
            playerScript.UpdateLife(+10);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            playerScript.UpdateLife(-10);
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            playerScript.ToggleEncounter();
        }
    }
}