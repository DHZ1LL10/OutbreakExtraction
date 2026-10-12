using Outbreak.Items;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Outbreak
{
    public sealed class ItemPreviewRenderer : MonoBehaviour
    {
        public const int PreviewLayer=31;
        private Camera previewCamera;
        private Light previewLight;
        private Transform modelRoot;
        private GameObject current;
        private RenderTexture target;
        private ItemPreviewLibrary library;
        private ItemDefinition selected;
        private RawImage output;
        public void Show(ItemDefinition item,RawImage image)
        {
            // Headless lifecycle/tests still construct all screens; rendering is a manual GPU check.
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) { image.gameObject.SetActive(false); return; }
            if(previewCamera==null) Initialize();
            if(output!=null && output!=image) output.texture=null;
            output=image;
            previewCamera.aspect=Mathf.Max(.1f,image.rectTransform.rect.width/Mathf.Max(1,image.rectTransform.rect.height));
            var model=library!=null?library.Find(item):null;
            if(selected!=item || current==null)
            {
                if(current!=null) { current.SetActive(false); Destroy(current); }
                selected=item; current=null;
                if(model!=null)
                {
                    // An inactive parent prevents Awake/OnEnable on imported behaviours before stripping.
                    modelRoot.gameObject.SetActive(false);
                    current=Instantiate(model,modelRoot,false);
                    foreach(var behaviour in current.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(behaviour);
                    foreach(var collider in current.GetComponentsInChildren<Collider>(true)) DestroyImmediate(collider);
                    foreach(var body in current.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(body);
                    foreach(var animator in current.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
                    foreach(var light in current.GetComponentsInChildren<Light>(true)) light.enabled=false;
                    foreach(var camera in current.GetComponentsInChildren<Camera>(true)) camera.enabled=false;
                    foreach(var audio in current.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                    foreach(var particle in current.GetComponentsInChildren<ParticleSystem>(true)) particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    foreach(var t in current.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=PreviewLayer;
                    foreach(var renderer in current.GetComponentsInChildren<Renderer>(true)) { renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false; }
                    current.transform.localPosition=Vector3.zero; current.transform.localRotation=Quaternion.identity;
                    modelRoot.rotation=Quaternion.Euler(0,item.Id=="debug_rifle"?75:25,0); modelRoot.gameObject.SetActive(true);
                    var renderers=current.GetComponentsInChildren<Renderer>();
                    if(renderers.Length>0)
                    {
                        var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        float radius=Mathf.Max(.03f,bounds.extents.magnitude);
                        current.transform.position+=modelRoot.position-bounds.center;
                        // Imported models use different units. Normalize only the isolated preview copy.
                        current.transform.localPosition/=radius; current.transform.localScale/=radius; radius=1;
                        previewCamera.transform.position=modelRoot.position+new Vector3(0,radius*.18f,-radius*3.1f);
                        previewCamera.transform.LookAt(modelRoot.position); previewCamera.nearClipPlane=Mathf.Max(.001f,radius*.01f);
                        previewCamera.farClipPlane=radius*10+1;
                        previewLight.transform.position=modelRoot.position+new Vector3(radius,radius,-radius*1.7f);
                        previewLight.range=radius*8;
                    }
                }
            }
            image.texture=current!=null?target:null; image.gameObject.SetActive(current!=null);
            previewCamera.enabled=current!=null; previewLight.enabled=current!=null;
        }
        private void Initialize()
        {
            library=Resources.Load<ItemPreviewLibrary>("ItemPreviews");
            var root=new GameObject("Isolated preview root"); root.transform.SetParent(transform,false); root.transform.position=new Vector3(10000,10000,10000); modelRoot=root.transform;
            var cameraObject=new GameObject("Preview camera",typeof(Camera)); cameraObject.transform.SetParent(transform,false);
            previewCamera=cameraObject.GetComponent<Camera>(); previewCamera.fieldOfView=38; previewCamera.cullingMask=1<<PreviewLayer;
            previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=GameUI.CardColor; previewCamera.allowHDR=false; previewCamera.enabled=false;
            target=new RenderTexture(512,384,16,RenderTextureFormat.ARGB32) { name="Selected item preview" }; target.Create(); previewCamera.targetTexture=target;
            var lamp=new GameObject("Preview light",typeof(Light)); lamp.transform.SetParent(transform,false);
            // A distant point light cannot compete with the world's URP main directional light.
            previewLight=lamp.GetComponent<Light>(); previewLight.type=LightType.Point; previewLight.cullingMask=1<<PreviewLayer; previewLight.intensity=6; previewLight.shadows=LightShadows.None;
            previewLight.enabled=false;
        }
        private void Update()
        {
            bool visible=output!=null && output.gameObject.activeInHierarchy && current!=null;
            if(previewCamera!=null) { previewCamera.enabled=visible; previewLight.enabled=visible; }
            if(visible) modelRoot.Rotate(0,Time.unscaledDeltaTime*8,0,Space.World);
        }
        public void Hide() { if(output!=null) output.texture=null; output=null; if(previewCamera!=null) previewCamera.enabled=false; if(previewLight!=null) previewLight.enabled=false; }
        private void OnDisable() => Hide();
        private void OnDestroy() { if(target!=null) { target.Release(); Destroy(target); } }
    }
}
