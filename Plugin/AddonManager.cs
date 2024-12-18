using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine.Playables;

namespace UniversalAssetLoader.AddonLibrary
{
    static class AddonManager
    {
        static public List<Addon> addons = null;
        public static List<Addon> GetAllAddons(string directory = ".")
        {
            string[] addonsFileNames = Directory.GetFiles(directory, "config.txt", SearchOption.AllDirectories).ToArray();
            addons = new List<Addon>();
            foreach (string filename in addonsFileNames)
            {
                Addon addon = new Addon();
                try
                {
                    if ((addon = Addon.Deserialize(filename)) != null)
                    {
                        addons.Add(Addon.Deserialize(filename));
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log($"Incorrect json format in file: {filename}");
                    UnityEngine.Debug.Log(e.Message);
                }
            }

            return addons;
        }

        public static Addon GetByName(this Addon[] addons, string addonName)
        {
            return addons.First((x => x.name == addonName));
        }
    }

}
