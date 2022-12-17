namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class General
    {
        internal static void ShowTargetsInfo()
        {
            Console.WriteLine($"\nUsed target replacers:");
            Console.WriteLine($"---");
            foreach (var target in Program.Settings.SearchData)
            {
                if (!target.Enabled) continue;

                var url = !string.IsNullOrWhiteSpace(target.Url) ? "\n Url: " + target.Url : "";
                var note = !string.IsNullOrWhiteSpace(target.Note) ? "\n Note: " + target.Note : "";
                Console.WriteLine($" ID: {target.ID}{url}{note}");
                Console.WriteLine($"-");
            }
            Console.WriteLine($"---\n");
        }
    }
}