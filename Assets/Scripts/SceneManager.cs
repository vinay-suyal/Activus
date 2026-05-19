using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

public class SceneManager : MonoBehaviour
{
    int i;
    int totalScenes;
    List<GameObject> allScenes;

    #region Singleton

    [SerializeField] VideoPlayer vp1;
    [SerializeField] VideoPlayer vp2;
    public static SceneManager instance;

    private void Awake()
    {
        instance = this;
        vp1.Prepare();
        vp2.Prepare();
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

        print("total scenes " + totalScenes);
        totalScenes = allScenes.Count;

        PlayFirstScene();
        DeActivateParticleSystem();
        DeActiveBgCanvas();
    }

    void PlayFirstScene()
    {
        i = 0;
        allScenes[0].SetActive(true);
        allScenes[0].GetComponent<Scene>().ChangeSceneIn(10);
    }

    public void RestartBtnClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }


    public void NextBtnClicked()
    {
        StartCoroutine(NextBtn());
        
        if(i==0)
            PlaySceneTransitionAnimation();
    }

    public void ReloadSceneAfterTime()
    {
        //Invoke("LoadNewScene" , 3);
    }

    public Canvas bg_canvas;

    public void LoadNewScene()
    {
        //UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public GameObject RestartBtn;

    IEnumerator NextBtn()
    {
        print("i = " + i + " ");
        //HideHuman();
        allScenes[i].SetActive(false);

        if(i==0)
            yield return new WaitForSeconds(0.5f);
        else
            yield return new WaitForSeconds(0f);

        if (i == totalScenes-1)
        {
            RestartBtn.SetActive(true);
            
            yield break;
        }

        i += 1;
        allScenes[i].SetActive(true);
       

        if(i==1)    // scene 2
        {
            SFXManager.instance.Play(SFXType.Sliding);
            bgVideo.SetActive(true);
            ActivateBgScene2();
            ShowHuman();
            Glass.SetActive(true);
            BlueCircle.SetActive(true);
        }
        else if(i==2)   // scene 3
        {
            ActivateBgScene3();
            Glass.SetActive(false);
            BlueCircle.SetActive(false);
            allScenes[i].GetComponent<Scene>().ChangeSceneIn(10);
        }
        else if (i == 3)   //scene 4
        {
            SFXManager.instance.Play(SFXType.Sliding);
            ActivateBgScene4();
        }
        else if(i==4)   //scene 5
        {
            ActivateBgScene5();
            allScenes[i].GetComponent<Scene>().ChangeSceneIn(15);
        }
        else if (i==5)
        {
            SFXManager.instance.Play(SFXType.Sliding);
            DeactivateScene5();
            HideHuman();
        }
        else if(i==6)
        {
            Camera.main.GetComponent<ModelViewController>().enabled = false;
            Camera.main.transform.position = new Vector3(0, 0, -5);
            Camera.main.transform.rotation = Quaternion.identity;

           
            Scene7Main.SetActive(true);
            SFXManager.instance.Play(SFXType.Sliding);
            activus.SetActive(false);
            Invoke("LoadRestartBtn", 37);
            Invoke("ActivateLastParticleSystem", 0.5f);
            //allScenes[i].GetComponent<Scene>().ChangeSceneIn(40);
        }

    }

    public GameObject lastParticleSystem;

    void ActivateLastParticleSystem()
    {
        lastParticleSystem.SetActive(true);
    }

    void LoadRestartBtn()
    {
        RestartBtn.SetActive(true);
    }

    public GameObject Scene7Main;
    public GameObject activus;

    void ActiveBgCanvas()
    {
        bg_canvas.gameObject.SetActive(true);
    }

    void DeActiveBgCanvas()
    {
        //bg_canvas.gameObject.SetActive(false);
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
        allScenes[1].GetComponent<Scene>().ChangeSceneIn(1);
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
        rate.constant *= 2.25f;
        emission.rateOverTime = rate;

        var main = bubble.main;
        main.simulationSpeed = 1.2f;

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
    public ParticleSystem explosion;

    public GameObject Elements_360;
    public void PlayRedClothAnim()
    {
        //redCloth.GetComponent<Animation>().Play("Red_Cloth_Anim");
        redCloth.GetComponent<Animation>().Play("RedCloth2");
        clothEvent.enabled = false;
        //activus_img.transform.GetComponent<Animation>().Play("Activus");
        SFXManager.instance.Play(SFXType.BoxAppear);
        Invoke("HideRedCloth", 0.5f);
        text1.gameObject.SetActive(false);
        text2.gameObject.SetActive(false);
        Invoke("Enable360Elements", 0.5f);
        activus.SetActive(true);
        Camera.main.GetComponent<ModelViewController>().enabled = true;

        explosion.gameObject.SetActive(true);
        explosion.Play();
    }

    void Enable360Elements()
    {
        Elements_360.SetActive(true);
    }

    public TextMeshProUGUI tap;
    public GameObject hand;
    public EventTrigger clothEvent;

    public void ActivateClick()
    {
        tap.gameObject.SetActive(true);
        hand.gameObject.SetActive(true);
        clothEvent.enabled = true;
    }

    public void DeActivateClick()
    {
        tap.gameObject.SetActive(false);
        hand.gameObject.SetActive(false);
        clothEvent.enabled = false;
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
        //HumanBody.SetActive(true);
        Glass.SetActive(true);
        BlueCircle.SetActive(true);
    }

    public void DeActivateSecondScene()
    {
        //HumanBody.SetActive(false);
        
        //BlueCircle.SetActive(false);
    }


    #endregion



    #region Bg Canvas

    [SerializeField] GameObject scene2;
    [SerializeField] GameObject scene3;
    [SerializeField] GameObject scene4;
    [SerializeField] GameObject scene5;
    
    [SerializeField] GameObject bgVideo;


    void ActivateBgScene2()
    {
        scene2.SetActive(true);
    }

    void ActivateBgScene3()
    {
        scene2.SetActive(false);
        scene3.SetActive(true);
    }

    void ActivateBgScene4()
    {
        scene3.SetActive(false);
        scene4.SetActive(true);
    }

    void ActivateBgScene5()
    {
        scene4.SetActive(false);
        scene5.SetActive(true);
    }

    void DeactivateScene5()
    {
        scene5.SetActive(false);
    }

    #endregion
}
