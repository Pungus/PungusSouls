using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Serialization;


namespace ValheimAddNewEnv
{
	[BepInPlugin(GUID, MOD_NAME, MOD_VERSION)]
	public class Main : BaseUnityPlugin
	{
		//Mod Settings
		public const string GUID = "base.newenv";
		public const string MOD_NAME = "PungusSouls";
		public const string MOD_VERSION = "1.0.0.0";

		//BepinEx
		Harmony _Harmony = new Harmony(GUID);
		public static ManualLogSource Log;

		//Custom Variable
		public static EnvSetup newEnv = new EnvSetup(); //Assembly_Valheim

		private void Awake()
		{
			//Logger Setup
#if DEBUG
			Log = Logger;
#else
            Log = new ManualLogSource(null);
#endif
			//Logger Setup End

			InitEnvSetup();

			_Harmony.PatchAll();
		}

		private void OnDestroy()
		{
			if (_Harmony != null) _Harmony.UnpatchSelf();
		}

		[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
		private static class EnvMan_Awake_Patches
		{
			public static void Postfix(EnvMan __instance)
			{
				__instance.m_environments.Add(newEnv);
				__instance.InitializeEnvironment(newEnv);
			}
		}


		private void InitEnvSetup()
		{
			newEnv.m_name = "FirelinkShrine";
			newEnv.m_ambColorNight = Color.white;


            //Below is defaults, if a Bool is not set it's "False"

            // Token: 0x020001B6 RID: 438
            /*			[Serializable]
						public class EnvSetup*/
            /*{
				// Token: 0x060010FC RID: 4348 RVA: 0x000730EA File Offset: 0x000712EA
				public EnvSetup Clone()
				{
					return base.MemberwiseClone() as EnvSetup;
				}

				// Token: 0x060010FD RID: 4349 RVA: 0x000730F8 File Offset: 0x000712F8
				public EnvSetup()
				{
				}

				// Token: 0x04001181 RID: 4481
				public string m_name = "FirelinkShrine";

				// Token: 0x04001182 RID: 4482
				public bool m_default;

				// Token: 0x04001183 RID: 4483
	*//*			[Header("Gameplay")]
				public bool m_isWet;*//*

				// Token: 0x04001184 RID: 4484
	*//*			public bool m_isFreezing;*//*

				// Token: 0x04001185 RID: 4485
	*//*			public bool m_isFreezingAtNight;*//*

				// Token: 0x04001186 RID: 4486
	*//*			public bool m_isCold;*//*

				// Token: 0x04001187 RID: 4487
	*//*			public bool m_isColdAtNight = true;*//*

				// Token: 0x04001188 RID: 4488
	*//*			public bool m_alwaysDark;*//*

				// Token: 0x04001189 RID: 4489
				[Header("Ambience")]
				public Color m_ambColorNight = Color.white;

				// Token: 0x0400118A RID: 4490
				public Color m_ambColorDay = Color.white;

				// Token: 0x0400118B RID: 4491
				[Header("Fog-ambient")]
				public Color m_fogColorNight = Color.white;

				// Token: 0x0400118C RID: 4492
				public Color m_fogColorMorning = Color.white;

				// Token: 0x0400118D RID: 4493
				public Color m_fogColorDay = Color.white;

				// Token: 0x0400118E RID: 4494
				public Color m_fogColorEvening = Color.white;

				// Token: 0x0400118F RID: 4495
				[Header("Fog-sun")]
				public Color m_fogColorSunNight = Color.white;

				// Token: 0x04001190 RID: 4496
				public Color m_fogColorSunMorning = Color.white;

				// Token: 0x04001191 RID: 4497
				public Color m_fogColorSunDay = Color.white;

				// Token: 0x04001192 RID: 4498
				public Color m_fogColorSunEvening = Color.white;

				// Token: 0x04001193 RID: 4499
				[Header("Fog-distance")]
				public float m_fogDensityNight = 0.0f;

				// Token: 0x04001194 RID: 4500
				public float m_fogDensityMorning = 0.0f;

				// Token: 0x04001195 RID: 4501
				public float m_fogDensityDay = 0.0f;

				// Token: 0x04001196 RID: 4502
				public float m_fogDensityEvening = 0.0f;

				// Token: 0x04001197 RID: 4503
				[Header("Sun")]
				public Color m_sunColorNight = Color.white;

				// Token: 0x04001198 RID: 4504
				public Color m_sunColorMorning = Color.white;

				// Token: 0x04001199 RID: 4505
				public Color m_sunColorDay = Color.white;

				// Token: 0x0400119A RID: 4506
				public Color m_sunColorEvening = Color.white;

				// Token: 0x0400119B RID: 4507
				public float m_lightIntensityDay = 1.2f;

				// Token: 0x0400119C RID: 4508
				public float m_lightIntensityNight;

				// Token: 0x0400119D RID: 4509
				public float m_sunAngle = 60f;

				// Token: 0x0400119E RID: 4510
				[Header("Wind")]
				public float m_windMin;

				// Token: 0x0400119F RID: 4511
				public float m_windMax = 1f;

				// Token: 0x040011A0 RID: 4512
				[Header("Effects")]
				public GameObject m_envObject;

				// Token: 0x040011A1 RID: 4513
				public GameObject[] m_psystems;

				// Token: 0x040011A2 RID: 4514
				public bool m_psystemsOutsideOnly;

				// Token: 0x040011A3 RID: 4515
				public float m_rainCloudAlpha;

				// Token: 0x040011A4 RID: 4516
				[Header("Audio")]
				public AudioClip m_ambientLoop;

				// Token: 0x040011A5 RID: 4517
				public float m_ambientVol = 0.3f;

				// Token: 0x040011A6 RID: 4518
				public string m_ambientList = "";

				// Token: 0x040011A7 RID: 4519
				[Header("Music overrides")]
				public string m_musicMorning = "";

				// Token: 0x040011A8 RID: 4520
				public string m_musicEvening = "";

				// Token: 0x040011A9 RID: 4521
				[FormerlySerializedAs("m_musicRandomDay")]
				public string m_musicDay = "";

				// Token: 0x040011AA RID: 4522
				[FormerlySerializedAs("m_musicRandomNight")]
				public string m_musicNight = "";
			}*/
        }
	}
}
