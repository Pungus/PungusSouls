using System.Collections;
using UnityEngine;

public class PrefabInitializer : MonoBehaviour
{
    private static bool _initialized;

    private void Start()
    {
        if (_initialized)
            return;

        _initialized = true;
        StartCoroutine(WaitForZNetScene());
    }

    private IEnumerator WaitForZNetScene()
    {
        while (ZNetScene.instance == null)
            yield return null;

        yield return new WaitForSeconds(2f);

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab == null)
                continue;

            if (prefab.name == "Sif")
            {
                //Debug.Log("[Agent] Attaching agent system to prefab " + prefab.name);
                NpcInstaller.AttachAgentSystem(prefab);
                break;
            }
        }
    }
}