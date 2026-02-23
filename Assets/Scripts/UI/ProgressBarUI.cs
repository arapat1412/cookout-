using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{

    [SerializeField] private GameObject hasProgressGameObject;
    [SerializeField] private Image barImage;

    private IHasProgress hasProgess;

    private void Start()
    {
        hasProgess = hasProgressGameObject.GetComponent<IHasProgress>();

        if (hasProgess == null) { 
            Debug.Log("Game Object" +  hasProgressGameObject + "khong co component IHasProgress!");
        }


        hasProgess.OnProgressChanged += HasProgess_OnProgessChanged;
        barImage.fillAmount = 0f;

        Hide();
    }

    private void HasProgess_OnProgessChanged(object sender, IHasProgress.OnProgessChangedEventArgs e)
    {
        barImage.fillAmount = e.progessNormalized;

        if (e.progessNormalized ==0f || e.progessNormalized==1f)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
