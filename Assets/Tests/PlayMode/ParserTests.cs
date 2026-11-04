using NUnit.Framework;

namespace AdventureGame.Tests
{
    public class ParserTests
    {
        [TestCase("{\"response\":\"Geç.\",\"action\":\"open_door\"}", NpcAction.OpenDoor, "Geç.")]
        [TestCase("{\"response\":\"Hayır.\",\"action\":\"none\"}", NpcAction.None, "Hayır.")]
        [TestCase("```json\n{\"response\":\"Kod bloğu\",\"action\":\"none\"}\n```", NpcAction.None, "Kod bloğu")]
        [TestCase("Tabii! İşte: {\"response\":\"Ön metin\",\"action\":\"OPEN-DOOR\"} umarım yardımcı olur", NpcAction.OpenDoor, "Ön metin")]
        [TestCase("{\"response\":\"Patlat\",\"action\":\"explode_door\"}", NpcAction.None, "Patlat")]
        [TestCase("{\"response\":\"Eksik aksiyon\"}", NpcAction.None, "Eksik aksiyon")]
        [TestCase("{\"response\":\"\",\"action\":\"open_door\"}", NpcAction.OpenDoor, "...")]
        public void GecerliVeyaKurtarilabilirJson_DogruAyristirilir(string raw, NpcAction expectedAction, string expectedDialogue)
        {
            Assert.IsTrue(NpcDecisionParser.TryParse(raw, out NpcDecision decision, out string error), error);
            Assert.AreEqual(expectedAction, decision.Action);
            Assert.AreEqual(expectedDialogue, decision.Dialogue);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("JSON yok, sadece düz metin")]
        [TestCase("{\"response\": \"Bozuk json, \"action\": ")]
        [TestCase("{\"response\": {\"ic\": 1}, \"action\": \"none\"}")]
        [TestCase("{\"response\":\"\",\"action\":\"none\"}")]
        [TestCase("null")]
        public void BozukJson_IstisnaFirlatmaz_FalseDoner(string raw)
        {
            bool ok = true;
            Assert.DoesNotThrow(() => ok = NpcDecisionParser.TryParse(raw, out _, out _));
            Assert.IsFalse(ok);
        }
    }
}
