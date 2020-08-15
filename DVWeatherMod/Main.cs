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
        private const float rainVolume = 1f;
        private static float defaultVolume;
        private static AudioSource env_birds;
        private static bool switchedAudio;
        private static Light sun;
        private static bool killedSun;
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

            rainActive = true;
            killedSun = false;
            switchedAudio = false;

            return true;
        }

        static void SwapMaterial(GameObject g, Shader s)
        {
            Material m = new Material(s);
            ParticleSystemRenderer r = g.GetComponent<ParticleSystemRenderer>();
            m.CopyPropertiesFromMaterial(r.material);
            // m.CopyPropertiesFromMaterial(trail ? r.trailMaterial : r.material);
            // if (trail)
            // r.trailMaterial = m;
            // else
            r.material = m;
        }

        // TODO: This is still probably glitchy.
        static bool OnToggle(UnityModManager.ModEntry _, bool active)
        {
            rainActive = active;
            if (rain != null)
                rain.SetActive(active);
            if (switchedAudio && env_birds != null)
            {
                env_birds.clip = active ? rainSFX : defaultSFX;
                env_birds.volume = active ? rainVolume : defaultVolume;
                env_birds.Play();
            }
            if (killedSun && sun != null)
                sun.gameObject.SetActive(!active);
            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            if (rain == null && rainActive)
            {
                rain = GameObject.Instantiate(rainPrefab);
                locoCollider = rain.transform.Find("LocoCollider").gameObject;
                Transform tf = rain.transform.Find("Rainclouds");
                SwapMaterial(tf.gameObject, customSurfaceShader); // , false);
                tf = rain.transform.Find("Rain");
                SwapMaterial(tf.gameObject, legacyAlphaBlended); // , false);
                Transform tf2 = tf.Find("Ripple");
                SwapMaterial(tf2.gameObject, legacyAlphaBlended); // , false);
                /*tf = tf.transform.Find("Ripple");
                SwapMaterial(tf.gameObject, legacyAlphaBlended, false);
                Transform tf2 = tf.Find("WaterSpray");
                SwapMaterial(tf2.gameObject, legacyAlphaBlended, false);*/
                tf2 = tf.Find("ImpactSplash");
                SwapMaterial(tf2.gameObject, legacyAlphaBlended); // , false);
                mod.Logger.Log("Successfully switched out materials.");
            }
            else if (!switchedAudio && rainActive)
            {
                env_birds = GameObject.Find("env_birds Audio Source")?.GetComponent<AudioSource>();
                if (env_birds == null)
                    return;
                if (defaultSFX == null)
                    defaultSFX = env_birds.clip;
                env_birds.clip = rainSFX;
                defaultVolume = env_birds.volume;
                env_birds.volume = rainVolume;
                env_birds.Play();
                switchedAudio = true;
            }
            else if (!killedSun && rainActive)
            {
                sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
                if (!sun)
                    return;
                sun.gameObject.SetActive(false);
                killedSun = true;
            }

            Transform playerTransform = PlayerManager.PlayerTransform;
            if (playerTransform == null || !rainActive)
                return;

            // TODO: Figure out how world height works.
            rain.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y + 150f, playerTransform.position.z);
            // Hope this isn't too costly.
            // TODO: Set up hitboxes for each loco interior.
            locoCollider.SetActive(PlayerManager.Car != null && PlayerManager.Car.IsLoco);
        }
    }
}