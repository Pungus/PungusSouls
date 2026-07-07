using CreatureManager;
using HarmonyLib;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    namespace PungusSouls
    {
    public class BonfireController : MonoBehaviour, Hoverable, Interactable
    {
        private const string ActivatedKey = "bonfire_activated";
        private const string BonfireListKey = "ps_bonfires";
        public class BonfireData
        {
            public Vector3 Position;
            public string Name;
        }

        private ZNetView m_nview;
        private GameObject enabledRoot;
        private void Awake()
        {
            Debug.Log(
                 $"[Bonfire] Awake {gameObject.name} {transform.position}");

            BonfireManager.RegisterBonfire(transform.position);

            m_nview = GetComponentInParent<ZNetView>();

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Enabled")
                {
                    enabledRoot = t.gameObject;
                }
                
            }
           RefreshState();
        }
        private void Start()
        {

            GameObject ps_bonfire = PrefabManager.RegisterPrefab(Animations.asset, "PS_Bonfire");
            Debug.Log(
    $"[Bonfire] ZNetView Valid = " +
    $"{m_nview != null && m_nview.IsValid()}");
            Debug.Log(
                $"[Bonfire] Registered prefab = {ps_bonfire != null}");

        }

        private void OnDestroy()
        {

            Debug.Log(
                $"[Bonfire] DESTROYING {gameObject.name} {transform.position}");

            Debug.Log(
                $"[Bonfire] Scene = {gameObject.scene.name}");

            Debug.Log(
                $"[Bonfire] ActiveInHierarchy = {gameObject.activeInHierarchy}");


            BonfireManager.UnregisterBonfire(transform.position);
        }

        private bool IsActivated()
        {
            if (m_nview == null)
            {
                return enabledRoot != null && enabledRoot.activeSelf;
            }

            return m_nview.IsValid()
                && m_nview.GetZDO().GetBool(ActivatedKey);
        }

        private void RefreshState()
        {
            if (enabledRoot == null)
            {
                return;
            }

            enabledRoot.SetActive(IsActivated());
        }
        public static class BonfireInjector
        {
            private static void Postfix()
            {
                foreach (Transform t in Object.FindObjectsByType<Transform>(
                                FindObjectsInactive.Include,
                                FindObjectsSortMode.None))
                {
                    if (t.name != "PS_Bonfire")
                        continue;

                    //Debug.Log($"Found bonfire: {t.name}");

                    if (t.GetComponent<BonfireController>() == null)
                    {
                        //Debug.Log("Injecting BonfireController");

                        t.gameObject.AddComponent<BonfireController>();
                    }
                }
            }
        }
        private void Activate()
        {
            if (m_nview != null && m_nview.IsValid())
            {
                if (!m_nview.IsOwner())
                {
                    m_nview.ClaimOwnership();
                }

                m_nview.GetZDO().Set(ActivatedKey, true);
            }

            if (enabledRoot != null)
            {
                enabledRoot.SetActive(true);

                foreach (ParticleSystem ps in
                            enabledRoot.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Play(true);
                }
            }

            RefreshState();

            Minimap.instance.AddPin(
                transform.position,
                Minimap.PinType.Icon3,
                "Bonfire",
                true,
                false);


            BonfireManager.StarterActivated = true;

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                "Bonfire lit");

        }


        private void Rest(Player player)
        {
            player.Heal(9999f);

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                "You rest at the bonfire.");
        }


        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }

            Player player = user as Player;

            if (player == null)
            {
                return false;
            }

            if (!IsActivated())
            {
                Activate();

            BonfireManager.RegisterBonfire(
                    transform.position);

                return true;
            }

            if (alt)
            {
                StartTravel();
                return true;
            }

            Rest(player);

            return true;
        }


        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            return "Ancient Bonfire";
        }
        public string GetHoverText()
        {
            if (!IsActivated())
            {
                return
                    "<color=yellow><b>E</b></color> Light Bonfire";
            }

            return
                "Ancient Bonfire\n" +
                "<color=yellow><b>E</b></color> Rest\n" +
                "<color=yellow><b>Shift+E</b></color> Travel";
        }

        private void StartTravel()
        {
            BonfireManager.TravelMode = true;

            Minimap.instance.SetMapMode(
                Minimap.MapMode.Large);

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                "Select a destination bonfire.");
        }


        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        public static class Minimap_OnMapLeftClick_Patch
        {

            private static void Postfix(Minimap __instance)
            {
                Debug.Log("[Bonfire] Map click detected");
                Debug.Log($"[Bonfire] TravelMode={BonfireManager.TravelMode}");
                if (!BonfireManager.TravelMode)
                {
                    return;
                }

                var pins =
                    Traverse.Create(__instance)
                        .Field("m_pins")
                        .GetValue<List<Minimap.PinData>>();

                Vector3 worldPos =
                    (Vector3)Traverse.Create(__instance)
                        .Method("ScreenToWorldPoint",
                            ZInput.mousePosition)
                        .GetValue();

                Minimap.PinData closest = null;
                float best = float.MaxValue;

                foreach (var pin in pins)
                {
                    if (!pin.m_save)
                    {
                        continue;
                    }

                    if (pin.m_name != "Bonfire")
                    {
                        continue;
                    }

                    float distance =
                        Utils.DistanceXZ(
                            worldPos,
                            pin.m_pos);

                    if (distance < best)
                    {
                        best = distance;
                        closest = pin;
                    }
                }

                Debug.Log(
                    $"[Bonfire] Closest pin = {(closest != null ? closest.m_name : "NULL")}");

                if (closest == null)
                {
                    return;
                }
                Debug.Log(
                    $"[Bonfire] Clicked pin position = {closest.m_pos}");

                Debug.Log($"[Bonfire] Teleporting to {closest.m_pos}");

                Player.m_localPlayer.TeleportTo(
                    closest.m_pos,
                    Player.m_localPlayer.transform.rotation,
                    true);

                BonfireManager.TravelMode = false;

                Minimap.instance.SetMapMode(
                    Minimap.MapMode.Small);
                Minimap.instance.SetMapMode(
                    Minimap.MapMode.Small);

                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    "Travelled to bonfire.");
            }

        }
        }

    }

