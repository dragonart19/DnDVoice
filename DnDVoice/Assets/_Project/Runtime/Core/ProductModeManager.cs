using System;
using UnityEngine;

namespace DndProximityVoice.Core
{
    [DisallowMultipleComponent]
    public sealed class ProductModeManager : MonoBehaviour
    {
        public event Action<ProductMode> ModeChanged;

        public ProductMode CurrentMode { get; private set; } = ProductMode.None;

        public bool HasSelection => CurrentMode != ProductMode.None;

        public bool TrySelect(ProductMode mode)
        {
            if (!ProductModeCatalog.IsAvailable(mode))
            {
                return false;
            }

            SetMode(mode);
            return true;
        }

        public void ClearSelection()
        {
            SetMode(ProductMode.None);
        }

        private void SetMode(ProductMode mode)
        {
            if (CurrentMode == mode)
            {
                return;
            }

            CurrentMode = mode;
            ModeChanged?.Invoke(CurrentMode);
        }
    }
}
