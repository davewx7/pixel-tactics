using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Assertions;


[System.Serializable]
public class UnitAttackCommandInfo
{
    public int seed;
    public string attackerGuid, targetGuid, attackId, counterId;
}

public class UnitAttackCommand : GameCommand
{
    public override string Serialize() { return Glowwave.Json.ToJson(info); }
    public override void Deserialize(string data) { info = Glowwave.Json.FromJson<UnitAttackCommandInfo>(data); }

    [SerializeField]
    Projectile _projectilePrefab = null;

    public UnitAttackCommandInfo info = new UnitAttackCommandInfo();
    ConsistentRandom _rng = null;

    List<Unit> _unitsCursed = new List<Unit>();

    IEnumerator Execute()
    {
        Unit attacker = GameController.instance.GetUnitByGuid(info.attackerGuid);
        Unit defender = GameController.instance.GetUnitByGuid(info.targetGuid);

        Debug.Log("DO ATTACK: " + info.attackerGuid + " / " + info.targetGuid + ": " + attacker.loc.ToString());

        Assert.IsNotNull(attacker);
        Assert.IsNotNull(defender);

        List<Unit> unitsInCombat = new List<Unit> { attacker, defender };

        if(attacker.team == defender.team || (attacker.team.player == false && defender.team.player && attacker.teamInfo.enemyOfPlayer == false)) {
            //Attack was scheduled but turns out we're not an enemy of the player, so cancel it.
            finished = true;
            yield break;
        }

        bool visible = attacker.tile.fogged == false || defender.tile.fogged == false;

        attacker.facing = Tile.DirOfLoc(attacker.loc, defender.loc);
        defender.facing = Tile.DirOfLoc(defender.loc, attacker.loc);

        if(attacker == null || defender == null) {
            Debug.LogError("Could not find units in attack command");
        }

        var attacks = attacker.unitInfo.GetAttacksForBattle(defender.unitInfo, true);
        var counters = defender.unitInfo.GetAttacksForBattle(attacker.unitInfo, false);

        AttackInfo? attack = null;
        AttackInfo? counter = null;

        foreach(AttackInfo a in attacks) {
            if(a.id == info.attackId) {
                attack = a;
                break;
            }
        }

        if(attack != null && attack.Value.numCharges > 0) {
            attacker.unitInfo.ExpendAttackCharge(attack.Value.id);
            Debug.Log("EXPEND CHARGE: " + attack.Value.id);
        }

        if(string.IsNullOrEmpty(info.counterId) == false) {
            foreach(AttackInfo a in counters) {
                if(a.id == info.counterId) {
                    counter = a;
                    break;
                }
            }
        }

        if(attacker.unitInfo.cursed) {
            if(_rng.Range(0,100) < 25) {
                attacker.FloatLabel("Cursed!", Color.red);
                _unitsCursed.Add(attacker);
                if(attack.HasValue) {
                    AttackInfo val = attack.Value;
                    val.accuracy = 0;
                    attack = val;
                }

                if(counter.HasValue) {
                    AttackInfo val = counter.Value;
                    val.accuracy = 100;
                    counter = val;
                }
            }
        }

        if(defender.unitInfo.cursed) {
            if(_rng.Range(0, 100) < 25) {
                defender.FloatLabel("Cursed!", Color.red);
                _unitsCursed.Add(defender);
                if(attack.HasValue) {
                    AttackInfo val = attack.Value;
                    val.accuracy = 100;
                    attack = val;
                }

                if(counter.HasValue) {
                    AttackInfo val = counter.Value;
                    val.accuracy = 0;
                    counter = val;
                }
            }
        }


        if(visible && attack != null) {
            GameController.instance.ShowAttackPreview(attacker.unitInfo, defender.unitInfo, attack.Value, counter);
        }

        System.Random rng = new System.Random();

        bool firstStrike = attack.Value.isFirstStrike == false && counter.HasValue && counter.Value.isFirstStrike;

        int nattacks = attack.Value.nstrikes;
        int ncounters = counter.HasValue ? counter.Value.nstrikes : 0;

        int startingAttacks = nattacks;
        int startingCounters = ncounters;

        bool berserk = attack.Value.isBerserk;

        List<int> attackerRolls = new List<int>();
        List<int> defenderRolls = new List<int>();

        Debug.Log("ATTACKS: " + nattacks + " / " + ncounters);
        while((nattacks > 0 || ncounters > 0) && attacker.unitInfo.dead == false && defender.unitInfo.dead == false) {
            if(nattacks > 0 && !firstStrike) {
                if(visible) {
                    yield return ExecuteStrike(attacker, defender, attackerRolls, nattacks, attack.Value, visible);
                } else {
                    ExecuteStrike(attacker, defender, attackerRolls, nattacks, attack.Value, visible);

                }

                --nattacks;
            }

            firstStrike = false;

            if(ncounters > 0 && attacker.unitInfo.dead == false && defender.unitInfo.dead == false) {
                if(visible) {
                    yield return ExecuteStrike(defender, attacker, defenderRolls, ncounters, counter.Value, visible);
                } else {
                    ExecuteStrike(defender, attacker, defenderRolls, ncounters, counter.Value, visible);
                }
                --ncounters;
            }

            if(berserk && nattacks == 0 && ncounters == 0) {
                nattacks = startingAttacks;
                ncounters = startingCounters;
            }
        }

        if(visible) {
            yield return null;
        }

        attacker.unitInfo.ExpendAttack();

        if(attacker.unitInfo.dead) {
            defender.AwardExperience(attacker.unitInfo.level == 0 ? 4 : attacker.unitInfo.level*8);
        } else {
            defender.AwardExperience(attacker.unitInfo.level);
        }

        if(defender.unitInfo.dead) {
            attacker.AwardExperience(defender.unitInfo.level == 0 ? 4 : defender.unitInfo.level*8);
        } else {
            attacker.AwardExperience(defender.unitInfo.level);
        }

        attacker.unitInfo.fightsThisTurn++;
        defender.unitInfo.fightsThisTurn++;

        bool playerInvolvedBattle = attacker.team.player || defender.team.player;

        if(GameController.instance.CheckUnitDeath(attacker, playerInvolved: playerInvolvedBattle)) {

            if(counter.HasValue && counter.Value.isZombify && attacker.unitInfo.isUndead == false) {
                //turn into a zombie!
                List<UnitTrait> traits = new List<UnitTrait>();
                if(attacker.unitInfo.zombieTrait != null) {
                    traits.Add(attacker.unitInfo.zombieTrait);
                }

                GameController.instance.ExecuteRecruit(new RecruitCommandInfo() {
                    unitType = GameConfig.instance.zombieUnitType,
                    loc = attacker.loc,
                    seed = 0,
                    team = defender.team,
                    summonerGuid = defender.summonerOrSelf.unitInfo.guid,
                    isFamiliar = true,
                    unitStatus = new List<UnitStatus>() { GameConfig.instance.statusTemporal },
                    unitTraits = traits,
                    haveHaste = true,
                });
            }

            if(visible) {
                yield return new WaitUntil(() => attacker == null || attacker.dieAnimFinished);
            }
        }

        if(GameController.instance.CheckUnitDeath(defender, playerInvolved: playerInvolvedBattle)) {
            if(attack.HasValue && attack.Value.isZombify && defender.unitInfo.isUndead == false) {
                //turn into a zombie!
                List<UnitTrait> traits = new List<UnitTrait>();
                if(defender.unitInfo.zombieTrait != null) {
                    traits.Add(defender.unitInfo.zombieTrait);
                }

                GameController.instance.ExecuteRecruit(new RecruitCommandInfo() {
                    unitType = GameConfig.instance.zombieUnitType,
                    loc = defender.loc,
                    seed = 0,
                    team = attacker.team,
                    summonerGuid = attacker.summonerOrSelf.unitInfo.guid,
                    isFamiliar = true,
                    unitStatus = new List<UnitStatus>() { GameConfig.instance.statusTemporal },
                    unitTraits = traits,
                    haveHaste = true,
                });
            }

            if(visible) {
                yield return new WaitUntil(() => defender == null || defender.dieAnimFinished);
            }
        }

        if(attacker.team.player) {
            defender.teamInfo.StartWarWithPlayer();
        }

        foreach(Unit unit in _removeBlessingOfProtection) {
            unit.unitInfo.status.Remove(GameConfig.instance.statusBlessingOfProtection);
        }

        foreach(var unit in _unitsCursed) {
            unit.unitInfo.status.Remove(GameConfig.instance.statusCurse);
        }

        if(visible) {
            GameController.instance.FadeOutAttackPreview();
        }

        finished = true;
    }

    int factorial(int n)
    {
        int result = 1;
        while(n > 1) {
            result *= n;
            --n;
        }
        return result;
    }

    int num_ways(int nhits, int nswings)
    {
        return factorial(nswings) / (factorial(nhits) * factorial(nswings-nhits));
    }

    List<float> calc(float chance, int nswings)
    {
        List<float> probs = new List<float>();

        float ev = chance*nswings;

        float calc_ev = 0.0f;
        float sum = 0.0f;
        for(int i = 0; i <= nswings; ++i) {

            float probability = Mathf.Pow(chance, i) * (Mathf.Pow(1.0f-chance, nswings-i) * num_ways(i, nswings));
            sum = sum + probability;
            calc_ev += probability*i;

            probs.Add(probability);
        }

        if(nswings > 1) {
            int counterlo = (int)ev;
            int counterhi = (int)Mathf.Ceil(ev);

            float reduce_miss_all = probs[0]*0.75f;
            if(reduce_miss_all > 0.08f) {
                reduce_miss_all = 0.08f;
            }

            float reduce_hit_all = probs[probs.Count-1]*0.75f;
            if(reduce_hit_all > 0.08f) {
                reduce_hit_all = 0.08f;
            }

            float evdelta = reduce_hit_all*nswings;

            float redistribution = reduce_miss_all+reduce_hit_all;

            float redistribution_mean = evdelta/redistribution;
            float r = redistribution_mean - counterlo;

            float increase_lo = (1.0f - r)*redistribution;
            float increase_hi = r*redistribution;

            probs[0] -= reduce_miss_all;
            probs[probs.Count-1] -= reduce_hit_all;
            probs[counterlo] += increase_lo;
            probs[counterhi] += increase_hi;

            float new_ev = 0.0f;
            for(int i = 0; i <= nswings; ++i) {
                sum += probs[i];
                new_ev += probs[i]*i;
            }

            Debug.LogFormat("COMBAT EV: {0} vs {1}", calc_ev, new_ev);
        }

        return probs;
    }


    void GenerateRolls(int nattacks, List<int> rolls, int chanceToHit)
    {
        if(chanceToHit < 0) {
            chanceToHit = 0;
        }

        if(chanceToHit > 100) {
            chanceToHit = 100;
        }

        float hitProbability = chanceToHit*0.01f;
        List<float> probs = calc(hitProbability, nattacks);

        float r = _rng.Range(0.0f, 1.0f);
        int numHits = probs.Count-1;
        for(int i = 0; i != probs.Count; ++i) {
            if(r < probs[i]) {
                numHits = i;
                break;
            }

            r -= probs[i];
        }

        for(int i = 0; i < nattacks; ++i) {
            if(i < numHits) {
                rolls.Add(_rng.Range(0, chanceToHit));
            } else {
                rolls.Add(_rng.Range(chanceToHit, 100));
            }
        }
    }

    List<Unit> _removeBlessingOfProtection = new List<Unit>();

    IEnumerator ExecuteStrike(Unit attacker, Unit defender, List<int> rolls, int nattacks, AttackInfo attack, bool visible)
    {
        int chanceToHit = (attack.accuracy+100);

        if(rolls.Count == 0) {
            GenerateRolls(nattacks, rolls, chanceToHit);
        }

        int rollIndex = _rng.Range(0, rolls.Count);

        int roll = rolls[rollIndex];
        rolls.RemoveAt(rollIndex);
        bool attackHits = roll < chanceToHit;

        if(defender.unitInfo.status.Contains(GameConfig.instance.statusBlessingOfProtection)) {
            attackHits = false;
            _removeBlessingOfProtection.Add(defender);

            defender.FloatLabel("Protected!", Color.white);
        } else if(defender.unitInfo.sleeping) {
            attackHits = true;
            defender.WakeUp();

            defender.FloatLabel("Awoken!", Color.red);
        }

        if(defender.unitInfo.charmed) {
            defender.RemoveCharmed();
            defender.FloatLabel("Charm Ends!", Color.red);
        }

        int rollCrit = _rng.Next(100);
        bool attackCrits = rollCrit < (attack.critical);

        int damage = attackHits ? (attackCrits ? attack.criticalDamage : attack.damage) : 0;

        bool unitDying = (defender.unitInfo.damageTaken + damage) >= (defender.unitInfo.temporaryHitpoints + defender.unitInfo.hitpointsMax);

        if(unitDying && defender.unitInfo.ruler) {
            defender.teamInfo.killedBy = attacker.team;
        }

        bool playerWins = false;
        bool gameOverTriggering = unitDying && defender.team.player && defender.unitInfo.ruler;
        if(unitDying && defender.team.primaryEnemy && defender.unitInfo.ruler) {
            gameOverTriggering = true;
            playerWins = true;
        }

        if(gameOverTriggering) {
            GameController.instance.unitKillingPlayer = attacker;
            GameController.instance.gameOverTimescale = true;
            Debug.Log("TRIGGERING GAME OVER");
            attacker.MoveAboveDeathLayer();
            defender.MoveAboveDeathLayer();
        }


        Debug.Log("CRIT ROLL: " + rollCrit + " < " + attack.critical + " = " + attackCrits);

        Debug.Log("ROLL: " + roll + " vs " + attack.accuracy);

        attacker.spriteRenderer.sortingOrder += 100;

        AnimInfo animInfo = attacker.GetAnim(new AnimMatch() { direction = attacker.facing, animType = AnimType.Other, tag = attack.id });
        attacker.PlayAnimation(animInfo);

        float hitTiming = visible ? animInfo.GetEventTiming("hit", animInfo.duration*0.5f) : 0f;


        Tweener tween = null;
        if(attack.range == AttackInfo.Range.Melee) {
            defender.PlayAnimation(AnimType.Defend);

            Vector3 a = GameController.instance.map.GetTile(attacker.tile.loc).unitPos;
            Vector3 b = GameController.instance.map.GetTile(defender.tile.loc).unitPos;
            Vector3[] path = new Vector3[] {
                a,
                Vector3.Lerp(a, b, 0.6f),
                a,
            };

            Debug.Log("EXECUTE STRIKE: " + path[0].ToString() + " / " + path[1].ToString() + " / " + path[2].ToString());

            tween = attacker.avatarTransform.DOPath(path, visible ? animInfo.duration : 0f, PathType.Linear).SetEase(Ease.Linear);
        }

        if(visible) {
            yield return new WaitForSeconds(hitTiming/GameController.timeScale);
        }

        Projectile projectile = null;

        if(attack.range == AttackInfo.Range.Ranged && visible) {
            projectile = Instantiate(_projectilePrefab);
            projectile.transform.position = attacker.transform.position;
            projectile.direction = attacker.facing;
            projectile.projectileType = attack.projectileType != null ? attack.projectileType : GameConfig.instance.defaultProjectile;
            projectile.targetPoint = defender.transform.position;
            projectile.hits = attackHits;
        }

        while(projectile != null && projectile.finished == false) {
            yield return null;
        }

        if(attackHits) {
            if(attackCrits) {
                defender.FlashCrit();
            } else {
                defender.FlashHit(Color.white);
            }

            if(gameOverTriggering) {
                GameController.instance.TriggerGameOver(playerWins);
            }

            defender.unitInfo.InflictDamage(damage);

            if(visible) {
                defender.FloatLabel(string.Format("{0}", damage), Color.red);
            }

            if(damage > 0) {
                foreach(UnitStatus status in attack.applyStatus) {
                    defender.ApplyStatus(status);
                }
            }

            int lifesteal = (attack.lifestealPercent*damage)/100;
            if((attack.lifestealPercent*damage)%100 > 0) {
                ++lifesteal;
            }

            if(lifesteal > attacker.unitInfo.damageTaken) {
                lifesteal = attacker.unitInfo.damageTaken;
            }

            if(lifesteal > 0) {
                attacker.Heal(lifesteal);

                if(attacker.unitInfo.isTemporal) {
                    //temporal lifestealers give life to their summoner.
                    Unit summoner = GameController.instance.GetUnitByGuid(attacker.unitInfo.summonerGuid);
                    if(summoner != null && summoner != attacker) {
                        summoner.Heal(lifesteal);
                    }
                }
            }
        }

        if(visible) {
            GameController.instance.RefreshUnitDisplayed();
        }

        if(tween != null && tween.active && visible) {
            yield return tween.WaitForCompletion();
        }
        attacker.spriteRenderer.sortingOrder -= 100;

        //blessing of strength goes away if we hit.
        if(attackHits) {
            attacker.unitInfo.status.Remove(GameConfig.instance.statusBlessingOfStrength);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _rng = new ConsistentRandom(info.seed);
        StartCoroutine(Execute());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
