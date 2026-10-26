using System;
using NUnit.Framework;

namespace InteractiveShowcase.Tests
{
    public sealed class SettingsTests
    {
        private const string Good = "{\"cameraIndex\":0,\"port\":5005,\"mirrorX\":true,\"flipY\":true,\"smoothTime\":0.08,\"timeoutSeconds\":0.5}";

        [Test]
        public void LoadsSharedDefaultsAndOverrides()
        {
            var defaults = VisionSettings.Load();
            Assert.AreEqual(5005, defaults.port);
            Assert.IsTrue(defaults.mirrorX);
            var custom = VisionSettings.Parse(Good.Replace("5005", "5006").Replace("\"mirrorX\":true", "\"mirrorX\":false"));
            Assert.AreEqual(5006, custom.port);
            Assert.IsFalse(custom.mirrorX);
            Assert.AreEqual(.08f, custom.smoothTime);
        }

        [TestCase("{}")]
        [TestCase("[]")]
        [TestCase("garbage")]
        public void RejectsIncompleteSettings(string json)
        {
            Assert.Throws<FormatException>(() => VisionSettings.Parse(json));
        }

        [TestCase("5005", "true")]
        [TestCase("5005", "0")]
        [TestCase("5005", "5005.0")]
        [TestCase("5005", "65536")]
        [TestCase("0.08", "NaN")]
        [TestCase("0.08", "0")]
        [TestCase("0.5", "1e99")]
        [TestCase("\"mirrorX\":true", "\"mirrorX\":0")]
        [TestCase("\"cameraIndex\":0", "\"cameraIndex\":-1")]
        [TestCase("\"flipY\":true", "\"host\":true")]
        [TestCase("\"port\":5005", "\"port\":5005,\"port\":5006")]
        public void RejectsInvalidTypesRangesAndDuplicateFields(string from, string to)
        {
            Assert.Throws<FormatException>(() => VisionSettings.Parse(Good.Replace(from, to)));
        }
    }
}
