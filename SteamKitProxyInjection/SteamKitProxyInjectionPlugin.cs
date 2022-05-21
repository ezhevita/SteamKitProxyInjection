using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace SteamKitProxyInjection {
	[Export(typeof(IPlugin))]
	[UsedImplicitly]
	public class SteamKitProxyInjectionPlugin : IASF {
		public Task OnLoaded()
		{
			ASF.ArchiLogger.LogGenericInfo($"{Name} by ezhevita | Support & source code: https://github.com/ezhevita/{Name}");

			return Task.CompletedTask;
		}

		public string Name => nameof(SteamKitProxyInjection);
		public Version Version => Assembly.GetExecutingAssembly().GetName().Version ?? throw new InvalidOperationException();

		public Task OnASFInit(IReadOnlyDictionary<string, JToken>? additionalConfigProperties = null)
		{
			ASF.ArchiLogger.LogGenericInfo("Injecting...");
			Harmony harmony = new("dev.ezhevita.SteamKitProxyInjection");

			ASF.ArchiLogger.LogGenericTrace("Retrieving WebSocketConnection and WebSocketContext types...");
			Type webSocketConnectionType = AccessTools.TypeByName("SteamKit2.WebSocketConnection");
			Type webSocketContextType = AccessTools.TypeByName("SteamKit2.WebSocketConnection+WebSocketContext");

			ASF.ArchiLogger.LogGenericTrace("Retrieving WebSocketContext constructor...");
			ConstructorInfo constructor = AccessTools.Constructor(webSocketContextType, new[] {webSocketConnectionType, typeof(EndPoint)});
			ASF.ArchiLogger.LogGenericTrace("Patching...");
			harmony.Patch(constructor, postfix: new HarmonyMethod(AccessTools.Method(typeof(SteamKitProxyInjectionPlugin), nameof(TargetMethod))));
			ASF.ArchiLogger.LogGenericInfo("Successfully injected!");

			return Task.CompletedTask;
		}

		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
		[SuppressMessage("ReSharper", "InconsistentNaming")]
#pragma warning disable CA1707
		public static void TargetMethod(ClientWebSocket ___socket) {
			if (___socket == null) {
				throw new ArgumentNullException(nameof(___socket));
			}

			ASF.ArchiLogger.LogGenericTrace("Retrieving WebProxy config value...");
			IWebProxy? webProxy = ASF.GlobalConfig?.WebProxy;
			if (webProxy == null) {
				return;
			}

			ASF.ArchiLogger.LogGenericTrace("Setting proxy...");
			___socket.Options.Proxy = webProxy;
		}
#pragma warning restore CA1707
	}
}
