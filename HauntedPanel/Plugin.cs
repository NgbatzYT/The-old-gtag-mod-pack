using System;
using System.Reflection;
using System.ComponentModel;

using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

using UnityEngine;

namespace HauntedModMenu
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
	public class HauntedModMenuPlugin : BaseUnityPlugin
	{
		private bool inRoom;
		private GameObject menuObject = null;

		private void Awake()
		{
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HauntedModMenu.Resources.font");
			if (stream == null)
				return;

			var fontBundle = AssetBundle.LoadFromStream(stream);
			if (fontBundle == null)
				return;

			Utils.RefCache.CustomFont = fontBundle.LoadAsset<Font>("ShortBaby");

			fontBundle.Unload(false);
		}

		private void Start()
		{
			foreach(BepInEx.PluginInfo plugin in Chainloader.PluginInfos.Values) {

				BaseUnityPlugin modPlugin = plugin.Instance;
				Type type = modPlugin.GetType();
				DescriptionAttribute modDescription = type.GetCustomAttribute<DescriptionAttribute>();

				if (modDescription == null)
					continue;

				if (modDescription.Description.Contains("HauntedModMenu")) {
					var enableImp = AccessTools.Method(type, "OnEnable");
					var disableImp = AccessTools.Method(type, "OnDisable");

					if(enableImp != null && disableImp != null)
						Utils.RefCache.ModList.Add(new Utils.ModInfo(modPlugin, plugin.Metadata.Name));
				}
			}

			GorillaTagger.OnPlayerSpawned(OnGameInitialized);
		}

		private void OnEnable()
		{
			if (menuObject != null && inRoom)
				menuObject.SetActive(true);
		}

		private void OnDisable()
		{
			if (menuObject != null)
				menuObject.SetActive(false);
		}

		private void OnGameInitialized()
		{
			Utils.RefCache.LeftHandFollower = GorillaLocomotion.Player.Instance.leftHandFollower.gameObject;
			Utils.RefCache.RightHandFollower = GorillaLocomotion.Player.Instance.rightHandFollower.gameObject;
			Utils.RefCache.CameraTransform = GorillaLocomotion.Player.Instance.headCollider.transform;
			Utils.RefCache.PlayerTransform = GorillaLocomotion.Player.Instance.turnParent.transform;

			Utils.RefCache.LeftHandRig = GorillaTagger.Instance.offlineVRRig.leftHandTransform.parent.gameObject;
            Utils.RefCache.RightHandRig = GorillaTagger.Instance.offlineVRRig.rightHandTransform.parent.gameObject;
        }

		public void OnJoin()
		{
			inRoom = true;

			if (menuObject != null)
				return;

			menuObject = CreateTrigger();

			if (menuObject != null) {
				menuObject.AddComponent<Menu.MenuController>();
				menuObject.SetActive(this.enabled && this.inRoom);
			}
		}

		public void OnLeave()
		{
            if (inRoom)
            {
                inRoom = false;
                UnityEngine.Object.Destroy(menuObject);
            }
            
		}

		private GameObject CreateTrigger()
		{
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			if (go == null)
				return null;

			Collider col = go.GetComponent<Collider>();
			if (col != null)
				col.isTrigger = true;

			MeshRenderer render = go.GetComponent<MeshRenderer>();
			if (render != null)
				UnityEngine.Object.Destroy(render);

			return go;
		}

		public void Update()
		{

            if(NetworkSystem.Instance.InRoom && NetworkSystem.Instance.GameModeString.Contains("MODDED"))
			{
				OnJoin();
			}
			else
			{
				OnLeave();
			}
		}
	}
}
