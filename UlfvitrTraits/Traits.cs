using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;

namespace TheWiseWolf
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs
        public static string[] myTraitList = ["ulfvitrcalltherain","ulfvitrmagnet","ulfvitrregenerator","ulfvitrconductor","ulfvitrlifebloom"];

        public static void DoCustomTrait(string _trait, ref Trait __instance)
        {
            // get info you may need
            Enums.EventActivation _theEvent = Traverse.Create(__instance).Field("theEvent").GetValue<Enums.EventActivation>();
            Character _character = Traverse.Create(__instance).Field("character").GetValue<Character>();
            Character _target = Traverse.Create(__instance).Field("target").GetValue<Character>();
            int _auxInt = Traverse.Create(__instance).Field("auxInt").GetValue<int>();
            string _auxString = Traverse.Create(__instance).Field("auxString").GetValue<string>();
            CardData _castedCard = Traverse.Create(__instance).Field("castedCard").GetValue<CardData>();
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            // activate traits
            // I don't know how to set the combatLog text I need to do that for all of the traits
            if (_trait == "ulfvitrcalltherain")
            { // apply 1 wet to all characters at start of turn
                string traitName = "Call the Rain";
                if ((Object) _character.HeroData != (Object) null){
                    bool heroFlag = true;
                    bool enemyFlag = true;
                    bool randomNpc = false;
                    bool randomHero = false;
                    
                    ApplyAuraCurseTo("wet",1,heroFlag, enemyFlag,randomHero,randomNpc,ref _character,ref teamHero, ref teamNpc, traitName,"drink");

                    
                    if (!((Object) _character.HeroItem != (Object) null))
                        return;
                    _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName), Enums.CombatScrollEffectType.Trait);
                    return;
                }
            }

                    
            else if (_trait == "ulfvitrmagnet")
            { // 3x per turn, if you play a lightning spell that costs energy, refund 1 energy
                string traitName = "Magnet";
                if (!((UnityEngine.Object)MatchManager.Instance != (UnityEngine.Object)null) || !((UnityEngine.Object)_castedCard != (UnityEngine.Object)null))
                    return;
                if (MatchManager.Instance.activatedTraits != null && MatchManager.Instance.activatedTraits.ContainsKey(_trait) && MatchManager.Instance.activatedTraits[_trait] > traitData.TimesPerTurn - 1)
                    return;
                if (_castedCard.GetCardTypes().Contains(Enums.CardType.Lightning_Spell) && _character.HeroData != null && _castedCard.EnergyCost >= 1)
                {
                    if (!MatchManager.Instance.activatedTraits.ContainsKey("ulfvitrmagnet"))
                    {
                        MatchManager.Instance.activatedTraits.Add("ulfvitrmagnet", 1);
                    }
                    else
                    {
                        Dictionary<string, int> activatedTraits = MatchManager.Instance.activatedTraits;
                        activatedTraits["ulfvitrmagnet"] = activatedTraits["ulfvitrmagnet"] + 1;
                    }
                    MatchManager.Instance.SetTraitInfoText();
                    _character.ModifyEnergy(1, true);
                    if (_character.HeroItem != null)
                    {
                        _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName, "") + TextChargesLeft(MatchManager.Instance.activatedTraits["ulfvitrmagnet"], traitData.TimesPerTurn), Enums.CombatScrollEffectType.Trait);
                        EffectsManager.Instance.PlayEffectAC("energy", true, _character.HeroItem.CharImageT, false, 0f);
                    }
                    NPC randNPC = teamNpc[MatchManager.Instance.GetRandomIntRange(0,3)];
                    if ((randNPC != null) && randNPC.Alive){
                        randNPC.SetAuraTrait(_character, "spark", 1);
                    }
                }
                return;
            }
                
             
            else if (_trait == "ulfvitrregenerator")
            {// When you apply regen, heal by wet x1
                if (_auxString=="regeneration" && _target!= null && _target.IsHero && _target.HasEffect("wet"))
                {
                    int targetWet = _target.EffectCharges("wet");
                    int healAmount = Functions.FuncRoundToInt((float) targetWet * 1.0f);
                    
                    TraitHeal(ref _character, ref _target, healAmount, _trait);
                }
            } 

            else if (_trait == "ulfvitrconductor")
            {// When you apply wet, deal sparks * 0.5 as indirect damage
                
                // done in SetEventPrefix? nvmd trying to do it here
                if (_character.HeroData!= (Object) null){
                    if (_auxString=="wet" && !_target.IsHero && _target.HasEffect("spark")){
                        _target.IndirectDamage(Enums.DamageType.Lightning, Functions.FuncRoundToInt((float)_target.GetAuraCharges("spark") * 0.5f));
                    }
                }

            } 
            else if (_trait == "ulfvitrlifebloom")
            { //At end of turn, heal all heroes by wet * 0.70
                Plugin.Log.LogDebug("Lifebloom Start:");
                if (_character.HeroData!=null){
                    for (int i = 0; i < teamHero.Length; i++)
                    {
                        if (teamHero[i] != null && teamHero[i].Alive)
                        {
                            int healAmount = Functions.FuncRoundToInt((float)teamHero[i].GetAuraCharges("wet") * 0.70f);
                            Plugin.Log.LogDebug("Lifebloom Heal Amount: " + healAmount);
                            TraitHealHero(ref _character, ref teamHero[i], healAmount, _trait);

                        }
                    }
                } 
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            Traverse.Create(__instance).Field("character").SetValue(_character);
            Traverse.Create(__instance).Field("target").SetValue(_target);
            Traverse.Create(__instance).Field("theEvent").SetValue(_theEvent);
            Traverse.Create(__instance).Field("auxInt").SetValue(_auxInt);
            Traverse.Create(__instance).Field("auxString").SetValue(_auxString);
            Traverse.Create(__instance).Field("castedCard").SetValue(_castedCard);
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "SetEvent")]
        public static void SetEventPrefix(ref Character __instance, ref Enums.EventActivation theEvent, Character target = null)
        {
            //if (theEvent == Enums.EventActivation.AuraCurseSet && __instance.IsHero && target != null && !target.IsHero && __instance.HaveTrait("ulfvitrconductor") && target.HasEffect("spark"))
            //if (theEvent == Enums.EventActivation.AuraCurseSet && !__instance.IsHero && target != null && target.IsHero && target.HaveTrait("ulfvitrconductor") && __instance.HasEffect("spark")){ // if NPC has wet applied to them, deal 50% of their sparks as indirect lightning damage
            //    __instance.IndirectDamage(Enums.DamageType.Lightning, Functions.FuncRoundToInt((float)__instance.GetAuraCharges("spark") * 0.5f));
            //}
        }
        public static void TraitHeal(ref Character _character, ref Character _target, int healAmount, string traitName)
        {
            int _hp = healAmount;
            if (_target.GetHpLeftForMax() < healAmount)
                _hp = _target.GetHpLeftForMax();
            if (_hp <= 0)
                return;
            _target.ModifyHp(_hp);
            CastResolutionForCombatText _cast = new CastResolutionForCombatText();
            _cast.heal = _hp;
            if ((Object) _target.HeroItem != (Object) null)
            {
                _target.HeroItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.HeroItem.CharImageT, false);
            }
            else
            {
            _target.NPCItem.ScrollCombatTextDamageNew(_cast);
            EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.NPCItem.CharImageT, false);
            }
            _target.SetEvent(Enums.EventActivation.Healed);
            _character.SetEvent(Enums.EventActivation.Heal, _target);
            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName), Enums.CombatScrollEffectType.Trait);
        }

        public static void TraitHealHero(ref Character _character, ref Hero _target, int healAmount, string traitName)
        {
            int _hp = healAmount;
            if (_target.GetHpLeftForMax() < healAmount)
                _hp = _target.GetHpLeftForMax();
            if (_hp <= 0)
                return;
            _target.ModifyHp(_hp);
            CastResolutionForCombatText _cast = new CastResolutionForCombatText();
            _cast.heal = _hp;
            if ((Object) _target.HeroItem != (Object) null)
            {
                _target.HeroItem.ScrollCombatTextDamageNew(_cast);
                EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.HeroItem.CharImageT, false);
            }
            else
            {
            _target.NPCItem.ScrollCombatTextDamageNew(_cast);
            EffectsManager.Instance.PlayEffectAC("healimpactsmall", true, _target.NPCItem.CharImageT, false);
            }
            _target.SetEvent(Enums.EventActivation.Healed);
            _character.SetEvent(Enums.EventActivation.Heal, _target);
            _character.HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName), Enums.CombatScrollEffectType.Trait);
        }

        public static void WhenYouPlayXRefund1Energy(Enums.CardType cardType, ref Character _character, string traitName, string traitID){
            // too lazy to write this since they all come with secondary effects
        }

        public static void ApplyAuraCurseTo(string auraCurse, int amount, bool allHeroFlag, bool allNpcFlag, bool randomHeroFlag, bool randomNpcFlag, ref Character _character, ref Hero[] teamHeroes, ref NPC[] teamNpc, string traitName, string soundEffect){
            if (allNpcFlag){
                for (int index = 0; index < teamNpc.Length; ++index)
                    {
                        if (teamNpc[index] != null && teamNpc[index].Alive)
                        {
                        teamNpc[index].SetAuraTrait(_character, auraCurse, amount);
                        if ((Object) teamNpc[index].NPCItem != (Object) null)
                        {
                            teamNpc[index].NPCItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName), Enums.CombatScrollEffectType.Trait);
                            EffectsManager.Instance.PlayEffectAC(soundEffect, true, teamNpc[index].NPCItem.CharImageT, false);
                        }
                        }
                    }
            }
            if (allHeroFlag){
                for (int index = 0; index < teamHeroes.Length; ++index)
                    {
                        if (teamHeroes[index] != null && teamHeroes[index].Alive)
                        {
                        teamHeroes[index].SetAuraTrait(_character, auraCurse, amount);
                        if ((Object) teamHeroes[index].NPCItem != (Object) null)
                        {
                            teamHeroes[index].HeroItem.ScrollCombatText(Texts.Instance.GetText("traits_"+traitName), Enums.CombatScrollEffectType.Trait);
                            EffectsManager.Instance.PlayEffectAC(soundEffect, true, teamHeroes[index].NPCItem.CharImageT, false);
                        }
                        }
                    }
            }


        }

        public static string TextChargesLeft(int currentCharges, int chargesTotal)
        {
            int cCharges = currentCharges;
            int cTotal = chargesTotal;
            return "<br><color=#FFF>" + cCharges.ToString() + "/" + cTotal.ToString() + "</color>";
        }
    }
}
