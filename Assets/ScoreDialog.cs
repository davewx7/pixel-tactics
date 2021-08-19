using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScoreDialog : MonoBehaviour
{
    [SerializeField]
    Transform _panel = null;

    [SerializeField]
    ScoreDialogEntry _entryProto = null;

    [SerializeField]
    Slider _experienceBar = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _levelText = null, _toNextLevelText = null, _levelExplanationText = null;

    List<ScoreDialogEntry> _entries = new List<ScoreDialogEntry>();

    ScoreDialogEntry _grandTotal = null;

    public int scoreTotal {
        get {
            if(_grandTotal == null) {
                return 0;
            }

            return _grandTotal.scoreField.count;
        }
    }

    public IEnumerator AnimateScores(float timeMult=1f)
    {
        ScoreDialogEntry grandTotal = _entries[_entries.Count-1];
        _grandTotal = grandTotal;
        grandTotal.scoreField.count = 0;

        for(int i = 0; i < _entries.Count-1; ++i) {
            ScoreDialogEntry entry = _entries[i];
            entry.color = Color.clear;
        }

        int totalScore = 0;

        for(int i = 0; i < _entries.Count-1; ++i) {
            ScoreDialogEntry entry = _entries[i];

            if(entry.scoreField.multiplier <= 0) {
                entry.color = new Color(0.9f, 0.9f, 0.9f);
                continue;
            }

            totalScore += entry.scoreField.score;
            float sumTime = entry.scoreField.score / 500f;

            var tween = DG.Tweening.DOTween.To(() => entry.color, (Color c) => entry.color = c, Color.white, 0.3f*timeMult);

            if(timeMult > 0f) {
                yield return null;
            }

            grandTotal.color = Color.white;

            if(timeMult > 0f) {
                yield return tween.WaitForCompletion();
            }

            var tweenInt = DG.Tweening.DOTween.To(() => grandTotal.scoreField.count, (int n) => grandTotal.scoreField.count = n, totalScore, sumTime*timeMult);

            if(timeMult > 0f) {
                yield return tweenInt.WaitForCompletion();
            }

            tween = DG.Tweening.DOTween.To(() => entry.color, (Color c) => entry.color = c, new Color(0.8f, 0.8f, 0.8f), 0.1f*timeMult);

            if(timeMult > 0f) {
                yield return tween.WaitForCompletion();
            }
        }
    }

    public void UpdateScores(TeamInfo team)
    {
        List<ScoreField> fields = team.scoreInfo.GetScores();

        int total = 0;
        foreach(ScoreField field in fields) {
            total += field.score;
        }

        fields = new List<ScoreField>(fields);
        fields.Add(new ScoreField() {
            description = "",
            multiplier = -1,
            count = 0,
        });

        fields.Add(new ScoreField() {
            description = "TOTAL RENOWN",
            multiplier = -1,
            count = total,
        });

        while(_entries.Count < fields.Count) {
            ScoreDialogEntry entry = Instantiate(_entryProto, _panel);
            entry.color = Color.clear;
            entry.gameObject.SetActive(true);
            entry.transform.localPosition += new Vector3(0f, _entries.Count*-20f, 0f);
            _entries.Add(entry);
        }

        while(_entries.Count > fields.Count) {
            GameObject.Destroy(_entries[_entries.Count-1].gameObject);
            _entries.RemoveAt(_entries.Count-1);
        }

        for(int i = 0; i != fields.Count; ++i) {
            _entries[i].scoreField = fields[i];
        }

        if(_experienceBar != null) {
            int totalScore = team.scoreInfo.totalScore;
            int levelup = team.scoreNeededForNextLevel;

            _experienceBar.value = totalScore/(float)levelup;
            _levelText.text = string.Format("Ruler Level {0}", team.rulerLevel+1);
            _toNextLevelText.text = string.Format("{0}/{1}", totalScore, levelup);

            if(totalScore >= levelup) {
                _levelExplanationText.text = "You will level up at the start of the next Moon.";
            } else {
                _levelExplanationText.text = "";
            }
        }
    }
}
