namespace WorldSim;

using System.Collections.Generic;
using System.IO;

public static class Plot
{
    public static void Food(int amount) => Line("food", amount);

    public static void Clear(string file) => File.Delete(file);

    public static void Line(string file, int amount)
    {
        File.AppendAllLines(file, new List<string> {$"{amount}"});
    }
}