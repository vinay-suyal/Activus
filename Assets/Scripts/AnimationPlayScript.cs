using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPlayScript : MonoBehaviour
{
    List<GameObject> childrens;
    int children_count;
    
    void Start()
    {
        children_count = transform.childCount;
        childrens = new();

        for(int i=0; i<children_count; i++)
        {
            childrens.Add(transform.GetChild(i).gameObject);
            childrens[i].SetActive(false);
        }

        StartCoroutine(PlayAnimation());
        PlayHighlightAnimation();
    }

    IEnumerator PlayAnimation()
    {
        yield return new WaitForSeconds(0.25f);
        for(int i=0;  i<children_count; i++)
        {
            childrens[i].SetActive(true);
            childrens[i].GetComponent<Animation>().Play("FeatureShowAnim");

            yield return new WaitForSeconds(0.2f);
        }
    }

    // 2 , 7 , 10 , 16, 24 , 28
    int[] timeGap = { 4, 9, 13, 18, 23, 27 };

    void PlayHighlightAnimation()
    {
        for (int i = 0; i < childrens.Count; i++)
        {
            StartCoroutine(PlayWithDelay(childrens[i], timeGap[i]));
        }
    }

    IEnumerator PlayWithDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        obj.transform.GetChild(0).GetComponent<Animation>().Play("HighlightBubble");
    }

}
