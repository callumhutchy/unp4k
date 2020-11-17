using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace p4kcomparer {
    class Program {
        static List<string> oldLines;
        static List<string> newLines;

        static bool processing = false;
        static string threadMessage = "";
        static int threadIndex = 0;
        static int threadMax = 0;
        static int oldFileCount = 0;
        static int modifiedCount = 0;
        static int sameCount = 0;
        static int newCount = 0;
        static void Main (string[] args) {
            oldLines = File.ReadAllLines (args[0]).ToList ();
            newLines = File.ReadAllLines (args[1]).ToList ();
            processing = true;
            new Thread (ProcessStuff).Start ();
            while (processing) {

                Console.Write ($"Processing {threadIndex}/{threadMax} | Old File Count {oldFileCount} | Modified Count {modifiedCount} | Same Count {sameCount} | New Count {newCount} \r");
            }
        }

        static void ProcessStuff () {
            List<string> newAndModified = newLines.Except (oldLines).ToList ();
            File.WriteAllLines ("samelines.csv", newLines.Except (newAndModified));
            List<string> oldNam = oldLines.Except (newLines.Except (newAndModified).ToList ()).ToList ();

            sameCount = newLines.Count - newAndModified.Count;

            List<FileData> newFD = new List<FileData> ();
            newAndModified.ForEach (x => {
                string[] temp = Regex.Split (x, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                newFD.Add (new FileData () {
                    path = temp[0],
                        lastModified = DateTime.Parse (temp[1]),
                        bytes = long.Parse (temp[2])
                });
            });

            List<FileData> oldFD = new List<FileData> ();
            oldNam.ForEach (x => {
                string[] temp = Regex.Split (x, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                oldFD.Add (new FileData () {
                    path = temp[0],
                        lastModified = DateTime.Parse (temp[1]),
                        bytes = long.Parse (temp[2])
                });
            });

            List<FileData> newFiles = new List<FileData> ();
            List<FileData> modifiedFiles = new List<FileData> ();
            List<FileData> removedFiles = new List<FileData> ();
            oldFD.Reverse ();
            threadMax = newFD.Count ();

            for (int i = 0; i < newFD.Count (); i++) {
                threadIndex = i + 1;
                bool found = false;
                FileData nl = newFD[i];
                for (int j = oldFD.Count () - 1; j >= 0; j--) {
                    FileData ol = oldFD[j];
                    if (nl.path.Equals (ol.path)) {
                        found = true;
                        modifiedFiles.Add (nl);
                        modifiedCount++;
                        oldFD.RemoveAt (j);
                        break;
                    }
                }
                oldFileCount = oldFD.Count ();
                if (!found) {

                    newFiles.Add (nl);
                    newCount++;
                }
            }
            processing = false;
            removedFiles = oldFD;
            foreach (FileData nl in newFiles) {
                File.AppendAllText ("newfiles.csv", nl.ToString ());
            }
            foreach (FileData ml in modifiedFiles) {
                File.AppendAllText ("modifiedfiles.csv", ml.ToString ());
            }
            Console.WriteLine($"Removed files {removedFiles.Count}");
            foreach (FileData rf in removedFiles) {
                File.AppendAllText ("removedfiles.csv", rf.ToString ());
            }
            
        }
    }

    class FileData {
        public string path { get; set; }
        public DateTime lastModified { get; set; }
        public long bytes { get; set; }

        public override string ToString () {
            return path + "," + lastModified + "," + bytes + "\n";
        }
    }
}