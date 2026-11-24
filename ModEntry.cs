using HarmonyLib;

namespace GuestMaidenCorrupt
{
    public class ModEntry : IMod
    {
        public const string ModId = "guest_maiden_corrupt";

        Harmony harmony;

        public void Init(ModManifest manifest)
        {
        }

        public void OnEnable()
        {
            GuestCorrupt.Register();
            harmony = new Harmony(ModId);
            harmony.UnpatchAll(ModId);
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }

        public void OnDisable()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(ModId);
            }
        }
    }
}
