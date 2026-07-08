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



            m_nview = GetComponentInParent<ZNetView>();
            Debug.Log($"[Bonfire] Awake {gameObject.name} {transform.position}");
            Debug.Log($"ZDO={(m_nview != null ? m_nview.GetZDO() != null : false)}");

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Enabled")
                {
                    enabledRoot = t.gameObject;
                }

                if (BonfireManager.IsActivated(transform.position))
                {
                    BonfireManager.EnsureMapPin(transform.position);
                    RefreshState();
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


            Debug.Log(
                $"[Bonfire] DESTROYING {gameObject.name} " +
                $"ZDO={(m_nview != null ? m_nview.GetZDO() != null : false)}");
        }


        private bool IsActivated()
        {
            return BonfireManager.IsActivated(transform.position);
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
            if (enabledRoot != null)
            {
                enabledRoot.SetActive(true);
                BonfireManager.RegisterBonfire(transform.position);
                foreach (ParticleSystem ps in enabledRoot.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Play(true);
                }
            }

            BonfireManager.RegisterBonfire(transform.position);

            RefreshState();

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

            BonfireManager.CurrentBonfirePosition = transform.position;
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
                if (!BonfireManager.TravelMode)
                {
                    return;
                }

                var pins =
                    Traverse.Create(__instance)
                        .Field("m_pins")
                        .GetValue<List<Minimap.PinData>>();

                if (pins == null)
                {
                    return;
                }

                Vector3 worldPos =
                    (Vector3)Traverse.Create(__instance)
                        .Method(
                            "ScreenToWorldPoint",
                            ZInput.mousePosition)
                        .GetValue();

                Minimap.PinData closest = null;
                float best = float.MaxValue;

                foreach (Minimap.PinData pin in pins)
                {
                    if (!pin.m_save)
                    {
                        continue;
                    }

                    if (!BonfireManager.IsBonfirePin(pin))
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

                if (closest == null)
                {
                    return;
                }

                Player player = Player.m_localPlayer;

                if (player == null)
                {
                    return;
                }

                player.TeleportTo(
                    closest.m_pos,
                    player.transform.rotation,
                    true);

                BonfireManager.TravelMode = false;

                Minimap.instance.SetMapMode(
                    Minimap.MapMode.Small);

                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    "Travelled to bonfire.");
            }
        }
    }

    }

