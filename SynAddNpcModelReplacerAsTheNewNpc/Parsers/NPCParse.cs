using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class NPCParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> NPCList = new();

        internal static void GetChangedNPC(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (ArmorParse.ChangedArmorsList.Count == 0)
            {
                Console.WriteLine("No skins was changed..");
                return;
            }

            //iterate lvln records and check all npc in them for changed race record, then make changed + add worn armor and remove use inventory flag
            //same as above but change only npc not using use inventory and use traits flags

            // search all npc where worn armor is equal found
            Console.WriteLine($"Process npc records to use changed skins..");
            var changedArmorsList = ArmorParse.ChangedArmorsList;
            foreach (var context in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                var getter = context.Record;

                //if (npcList.ContainsKey(getter.FormKey)) continue;

                var wArmrFormKey = GetWornArmorFlag(getter, state);
                if (!changedArmorsList.ContainsKey(wArmrFormKey)) continue;

                FormKey raceFKey = FormKey.Null;
                if (!getter.Race.IsNull)
                {
                    if(!RaceParse.RaceList.ContainsKey(getter.Race.FormKey)) continue;

                    raceFKey = RaceParse.RaceList[getter.Race.FormKey][0].FormKey;
                }

                var adlist = changedArmorsList[wArmrFormKey];
                foreach (var ad in adlist)
                {
                    if (!IsValidFlags(ad, getter)) continue;

                    // create copy of npc which to place as extra lnpc recors and relink worn armor to changed
                    var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                    changed.WornArmor.SetTo(ad.FormKey);
                    changed.EditorID = getter.EditorID + ad.Data!.ID;

                    if(raceFKey != FormKey.Null)
                    {
                        changed.Race.SetTo(raceFKey);
                    }

                    var d = new TargetFormKeyData
                    {
                        FormKey = changed.FormKey,
                        Data = ad.Data,
                        Pair = ad.Pair,
                    };

                    if (!NPCList.ContainsKey(getter.FormKey))
                    {
                        NPCList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else NPCList[getter.FormKey].Add(d);
                }
            }

            Console.WriteLine($"Search template refs for original of changed npcs..");
            foreach (var getter in state.LoadOrder.PriorityOrder
                .Npc()
                .WinningOverrides()
                .Where(g => g.FormKey.ModKey != state.PatchMod.ModKey))
            {
                if (getter.Template.IsNull) continue;

                var templateFormKey = getter.Template.FormKey;

                // search template ref to changed original
                if (!NPCList.ContainsKey(templateFormKey)) continue;

                if (!getter.Template.TryResolve<INpcGetter>(state.LinkCache, out var npcGetter)) continue;

                var npcdatas = NPCList[templateFormKey];

                var lnpc = state.PatchMod.LeveledNpcs.AddNew("LNpc" + npcGetter.EditorID + "Sublist");
                lnpc.Entries = new ExtendedList<LeveledNpcEntry>
                {
                    LNPCParse.GetLeveledNpcEntrie(templateFormKey)
                };

                // add all changed npcs
                foreach (var npcdata in npcdatas)
                {
                    if (!IsValidFlags(npcdata, getter)) continue;

                    lnpc.Entries.Add(LNPCParse.GetLeveledNpcEntrie(npcdata.FormKey));
                }

                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);

                // relink template to merged lnpc
                npc.Template.SetTo(lnpc.FormKey);

                //var d = new TargetFormKeyData
                //{
                //    FormKey = lnpc.FormKey,
                //    Data = npcdatas[0].Data,
                //    Pair = null // null, it will not be use later anyway
                //};

                //npcdatas.Add(d);
            }

            Console.WriteLine($"Created {NPCList.Count} modified npcss");
        }

        internal static Dictionary<FormKey, FormKey> NPCCache = new();
        internal static void GetChangedNPC2(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (ArmorParse.ChangedArmorsList.Count == 0)
            {
                Console.WriteLine("No skins was changed..");
                return;
            }

            if (RaceParse.RaceList.Count == 0)
            {
                Console.WriteLine("No races was changed..");
                return;
            }

            int changedNpcTemplates = 0;
            var patchMod = state.PatchMod;
            var cache = state.LinkCache;
            var patchModKey = patchMod.ModKey;
            var racelist = RaceParse.RaceList;
            var skinarmorlist = ArmorParse.ChangedArmorsList;
            Console.WriteLine($"Search template refs for original of changed npcs..");
            foreach (var getter in state.LoadOrder.PriorityOrder
                .Npc()
                .WinningOverrides()
                .Where(g => g.FormKey.ModKey != patchModKey))
            {
                if (getter.Template.IsNull) continue;

                if (!getter.Template.TryResolve<INpcGetter>(cache, out var npcGetter)) continue;

                if (npcGetter.Configuration.TemplateFlags
                    .HasFlag(NpcConfiguration.TemplateFlag.Traits)) continue;

                var racefkey = npcGetter.Race.FormKey;
                if (!racelist.ContainsKey(racefkey)) continue;
                var wornarmorfkey = npcGetter.WornArmor.FormKey;
                if (!skinarmorlist.ContainsKey(wornarmorfkey)) continue;

                var lnpc = state.PatchMod.LeveledNpcs.AddNew("LNpc" + npcGetter.EditorID + "Sublist");
                lnpc.Entries = new ExtendedList<LeveledNpcEntry>
                {
                    LNPCParse.GetLeveledNpcEntrie(npcGetter.FormKey)
                };

                var rdlist = racelist[racefkey];
                var walist = skinarmorlist[wornarmorfkey];
                foreach (var rd in rdlist)
                {
                    if (!IsValidFlags(rd, npcGetter)) continue;

                    foreach (var wd in walist)
                    {
                        if (!IsValidFlags(wd, npcGetter)) continue;

                        if (NPCCache.ContainsKey(npcGetter.FormKey))
                        {
                            lnpc.Entries.Add(LNPCParse.GetLeveledNpcEntrie(NPCCache[npcGetter.FormKey], 1, 1));
                        }
                        else
                        {
                            var newnpc = patchMod.Npcs.DuplicateInAsNewRecord(npcGetter);
                            newnpc.EditorID = npcGetter.EditorID + wd.Data!.ID;

                            newnpc.Race.SetTo(rd.FormKey);
                            newnpc.WornArmor.SetTo(wd.FormKey);
                            lnpc.Entries.Add(LNPCParse.GetLeveledNpcEntrie(newnpc.FormKey, 1, 1));

                            // remember changed npcs to not create them many times
                            NPCCache.Add(npcGetter.FormKey, newnpc.FormKey);
                        }
                    }
                }

                if (lnpc.Entries.Count == 1)
                {
                    // remove litem if no extra items was added
                    patchMod.LeveledNpcs.Remove(lnpc.FormKey);
                    continue;
                }

                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(getter);

                // relink template to merged lnpc
                npc.Template.SetTo(lnpc.FormKey);

                changedNpcTemplates++;
            }

            Console.WriteLine($"Changed {changedNpcTemplates} npc templates");
        }

        internal static FormKey GetRaceData(INpcGetter getter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!getter.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits))
            {
                return getter.FormKey;
            }
            if(getter.Template.IsNull) return getter.FormKey;

            return GetRaceTemplateFormKey(getter.Template, state);
        }

        internal static bool IsValidFlags(TargetFormKeyData ad, INpcGetter getter)
        {
            if (ad.Data!.NpcSkipUnique
                        && getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique)) return false;

            if (ad.Data.NpcGender == WorldModelGender.FemaleOnly
                && !getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
            {
                return false;
            }
            else if (ad.Data.NpcGender == WorldModelGender.MaleOnly
                && !getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
            {
                return false;
            }

            return true;
        }

        internal static FormKey GetWornArmorFlag(INpcGetter getter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!getter.WornArmor.IsNull)
            {
                return getter.WornArmor.FormKey;
            }

            if (!getter.Template.IsNull)
            {
                return GetWornArmorTemplateFormKey(getter.Template, state);
            }

            return FormKey.Null;
        }

        internal static FormKey GetRaceTemplateFormKey(IFormLinkNullableGetter<INpcSpawnGetter> template, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!template.TryResolve(state.LinkCache, out var npc)) return FormKey.Null;

            if (npc is not INpcGetter n) return FormKey.Null;

            if (!n.Race.IsNull 
                && !n.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits))
            {
                return n.Race.FormKey;
            }

            if (!n.Template.IsNull)
            {
                return GetRaceTemplateFormKey(n.Template, state);
            }

            return FormKey.Null;
        }

        internal static FormKey GetWornArmorTemplateFormKey(IFormLinkNullableGetter<INpcSpawnGetter> template, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!template.TryResolve(state.LinkCache, out var npc)) return FormKey.Null;

            if (npc is not INpcGetter n) return FormKey.Null;

            if (!n.WornArmor.IsNull)
            {
                return n.WornArmor.FormKey;
            }

            if (!n.Template.IsNull)
            {
                return GetWornArmorTemplateFormKey(n.Template, state);
            }

            return FormKey.Null;
        }
    }
}