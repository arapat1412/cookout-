using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // C?n thêm d?ng này ð? làm vi?c v?i Image

public class StoveBurnWarningUI : MonoBehaviour
{
    
    [SerializeField] private StoveCounter stoveCounter;

    private void Start()
    {
        stoveCounter.OnProgressChanged += StoveCounter_OnProgessChanged;
        Hide();
    }

    private void StoveCounter_OnProgessChanged(object sender, IHasProgress.OnProgessChangedEventArgs e)
    {
        float burnShowProgessAmout = .5f;
        bool show =stoveCounter.IsFried() && e.progessNormalized>=burnShowProgessAmout;
        if (show)
        {
            Show();
        }else
        {
            Hide();
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