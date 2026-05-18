using System.Collections;
using UnityEngine;

public class Scene : MonoBehaviour
{
    public GameObject nxtBtn;
    bool btnActivated;
    
    void Start()
    {
        btnActivated = false;
        nxtBtn.SetActive(false);
    }

    public void ChangeSceneIn(float sec)
    {
        //nxtBtn.SetActive(true);
        StartCoroutine(Timer(sec));
    }

    IEnumerator Timer(float tt)
    {

        while(tt > 0)
        {
            tt -= Time.deltaTime;

            if(tt <= 5 && !btnActivated)
            {
                btnActivated = true;
                //nxtBtn.SetActive(true);
            }

            yield return null;
        }

        // change scene yourself then.
        SceneManager.instance.NextBtnClicked();
    }

    // if nxtbtn is pressed.
    void StopCoroutine()
    {
        StopAllCoroutines();
    }
}
