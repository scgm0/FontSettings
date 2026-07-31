using System.Reflection;
using HarmonyLib;
using ImGuiController_OpenTK;

namespace FontSettings;

public static class ImGuiControllerSingletonPatch {
	static private readonly ConstructorInfo ImGuiControllerCtor = AccessTools.Constructor(typeof(ImGuiController), [typeof(IWindow), typeof(bool)]);
	static private readonly MethodInfo ImGuiControllerPostfix = AccessTools.Method(typeof(ImGuiControllerSingletonPatch), "Postfix");
	public static ImGuiController? Instance { get; private set; }

	public static void Postfix(ImGuiController __instance) {
		Instance = __instance;
	}

	public static void Patch(Harmony harmony) { harmony.Patch(ImGuiControllerCtor, postfix: ImGuiControllerPostfix); }

	public static void Unpatch(Harmony harmony) { harmony.Unpatch(ImGuiControllerCtor, ImGuiControllerPostfix); }
}