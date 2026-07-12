using UnityEngine;

namespace PungusSouls
{
    public class RuntimeFollowerAnimatorAssigner : MonoBehaviour
    {
        public string m_sourcePrefabName = "Player";
        public bool m_rebind = true;

        private bool m_assigned;

        private void Start()
        {
            AssignAnimatorController();
        }

        private void Update()
        {
            if (!m_assigned)
            {
                AssignAnimatorController();
            }
        }

        private void AssignAnimatorController()
        {
            Animator targetAnimator = GetComponentInChildren<Animator>(true);

            if (targetAnimator == null)
            {
                return;
            }

            if (targetAnimator.runtimeAnimatorController != null)
            {
                m_assigned = true;
                return;
            }

            RuntimeAnimatorController controller = FindControllerFromPrefab(m_sourcePrefabName);

            if (controller == null)
            {
                return;
            }

            targetAnimator.runtimeAnimatorController = controller;
            m_assigned = true;

            if (m_rebind)
            {
                targetAnimator.Rebind();
                targetAnimator.Update(0f);
            }
        }

        private static RuntimeAnimatorController FindControllerFromPrefab(string prefabName)
        {
            if (ZNetScene.instance == null)
            {
                return null;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);

            if (prefab == null)
            {
                return null;
            }

            Animator sourceAnimator = prefab.GetComponentInChildren<Animator>(true);

            if (sourceAnimator == null)
            {
                return null;
            }

            return sourceAnimator.runtimeAnimatorController;
        }
    }
}