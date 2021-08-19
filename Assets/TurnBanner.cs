using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TurnBanner : MonoBehaviour
{
    static public int numActive = 0;

    [SerializeField]
    Image _bg = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _roundText = null, _seasonText = null;

    public void Display(CalendarMonth month)
    {
        string[] years = { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight" };
        string year = (GameController.instance.currentYear+1).ToString();
        if(GameController.instance.currentYear < years.Length) {
            year = years[GameController.instance.currentYear];
        }
        gameObject.SetActive(true);
        _roundText.text = string.Format("{0} Moon, Year {1}", GameController.instance.currentMonth.ordinal, year);
        _seasonText.text = GameController.instance.currentMonth.season.description;
    }

    private void Start()
    {
        ++numActive;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {

        Color bgColor = _bg.color;
        bgColor.a = 0f;
        _bg.color = bgColor;
        bgColor.a = 0.4f;

        _bg.DOColor(bgColor, 1f);

        _roundText.color = new Color(1f, 1f, 1f, 0f);
        _seasonText.color = new Color(1f, 1f, 1f, 0f);
        
        _roundText.DOColor(Color.white, 1f);
        _seasonText.DOColor(Color.white, 1f);

        yield return new WaitForSeconds(3f);

        bgColor.a = 0f;
        _bg.DOColor(bgColor, 1f);

        _roundText.DOColor(new Color(1f, 1f, 1f, 0f), 1f);
        _seasonText.DOColor(new Color(1f, 1f, 1f, 0f), 1f);

        yield return new WaitForSeconds(2f);

        --numActive;

        GameObject.Destroy(gameObject);
    }
}
