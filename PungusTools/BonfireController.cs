using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace PungusSouls
{
    public class BonfireController : MonoBehaviour, Hoverable, Interactable
    {
        private ZNetView _zNetView;
        private GameObject _enabledRoot;

        private void Awake()
        {
            _zNetView = GetComponentInParent<ZNetView>();
            _enabledRoot = FindChildObject("Enabled");

            if (BonfireManager.IsActivated(GetBonfirePosition()))
                BonfireManager.EnsureMapPin(GetBonfirePosition());

            RefreshState();
        }

        private GameObject FindChildObject(string childName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];

                if (child != null && child.name == childName)
                    return child.gameObject;
            }

            return null;
        }

        private Vector3 GetBonfirePosition()
        {
            Transform marker = transform.Find("BonfirePinPoint");

            if (marker != null)
                return marker.position;

            return transform.position;
        }

        private bool IsActivated()
        {
            return BonfireManager.IsActivated(GetBonfirePosition());
        }

        private void RefreshState()
        {
            if (_enabledRoot == null)
                return;

            _enabledRoot.SetActive(IsActivated());
        }

        private void Activate()
        {
            Vector3 position = GetBonfirePosition();

            if (_enabledRoot != null)
            {
                _enabledRoot.SetActive(true);

                ParticleSystem[] particleSystems = _enabledRoot.GetComponentsInChildren<ParticleSystem>(true);

                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                        particleSystems[i].Play(true);
                }
            }

            BonfireManager.RegisterBonfire(position);
            RefreshState();

            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Bonfire lit");
        }

        private void Rest(Player player)
        {
            if (player == null)
                return;

            player.Heal(9999f);

            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "You rest at the bonfire.");
        }

        private void StartTravel()
        {
            BonfireManager.CurrentBonfirePosition = GetBonfirePosition();
            BonfireManager.TravelMode = true;
            BonfireManager.RefreshMapPins();

            if (Minimap.instance != null)
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);

            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Select a destination bonfire.");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
                return false;

            Player player = user as Player;

            if (player == null)
                return false;
            BonfireVegvisirGate gate =
                BonfireVegvisirGateUtility.FindGate(this);

            if (gate != null &&
                player != null &&
                !gate.PlayerHasRegistered(player))
            {
                Vegvisir vegvisir = gate.GetVegvisir();

                if (vegvisir != null)
                {
                    bool result = vegvisir.Interact(user, hold, alt);

                    if (result)
                    {
                        gate.RegisterPlayer(player);

                        MessageHud.instance.ShowMessage(
                            MessageHud.MessageType.Center,
                            "Bonfire route discovered.");
                    }

                    return result;
                }
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
                return "<color=yellow><b>E</b></color> Light Bonfire";

            return "Ancient Bonfire\n" +
                   "<color=yellow><b>E</b></color> Rest\n" +
                   "<color=yellow><b>Shift+E</b></color> Travel";
        }

        [HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
        public static class Minimap_OnMapLeftClick_Patch
        {
            private static void Postfix(Minimap __instance)
            {
                if (!BonfireManager.TravelMode)
                    return;

                if (__instance == null)
                    return;

                List<Minimap.PinData> pins = Traverse.Create(__instance)
                    .Field("m_pins")
                    .GetValue<List<Minimap.PinData>>();

                if (pins == null)
                    return;

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
                float bestDistance = float.MaxValue;

                for (int i = 0; i < pins.Count; i++)
                {
                    Minimap.PinData pin = pins[i];

                    if (!BonfireManager.IsBonfirePin(pin))
                        continue;

                    float distance = Utils.DistanceXZ(worldPos, pin.m_pos);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        closest = pin;
                    }
                }

                if (closest == null || bestDistance > maxClickDistance)
                {
                    if (MessageHud.instance != null)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Select a bonfire pin.");

                    return;
                }

                if (BonfireManager.CurrentBonfirePosition.HasValue &&
                    Utils.DistanceXZ(closest.m_pos, BonfireManager.CurrentBonfirePosition.Value) < 2f)
                {
                    if (MessageHud.instance != null)
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Already at this bonfire.");

                    return;
                }

                Player player = Player.m_localPlayer;

                if (player == null)
                    return;

                player.TeleportTo(closest.m_pos, player.transform.rotation, true);
                BonfireManager.CancelTravel();

                if (Minimap.instance != null)
                    Minimap.instance.SetMapMode(Minimap.MapMode.Small);

                if (MessageHud.instance != null)
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Travelled to bonfire.");
            }
        }
    }
}