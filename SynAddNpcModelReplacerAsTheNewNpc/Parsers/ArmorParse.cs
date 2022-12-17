using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class ArmrParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> aList = new();

        internal static void GetChangedSkinArmors(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //var data = Program.Settings.SearchData;

            // search all armors referring found aa
            Console.WriteLine($"Process skins to use new models..");
            var aList = new Dictionary<FormKey, List<TargetFormKeyData>>();
            foreach (var context in state.LoadOrder.PriorityOrder.Armor().WinningContextOverrides())
            {
                var getter = context.Record;

                if (!getter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)
                    && getter.BodyTemplate != null
                    && !getter.BodyTemplate.Flags.HasFlag(BodyTemplate.Flag.NonPlayable)) continue;
                if (getter.Armature.Count != 1) continue;
                if (aList.ContainsKey(getter.FormKey)) continue;

                var aafKey = getter.Armature[0].FormKey;
                if (!AAParse.aaList.ContainsKey(aafKey)) continue;

                var aadlist = AAParse.aaList[aafKey];

                foreach (var aad in aadlist)
                {
                    // create copy of found armors and relink aa there to changed aa
                    var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                    changed.Armature.Clear();
                    changed.Armature.Add(aad.FormKey);
                    changed.EditorID = getter.EditorID + aad.Data!.ID;

                    var d = new TargetFormKeyData
                    {
                        FormKey = changed.FormKey,
                        Data = aad.Data,
                        Pair = aad.Pair,
                    };

                    if (!aList.ContainsKey(getter.FormKey))
                    {
                        aList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else aList[getter.FormKey].Add(d);
                }
            }
            Console.WriteLine($"Created {aList.Count} modified a skins");
        }
    }
}