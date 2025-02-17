﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using static TheWiseWolf.CustomFunctions;
using static TheWiseWolf.Plugin;

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
            if (!IsLivingHero(_character))
            {
                return;
            }
            string traitId = _trait;

            if (_trait == "ulfvitrcalltherain")
            { // apply 1 wet to all characters at start of turn
                string traitName = "Call the Rain";
                if ((Object) _character.HeroData != (Object) null){
                    LogDebug($"Executing Trait {traitId}: {traitName}");

                    ApplyAuraCurseToAll("wet",1,AppliesTo.Global,sourceCharacter:_character, useCharacterMods:true);
                                        
                    // DisplayTraitScroll(ref _character, traitData);                    
                }
            }

                    
            else if (_trait == "ulfvitrmagnet")
            { // 3x per turn, if you play a lightning spell that costs energy, refund 1 energy
                // string traitName = "Magnet";

                if(CanIncrementTraitActivations(traitId) && _castedCard.HasCardType(Enums.CardType.Lightning_Spell) && _castedCard.EnergyCost >= 1)
                {
                    _character.ModifyEnergy(1, true);  
                    Character randNPC = GetRandomCharacter(teamNpc);
                    // NPC randNPC = teamNpc[MatchManager.Instance.GetRandomIntRange(0,3)];
                    if (IsLivingNPC(randNPC)){
                        randNPC.SetAuraTrait(_character, "spark", 1);
                    } 
                    IncrementTraitActivations(traitId);
                }                
                return;
            }
                
             
            else if (_trait == "ulfvitrregenerator")
            {// When you apply regen, heal by wet x1                
                if (_auxString=="regeneration" && IsLivingHero(_target))
                {
                    int targetWet = _target.GetAuraCharges("wet");
                    float multiplier = 1.0f;
                    int healAmount = Functions.FuncRoundToInt((float) targetWet * multiplier);
                    
                    TraitHeal(ref _character, ref _target, healAmount, _trait);
                }
            } 

            else if (_trait == "ulfvitrconductor")
            {// When you apply wet, deal sparks * 0.5 as indirect damage
                
                // done in SetEventPrefix? nvmd trying to do it here
                if (IsLivingHero(_character) && IsLivingNPC(_target) && _auxString=="wet"){
                    int amountToDeal = Functions.FuncRoundToInt((float)_target.GetAuraCharges("spark") * 0.5f);
                    _target.IndirectDamage(Enums.DamageType.Lightning, amountToDeal);
                }
            } 
            
            else if (_trait == "ulfvitrlifebloom")
            { //At end of turn, heal all heroes by wet * 0.70
                Plugin.Log.LogDebug("Lifebloom Start:");
                if (_character.HeroData!=null){
                    for (int i = 0; i < teamHero.Length; i++)
                    {
                        if (IsLivingHero(teamHero[i]))
                        {
                            int healAmount = Functions.FuncRoundToInt((float)teamHero[i].GetAuraCharges("wet") * 0.70f);
                            LogDebug("Lifebloom Heal Amount: " + healAmount);
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

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), "SetEvent")]
        // public static void SetEventPrefix(ref Character __instance, ref Enums.EventActivation theEvent, Character target = null)
        // {
        //     //if (theEvent == Enums.EventActivation.AuraCurseSet && __instance.IsHero && target != null && !target.IsHero && __instance.HaveTrait("ulfvitrconductor") && target.HasEffect("spark"))
        //     //if (theEvent == Enums.EventActivation.AuraCurseSet && !__instance.IsHero && target != null && target.IsHero && target.HaveTrait("ulfvitrconductor") && __instance.HasEffect("spark")){ // if NPC has wet applied to them, deal 50% of their sparks as indirect lightning damage
        //     //    __instance.IndirectDamage(Enums.DamageType.Lightning, Functions.FuncRoundToInt((float)__instance.GetAuraCharges("spark") * 0.5f));
        //     //}
        // }
    }
}
