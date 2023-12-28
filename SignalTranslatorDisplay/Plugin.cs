using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SignalTranslatorDisplay;

[BepInPlugin(modGUID, "SignalTranslatorDisplay", modVersion)]
internal class PluginLoader : BaseUnityPlugin
{
    private const string modGUID = "Dev1A3.SignalTranslatorDisplay";

    private readonly Harmony harmony = new Harmony(modGUID);

    private const string modVersion = "1.0.1";

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

        SignalTranslatorConfig.InitConfig();
    }

    public void BindConfig<T>(ref ConfigEntry<T> config, string section, string key, T defaultValue, string description = "")
    {
        config = ((BaseUnityPlugin)this).Config.Bind<T>(section, key, defaultValue, description);
    }
}

internal class SignalTranslatorConfig
{
    public static ConfigEntry<bool> OriginalEnabled;
    public static ConfigEntry<bool> ChatEnabled;
    public static ConfigEntry<bool> TipEnabled;

    public static void InitConfig()
    {
        PluginLoader.Instance.BindConfig(ref OriginalEnabled, "Settings", "Default Popup", true, "Should the signal transmission be shown as the default popup?");
        PluginLoader.Instance.BindConfig(ref ChatEnabled, "Settings", "Chat Message", true, "Should the signal transmission be shown as a chat message?");
        PluginLoader.Instance.BindConfig(ref TipEnabled, "Settings", "Small Popup", false, "Should the signal transmission be shown as a small popup?");
    }
}

[HarmonyPatch]
class Patch
{
    private static MethodInfo AddChatMessage = AccessTools.Method(typeof(HUDManager), "AddChatMessage");

    [HarmonyPatch(typeof(HUDManager), "DisplaySignalTranslatorMessage")]
    [HarmonyPrefix]
    internal static bool DisplaySignalTranslatorMessage(ref IEnumerator __result, HUDManager __instance, string signalMessage, int seed, SignalTranslator signalTranslator)
    {
        if (signalTranslator != null)
        {
            __result = DisplayMessageCoroutine(__instance, signalMessage, seed, signalTranslator);
        }

        return false;
    }

    static IEnumerator DisplayMessageCoroutine(HUDManager __instance, string signalMessage, int seed, SignalTranslator signalTranslator)
    {
        System.Random signalMessageRandom = new System.Random(seed + StartOfRound.Instance.randomMapSeed);
        if (SignalTranslatorConfig.OriginalEnabled.Value)
        {
            __instance.signalTranslatorAnimator.SetBool("transmitting", value: true);
        }
        signalTranslator.localAudio.Play();
        __instance.UIAudio.PlayOneShot(signalTranslator.startTransmissionSFX, 1f);
        __instance.signalTranslatorText.text = "";
        yield return new WaitForSeconds(1.21f);
        if (signalTranslator != null)
        {
            string signalTitle = "Receiving Signal";
            if (SignalTranslatorConfig.ChatEnabled.Value)
            {
                AddChatMessage?.Invoke(__instance, [signalMessage, signalTitle]);
            }
            if (SignalTranslatorConfig.TipEnabled.Value)
            {
                __instance.tipsPanelHeader.text = signalTitle;
                __instance.tipsPanelBody.text = signalMessage;
                __instance.tipsPanelAnimator.SetTrigger("TriggerHint");
            }
        }
        if (SignalTranslatorConfig.OriginalEnabled.Value)
        {
            for (int i = 0; i < signalMessage.Length; i++)
            {
                if (signalTranslator == null)
                {
                    break;
                }

                if (!signalTranslator.gameObject.activeSelf)
                {
                    break;
                }

                __instance.UIAudio.PlayOneShot(signalTranslator.typeTextClips[UnityEngine.Random.Range(0, signalTranslator.typeTextClips.Length)]);
                __instance.signalTranslatorText.text += signalMessage[i];
                float num = Mathf.Min((float)signalMessageRandom.Next(-1, 4) * 0.5f, 0f);
                yield return new WaitForSeconds(0.7f + num);
            }
        }

        if (signalTranslator != null)
        {
            __instance.UIAudio.PlayOneShot(signalTranslator.finishTypingSFX);
            signalTranslator.localAudio.Stop();
        }

        yield return new WaitForSeconds(0.5f);
        __instance.signalTranslatorAnimator.SetBool("transmitting", value: false);
    }
}