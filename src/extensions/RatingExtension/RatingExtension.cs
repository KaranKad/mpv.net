
// This extension writes a rating to the filename of rated videos when mpv.net shuts down.

// The input.conf setup:

// KP0 script-message rate-file 0     #menu: Extensions > Rating > 0stars
// KP1 script-message rate-file 1     #menu: Extensions > Rating > 1stars
// KP2 script-message rate-file 2     #menu: Extensions > Rating > 2stars
// KP3 script-message rate-file 3     #menu: Extensions > Rating > 3stars
// KP4 script-message rate-file 4     #menu: Extensions > Rating > 4stars
// KP5 script-message rate-file 5     #menu: Extensions > Rating > 5stars
// _   ignore                         #menu: Extensions > Rating > -
// _   script-message rate-file about #menu: Extensions > Rating > About

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using Microsoft.VisualBasic.FileIO;

using mpvnet;
using static mpvnet.Core;

namespace RatingExtension // the assembly name must end with 'Extension'
{
    [Export(typeof(IExtension))]
    public class RatingExtension : IExtension
    {
        // dictionory to store the filename and the rating
        Dictionary<string, int> Dic = new Dictionary<string, int>();

        string FileToDelete;
        DateTime DeleteTime;

        public RatingExtension() // plugin initialization
        {
            core.ClientMessage += ClientMessage; //handles keys defined in input.conf
            core.Shutdown += Shutdown; // handles MPV_EVENT_SHUTDOWN
        }

        // handles MPV_EVENT_SHUTDOWN
        void Shutdown()
        {
            foreach (var i in Dic)
            {
                string filepath = i.Key;
                int rating = i.Value;

                if (String.IsNullOrEmpty(filepath) || !File.Exists(filepath))
                    return;

                string basename = Path.GetFileNameWithoutExtension(filepath);

                for (int x = 0; x < 6; x++)
                    if (basename.Contains(" (" + x + "stars)"))
                        basename = basename.Replace(" (" + x + "stars)", "");

                basename += $" ({rating}stars)";

                string newPath = Path.Combine(Path.GetDirectoryName(filepath),
                    basename + Path.GetExtension(filepath));

                if (filepath.ToLower() != newPath.ToLower())
                    File.Move(filepath, newPath);

                File.SetLastWriteTime(newPath, DateTime.Now);
            }            
        }

        //handles keys defined in input.conf
        void ClientMessage(string[] args)
        {
            if (args[0] != "rate-file")
                return;

            if (int.TryParse(args[1], out int rating))
            {
                string path = core.get_property_string("path");

                if (!File.Exists(path))
                    return;

                if (rating == 0 || rating == 1)
                    Delete(rating);
                else
                {
                    Dic[path] = rating;
                    core.commandv("show-text", $"Rating: {rating}");
                }
            }
            else if (args[1] == "about")
                Msg.Show("Rating Extension", "This extension writes a rating to the filename of rated videos when mpv.net shuts down.\n\nThe input.conf defaults contain key bindings for this extension to set ratings.");
        }

        void Delete(int rating)
        {
            if (rating == 0)
            {
                FileToDelete = core.get_property_string("path");
                DeleteTime = DateTime.Now;
                core.commandv("show-text", "Press 1 to delete file", "5000");
            }
            else
            {
                TimeSpan ts = DateTime.Now - DeleteTime;
                string path = core.get_property_string("path");
                
                if (FileToDelete == path && ts.TotalSeconds < 5 && File.Exists(FileToDelete))
                {
                    core.command("playlist-remove current");
                    int pos = core.get_property_int("playlist-pos");

                    if (pos == -1)
                    {
                        int count = core.get_property_int("playlist-count");

                        if (count > 0)
                            core.set_property_int("playlist-pos", count - 1);
                    }

                    Thread.Sleep(2000);
                    FileSystem.DeleteFile(FileToDelete, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
        }
    }
}