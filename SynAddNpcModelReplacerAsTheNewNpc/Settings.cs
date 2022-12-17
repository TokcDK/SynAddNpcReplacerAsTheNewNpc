using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using System.Collections.Generic;

namespace SynAddNpcModelReplacerAsTheNewNpc
{
    public enum WorldModelGender
    {
        Any,
        FemaleOnly,
        MaleOnly
    }

    [SynthesisObjectNameMember(nameof(EDIDSuffix))]
    public class NPCReplacerData
    {
        [SynthesisTooltip("Unique Editor ID suffix which will be added for each changed records EDID for this data")]
        public string? EDIDSuffix;
        [SynthesisTooltip("Search string of the model path. Subpath in the Meshes dir")]
        public string? SearchWorldModelPath;
        [SynthesisTooltip("Case insensitive search. Enabled by default")]
        public bool CaseInsensitiveSearch = true;
        [SynthesisTooltip("Subpath of the replacer to replace with")]
        public string? ReplaceWith;
        [SynthesisTooltip("Npc specific gender. For case when need to search npcs with specific gender.")]
        public WorldModelGender NpcGender = WorldModelGender.Any;
        [SynthesisTooltip("Url of the web page where the replacer can be downloaded")]
        public string Url = "";
        [SynthesisTooltip("Unique npcs will be ignored")]
        public bool NpcSkipUnique  = true;
    }

    public class Settings
    {
        [SynthesisTooltip("Mods list of changing npc appearance mods")]
        public List<NPCReplacerData> SearchData { get; } = new();
    }
}
