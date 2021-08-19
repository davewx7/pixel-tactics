using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wesnoth/DiplomacyRegistry")]
public class DiplomacyRegistry : GWScriptableObject
{
    public DiplomacyRegistry baseRegistry;

    [SerializeField]
    List<DiplomacyNode> _initialGreeting = null;

    public List<DiplomacyNode> initialGreeting {
        get {
            return _initialGreeting ?? baseRegistry?.initialGreeting;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _initialGreetingPleased = null;

    public List<DiplomacyNode> initialGreetingPleased {
        get {
            return _initialGreetingPleased ?? baseRegistry?.initialGreetingPleased;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _initialGreetingQuestCompleted = null;

    public List<DiplomacyNode> initialGreetingQuestCompleted {
        get {
            return _initialGreetingQuestCompleted ?? baseRegistry?.initialGreetingQuestCompleted;
        }
    }


    [SerializeField]
    List<DiplomacyNode> _initialGreetingLeaderDead = null;

    public List<DiplomacyNode> initialGreetingLeaderDead {
        get {
            return _initialGreetingLeaderDead ?? baseRegistry?.initialGreetingLeaderDead;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _offerJoinLeaderDead = null;

    public List<DiplomacyNode> offerJoinLeaderDead {
        get {
            return _offerJoinLeaderDead ?? baseRegistry?.offerJoinLeaderDead;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _talkLeaderDead = null;

    public List<DiplomacyNode> talkLeaderDead {
        get {
            return _talkLeaderDead ?? baseRegistry?.talkLeaderDead;
        }
    }


    [SerializeField]
    List<DiplomacyNode> _initialGreetingSwornEnemies = null;

    public List<DiplomacyNode> initialGreetingSwornEnemies {
        get {
            return _initialGreetingSwornEnemies ?? baseRegistry?.initialGreetingSwornEnemies;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _initialGreetingAlreadyWar = null;

    public List<DiplomacyNode> initialGreetingAlreadyWar {
        get {
            return _initialGreetingAlreadyWar ?? baseRegistry?.initialGreetingAlreadyWar;
        }
    }


    [SerializeField]
    List<DiplomacyNode> _declareWar = null;

    public List<DiplomacyNode> declareWar {
        get {
            return _declareWar ?? baseRegistry?.declareWar;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _peacefulAgreement = null;

    public List<DiplomacyNode> peacefulAgreement {
        get {
            return _peacefulAgreement ?? baseRegistry?.peacefulAgreement;
        }
    }


    [SerializeField]
    List<DiplomacyNode> _friendlyGreetingHasQuest = null;

    public List<DiplomacyNode> friendlyGreetingHasQuest {
        get {
            return _friendlyGreetingHasQuest ?? baseRegistry?.friendlyGreetingHasQuest;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _friendlyGreetingNoQuest = null;

    public List<DiplomacyNode> friendlyGreetingNoQuest {
        get {
            return _friendlyGreetingNoQuest ?? baseRegistry?.friendlyGreetingNoQuest;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _friendlyFarewell = null;

    public List<DiplomacyNode> friendlyFarewell {
        get {
            return _friendlyFarewell ?? baseRegistry?.friendlyFarewell;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _offerGift = null;

    public List<DiplomacyNode> offerGift {
        get {
            return _offerGift ?? baseRegistry?.offerGift;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _offerGiftForRejectingEnemies = null;

    public List<DiplomacyNode> offerGiftForRejectingEnemies {
        get {
            return _offerGiftForRejectingEnemies ?? baseRegistry?.offerGiftForRejectingEnemies;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _questCompleted = null;

    public List<DiplomacyNode> questCompleted {
        get {
            return _questCompleted ?? baseRegistry?.questCompleted;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _offerFealty = null;

    public List<DiplomacyNode> offerFealty {
        get {
            return _offerFealty ?? baseRegistry?.offerFealty;
        }
    }

    [SerializeField]
    List<DiplomacyNode> _allyGreeting = null;

    public List<DiplomacyNode> allyGreeting {
        get {
            return _allyGreeting ?? baseRegistry?.allyGreeting;
        }
    }

}
