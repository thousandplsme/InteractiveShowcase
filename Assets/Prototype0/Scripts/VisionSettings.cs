using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace InteractiveShowcase
{
    [Serializable]
    public sealed class VisionSettings
    {
        public int cameraIndex;
        public int port;
        public bool mirrorX;
        public bool flipY;
        public float smoothTime;
        public float timeoutSeconds;

        public static VisionSettings Load()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Parse(File.ReadAllText(Path.Combine(root, "config/vision.json")));
        }

        public static VisionSettings Parse(string json)
        {
            // This flat configuration accepts only the documented primitive fields.
            // Validate types before JsonUtility, which otherwise tolerates missing values.
            const string number = @"-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?";
            string member = "\\\"(?<key>cameraIndex|port|mirrorX|flipY|smoothTime|timeoutSeconds)\\\"\\s*:\\s*(?<value>true|false|" + number + ")";
            if (json == null || !Regex.IsMatch(json, @"\A\s*\{\s*" + member + @"(?:\s*,\s*" + member + @")*\s*\}\s*\z"))
                throw new FormatException("Invalid vision settings JSON or unknown fields.");
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (Match match in Regex.Matches(json, member))
            {
                string key = match.Groups["key"].Value;
                string value = match.Groups["value"].Value;
                if (!seen.Add(key)) throw new FormatException("Duplicate settings field: " + key);
                if (key == "mirrorX" || key == "flipY")
                {
                    if (value != "true" && value != "false") throw new FormatException("Expected boolean: " + key);
                    continue;
                }
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double n)
                    || double.IsNaN(n) || double.IsInfinity(n)) throw new FormatException("Expected finite number: " + key);
                double low = key == "cameraIndex" ? 0 : key == "port" ? 1 : key == "smoothTime" ? .001 : .1;
                double high = key == "cameraIndex" ? 99 : key == "port" ? 65535 : key == "smoothTime" ? 5 : 10;
                if (n < low || n > high || ((key == "cameraIndex" || key == "port") && !Regex.IsMatch(value, @"\A\d+\z")))
                    throw new FormatException("Out of range or wrong type: " + key);
            }
            if (seen.Count != 6) throw new FormatException("All six settings fields are required.");
            return JsonUtility.FromJson<VisionSettings>(json);
        }
    }
}
