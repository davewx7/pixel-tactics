using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackDialog : MonoBehaviour
{
    [SerializeField]
    TMPro.TextMeshProUGUI _attackIndifferentPanel = null;

    public RectTransform mainPanel;

    public Unit attacker;
    public Unit defender;
    public Loc attackerLocOverride;
    public Pathfind.Path attackerPath = null;

    public AttackInfo chosenAttack { get { return _chosenAttack.attackInfo; } }
    public AttackInfo? chosenCounter { get { return _chosenAttack.counterattackInfo; } }

    public List<AttackPanel> attackPanels = new List<AttackPanel>();

    AttackPanel _chosenAttack = null;

    [SerializeField]
    UnitStatusPanel _attackerStatus = null;

    [SerializeField]
    UnitStatusPanel _defenderStatus = null;

    [SerializeField]
    Transform _attackNonHostileWarning = null;

    public void HighlightAttack(AttackPanel panel)
    {
        _chosenAttack = panel;
        foreach(AttackPanel attackPanel in attackPanels) {
            attackPanel.highlight = (attackPanel == panel);
        }
    }

    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    // Start is called before the first frame update
    void Start()
    {
        Loc attackerLocBackup= new Loc();
        if(attackerLocOverride.valid) {
            attackerLocBackup = attacker.loc;
            attacker.loc = attackerLocOverride;
        }

        _attackerStatus.Init(attacker);
        _defenderStatus.Init(defender);

        var attacks = attacker.unitInfo.GetAttacksForBattle(defender.unitInfo, true, true);
        int numPanels = attacks.Count;
        mainPanel.sizeDelta = new Vector2(mainPanel.sizeDelta.x, mainPanel.sizeDelta.y + (numPanels-1)*80f);
        for(int i = 1; i < numPanels; ++i) {
            AttackPanel newPanel = Instantiate(attackPanels[attackPanels.Count-1], mainPanel.transform);
            newPanel.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, -80f);
            attackPanels.Add(newPanel);
        }

        for(int i = 0; i != numPanels; ++i) {
            attackPanels[i].attacker = attacker.unitInfo;
            attackPanels[i].defender = defender.unitInfo;

            attackPanels[i].attackInfo = attacks[i];
            attackPanels[i].counterattackInfo = defender.unitInfo.GetBestCounterattack(attacker.unitInfo, attacks[i], true);
        }

        int startingIndex = 0;
        float bestScore = 0f;
        for(int i = 0; i != attacks.Count; ++i) {
            float score = AI.ScoreAttack(attacks[i]);
            if(score > bestScore) {
                bestScore = score;
                startingIndex = i;
            }
        }

        HighlightAttack(attackPanels[startingIndex]);

        foreach(AttackPanel panel in attackPanels) {
            panel.Init();
        }

        _attackNonHostileWarning.gameObject.SetActive(attacker.IsEnemy(defender) == false);

        if(attacker.IsEnemy(defender) == false) {
            _attackIndifferentPanel.text = string.Format("The {0} are {1}. If we attack them they will become hostile to us!", defender.team.teamName, attacker.IsAlly(defender) ? "our allies" : "still making up their mind about us");
        }

        if(attackerLocBackup.valid) {
            attacker.loc = attackerLocBackup;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
