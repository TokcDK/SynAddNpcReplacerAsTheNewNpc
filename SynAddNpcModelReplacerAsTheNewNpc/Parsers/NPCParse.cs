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
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> npcList = new();

        internal static void GetChangedNPC(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // search all npc where worn armor is equal found
            Console.WriteLine($"Process npc records to use new skins..");
            var npcList = new Dictionary<FormKey, List<TargetFormKeyData>>();
            foreach (var context in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                var getter = context.Record;

                //if (npcList.ContainsKey(getter.FormKey)) continue;

                var wArmrFormKey = GetWornArmorFlag(getter, state);
                if (!ArmrParse.aList.ContainsKey(wArmrFormKey)) continue;
                var adlist = ArmrParse.aList[wArmrFormKey];

                foreach (var ad in adlist)
                {
                    if (ad.Data!.NpcSkipUnique
                        && getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique)) continue;

                    if (ad.Data.NpcGender == WorldModelGender.FemaleOnly 
                        && !getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    {
                        continue;
                    }
                    else if (ad.Data.NpcGender == WorldModelGender.MaleOnly
                        && !getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female))
                    {
                        continue;
                    }

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

                    if (!npcList.ContainsKey(getter.FormKey))
                    {
                        npcList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else npcList[getter.FormKey].Add(d);
                }
            }

            // Note: after create npc with changed skin armors need to search
            // all npc referring to originals of npc changed copy of one was created and
            // replace template ref to original by LNPC list where will be changed and original record

            Console.WriteLine($"Created {npcList.Count} modified npcss");
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