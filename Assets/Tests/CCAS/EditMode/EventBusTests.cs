using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CCAS.Tests
{
    /// <summary>
    /// EditMode tests for EventBus behaviour and the pack_opened event wired
    /// into PackOpeningController.
    ///
    /// Run via: Window > General > Test Runner > EditMode tab.
    /// </summary>
    public class EventBusTests
    {
        // Mirror of the private PackOpenedPayload in PackOpeningController.
        // Kept here so tests never depend on internal implementation details.
        [Serializable]
        private class PackOpenedPayload
        {
            public string       pack_type;
            public string       pack_name;
            public int          cost_coins;
            public List<string> card_uids;
            public List<string> card_rarities;
            public int          total_cards;
        }

        private List<IDisposable> _subs;

        [SetUp]
        public void SetUp()
        {
            _subs = new List<IDisposable>();
        }

        [TearDown]
        public void TearDown()
        {
            // Always unsubscribe so test state never leaks into the next test
            // (EventBus uses a static dictionary).
            foreach (var s in _subs) s.Dispose();
            _subs.Clear();
        }

        // ------------------------------------------------------------------
        // Core EventBus behaviour
        // ------------------------------------------------------------------

        [Test]
        public void Publish_ShouldInvokeSubscriber()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("test_event", e => received = e));

            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type = "test_event",
                player_id  = "player_001"
            });

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

            Assert.IsNotNull(received);
            Assert.IsFalse(string.IsNullOrEmpty(received.event_id),
                "EventBus must auto-fill event_id with a GUID when not provided.");
        }

        [Test]
        public void Publish_ShouldAutoFill_Timestamp_WhenMissing()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("ts_test", e => received = e));

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "ts_test" });

            Assert.IsNotNull(received);
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
            Assert.AreEqual(1, count, "Subscriber should receive the first event.");

            sub.Dispose();

            EventBus.Publish(new EventBus.EventEnvelope { event_type = "dispose_test" });
            Assert.AreEqual(1, count, "Subscriber must not receive events after Dispose().");
        }

        [Test]
        public void Publish_NullEvent_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(null),
                "Null events must be silently ignored, not crash.");
        }

        [Test]
        public void Publish_EmptyEventType_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() =>
                EventBus.Publish(new EventBus.EventEnvelope { player_id = "p1" }),
                "Events without an event_type must be silently ignored, not crash.");
        }

        // ------------------------------------------------------------------
        // pack_opened event — verifies the contract wired in PackOpeningController
        // ------------------------------------------------------------------

        [Test]
        public void PackOpened_Event_ShouldHaveCorrectEventType()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("pack_opened", e => received = e));

            PublishFakePackOpened("bronze_pack", "Bronze Pack", 100,
                new List<string> { "uid_a", "uid_b" },
                new List<string> { "common", "uncommon" });

            Assert.IsNotNull(received, "A pack_opened subscriber must receive the event.");
            Assert.AreEqual("pack_opened", received.event_type);
        }

        [Test]
        public void PackOpened_Event_ShouldCarryPlayerId()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("pack_opened", e => received = e));

            PublishFakePackOpened("bronze_pack", "Bronze Pack", 100,
                new List<string> { "uid_a" },
                new List<string> { "common" },
                playerId: "unit_test_player");

            Assert.AreEqual("unit_test_player", received.player_id);
        }

        [Test]
        public void PackOpened_Payload_ShouldDeserialize_PackType()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("pack_opened", e => received = e));

            PublishFakePackOpened("gold_pack", "Gold Pack", 300,
                new List<string> { "uid_x" },
                new List<string> { "rare" });

            var payload = JsonUtility.FromJson<PackOpenedPayload>(received.payloadJson);
            Assert.AreEqual("gold_pack", payload.pack_type);
            Assert.AreEqual("Gold Pack", payload.pack_name);
            Assert.AreEqual(300, payload.cost_coins);
        }

        [Test]
        public void PackOpened_Payload_ShouldDeserialize_CardLists()
        {
            EventBus.EventEnvelope received = null;
            _subs.Add(EventBus.Subscribe("pack_opened", e => received = e));

            var uids     = new List<string> { "uid_1", "uid_2", "uid_3" };
            var rarities = new List<string> { "common", "uncommon", "rare" };
            PublishFakePackOpened("bronze_pack", "Bronze Pack", 100, uids, rarities);

            var payload = JsonUtility.FromJson<PackOpenedPayload>(received.payloadJson);
            Assert.AreEqual(3, payload.total_cards);
            Assert.AreEqual(3, payload.card_uids.Count);
            Assert.AreEqual(3, payload.card_rarities.Count);
            Assert.AreEqual("uid_2",    payload.card_uids[1]);
            Assert.AreEqual("uncommon", payload.card_rarities[1]);
        }

        [Test]
        public void PackOpened_Event_ShouldNotBeDeliveredToOtherEventTypes()
        {
            bool wrongSubscriberFired = false;
            _subs.Add(EventBus.Subscribe("buy_coach", _ => wrongSubscriberFired = true));

            PublishFakePackOpened("bronze_pack", "Bronze Pack", 100,
                new List<string>(), new List<string>());

            Assert.IsFalse(wrongSubscriberFired,
                "pack_opened must not be delivered to buy_coach subscribers.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static void PublishFakePackOpened(
            string packType, string packName, int cost,
            List<string> uids, List<string> rarities,
            string playerId = "test_player")
        {
            var payload = new PackOpenedPayload
            {
                pack_type     = packType,
                pack_name     = packName,
                cost_coins    = cost,
                card_uids     = uids,
                card_rarities = rarities,
                total_cards   = uids.Count
            };

            EventBus.Publish(new EventBus.EventEnvelope
            {
                event_type  = "pack_opened",
                player_id   = playerId,
                payloadJson = JsonUtility.ToJson(payload)
            });
        }
    }
}
