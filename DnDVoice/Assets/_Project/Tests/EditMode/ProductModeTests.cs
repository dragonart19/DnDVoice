using DndProximityVoice.Core;
using NUnit.Framework;
using UnityEngine;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class ProductModeTests
    {
        private GameObject root;
        private ProductModeManager manager;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Product mode tests");
            manager = root.AddComponent<ProductModeManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void StartsWithoutASelectedMode()
        {
            Assert.That(manager.CurrentMode, Is.EqualTo(ProductMode.None));
            Assert.That(manager.HasSelection, Is.False);
        }

        [Test]
        public void SelectsTheAvailableTabletop2DMode()
        {
            Assert.That(manager.TrySelect(ProductMode.Tabletop2D), Is.True);
            Assert.That(manager.CurrentMode, Is.EqualTo(ProductMode.Tabletop2D));
            Assert.That(manager.HasSelection, Is.True);
        }

        [Test]
        public void RejectsTheUnavailableWorldBuilder3DMode()
        {
            Assert.That(manager.TrySelect(ProductMode.WorldBuilder3D), Is.False);
            Assert.That(manager.CurrentMode, Is.EqualTo(ProductMode.None));
        }

        [Test]
        public void ClearingSelectionNotifiesOnlyWhenTheModeChanges()
        {
            var notifications = 0;
            manager.ModeChanged += _ => notifications++;

            manager.ClearSelection();
            manager.TrySelect(ProductMode.Tabletop2D);
            manager.TrySelect(ProductMode.Tabletop2D);
            manager.ClearSelection();

            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(manager.CurrentMode, Is.EqualTo(ProductMode.None));
        }
    }
}
