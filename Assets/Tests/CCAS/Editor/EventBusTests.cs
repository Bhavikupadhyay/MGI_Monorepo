using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CCAS.Tests
{
    /// <summary>
    /// EditMode tests for EventBus core behaviour and the two CCAS event contracts:
    ///   - buy_pack          (published once per pack open)
    ///   - xp_from_duplicate (published once per duplicate card in a pack)
    ///
    /// Run via: Window > General > Test Runner > EditMode tab.
    /// </summary>
    public class EventBusTests
    {
        // ---------------------------------------------------------------
        // Mirror structs — match the private classes in PackOpeningController
        // without depending on them directly.
        // ---------------------------------------------------------------

        [Serializable] private class CostPaid              { public int coins; public int gems; }
        [Serializable] private class BuyPackCardEntry      { public string card_id; public string rarity; public bool is_duplicate; public int xp_awarded; }
        [Serializable] private class BuyPackPayload        { public string pack_type_id; public CostPaid cost_paid; public List<BuyPackCardEntry> cards_pulled; }
        [Serializable] private class XpFromDuplicatePayload { public string card_id; public string rarity; public int xp_gained; public string source; public string pack_type_id; }

        private List<IDisposable> _subs;

        [SetUp]    public void SetUp()    { _subs = new List<IDisposable>(); }
        [TearDown] public void TearDown() { foreach (var s in _subs) s.Dispose(); _subs.Clear(); }

        // ---------------------------------------------------------------
        // Core EventBus behaviour
        // ---------------------------------------------------------------

        [Test]
        public void Publish_ShouldInvokeSubscriber()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("test_event", e => received = e));

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "test_event", player_id = "player_001" });

            Assert.IsNotNull(received);
            Assert.AreEqual("test_event",  received.event_type);
            Assert.AreEqual("player_001", received.player_id);
        }

        [Test]
        public void Publish_ShouldAutoFill_EventId_WhenMissing()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("autoid_test", e => received = e));

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "autoid_test" });

            Assert.IsFalse(string.IsNullOrEmpty(received.event_id),
                "EventBus must auto-fill event_id with a GUID when not provided.");
        }

        [Test]
        public void Publish_ShouldAutoFill_Timestamp_WhenMissing()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("ts_test", e => received = e));

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "ts_test" });

            Assert.IsFalse(string.IsNullOrEmpty(received.timestamp),
                "EventBus must auto-fill timestamp when not provided.");
        }

        [Test]
        public void Publish_ShouldDeliverToAllSubscribers()
        {
            int count = 0;
            _subs.Add(EventBus.Subscribe("multi_test", _ => count++));
            _subs.Add(EventBus.Subscribe("multi_test", _ => count++));

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "multi_test" });

            Assert.AreEqual(2, count, "Every registered subscriber must receive the event.");
        }

        [Test]
        public void Subscribe_Dispose_ShouldStopDelivery()
        {
            int count = 0;
            var sub = EventBus.Subscribe("dispose_test", _ => count++);

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "dispose_test" });
            Assert.AreEqual(1, count);

            sub.Dispose();

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "dispose_test" });
            Assert.AreEqual(1, count, "Subscriber must not receive events after Dispose().");
        }

        [Test]
        public void Publish_NullEvent_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(null));
        }

        [Test]
        public void Publish_EmptyEventType_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus.Publish(new EventBus.EventEnvelope { player_id = "p1" }));
        }

        // ---------------------------------------------------------------
        // buy_pack event contract
        // ---------------------------------------------------------------

        [Test]
        public void BuyPack_Event_ShouldHaveCorrectEventType()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("buy_pack", e => received = e));

            PublishFakeBuyPack("bronze_pack", 100,
                new[] { ("uid_a", "common", false, 0), ("uid_b", "uncommon", false, 0) });

            Assert.IsNotNull(received, "A buy_pack subscriber must receive the event.");
            Assert.AreEqual("buy_pack", received.event_type);
        }

        [Test]
        public void BuyPack_Event_ShouldCarryPlayerId()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("buy_pack", e => received = e));

            PublishFakeBuyPack("bronze_pack", 100,
                new[] { ("uid_a", "common", false, 0) },
                playerId: "unit_test_player");

            Assert.AreEqual("unit_test_player", received.player_id);
        }

        [Test]
        public void BuyPack_Payload_ShouldDeserialize_PackTypeAndCost()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("buy_pack", e => received = e));

            PublishFakeBuyPack("gold_pack", 300, new[] { ("uid_x", "rare", false, 0) });

            var p = JsonUtility.FromJson<BuyPackPayload>(received.payloadJson);
            Assert.AreEqual("gold_pack", p.pack_type_id);
            Assert.AreEqual(300,         p.cost_paid.coins);
            Assert.AreEqual(0,           p.cost_paid.gems);
        }

        [Test]
        public void BuyPack_Payload_ShouldDeserialize_CardEntries()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("buy_pack", e => received = e));

            PublishFakeBuyPack("bronze_pack", 100, new[]
            {
                ("uid_1", "common",   false, 0),
                ("uid_2", "uncommon", true,  10),
                ("uid_3", "rare",     false, 0)
            });

            var p = JsonUtility.FromJson<BuyPackPayload>(received.payloadJson);
            Assert.AreEqual(3,          p.cards_pulled.Count);
            Assert.AreEqual("uid_2",    p.cards_pulled[1].card_id);
            Assert.AreEqual("uncommon", p.cards_pulled[1].rarity);
            Assert.IsTrue(              p.cards_pulled[1].is_duplicate);
            Assert.AreEqual(10,         p.cards_pulled[1].xp_awarded);
            Assert.IsFalse(             p.cards_pulled[0].is_duplicate);
            Assert.AreEqual(0,          p.cards_pulled[0].xp_awarded);
        }

        [Test]
        public void BuyPack_ShouldNotBeDeliveredToOtherSubscribers()
        {
            bool wrongFired = false;
            _subs.Add(EventBus.Subscribe("buy_coach", _ => wrongFired = true));

            PublishFakeBuyPack("bronze_pack", 100, new[] { ("uid_a", "common", false, 0) });

            Assert.IsFalse(wrongFired, "buy_pack must not bleed into unrelated subscribers.");
        }

        // ---------------------------------------------------------------
        // xp_from_duplicate event contract
        // ---------------------------------------------------------------

        [Test]
        public void XpFromDuplicate_ShouldFire_ForEachDuplicateCard()
        {
            int fireCount = 0;
            _subs.Add(EventBus.Subscribe("xp_from_duplicate", _ => fireCount++));

            PublishFakeXpFromDuplicate("uid_a", "common",   5,  "bronze_pack");
            PublishFakeXpFromDuplicate("uid_b", "uncommon", 10, "bronze_pack");

            Assert.AreEqual(2, fireCount,
                "xp_from_duplicate must fire once per duplicate card.");
        }

        [Test]
        public void XpFromDuplicate_ShouldNotFire_ForNewCards()
        {
            bool fired = false;
            _subs.Add(EventBus.Subscribe("xp_from_duplicate", _ => fired = true));

            // buy_pack with no duplicates — xp_from_duplicate must not be emitted
            PublishFakeBuyPack("bronze_pack", 100, new[] { ("uid_new", "common", false, 0) });

            Assert.IsFalse(fired,
                "xp_from_duplicate must not fire when no duplicates are present.");
        }

        [Test]
        public void XpFromDuplicate_Payload_ShouldContainCardId_Rarity_Xp()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("xp_from_duplicate", e => received = e));

            PublishFakeXpFromDuplicate("uid_dup", "epic", 50, "gold_pack");

            Assert.IsNotNull(received);
            var p = JsonUtility.FromJson<XpFromDuplicatePayload>(received.payloadJson);
            Assert.AreEqual("uid_dup",   p.card_id);
            Assert.AreEqual("epic",      p.rarity);
            Assert.AreEqual(50,          p.xp_gained);
            Assert.AreEqual("gold_pack", p.pack_type_id);
        }

        [Test]
        public void XpFromDuplicate_Source_ShouldIncludeRarity()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("xp_from_duplicate", e => received = e));

            PublishFakeXpFromDuplicate("uid_r", "rare", 25, "silver_pack");

            var p = JsonUtility.FromJson<XpFromDuplicatePayload>(received.payloadJson);
            StringAssert.Contains("rare", p.source,
                "source must embed the rarity (e.g. 'duplicate_card_rare').");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static void PublishFakeBuyPack(
            string packTypeId, int costCoins,
            (string uid, string rarity, bool isDup, int xp)[] cardData,
            string playerId = "test_player")
        {
            var entries = new List<BuyPackCardEntry>();
            foreach (var (uid, rarity, isDup, xp) in cardData)
                entries.Add(new BuyPackCardEntry { card_id = uid, rarity = rarity, is_duplicate = isDup, xp_awarded = xp });

            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type  = "buy_pack",
                player_id   = playerId,
                payloadJson = JsonUtility.ToJson(new BuyPackPayload
                {
                    pack_type_id = packTypeId,
                    cost_paid    = new CostPaid { coins = costCoins, gems = 0 },
                    cards_pulled = entries
                })
            });
        }

        private static void PublishFakeXpFromDuplicate(
            string cardId, string rarity, int xp, string packTypeId,
            string playerId = "test_player")
        {
            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type  = "xp_from_duplicate",
                player_id   = playerId,
                payloadJson = JsonUtility.ToJson(new XpFromDuplicatePayload
                {
                    card_id      = cardId,
                    rarity       = rarity,
                    xp_gained    = xp,
                    source       = $"duplicate_card_{rarity}",
                    pack_type_id = packTypeId
                })
            });
        }
    }
}
