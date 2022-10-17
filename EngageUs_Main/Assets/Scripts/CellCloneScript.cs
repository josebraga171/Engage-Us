using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellCloneScript : MonoBehaviour
{
    private GameObject clouds;
    
    public GameObject playerClone;
    
    private GameObject model0Life;
    private GameObject model0To25Life;
    private GameObject model25To50Life;
    private GameObject model50To75Life;
    private GameObject model75To100Life;
    
    private bool isShrinking;
    private float scaleStep = 0.1f;

    private Vector3 position;
    
    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;
        
        clouds = transform.Find("Clouds").gameObject;
        
        playerClone = transform.Find("Character").gameObject;
        
        model0Life = transform.Find("0% Life").gameObject;
        model0To25Life = transform.Find("25%-0% Life").gameObject;
        model25To50Life = transform.Find("50%-25% Life").gameObject;
        model50To75Life = transform.Find("75%-50% Life").gameObject;
        model75To100Life = transform.Find("100%-75% Life").gameObject;
        
        model75To100Life.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        HandleCloudScale();
    }

    public void UpdateModel(int life)
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
    }
    
    public void ToggleVisibility()
    {
        if (!isShrinking) isShrinking = true;
        else isShrinking = false;
    }

    private void HandleCloudScale()
    {
        var currentScale = clouds.transform.localScale;

        if (currentScale.x < 1.2f && !isShrinking)
        {
            if(currentScale.x > 0.1f) clouds.SetActive(true);
            clouds.transform.localScale = new Vector3(currentScale.x + scaleStep, currentScale.y + scaleStep, currentScale.z + scaleStep);
        }
        
        else if (clouds.activeSelf && isShrinking)
        {
            if(currentScale.x < 0.1f) clouds.SetActive(false);
            clouds.transform.localScale = new Vector3(currentScale.x - scaleStep, currentScale.y - scaleStep, currentScale.z - scaleStep);
        }
    }
}
