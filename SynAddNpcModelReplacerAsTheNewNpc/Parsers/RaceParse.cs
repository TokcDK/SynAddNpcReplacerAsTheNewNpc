using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class RaceParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> RaceList = new();

        internal static void GetChangedSkinArmors(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (ArmorParse.ChangedArmorsList.Count == 0)
            {
                Console.WriteLine("No skins was changed..");
                return;
            }

            Console.WriteLine($"Process race records to use changed skins..");
            var changedArmorsList = ArmorParse.ChangedArmorsList;
            foreach (var context in state.LoadOrder.PriorityOrder.Race().WinningContextOverrides())
            {
                var getter = context.Record;

                var formkey = getter.Skin.FormKey;
                if (!changedArmorsList.ContainsKey(formkey)) continue;

                var adlist = changedArmorsList[formkey];
                foreach (var ad in adlist)
                {
                    // create copy of npc which to place as extra lnpc recors and relink worn armor to changed
                    var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                    changed.Skin.SetTo(ad.FormKey);
                    changed.EditorID = getter.EditorID + ad.Data!.ID;

                    var d = new TargetFormKeyData
                    {
                        FormKey = changed.FormKey,
                        Data = ad.Data,
                        Pair = ad.Pair,
                    };

                    if (!RaceList.ContainsKey(getter.FormKey))
                    {
                        RaceList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else RaceList[getter.FormKey].Add(d);
                }
            }
            Console.WriteLine($"Created {RaceList.Count} modified races");
        }
    }
}