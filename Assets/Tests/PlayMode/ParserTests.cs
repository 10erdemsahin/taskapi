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

        [TestCase("{\"response\":\"Doğru\",\"action\":\"none\",\"riddle_solved\":true,\"is_answer_attempt\":true}", true, true)]
        [TestCase("{\"response\":\"Yanlış\",\"action\":\"none\",\"riddle_solved\":false,\"is_answer_attempt\":true}", false, true)]
        [TestCase("{\"response\":\"Alan yok\",\"action\":\"none\"}", false, false)]
        [TestCase("{\"response\":\"Metin bool\",\"action\":\"none\",\"riddle_solved\":\"true\",\"is_answer_attempt\":\"false\"}", true, false)]
        [TestCase("{\"response\":\"Sayı\",\"action\":\"none\",\"riddle_solved\":1,\"is_answer_attempt\":0}", false, false)]
        [TestCase("{\"response\":\"Nesne\",\"action\":\"none\",\"riddle_solved\":{\"x\":1},\"is_answer_attempt\":null}", false, false)]
        public void BilmeceAlanlari_TureGoreGuvenleOkunur(string raw, bool expectedSolved, bool expectedAttempt)
        {
            Assert.IsTrue(NpcDecisionParser.TryParse(raw, out NpcDecision decision, out string error), error);
            Assert.AreEqual(expectedSolved, decision.RiddleSolved, "riddle_solved");
            Assert.AreEqual(expectedAttempt, decision.IsAnswerAttempt, "is_answer_attempt");
        }

        [TestCase("{\"response\":\"a\",\"action\":\"none\",\"tone\":\"polite\"}", PlayerTone.Polite)]
        [TestCase("{\"response\":\"a\",\"action\":\"none\",\"tone\":\"RUDE\"}", PlayerTone.Rude)]
        [TestCase("{\"response\":\"a\",\"action\":\"none\",\"tone\":\"neutral\"}", PlayerTone.Neutral)]
        [TestCase("{\"response\":\"a\",\"action\":\"none\"}", PlayerTone.Neutral)]
        [TestCase("{\"response\":\"a\",\"action\":\"none\",\"tone\":\"furious\"}", PlayerTone.Neutral)]
        [TestCase("{\"response\":\"a\",\"action\":\"none\",\"tone\":5}", PlayerTone.Neutral)]
        public void TonAlani_BeyazListeyeGoreOkunur_BilinmeyenNotr(string raw, PlayerTone expected)
        {
            Assert.IsTrue(NpcDecisionParser.TryParse(raw, out NpcDecision decision, out string error), error);
            Assert.AreEqual(expected, decision.Tone);
        }

        [Test]
        public void Persona_GuvenDegisimi_KabaDusurur_DigerleriArtirir()
        {
            var persona = UnityEngine.ScriptableObject.CreateInstance<NpcPersona>();
            Assert.Greater(persona.GetTrustDelta(PlayerTone.Polite), persona.GetTrustDelta(PlayerTone.Neutral));
            Assert.Greater(persona.GetTrustDelta(PlayerTone.Neutral), 0);
            Assert.Less(persona.GetTrustDelta(PlayerTone.Rude), 0);
            Assert.Less(persona.InitialTrust, persona.RequiredTrust, "Başlangıçta güven kapı için yetmemeli");
            UnityEngine.Object.DestroyImmediate(persona);
        }

        [TestCase("Bence cevap SESSİZLİK!", 0, true)]
        [TestCase("sükût", 0, true)]
        [TestCase("sukut olabilir mi", 0, true)]
        [TestCase("ayna", 0, false)]
        [TestCase("Ayak İzi", 1, true)]
        [TestCase("ayakizleri", 1, true)]
        [TestCase("sessizlik", 1, false)]
        public void Persona_CevapKontrolu_TurkceKarakterVeBuyukHarfeDuyarsiz(string message, int riddleIndex, bool expected)
        {
            var persona = UnityEngine.ScriptableObject.CreateInstance<NpcPersona>();
            Riddle riddle = persona.GetRiddle(riddleIndex);
            Assert.NotNull(riddle);
            Assert.AreEqual(expected, riddle.IsMentionedIn(message));
            UnityEngine.Object.DestroyImmediate(persona);
        }
    }
}
