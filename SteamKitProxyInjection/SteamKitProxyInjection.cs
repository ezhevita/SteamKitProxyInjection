using System;
using System.Collections.Generic;
using System.Composition;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SteamKit2;

namespace SteamKitProxyInjection {
	[Export(typeof(IPlugin))]
	// ReSharper disable UnusedMember.Global
	public class SteamKitProxyInjection : IASF {
		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("Loaded " + Name);
		}
		
		public string Name => nameof(SteamKitProxyInjection);
		public Version Version => new Version(1, 0, 0, 0);
		public void OnASFInit(IReadOnlyDictionary<string, JToken> additionalConfigProperties = null) {
			ASF.ArchiLogger.LogGenericInfo("Injecting...");
			Harmony harmony = new Harmony("com.Vital7.SteamKitProxyInjection");
			
			ASF.ArchiLogger.LogGenericTrace("Retrieving WebSocketContext type...");
			Assembly steamkitAssembly = typeof(SteamID).Assembly;
			Type webSocketConnectionType = steamkitAssembly.GetType("SteamKit2.WebSocketConnection");
			Type webSocketContextType = webSocketConnectionType.GetNestedTypes(BindingFlags.NonPublic)[0];

			ASF.ArchiLogger.LogGenericTrace("Retrieving WebSocketContext constructor...");
			ConstructorInfo constructor = AccessTools.Constructor(webSocketContextType, new[] {webSocketConnectionType, typeof(EndPoint)});
			ASF.ArchiLogger.LogGenericTrace("Patching...");
			harmony.Patch(constructor, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamKitProxyInjection), "TargetMethod")));
			ASF.ArchiLogger.LogGenericInfo("Successfully injected!");
		}

		public static void TargetMethod(ClientWebSocket ___socket) {
			ASF.ArchiLogger.LogGenericTrace("Retrieving WebProxy config value...");
			IWebProxy webProxy = (IWebProxy) AccessTools.Property(typeof(GlobalConfig), "WebProxy").GetValue(ASF.GlobalConfig);
			ASF.ArchiLogger.LogGenericTrace("Setting proxy...");
			___socket.Options.Proxy = webProxy;
		}
	}
}