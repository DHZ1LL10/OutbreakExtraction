using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Outbreak.Weapons
{
    // Replaceable placeholder consumer. Fixed pools; no per-shot GameObject/material allocation.
    [DefaultExecutionOrder(200), RequireComponent(typeof(FirearmController))]
    public sealed class WeaponEffects : MonoBehaviour
    {
        [SerializeField] private Material flashMaterial, shellMaterial, worldImpactMaterial, targetImpactMaterial;
        [SerializeField, Range(4, 64)] private int shellPoolSize = 24;
        [SerializeField, Range(4, 64)] private int impactPoolSize = 24;
        [SerializeField, Range(0.02f, 0.08f)] private float flashDuration = 0.035f;
        [SerializeField, Range(0.5f, 4)] private float shellLifetime = 2.2f;
        [SerializeField, Range(0.1f, 2)] private float impactLifetime = 0.4f;
        private FirearmController firearm;
        private GameObject poolRoot, flash;
        private Light flashLight;
        private Transform flashAnchor;
        private float flashUntil;
        private Rigidbody[] shells;
        private GameObject[] impacts;
        private float[] shellUntil, impactUntil;
        private int nextShell, nextImpact;
        private void Awake()
        {
            firearm = GetComponent<FirearmController>();
            poolRoot = new GameObject("Gunplay temporary effects (bounded pool)");
            SceneManager.MoveGameObjectToScene(poolRoot, gameObject.scene);
            flash = Primitive("Muzzle flash", flashMaterial);
            flashLight = flash.AddComponent<Light>(); flashLight.type = LightType.Point;
            flashLight.color = new Color(1, 0.65f, 0.2f); flashLight.range = 2; flashLight.shadows = LightShadows.None;
            shells = new Rigidbody[Mathf.Clamp(shellPoolSize, 4, 64)]; shellUntil = new float[shells.Length];
            for (int i = 0; i < shells.Length; i++)
            {
                var shell = Primitive("Shell " + i, shellMaterial);
                shells[i] = shell.AddComponent<Rigidbody>();
                shells[i].mass = 0.008f; shells[i].detectCollisions = false;
            }
            impacts = new GameObject[Mathf.Clamp(impactPoolSize, 4, 64)]; impactUntil = new float[impacts.Length];
            for (int i = 0; i < impacts.Length; i++) impacts[i] = Primitive("Impact " + i, worldImpactMaterial);
        }
        private GameObject Primitive(string label, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label; go.layer = 2;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            go.transform.SetParent(poolRoot.transform, false);
            var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            go.SetActive(false); return go;
        }
        private void OnEnable() { firearm.OnShotFeedback += Shot; firearm.OnImpact += Impact; }
        private void OnDisable()
        {
            firearm.OnShotFeedback -= Shot; firearm.OnImpact -= Impact;
            if (poolRoot != null) foreach (Transform child in poolRoot.transform) child.gameObject.SetActive(false);
            flashAnchor = null; flashUntil = 0;
        }
        private void Shot(WeaponShotFeedback shot)
        {
            float intensity = Mathf.Clamp(shot.FlashMultiplier, 0, 4);
            flashAnchor = shot.Muzzle; flashUntil = Time.time + flashDuration;
            flash.SetActive(intensity > 0.001f);
            float size = 0.028f * Mathf.Sqrt(intensity) * Random.Range(0.8f, 1.15f) * Mathf.Lerp(1, 0.7f, shot.Aim);
            flash.transform.localScale = new Vector3(size, size, size * 2);
            flashLight.intensity = 1.8f * intensity;
            int i = nextShell++ % shells.Length;
            var shell = shells[i]; bool rifle = shot.State.Definition.weaponClass == WeaponClass.Rifle;
            shell.gameObject.SetActive(true);
            shell.transform.localScale = rifle ? new Vector3(0.009f, 0.009f, 0.034f) : new Vector3(0.008f, 0.008f, 0.018f);
            shell.position = shot.EjectionPosition; shell.rotation = shot.EjectionRotation;
            shell.velocity = shot.EjectionRotation * new Vector3(Random.Range(1.1f, 1.7f), rifle ? 1.1f : 0.8f, Random.Range(-0.35f, 0.1f));
            shell.angularVelocity = Random.onUnitSphere * Random.Range(8, 18);
            shell.WakeUp(); shellUntil[i] = Time.time + shellLifetime;
        }
        private void Impact(WeaponImpactFeedback impact)
        {
            int i = nextImpact++ % impacts.Length;
            var go = impacts[i]; go.SetActive(true);
            go.transform.SetPositionAndRotation(impact.Position + impact.Normal * 0.006f, Quaternion.LookRotation(impact.Normal));
            go.transform.localScale = Vector3.one * (impact.HitDamageable ? 0.055f : 0.035f);
            go.GetComponent<Renderer>().sharedMaterial = impact.HitDamageable ? targetImpactMaterial : worldImpactMaterial;
            impactUntil[i] = Time.time + impactLifetime;
        }
        private void LateUpdate()
        {
            if (flash.activeSelf)
            {
                if (Time.time >= flashUntil || flashAnchor == null || !flashAnchor.gameObject.activeInHierarchy) flash.SetActive(false);
                else flash.transform.SetPositionAndRotation(flashAnchor.position, flashAnchor.rotation * Quaternion.Euler(0, 0, Random.Range(0, 90)));
            }
            for (int i = 0; i < shells.Length; i++)
                if (shells[i].gameObject.activeSelf && Time.time >= shellUntil[i]) shells[i].gameObject.SetActive(false);
            for (int i = 0; i < impacts.Length; i++)
                if (impacts[i].activeSelf)
                {
                    if (Time.time >= impactUntil[i]) impacts[i].SetActive(false);
                    else
                    {
                        float remaining = Mathf.Clamp01((impactUntil[i] - Time.time) / impactLifetime);
                        var size = impacts[i].transform.localScale;
                        impacts[i].transform.localScale = new Vector3(size.x, size.y, 0.002f + remaining * 0.025f);
                    }
                }
        }
        private void OnDestroy() { if (poolRoot != null) Destroy(poolRoot); }
    }
}
