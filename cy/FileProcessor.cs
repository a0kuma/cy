using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cy
{
    internal class FileProcessor
    {
        public static void ProcessFile(string inputFile, string outputFile)
        {
            using (var infile = new StreamReader(inputFile))
            using (var outfile = new StreamWriter(outputFile))
            {
                // Write the header
                outfile.WriteLine("id,home_team_win");

                string line;
                int index = 0;

                while ((line = infile.ReadLine()) != null)
                {
                    line = line.Trim();
                    string homeTeamWin = line == "1" ? "TRUE" : "FALSE";
                    outfile.WriteLine($"{index},{homeTeamWin}");
                    index++;
                }
            }
        }
    }
}
