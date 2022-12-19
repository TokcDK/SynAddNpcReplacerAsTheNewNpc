using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class ArmorParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> ChangedArmorsList = new();

        internal static void GetChangedSkinArmors(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (AAParse.ChangedSkinAAList.Count == 0)
            {
                Console.WriteLine("No skin model paths was changed. Exit");
                return;
            }

            //var data = Program.Settings.SearchData;

            // search all armors referring found aa
            Console.WriteLine($"Process skins to use new models..");
            var changedSkinAAList = AAParse.ChangedSkinAAList;
            foreach (var context in state.LoadOrder.PriorityOrder.Armor().WinningContextOverrides())
            {
                var getter = context.Record;

                if (!getter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)
                    && getter.BodyTemplate != null
                    && !getter.BodyTemplate.Flags.HasFlag(BodyTemplate.Flag.NonPlayable)) continue;
                if (getter.Armature.Count != 1) continue;
                //if (ChangedArmorsList.ContainsKey(getter.FormKey)) continue;

                var aafKey = getter.Armature[0].FormKey;
                if (!changedSkinAAList.ContainsKey(aafKey)) continue;

                var aadlist = changedSkinAAList[aafKey];
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

                    aad.IsChanged = true;

                    if (!ChangedArmorsList.ContainsKey(getter.FormKey))
                    {
                        ChangedArmorsList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else ChangedArmorsList[getter.FormKey].Add(d);
                }
            }
            Console.WriteLine($"Created {ChangedArmorsList.Count} modified a skins");
        }
    }
}