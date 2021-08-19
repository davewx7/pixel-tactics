using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScoreField
{
    public string description;
    public int multiplier;
    public int divider = 1;
    public int count;

    public int score {
        get {
            if(count < 0) {
                return 0;
            }
            return (count * multiplier) / divider;
        }
    }
}

public class ScoreDialogEntry : MonoBehaviour
{
    public ScoreField scoreField;

    [SerializeField]
    TMPro.TextMeshProUGUI _description = null, _count = null, _multiplier = null, _score = null; 

    public Color color {
        get {
            return _description.color;
        }
        set {
            _description.color = value;
            _count.color = value;
            _multiplier.color = value;
            _score.color = value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(scoreField != null) {
            _description.text = scoreField.description;

            if(scoreField.multiplier < 0) {
                _count.text = "";
                _multiplier.text = "";

                if(scoreField.count > 0) {
                    _score.text = scoreField.count.ToString();
                } else {
                    _score.text = "";
                }
            } else {
                _count.text = scoreField.count.ToString();
                if(scoreField.divider != 1) {
                    _multiplier.text = string.Format("/{0}", scoreField.divider);
                } else {
                    _multiplier.text = string.Format("x{0}", scoreField.multiplier);
                }

                _score.text = scoreField.score.ToString();
            }
        }
    }
}
