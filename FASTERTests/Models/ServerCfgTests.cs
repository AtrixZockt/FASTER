using NUnit.Framework;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FASTER.Models.Tests
{
    [TestFixture()]
    public class ServerCfgTests
    {
        ServerCfg _cfg;

        [SetUp]
        public void SetUp()
        { _cfg = new ServerCfg(); }

        private static string AdvancedOptionsBlock(string output)
        {
            var match = Regex.Match(output, @"class AdvancedOptions\r\n\{(.*?)\r\n\};", RegexOptions.Singleline);
            Assert.That(match.Success, Is.True, "class AdvancedOptions block not found");
            return match.Groups[1].Value;
        }

        #region ignoreMissionLoadErrors

        [Test()]
        public void IgnoreMissionLoadErrorsIsWrittenAsOneInsideAdvancedOptions()
        {
            _cfg.IgnoreMissionLoadErrors = true;

            Assert.That(AdvancedOptionsBlock(_cfg.ProcessFile()), Does.Contain("\tignoreMissionLoadErrors = 1;"));
        }

        [Test()]
        public void IgnoreMissionLoadErrorsDefaultsToZero()
        {
            Assert.That(AdvancedOptionsBlock(_cfg.ProcessFile()), Does.Contain("\tignoreMissionLoadErrors = 0;"));
        }

        #endregion

        #region zeusCompositionScriptLevel

        [Test()]
        public void ZeusCompositionScriptLevelIsWrittenAfterAdvancedOptionsWhenChecked()
        {
            _cfg.AllowZeusCompositionScripts = true;
            var output = _cfg.ProcessFile();

            Assert.That(output, Does.Contain("};\r\nzeusCompositionScriptLevel = 2;"));
            Assert.That(AdvancedOptionsBlock(output), Does.Not.Contain("zeusCompositionScriptLevel"));
        }

        [Test()]
        public void ZeusCompositionScriptLevelOmittedByDefault()
        {
            Assert.That(_cfg.AllowZeusCompositionScripts, Is.False);
            Assert.That(_cfg.ProcessFile(), Does.Not.Contain("zeusCompositionScriptLevel"));
        }

        [Test()]
        public void TogglingZeusCompositionScriptsRegeneratesContent()
        {
            // The view binds to ServerCfg, which subscribes to PropertyChanged
            _cfg.PropertyChanged += (_, _) => { };

            _cfg.AllowZeusCompositionScripts = true;
            Assert.That(_cfg.ServerCfgContent, Does.Contain("zeusCompositionScriptLevel = 2;"));

            _cfg.AllowZeusCompositionScripts = false;
            Assert.That(_cfg.ServerCfgContent, Does.Not.Contain("zeusCompositionScriptLevel"));
        }

        #endregion

        #region Profile storage

        [Test()]
        public void NewLayoutIsNotTreatedAsLegacy()
        {
            _cfg.IgnoreMissionLoadErrors     = true;
            _cfg.AllowZeusCompositionScripts = true;
            _cfg.ServerCfgContent            = _cfg.ProcessFile();

            Assert.That(_cfg.MigrateLegacyLayout(), Is.False);
        }

        [Test()]
        public void AdvancedOptionsSurviveXmlRoundTrip()
        {
            _cfg.IgnoreMissionLoadErrors     = true;
            _cfg.AllowZeusCompositionScripts = true;

            var serializer = new XmlSerializer(typeof(ServerCfg));
            using var writer = new StringWriter();
            serializer.Serialize(writer, _cfg);
            using var reader = new StringReader(writer.ToString());
            var loaded = (ServerCfg)serializer.Deserialize(reader);

            Assert.That(loaded.IgnoreMissionLoadErrors, Is.True);
            Assert.That(loaded.AllowZeusCompositionScripts, Is.True);
        }

        [Test()]
        public void ProfilesSavedBeforeZeusOptionLoadWithItOff()
        {
            var serializer = new XmlSerializer(typeof(ServerCfg));
            using var writer = new StringWriter();
            serializer.Serialize(writer, _cfg);
            var oldXml = Regex.Replace(writer.ToString(), @"\s*<AllowZeusCompositionScripts>.*?</AllowZeusCompositionScripts>", "");
            Assert.That(oldXml, Does.Not.Contain("AllowZeusCompositionScripts"));

            using var reader = new StringReader(oldXml);
            var loaded = (ServerCfg)serializer.Deserialize(reader);

            Assert.That(loaded.AllowZeusCompositionScripts, Is.False);
            Assert.That(loaded.ProcessFile(), Does.Not.Contain("zeusCompositionScriptLevel"));
        }

        #endregion
    }
}
