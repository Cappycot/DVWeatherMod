using UnityEngine;
using UnityModManagerNet;

namespace DVWeatherMod
{
    static class Main
    {

        private static UnityModManager.ModEntry mod;
        private static AssetBundle assetBundle;
        private static GameObject rainPrefab;
        private static GameObject rain;
        private static GameObject locoCollider;
        private static Shader customSurfaceShader;
        private static Shader legacyAlphaBlended;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            mod.OnUpdate = OnUpdate;
            assetBundle = AssetBundle.LoadFromFile(mod.Path + "Resources/rain");
            rainPrefab = assetBundle.LoadAsset<GameObject>("Assets/Particle Effects/Rain/Rain.prefab");
            customSurfaceShader = assetBundle.LoadAsset<Shader>("Assets/EffectExamples/Shared/Shaders/SurfaceShader_VC.shader");
            legacyAlphaBlended = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (customSurfaceShader == null)
            {
                mod.Logger.Log("Could not find SurfaceShader_VC");
                return false;
            }
            else if (legacyAlphaBlended == null)
            {
                mod.Logger.Log("Could not find Legacy Alpha Blended shader");
                return false;
            }

            return true;
        }

        private static bool killedSun = false;

        static void SwapMaterial(GameObject g, Shader s, bool trail)
        {
            Material m = new Material(s);
            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            m.CopyPropertiesFromMaterial(trail ? r.trailMaterial : r.material);
            if (trail)
                r.trailMaterial = m;
            else
                r.material = m;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            if (rain == null)
            {
                if (!killedSun)
                {
                    // Light light2 = UnityEngine.Object.FindObjectsOfType<Light>().FirstOrDefault((Light light) => light.type == LightType.Directional && light.name == "Directional Light");
                    Light light2 = GameObject.Find("Directional Light")?.GetComponent<Light>();
                    if (!light2)
                    {
                        return;
                    }
                    light2.gameObject.SetActive(false);
                    killedSun = true;
                }
                else
                {
                    rain = GameObject.Instantiate(rainPrefab);
                    locoCollider = rain.transform.Find("LocoCollider").gameObject;
                    Transform tf = rain.transform.Find("Rainclouds");
                    SwapMaterial(tf.gameObject, customSurfaceShader, false);
                    tf = rain.transform.Find("Rain");
                    SwapMaterial(tf.gameObject, legacyAlphaBlended, true);
                    tf = tf.transform.Find("Ripple");
                    SwapMaterial(tf.gameObject, legacyAlphaBlended, false);
                    Transform tf2 = tf.Find("WaterSpray");
                    SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);
                    tf2 = tf.Find("ImpactSplash");
                    SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);
                    mod.Logger.Log("Successfully switched out materials.");
                }
            }

            if (PlayerManager.PlayerTransform == null)
                return;

            rain.transform.position = PlayerManager.PlayerTransform.position;
            // Hope this isn't too costly.
            locoCollider.SetActive(PlayerManager.Car != null && PlayerManager.Car.IsLoco);
        }
    }
}