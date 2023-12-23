using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace SignalTranslatorTip;

[BepInPlugin(modGUID, "SignalTranslatorTip", modVersion)]
internal class PluginLoader : BaseUnityPlugin
{
    private const string modGUID = "Dev1A3.SignalTranslatorTip";

    private readonly Harmony harmony = new Harmony(modGUID);

    private const string modVersion = "1.0.0";

    private static bool initialized;

    public static PluginLoader Instance { get; private set; }

    private void Awake()
    {
        if (initialized)
        {
            return;
        }
        initialized = true;
        Instance = this;
        Assembly patches = Assembly.GetExecutingAssembly();
        harmony.PatchAll(patches);
    }
}

[HarmonyPatch]
class Patch
{
    [HarmonyPatch(typeof(HUDManager), "DisplaySignalTranslatorMessage")]
    [HarmonyPrefix]
    internal static bool DisplaySignalTranslatorMessage(HUDManager __instance, string signalMessage, int seed, SignalTranslator signalTranslator)
    {
        if (signalTranslator != null)
        {
            signalTranslator.localAudio.Play();
            __instance.DisplayTip("Signal Translator", signalMessage);
            __instance.UIAudio.PlayOneShot(signalTranslator.finishTypingSFX);
            signalTranslator.localAudio.Stop();
        }

        return false;
    }
}