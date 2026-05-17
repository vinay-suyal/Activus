using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    int i;
    int totalScenes;
    List<GameObject> allScenes;

    #region Singleton

    public static SceneManager instance;

    private void Awake()
    {
        instance = this;
    }

    #endregion

    void Start()
    {
        Application.targetFrameRate = 60;
        SceneTransitionGo.SetActive(false);
        allScenes = new();

        for (int i = 0; i < transform.childCount; i++)
        {
            allScenes.Add(transform.GetChild(i).gameObject);
            allScenes[i].SetActive(false);
        }

        totalScenes = allScenes.Count;

        PlayFirstScene();
        DeActivateParticleSystem();
        DeActiveBgCanvas();
    }

    void PlayFirstScene()
    {
        i = 0;
        allScenes[0].SetActive(true);
        allScenes[0].GetComponent<Scene>().ChangeSceneIn(13);
    }


    public void NextBtnClicked()
    {
        StartCoroutine(NextBtn());
        
        if(i==0)
            PlaySceneTransitionAnimation();
    }

    public void ReloadSceneAfterTime()
    {
        Invoke("LoadNewScene" , 15);
    }

    public Canvas bg_canvas;

    public void LoadNewScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    IEnumerator NextBtn()
    {
        HideHuman();
        allScenes[i].SetActive(false);
        yield return new WaitForSeconds(0.5f);

        if (i == totalScenes - 1)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            yield break;
        }

        i += 1;
        if (i == 1)
        {
            ActiveBgCanvas();

            ActivateSecondScene();
        }
        else
        {
            DeActivateSecondScene();
        }

        
        if (i == 5)
        {
            //DeActiveBgCanvas();
            DeActivateParticleSystem();
        }
        else if (i == 7)
        {
            HideHuman();
        }

        allScenes[i].SetActive(true);

        if (i == 2)
        {
            allScenes[2].GetComponent<Scene>().ChangeSceneIn(12);
        }

        if(i==4)
        {
            allScenes[4].GetComponent<Scene>().ChangeSceneIn(13);
        }

        
        if (i > 0 && i < 5)
            ShowHuman();

        if (i == 6)
        {
            activus.SetActive(false);
            Camera.main.GetComponent<ModelViewController>().enabled = true;
            //allScenes[6].GetComponent<Scene>().ChangeSceneIn(30);
        }
        else
        {
            activus.SetActive(false);
            Camera.main.GetComponent<ModelViewController>().enabled = false;

            // Reset camera position
            Camera.main.transform.position = new Vector3(0, 0, -5);

            // Reset camera rotation
            Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

    }

    public GameObject activus;

    void ActiveBgCanvas()
    {
        bg_canvas.gameObject.SetActive(true);
    }

    void DeActiveBgCanvas()
    {
        bg_canvas.gameObject.SetActive(false);
    }

    #region Human_Body

    public GameObject human;

    void ShowHuman()
    {
        human.SetActive(true);
    }

    void HideHuman()
    {
        human.SetActive(false);
    }

    #endregion


    #region Particle System

    public ParticleSystem bubble;

    public void ActivateParticleSystem()
    {
        bubble.gameObject.SetActive(true);
        allScenes[1].GetComponent<Scene>().ChangeSceneIn(10);
    }

    void DeActivateParticleSystem()
    {
        bubble.gameObject.SetActive(false);
    }

    public void SpeedUpParticleSystem()
    {
        bubble.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var emission = bubble.emission;
        ParticleSystem.MinMaxCurve rate = emission.rateOverTime;
        rate.constant *= 3f;
        emission.rateOverTime = rate;

        bubble.Play();
    }


    #endregion

    #region Scene Transition Animation

    [SerializeField] GameObject SceneTransitionGo;
    void PlaySceneTransitionAnimation()
    {

        //HideHuman();
        SceneTransitionGo.SetActive(true);
        SceneTransitionGo.transform.GetChild(0).GetComponent<Animation>().Play("SceneTransition");

        //if(i > 0 && i<5)
        //  Invoke("ShowHuman", .5f);

        Invoke("DeactivateTransition", 1f);
    }

    void DeactivateTransition()
    {
        SceneTransitionGo.SetActive(false);
    }

    #endregion


    public GameObject redCloth;
    public TextMeshProUGUI text1;
    public TextMeshProUGUI text2;
    public GameObject activus_img;
    public GameObject btn;

    public void PlayRedClothAnim()
    {
        //redCloth.GetComponent<Animation>().Play("Red_Cloth_Anim");
        redCloth.GetComponent<Animation>().Play("RedCloth2");
        //activus_img.transform.GetComponent<Animation>().Play("Activus");
        Invoke("HideRedCloth", 0.5f);
        text1.gameObject.SetActive(false);
        text2.gameObject.SetActive(false);
        activus.SetActive(true);
        Camera.main.GetComponent<ModelViewController>().enabled = true;
    }

    void HideRedCloth()
    {
        redCloth.SetActive(false);
        btn.SetActive(true);
    }



    #region 2nd scene

    public GameObject HumanBody;
    public GameObject Glass;
    public GameObject BlueCircle;


    void ActivateSecondScene()
    {
        HumanBody.SetActive(true);
        Glass.SetActive(true);
        BlueCircle.SetActive(true);
    }

    void DeActivateSecondScene()
    {
        HumanBody.SetActive(false);
        Glass.SetActive(false);
        BlueCircle.SetActive(false);
    }


    #endregion
}
