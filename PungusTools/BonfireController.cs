using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public class BonfireController : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView m_nview;
        private GameObject enabledRoot;

        private void Awake()
        {
            m_nview = GetComponentInParent<ZNetView>();

            Debug.Log($"[Bonfire] Awake {gameObject.name} {GetBonfirePosition()}");
            Debug.Log($"[Bonfire] ZDO={(m_nview != null && m_nview.GetZDO() != null)}");

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Enabled")
                {
                    enabledRoot = t.gameObject;
                }
            }

            if (BonfireManager.IsActivated(GetBonfirePosition()))
            {
                BonfireManager.EnsureMapPin(GetBonfirePosition());
            }

            RefreshState();
        }

        private void Start()
        {
            Debug.Log($"[Bonfire] ZNetView Valid = {m_nview != null && m_nview.IsValid()}");
        }

        private void OnDestroy()
        {
            Debug.Log($"[Bonfire] DESTROYING {gameObject.name} {GetBonfirePosition()}");
            Debug.Log($"[Bonfire] Scene = {gameObject.scene.name}");
            Debug.Log($"[Bonfire] ActiveInHierarchy = {gameObject.activeInHierarchy}");
        }

        private Vector3 GetBonfirePosition()
        {
            Transform marker = transform.Find("BonfirePinPoint");

            if (marker != null)
            {
                return marker.position;
            }

            return transform.position;
        }

        private bool IsActivated()
        {
            return BonfireManager.IsActivated(GetBonfirePosition());
        }

        private void RefreshState()
        {
            if (enabledRoot == null)
            {
                return;
            }

            enabledRoot.SetActive(IsActivated());
        }

        private void Activate()
        {
            Vector3 position = GetBonfirePosition();

            if (enabledRoot != null)
            {
                enabledRoot.SetActive(true);

                foreach (ParticleSystem ps in enabledRoot.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Play(true);
                }
            }

            BonfireManager.RegisterBonfire(position);
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

        private void StartTravel()
        {
            BonfireManager.CurrentBonfirePosition = GetBonfirePosition();
            BonfireManager.TravelMode = true;
            BonfireManager.RefreshMapPins();

            Minimap.instance.SetMapMode(
                Minimap.MapMode.Large);

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                "Select a destination bonfire.");
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
                return "<color=yellow><b>E</b></color> Light Bonfire";
            }

            return
                "Ancient Bonfire\n" +
                "<color=yellow><b>E</b></color> Rest\n" +
                "<color=yellow><b>Shift+E</b></color> Travel";
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

                List<Minimap.PinData> pins = Traverse.Create(__instance)
                    .Field("m_pins")
                    .GetValue<List<Minimap.PinData>>();

                if (pins == null)
                {
                    return;
                }

                Vector3 worldPos = (Vector3)Traverse.Create(__instance)
                    .Method("ScreenToWorldPoint", ZInput.mousePosition)
                    .GetValue();

                float removeRadius = Traverse.Create(__instance)
                    .Field("m_removeRadius")
                    .GetValue<float>();

                float largeZoom = Traverse.Create(__instance)
                    .Field("m_largeZoom")
                    .GetValue<float>();

                float maxClickDistance = removeRadius * (largeZoom * 2f);

                Minimap.PinData closest = null;
                float best = float.MaxValue;

                foreach (Minimap.PinData pin in pins)
                {
                    if (!BonfireManager.IsBonfirePin(pin))
                    {
                        continue;
                    }

                    float distance = Utils.DistanceXZ(worldPos, pin.m_pos);

                    if (distance < best)
                    {
                        best = distance;
                        closest = pin;
                    }
                }

                if (closest == null || best > maxClickDistance)
                {
                    MessageHud.instance.ShowMessage(
                        MessageHud.MessageType.Center,
                        "Select a bonfire pin.");

                    return;
                }

                if (BonfireManager.CurrentBonfirePosition.HasValue &&
                    Utils.DistanceXZ(closest.m_pos, BonfireManager.CurrentBonfirePosition.Value) < 2f)
                {
                    MessageHud.instance.ShowMessage(
                        MessageHud.MessageType.Center,
                        "Already at this bonfire.");

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
                BonfireManager.CurrentBonfirePosition = null;

                Minimap.instance.SetMapMode(
                    Minimap.MapMode.Small);

                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    "Travelled to bonfire.");
            }
        }
    }
}
