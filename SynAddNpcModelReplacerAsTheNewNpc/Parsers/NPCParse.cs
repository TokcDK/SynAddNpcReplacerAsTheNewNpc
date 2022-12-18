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
            // search all npc where worn armor is equal found
            Console.WriteLine($"Process npc records to use changed skins..");
            var changedArmorsList = ArmorParse.ChangedArmorsList;
            foreach (var context in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                var getter = context.Record;

                //if (npcList.ContainsKey(getter.FormKey)) continue;

                var wArmrFormKey = GetWornArmorFlag(getter, state);
                if (!changedArmorsList.ContainsKey(wArmrFormKey)) continue;

                var adlist = changedArmorsList[wArmrFormKey];
                foreach (var ad in adlist)
                {
                    if (!IsValidFlags(ad, getter)) continue;

                    // create copy of npc which to place as extra lnpc recors and relink worn armor to changed
                    var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                    changed.WornArmor.SetTo(ad.FormKey);
                    changed.EditorID = getter.EditorID + ad.Data!.ID;

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

        private static bool IsValidFlags(TargetFormKeyData ad, INpcGetter getter)
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

        private static FormKey GetWornArmorFlag(INpcGetter getter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!getter.WornArmor.IsNull)
            {
                return getter.WornArmor.FormKey;
            }

            if (!getter.Template.IsNull)
            {
                return GetTemplateFormKey(getter.Template, state);
            }

            return FormKey.Null;
        }

        private static FormKey GetTemplateFormKey(IFormLinkNullableGetter<INpcSpawnGetter> template, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!template.TryResolve(state.LinkCache, out var npc)) return FormKey.Null;

            if (npc is not INpcGetter n) return FormKey.Null;

            if (!n.WornArmor.IsNull)
            {
                return n.WornArmor.FormKey;
            }

            if (!n.Template.IsNull)
            {
                return GetTemplateFormKey(n.Template, state);
            }

            return FormKey.Null;
        }
    }
}