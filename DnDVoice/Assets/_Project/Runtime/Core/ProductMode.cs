namespace DndProximityVoice.Core
{
    public enum ProductMode
    {
        None = 0,
        Tabletop2D = 1,
        WorldBuilder3D = 2
    }

    public static class ProductModeCatalog
    {
        public static bool IsAvailable(ProductMode mode)
        {
            return mode == ProductMode.Tabletop2D;
        }

        public static string GetDisplayName(ProductMode mode)
        {
            switch (mode)
            {
                case ProductMode.Tabletop2D:
                    return "Tavolo 2D";
                case ProductMode.WorldBuilder3D:
                    return "World Builder 3D";
                default:
                    return "Nessuna modalità";
            }
        }
    }
}
