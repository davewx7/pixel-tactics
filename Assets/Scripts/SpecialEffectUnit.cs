using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpecialEffectUnit : SpecialEffect
{
    [SerializeField]
    ParticleSystem _particleSystem = null;

    [SerializeField]
    ParticleSystemRenderer _renderer = null;

    [SerializeField]
    float _intensity = 1.0f;

    [SerializeField]
    float _intensityPeakDie = 1.0f;

    [SerializeField]
    float _intensityPeakRampTime = 0.5f;

    public Unit unit;

    bool _finishing = false;

    IEnumerator DieCo()
    {
        var tween = _renderer.material.DOFloat(_intensityPeakDie, "_Intensity", _intensityPeakRampTime);

        yield return tween.WaitForCompletion();

        tween = _renderer.material.DOFloat(0f, "_Intensity", _intensityPeakRampTime);

        yield return tween.WaitForCompletion();

        GameObject.Destroy(gameObject);
    }

    public void Finish()
    {
        if(_finishing) {
            return;
        }

        _finishing = true;

        if(gameObject.activeInHierarchy) {
            StartCoroutine(DieCo());
        } else {
            GameObject.Destroy(gameObject);
        }
    }

    public override void Start()
    {
        base.Start();

        _renderer.material = Instantiate(_renderer.material);
        _renderer.material.SetFloat("_Intensity", _intensity);

        if(unit != null) {
            ParticleSystem.ShapeModule shapeModule = _particleSystem.shape;
            shapeModule.spriteRenderer = unit.spriteRenderer;
            shapeModule.sprite = unit.spriteRenderer.sprite;
        }
    }

    public override void Update()
    {
        base.Update();

    }
}
