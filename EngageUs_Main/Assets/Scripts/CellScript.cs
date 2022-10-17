using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Xsl;
using Mirror;
using UnityEngine;

public class CellScript : NetworkBehaviour
{
    /*
    Script to attach to the Cell Game Object
    */
    
    private TextMesh textMesh;
    private GameObject clouds;

    private GameScript gameScript;
    
    private GameObject model0Life;
    private GameObject model0To25Life;
    private GameObject model25To50Life;
    private GameObject model50To75Life;
    private GameObject model75To100Life;

    // Must be done differently!
    private List<CellCloneScript> cloneScripts;

    public List<PlayerScript> playerReferences;

    private int life = 100;
    private int maxLife = 100;
    private int population = 0;
    
    public bool locked = false;
    
    private int[] gridPos = {0, 0};

    private bool isShrinking = false;
    private float scaleStep = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        GameObject gameController = GameObject.Find("Game Controller");
        gameScript = gameController.GetComponent<GameScript>();
        
        //textMesh = transform.GetComponentInChildren<TextMesh>();
        clouds = transform.Find("Clouds").gameObject;

        playerReferences = new List<PlayerScript>();

        cloneScripts = new List<CellCloneScript>();
        
        model0Life = transform.Find("0% Life").gameObject;
        model0To25Life = transform.Find("25%-0% Life").gameObject;
        model25To50Life = transform.Find("50%-25% Life").gameObject;
        model50To75Life = transform.Find("75%-50% Life").gameObject;
        model75To100Life = transform.Find("100%-75% Life").gameObject;
        
        model75To100Life.SetActive(true);

        // Start cell at max life
        life = maxLife;
        
        // Save original cell position
        gridPos = gameScript.ConvertToGridPos(transform.position);
        
        // BAD
        ActivateClones();
        
        //textMesh.text = "HP:" + life + " Pop:" + population + "\nX:" + gridPos[0] + " Z:" + gridPos[1];
        
        // How to change cell model:
    }

    // Update is called once per frame
    void Update()
    {
        HandleCloudScale();
    }

    private void Awake()
    {
        
    }
    
    public void UpdateLife(int amount) 
    {
        // Update and restrict cell's life
        life += amount;
        
        if (life < 0) life = 0;
        else if (life > maxLife) life = maxLife;
        
        // Update debug text
        //textMesh.text = "HP:" + life + " Pop:" + population + "\nX:" + gridPos[0] + " Z:" + gridPos[1];
        
        UpdateModel();
    }
    
    private void UpdateModel() 
    {
        model0Life.SetActive(false);
        model0To25Life.SetActive(false);
        model25To50Life.SetActive(false);
        model50To75Life.SetActive(false);
        model75To100Life.SetActive(false);
        
        if (life > 75)
        {
            model75To100Life.SetActive(true);
        }

        else if (life > 50 && life <= 75)
        {
            model50To75Life.SetActive(true);
        }
        
        else if (life > 25 && life <= 50)
        {
            model25To50Life.SetActive(true);
        }
        
        else if (life > 0 && life <= 25)
        {
            model0To25Life.SetActive(true);
        }
        
        else if (life == 0)
        {
            model0Life.SetActive(true);
        }

        // Update models of clones
        foreach (var clone in cloneScripts)
        {
            clone.UpdateModel(life);
        }
    }

    public void AddPlayer(PlayerScript script)
    {
        // Add player script to the references in the cell
        playerReferences.Add(script);
        
        // Update cell's population
        population += 1;
        
        // Trigger encounter
        if (population == 2)
        {
            locked = true;
            
            // Only works on the server:
            gameScript.StartEncounter(playerReferences);
        }

        // Update debug text
        //textMesh.text = "HP:" + life + " Pop:" + population + "\nX:" + gridPos[0] + " Z:" + gridPos[1];
    }
    
    public void RemovePlayer(PlayerScript script)
    {
        // Add player script to the references in the cell
        playerReferences.Remove(script);
        
        // Update and restrict cell's population
        population -= 1;
        if (population <= 0)
        {
            population = 0;
        }

        if(population < 2) locked = false;
        
        // Update debug text
        //textMesh.text = "HP:" + life + " Pop:" + population + "\nX:" + gridPos[0] + " Z:" + gridPos[1];
    }
    
    // Function to scale up the Cell using the localScale transform
    public void SetScale(int scale)
    {
        // Apply scale transform to the cell
        transform.localScale = new Vector3(scale, scale, scale);
    }

    public void ToggleVisibility()
    {
        if (!isShrinking) isShrinking = true;
        else isShrinking = false;
        
        // Update visibility of clones
        foreach (var clone in cloneScripts)
        {
            clone.ToggleVisibility();
        }
    }

    private void HandleCloudScale()
    {
        var currentScale = clouds.transform.localScale;

        if (currentScale.x < 1.2f && !isShrinking)
        {
            if(currentScale.x > 0.2f) clouds.SetActive(true);
            clouds.transform.localScale = new Vector3(currentScale.x + scaleStep, currentScale.y + scaleStep, currentScale.z + scaleStep);
        }
        
        else if (clouds.activeSelf && isShrinking)
        {
            if(currentScale.x < 0.2f) clouds.SetActive(false);
            clouds.transform.localScale = new Vector3(currentScale.x - scaleStep, currentScale.y - scaleStep, currentScale.z - scaleStep);
        }
    }

    public int GetLife()
    {
        return life;
    }

    public bool IsLocked()
    {
        return locked;
    }
    
    private void ActivateClones()
    {
        int low = 3;
        int high = 9;
        
        if (gridPos[0] < low)
        {
            GameObject cloneE = transform.Find("Clone E").gameObject;
            cloneE.SetActive(true);
            cloneScripts.Add(cloneE.GetComponent<CellCloneScript>());
        }
        
        else if (gridPos[0] > high)
        {
            GameObject cloneW = transform.Find("Clone W").gameObject;
            cloneW.SetActive(true);
            cloneScripts.Add(cloneW.GetComponent<CellCloneScript>());
        }

        if (gridPos[1] < low)
        {
            GameObject cloneN = transform.Find("Clone N").gameObject;
            cloneN.SetActive(true);
            cloneScripts.Add(cloneN.GetComponent<CellCloneScript>());
        }

        else if (gridPos[1] > high)
        {
            GameObject cloneS = transform.Find("Clone S").gameObject;
            cloneS.SetActive(true);
            cloneScripts.Add(cloneS.GetComponent<CellCloneScript>());
        }

        if (gridPos[0] < low && gridPos[1] < low)
        {
            GameObject cloneNE = transform.Find("Clone NE").gameObject;
            cloneNE.SetActive(true);
            cloneScripts.Add(cloneNE.GetComponent<CellCloneScript>());
        }
        
        else if (gridPos[0] < low && gridPos[1] > high)
        {
            GameObject cloneSE = transform.Find("Clone SE").gameObject;
            cloneSE.SetActive(true);
            cloneScripts.Add(cloneSE.GetComponent<CellCloneScript>());
        }
        
        else if (gridPos[0] > high && gridPos[1] > high)
        {
            GameObject cloneSW = transform.Find("Clone SW").gameObject;
            cloneSW.SetActive(true);
            cloneScripts.Add(cloneSW.GetComponent<CellCloneScript>());
        }
        
        else if (gridPos[0] > high && gridPos[1] < low)
        {
            GameObject cloneNW = transform.Find("Clone NW").gameObject;
            cloneNW.SetActive(true);
            cloneScripts.Add(cloneNW.GetComponent<CellCloneScript>());
        }
    }
}