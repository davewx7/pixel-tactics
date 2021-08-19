using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/SpeechPrompt")]
public class SpeechPrompt : GWScriptableObject
{
    [System.Serializable]
    public class UnitFilter
    {
        public List<UnitType> unitTypesMatching;
        public List<Team> teamsMatching;
        public bool playerUnitsMatch = false;
        public bool aiUnitsMatch = false;
        public bool barbarianUnitsMatch = false;
        public bool primaryEnemyUnitsMatch = false;
        public bool unitMustBeAbleToSpeak = false;

        public bool UnitMatches(Unit unit)
        {
            if(unitTypesMatching.Count != 0 && unitTypesMatching.Contains(unit.unitInfo.unitType) == false) {
                return false;
            }

            if(teamsMatching.Count != 0 && teamsMatching.Contains(unit.team) == false) {
                return false;
            }

            if(playerUnitsMatch == false && unit.team.player) {
                return false;
            }

            if(aiUnitsMatch == false && unit.team.ai != null) {
                return false;
            }
            if(barbarianUnitsMatch == false && unit.team.barbarian) {
                return false;
            }

            if(primaryEnemyUnitsMatch == false && unit.team.primaryEnemy) {
                return false;
            }

            if(unitMustBeAbleToSpeak && unit.canSpeak == false) {
                return false;
            }

            return true;
        }
    }

    public bool oncePerGame = true;
    public float embargoTime = -1.0f;

    public UnitFilter filterSpeaker = new UnitFilter();
    public List<UnitFilter> filterSightedUnits = new List<UnitFilter>();
    public List<UnitFilter> filterRecruitedUnits = new List<UnitFilter>();
    public bool triggerOnSightUnderworldGate = false;

    public SpeechPrompt nextPrompt;

    [SerializeField]
    List<string> _speech = new List<string>();

    static public void InitNewGame(Team playerTeam)
    {
        GameController.instance.gameState.globalSpeechPrompts.Clear();
        foreach(var prompt in GameConfig.instance.globalSpeechPrompts) {
            GameController.instance.gameState.globalSpeechPrompts.Add(new SpeechPromptInstance() {
                prompt = prompt,
            });
        }

        foreach(var prompt in playerTeam.speechPrompts) {
            GameController.instance.gameState.globalSpeechPrompts.Add(new SpeechPromptInstance() {
                prompt = prompt,
            });
        }
    }

    static public List<SpeechPromptInstance> GetSpeechPromptsForUnit(Unit unit)
    {
        List<SpeechPromptInstance> prompts = new List<SpeechPromptInstance>(GameController.instance.gameState.globalSpeechPrompts);
        return prompts;
    }

    static public void UnitSighted(Unit caster, Unit sighted)
    {
        foreach(var instance in GetSpeechPromptsForUnit(caster)) {
            instance.prompt.OnUnitSighted(instance, caster, sighted);
        }
    }

    static public void UnderworldGateSighted(Unit caster, Tile sighted)
    {
        foreach(var instance in GetSpeechPromptsForUnit(caster)) {
            instance.prompt.OnUnderworldGateSighted(instance, caster, sighted);
        }
    }

    static public void UnitRecruited(Unit recruiter, Unit recruited)
    {
        foreach(var instance in GetSpeechPromptsForUnit(recruiter)) {
            instance.prompt.OnUnitRecruited(instance, recruiter, recruited);
        }
    }


    public bool UnitEligible(SpeechPromptInstance instance, Unit unit)
    {
        if(filterSpeaker.UnitMatches(unit) == false) {
            return false;
        }

        return true;
    }

    bool IsEmbargoed()
    {
        if(GameController.instance.gameState.embargoedSpeechPrompts.Contains(this)) {
            return true;
        }

        if(embargoTime > 0f) {
            foreach(var instance in GameController.instance.speechQueue.history) {
                if(instance.instance.prompt == this && instance.timestamp + embargoTime > Time.time) {
                    return true;
                }
            }
        }

        return false;
    }

    public void OnUnderworldGateSighted(SpeechPromptInstance instance, Unit caster, Tile sighted)
    {
        if(triggerOnSightUnderworldGate == false) {
            return;
        }

        if(UnitEligible(instance, caster) == false) {
            return;
        }

        if(IsEmbargoed()) {
            return;
        }

        Enqueue(instance, caster);
    }

    public void OnUnitSighted(SpeechPromptInstance instance, Unit caster, Unit sighted)
    {
        if(filterSightedUnits.Count == 0) {
            return;
        }

        if(UnitEligible(instance, caster) == false) {
            return;
        }

        if(IsEmbargoed()) {
            return;
        }

        bool trigger = false;
        foreach(UnitFilter filter in filterSightedUnits) {
            if(filter.UnitMatches(sighted)) {
                trigger = true;
                break;
            }
        }

        if(trigger) {
            Enqueue(instance, caster);
        }
    }

    public void OnUnitRecruited(SpeechPromptInstance instance, Unit recruiter, Unit recruited)
    {
        if(filterRecruitedUnits.Count == 0) {
            return;
        }

        if(UnitEligible(instance, recruiter) == false) {
            return;
        }

        if(IsEmbargoed()) {
            return;
        }

        bool trigger = false;
        foreach(UnitFilter filter in filterRecruitedUnits) {
            if(filter.UnitMatches(recruited)) {
                trigger = true;
                break;
            }
        }

        if(trigger) {
            Enqueue(instance, recruiter);
        }
    }


    public void Enqueue(SpeechPromptInstance instance, Unit caster)
    {
        GameController.instance.speechQueue.Enqueue(new SpeechPromptQueue.Item() {
            instance = instance,
            unit = caster,
            timestamp = Time.time,
        });

        if(oncePerGame) {
            GameController.instance.gameState.embargoedSpeechPrompts.Add(this);
        }
    }

    public void Trigger(SpeechPromptInstance instance, Unit caster)
    {
        if(_speech.Count > 0) {
            int nindex = GameController.instance.rng.Range(0, _speech.Count);
            caster.ShowSpeechBubble(_speech[nindex]);
        }

        if(oncePerGame) {
            GameController.instance.gameState.embargoedSpeechPrompts.Add(this);
        }
    }
}

public class SpeechPromptQueue
{
    public class Item
    {
        public Unit unit;
        public SpeechPromptInstance instance;
        public float timestamp;
    }

    public List<Item> history = new List<Item>();

    List<Item> _queue = new List<Item>();
    Unit _unitSpeaking = null;

    public bool unitCurrentlySpeaking {
        get {
            return _queue.Count != 0 || _unitSpeaking != null;
        }
    }

    public void Enqueue(Item item)
    {
        _queue.Add(item);
        history.Add(item);
    }

    public void Update()
    {
        if(_unitSpeaking != null && _unitSpeaking.speaking) {
            return;
        }

        _unitSpeaking = null;

        if(_queue.Count > 0) {
            Item item = _queue[0];
            _unitSpeaking = item.unit;
            item.instance.prompt.Trigger(item.instance, item.unit);

            if(item.instance.prompt.nextPrompt != null) {
                item.instance = item.instance.Clone();
                item.instance.prompt = item.instance.prompt.nextPrompt;
            } else {
                _queue.RemoveAt(0);
            }
        }
    }
}

[System.Serializable]
public class SpeechPromptInstance
{
    public SpeechPrompt prompt;

    public SpeechPromptInstance Clone()
    {
        return (SpeechPromptInstance)this.MemberwiseClone();
    }
}