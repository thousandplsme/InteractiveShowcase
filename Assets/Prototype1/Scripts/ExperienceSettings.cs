using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace InteractiveShowcase.Prototype1
{
    [Serializable]
    public sealed class ExperienceSettings
    {
        public float detectionHoldSeconds = 1;
        public float greetingSeconds = 2;
        public float exitHoldSeconds = 1;

        public static ExperienceSettings Load(string path = null) => Parse(File.ReadAllText(path ??
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "config/experience.json")));

        public static ExperienceSettings Parse(string json)
        {
            const string number = @"-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?";
            string member = "\"(?<key>detectionHoldSeconds|greetingSeconds|exitHoldSeconds)\"\\s*:\\s*(?<value>" + number + ")";
            if (json == null || !Regex.IsMatch(json, @"\A\s*\{\s*" + member + @"(?:\s*,\s*" + member + @")*\s*\}\s*\z"))
                throw new FormatException("Invalid experience settings JSON.");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (Match match in Regex.Matches(json, member))
            {
                if (!seen.Add(match.Groups["key"].Value)) throw new FormatException("Duplicate experience field.");
                if (!double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double n)
                    || double.IsNaN(n) || double.IsInfinity(n) || n < .1 || n > 10)
                    throw new FormatException("Experience durations must be finite and 0.1..10 seconds.");
            }
            if (seen.Count != 3) throw new FormatException("Three experience durations are required.");
            return JsonUtility.FromJson<ExperienceSettings>(json);
        }
    }
}
