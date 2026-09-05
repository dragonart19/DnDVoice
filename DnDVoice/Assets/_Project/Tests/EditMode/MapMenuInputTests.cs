using System.Reflection;
using DndProximityVoice.Map;
using NUnit.Framework;
using UnityEngine;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class MapMenuInputTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private ProximityMapOverlay overlay;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Map menu input regression");
            overlay = root.AddComponent<ProximityMapOverlay>();
            SetField("mapScroll", new Vector2(150f, 180f));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [TestCase(EventModifiers.None)]
        [TestCase(EventModifiers.Control)]
        [TestCase(EventModifiers.Shift)]
        public void OpenMenuLeavesWheelForDrawerAndPreservesMapView(EventModifiers modifiers)
        {
            SetField("burgerMenuOpen", true);
            var input = new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = new Vector2(100f, 100f),
                delta = new Vector2(0f, 3f),
                modifiers = modifiers
            };

            InvokeWheel(input);

            Assert.That(GetField<Vector2>("mapScroll"), Is.EqualTo(new Vector2(150f, 180f)));
            Assert.That(GetField<float>("mapPixelsPerMeter"), Is.EqualTo(28f));
            Assert.That(input.type, Is.EqualTo(EventType.ScrollWheel),
                "The drawer must receive the wheel instead of the underlying map.");
        }

        [Test]
        public void ClosingMenuRestoresMapScrolling()
        {
            SetField("burgerMenuOpen", false);
            var input = new Event
            {
                type = EventType.ScrollWheel,
                mousePosition = new Vector2(100f, 100f),
                delta = new Vector2(0f, 1f)
            };

            InvokeWheel(input);

            Assert.That(GetField<Vector2>("mapScroll").y, Is.GreaterThan(180f));
            Assert.That(input.type, Is.EqualTo(EventType.Used));
        }

        [Test]
        public void OpenMenuCancelsMapDragWithoutConsumingButtonClick()
        {
            SetField("burgerMenuOpen", true);
            SetField("draggingPlayerId", 42UL);
            SetField("wallDragActive", true);
            var input = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = new Vector2(100f, 100f)
            };
            // Outside OnGUI, Unity may expose a mouse event as Ignore instead of MouseDown.
            var originalType = input.type;

            typeof(ProximityMapOverlay).GetMethod("HandleMapInput", PrivateInstance).Invoke(
                overlay, new object[] { new Rect(0f, 0f, 800f, 600f), 28f, true, input });

            Assert.That(GetField<ulong>("draggingPlayerId"), Is.Zero);
            Assert.That(GetField<bool>("wallDragActive"), Is.False);
            Assert.That(input.type, Is.EqualTo(originalType));
        }

        private void InvokeWheel(Event input)
        {
            typeof(ProximityMapOverlay).GetMethod("HandleMapWheel", PrivateInstance).Invoke(
                overlay, new object[] { new Rect(0f, 0f, 800f, 600f), new Vector2(48f, 48f), input });
        }

        private void SetField(string name, object value)
        {
            typeof(ProximityMapOverlay).GetField(name, PrivateInstance).SetValue(overlay, value);
        }

        private T GetField<T>(string name)
        {
            return (T)typeof(ProximityMapOverlay).GetField(name, PrivateInstance).GetValue(overlay);
        }
    }
}
