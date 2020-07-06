using UnityEngine;
using UnityModManagerNet;

namespace DVWeatherMod
{
    static class Main
    {

        private static UnityModManager.ModEntry mod;
        private static bool rainActive;
        private static AssetBundle assetBundle;
        private static GameObject rainPrefab;
        private static GameObject rain;
        private static GameObject locoCollider;
        private static AudioClip rainSFX;
        private static AudioClip defaultSFX;
        private static AudioSource env_birds;
        private static bool switchedAudio;
        private static Shader customSurfaceShader;
        private static Shader legacyAlphaBlended;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            mod.OnToggle = OnToggle;
            mod.OnUpdate = OnUpdate;
            assetBundle = AssetBundle.LoadFromFile(mod.Path + "Resources/rain");
            rainPrefab = assetBundle.LoadAsset<GameObject>("Assets/Particle Effects/Rain/Rain.prefab");
            customSurfaceShader = assetBundle.LoadAsset<Shader>("Assets/Packages/EffectExamples/Shared/Shaders/SurfaceShader_VC.shader");
            rainSFX = assetBundle.LoadAsset<AudioClip>("Assets/Particle Effects/Rain/RainSFX.ogg");
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
            else if (rainSFX == null)
            {
                mod.Logger.Log("Could not find RainSFX AudioClip");
                return false;
            }

            killedSun = false;
            rainActive = true;
            switchedAudio = false;

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

        // TODO: This is still probably glitchy.
        static bool OnToggle(UnityModManager.ModEntry _, bool active)
        {
            rainActive = active;
            if (killedSun)
            {
                Light light2 = GameObject.Find("Directional Light")?.GetComponent<Light>();
                if (!light2)
                {
                    return false;
                }
                light2.gameObject.SetActive(true);
                killedSun = false;
            }
            if (rain != null)
                rain.SetActive(active);
            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            if (rain == null && rainActive)
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
                    return; // Make rain in next frame.
                }
                else
                {
                    rain = GameObject.Instantiate(rainPrefab);
                    locoCollider = rain.transform.Find("LocoCollider").gameObject;
                    Transform tf = rain.transform.Find("Rainclouds");
                    SwapMaterial(tf.gameObject, customSurfaceShader, false);
                    tf = rain.transform.Find("Rain");
                    SwapMaterial(tf.gameObject, legacyAlphaBlended, true);
                    Transform tf2 = tf.Find("Ripple");
                    SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);
                    /*tf = tf.transform.Find("Ripple");
                    SwapMaterial(tf.gameObject, legacyAlphaBlended, false);
                    Transform tf2 = tf.Find("WaterSpray");
                    SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);*/
                    tf2 = tf.Find("ImpactSplash");
                    SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);
                    mod.Logger.Log("Successfully switched out materials.");
                }
            }
            else if (!switchedAudio && rainActive)
            {
                env_birds = GameObject.Find("env_birds Audio Source")?.GetComponent<AudioSource>();
                if (env_birds == null)
                    return;
                if (defaultSFX == null)
                    defaultSFX = env_birds.clip;
                env_birds.clip = rainSFX;
                env_birds.volume = 1f;
                env_birds.Play();
                switchedAudio = true;
            }

            if (PlayerManager.PlayerTransform == null || !rainActive)
                return;

            rain.transform.position = PlayerManager.PlayerTransform.position;
            // Hope this isn't too costly.
            // TODO: Set up hitboxes for each loco interior.
            locoCollider.SetActive(PlayerManager.Car != null && PlayerManager.Car.IsLoco);
        }
    }
}