using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using SynAddNpcModelReplacerAsTheNewNpc.Data;

namespace SynAddNpcModelReplacerAsTheNewNpc
{
    public class Program
    {
        static Lazy<Settings> _settings = null!;
        internal static Settings Settings { get => _settings.Value; }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("Settings", "settings.json", out _settings)
                .SetTypicalOpen(GameRelease.SkyrimLE, "SynAddNpcModelReplacerAsTheNewNpc.esp")
                .Run(args);
        }

        public class TargetFormKeyData
        {
            public FormKey FormKey;
            public NPCReplacerData? Data;
            public SearchReplacePair? Pair;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            General.ShowTargetsInfo();

            AAParse.GetChangedAAList(state);

            ArmrParse.GetChangedSkinArmors(state);

            NPCParse.GetChangedNPC(state);

            LNPCParse.AddChangedNPC(state);
        }
    }
}
