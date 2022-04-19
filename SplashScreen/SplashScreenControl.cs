using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplashScreenControl : MonoBehaviour
{
    [SerializeField] private Sprite[] _logoList;
    [Space(10)]
    [SerializeField] private Image _logo;
    [SerializeField] private Animation _animPlyr;

    private int _index;

    void Start()
    {
        _logo.sprite = _logoList[_index];
        _animPlyr.Play("Splash");
    }

    [SerializeField]
    private void OnAnimationFinished()
    {
        _index++;
        if (_index < _logoList.Length)
        {
            _logo.sprite = _logoList[_index];
            _animPlyr.Play("Splash");
        }
        // else   go to title
        //     SceneLoader.Ins.SceneLoad(TitlePath, Color.Black, 2);
    }
}
