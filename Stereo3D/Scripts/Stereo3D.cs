
// Stereoscopic 3D system by Vital Volkov
// Usage:
// Add this script to any Camera to make it Stereoscopic.
// 1) The main parameter for the correct size of `User IPD`(Interpupillary distance) in millimeters is `PPI`(Pixels Per Inch) or `Pixel Pitch`(distance between pixel centers) of the screen. Required for precision to calculate screen width.
// The system will try to autodetect `PPI` of the screen (In my case PPI = 96 on 23 inch LG D2342P monitor). If correct `PPI` failed autodetected from the screen then find one of two in Tech Specs of your screen and set it manually.
// 2) If PPI or Pixel Pitch is set correctly then "User IPD" will have real distance in millimeters and for precision realistic Stereo3D vision, it must be set the same as user real IPD.
// If you don't know your IPD you can measure it with a mirror and ruler - put a ruler on the mirror in front of your face. Close right eye and move such that left eye pupillary look at themself through Zero mark on a scale of Ruler, at this moment, is important to stay still, close the left eye and open the right eye, and look at the right eye pupillary in the mirror through the Ruler scale and you will see your IPD in millimeters. 
// 3) Select the Stereo 3D Method. Set your real `User IPD` in the Stereo 3D system and go. If you don't see Stereo 3D then toggle `Swap Left-Right Cameras`. If you want to see virtual reality in a different size feel then uncheck the `Match User IPD` mark and set `Virtual IPD` larger than `User IPD` for toy world and vise versa.
// 4) `Screen Distance` shows the distance between eyes and screen where real FOV(Field Of View) will match the virtual FOV. So, measure the distance from your eyes position to screen, tune FOV till `Screen Distance` matches the measured one and you get the most realistic view.
// 5) Default shortcut Keys: `Tab` Show/Hide S3D settings panel. Numpad `*` On/Off Stereo3D and `Left Ctrl + *` swap left-right cameras. `+`,`-` FOV tune. `Ctrl` + `+`,`-` Virtual IPD tune if unlocked from `User IPD`(`Match User IPD` unchecked). Hold `Shift` for a faster tune.
// Tested on Unity 2018, 2019 and 2020 with default render + `Post Processing Stack v2`, URP, and HDRP.
// Enjoy.

using UnityEngine;
using UnityEngine.Rendering;
#if URP
using UnityEngine.Rendering.Universal;
#elif HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Reflection;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

//using System.ComponentModel;
//using UnityEngine.InputSystem.Composites;
//using UnityEngine.InputSystem.Layouts;
//using UnityEngine.InputSystem.Utilities;

//#if UNITY_EDITOR
//using UnityEditor;
//[InitializeOnLoad] // Automatically register in editor.
//#endif

//[DisplayStringFormat("{modifier}+{negative}/{positive}")]
//[DisplayName("Positive/Negative Binding with Modifier")]
//public class AxisModifierComposite : AxisComposite
//{
//    // Each part binding is represented as a field of type int and annotated with
//    // InputControlAttribute. Setting "layout" restricts the controls that
//    // are made available for picking in the UI.
//    //
//    // On creation, the int value is set to an integer identifier for the binding
//    // part. This identifier can read values from InputBindingCompositeContext.
//    // See ReadValue() below.
//    [InputControl(layout = "Button")] public int modifier;

//    // This method computes the resulting input value of the composite based
//    // on the input from its part bindings.
//    public override float ReadValue(ref InputBindingCompositeContext context)
//    {
//        if (context.ReadValueAsButton(modifier))
//            return base.ReadValue(ref context);
//        return default;
//    }

//    // This method computes the current actuation of the binding as a whole.
//    public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
//    {
//        if (context.ReadValueAsButton(modifier))
//            return base.EvaluateMagnitude(ref context);
//        return default;
//    }

//    static AxisModifierComposite()
//    {
//        InputSystem.RegisterBindingComposite<AxisModifierComposite>();
//    }

//    [RuntimeInitializeOnLoadMethod]
//    static void Init() { } // Trigger static constructor.
//}
#endif

#if STARTER_ASSETS_PACKAGES_CHECKED
using StarterAssets;
#endif

#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

public class Stereo3D : MonoBehaviour
{
    public enum Method {Interlace_Horizontal, Interlace_Vertical, Interlace_Checkerboard, SideBySide, SideBySide_HMD, OverUnder, Anaglyph_RedCyan, Anaglyph_RedBlue, Anaglyph_GreenMagenta, Anaglyph_AmberBlue};
    public enum InterlaceType {Horizontal, Vertical, Checkerboard};
    public enum EyePriority {Left, Center, Right};

    [Header("Settings")]
    public bool S3DEnabled = true; //mono or Stereo3D enabled
    public EyePriority eyePriority = EyePriority.Center; //for which of eye parent camera renders: left, right or center-symmetric(important for sight aiming in VR)
    public bool swapLR; //swap left-right cameras
    public bool optimize; //optimize render due full resolution is not using per eye
    public bool vSync = true; //required for OverUnder S3D method
    public Method method = Method.Interlace_Horizontal; //Stereo3D output method
    //public InterlaceType interlaceType = InterlaceType.Horizontal; //Type of Interlace Stereo3D output method
    public float PPI = 96; //how many Pixels Per Inch screen have to correct real screen size calculation(see tech specs of the screen and set PPI or pixel pitch)
    //public float pixelPitch = .265f; //distance between pixels centers in mm. If no PPI then pixel pitch must be in the tech specs of the screen
    public float userIPD = 66; //an important setting in mm for correct Stereo3D. The user should set his REAL IPD(Interpupillary Distance) and REAL screen size via PPI or pixel pitch to match real millimeters
    public float virtualIPD = 66; //virtual IPD can be different from user IPD to see the world in different size feel as another creature or armed eyes by binoculars or other stereoscopic optics with a different stereo base
    public float virtualIPDMax = 1000; //max value of the virtual IPD
    public bool matchUserIPD = true; //set virtual IPD match to User IPD to a realistic view of the naked eye
    public bool FOVControl = true;
    public float FOV = 90; //horizontal Field Of View
    public Vector2 FOVMinMax = new Vector2(10, 170);
    //public float panelDepth = 1; //panelDepth as screenDistance multiplyer
    public float panelDepth = 0; //panelDepth as image offset(if GUIAsOverlay = true) or as screenDistance multiplyer(if GUIAsOverlay = false)
    //public Vector2 panelDepthMinMax = new Vector2(1, 100);
    public Vector2 panelDepthMinMax = new Vector2(0, 1);
    public bool GUIAsOverlay = true; //(S3D panel renders with it own camera as overlay on render texture or S3D panel renders by scene S3D cameras)
    public bool GUISizeKeep = true; //keep size of the canvas while resize window
    public bool GUIOpened = true; //GUI window visible or not on the start
    public float GUIAutoshowTime = 3; //automatically show GUI duration in seconds when S3D setting changes by hotkeys
    public float toolTipShowDelay = 3; //delay in seconds after mouse stop while hovering before toolTip shows
    //public Color anaglyphLeftColor = Color.red; //tweak colors at runtime to best match different goggles
    //public Color anaglyphRightColor = Color.cyan;
    public bool cloneCamera = true;
    public GameObject cameraPrefab; //if empty, Stereo3D cameras are copies of the main cam. Set prefab if need custom settings &/or components
    public RenderTextureFormat RTFormat = RenderTextureFormat.DefaultHDR; //DefaultHDR(16bitFloat) be able to contain Post Process Effects and give fps gain from 328 to 343. In my case RGB111110Float is fastest - 346fps.
    public bool setMatrixDirectly = true; //shift image Vanish points to User IPD directly via camera Matrix(gives fps boost) or via camera's "physically" settings "lensShift"(required for Post Processing Stack V2 pack as it resets matrix and yields incorrect aspect)
    public string slotName = "User1";
    public bool loadSettingsFromFile = true; //autoload settings at start from last saved file
    public bool nearClipHack; //Hack for FPS gain in SRP(Scriptable Render Pipeline) and required for custom Viewport Rect but can cause terrain gaps(Test Track Demo in Unity 2021)
    public bool matrixKillHack; //Hack for FPS gain from 308 to 328 and required to fix terrain gaps caused by nearClipHack(Test Track Demo in Unity 2021)
    //public Canvas canvas;
    //public CameraDataStruct cameraDataStruct;

#if ENABLE_INPUT_SYSTEM
    public InputAction GUIAction;
    public InputAction S3DAction;
    public InputAction FOVAction;
    public InputAction modifier1Action;
    public InputAction modifier2Action;
    public InputAction modifier3Action;
    //bool inputSystem = true;
    bool cursorLockedDefault;
    bool cursorInputForLookDefault;
    //InputActionAsset S3DInputActionAsset;
    PlayerInput playerInput;
#else
    public KeyCode GUIKey = KeyCode.Tab; //GUI window show/hide Key
    public KeyCode S3DKey = KeyCode.KeypadMultiply; //S3D enable/disable shortcut Key and hold "LeftControl" Key to swap left-right cameras
    public KeyCode increaseKey = KeyCode.KeypadPlus; //increase Field Of View shortcut Key + hold "Shift" Key to faster change + hold "LeftControl" Key to increase virtual IPD if "matchUserIPD" unchecked
    public KeyCode decreaseKey = KeyCode.KeypadMinus; //decrease Field Of View shortcut Key + hold "Shift" Key to faster change + hold "LeftControl" Key to decrease virtual IPD if "matchUserIPD" unchecked
#endif

    //public Camera[] additionalS3DCameras;
    public List<Camera> additionalS3DCameras;
    //public AdditionalS3DCamera[] additionalS3DCamerasStruct;

#if STARTER_ASSETS_PACKAGES_CHECKED
    StarterAssetsInputs starterAssetsInputs;
#endif

    [Header("Info")]
    public Material S3DMaterial; //generated material
    Material S3DPanelMaterial; //generated material

    Camera cam;
    //Camera camClone;
    Camera leftCam;
    Camera rightCam;
    int cullingMask;
    float nearClip;
    float sceneNearClip;
    float farClip;
    float sceneFarClip;
    //float canvasNearClip;
    Vector2[] verticesPos = new Vector2[4];
    Vector2[] verticesUV = new Vector2[4];

    bool defaultRender;
    float canvasLocalPosZ;
    bool lastGUIOpened;
    bool lastS3DEnabled;
    EyePriority lastEyePriority;
    bool lastSwapLR;
    bool lastOptimize;
    bool lastVSync;
    Method lastMethod;
    float lastUserIPD;
    float lastVirtualIPD;
    float lastVirtualIPDMax;
    bool lastMatchUserIPD;
    float lastPPI;
    //float lastPixelPitch;
    float lastFOV;
    Vector2 lastMinMaxFOV;
    float lastPanelDepth;
    Vector2 lastPanelDepthMinMax;
    bool lastGUIAsOverlay;
    bool lastGUISizeKeep;
    GameObject lastCameraPrefab;
    RenderTextureFormat lastRTFormat;
    bool lastSetMatrixDirectly;
    //InterlaceType lastInterlaceType;
    //Color lastAnaglyphLeftColor;
    //Color lastAnaglyphRightColor;
    string lastSlotName;
    Rect lastCamRect;
    bool lastNearClipHack;
    bool lastMatrixKillHack;
    float lastCanvasLocalPosZ;
    bool lastCloneCamera;
    CameraDataStruct lastCameraDataStruct;
    bool lastLoadSettingsFromFile;
    //List<Camera> lastAdditionalS3DCameras;
    Camera[] lastCameraStack;
    Camera[] lastAdditionalS3DCameras;
    //AdditionalS3DCamera lastAdditionalS3DCameras;
    Canvas canvas;
    Camera canvasRayCam;
    Camera canvasCam;
    Matrix4x4 canvasCamMatrix;
    RectTransform cursorRectTransform;
    //Transform cursorTransform;
    Vector2 canvasDefaultSize;
    Vector2 canvasSize;
    Vector2 windowSize = new Vector2(Screen.width, Screen.height);
    //Vector2 viewportSize;
    Transform panel;
    Toggle enableS3D_toggle;
    Toggle swapLR_toggle;
    Toggle optimize_toggle;
    Toggle vSync_toggle;
    //Button interlace_button;
    //Button vertical_button;
    //Button checkerboard_button;
    //Button sideBySide_button;
    //Button overUnder_button;
    //Button anaglyph_button;
    InputField PPI_inputField;
    CanvasRenderer caret;
    Slider userIPD_slider;
    InputField userIPD_inputField;
    Slider virtualIPD_slider;
    InputField virtualIPD_inputField;
    Slider FOV_slider;
    InputField FOV_inputField;
    Toggle matchUserIPD_toggle;
    Slider panelDepth_slider;
    InputField panelDepth_inputField;
    InputField screenDistance_inputField;
    InputField slotName_inputField;
    Button save_button;
    Button load_button;
    Dropdown slotName_dropdown;
    Dropdown outputMethod_dropdown;
    GameObject tooltip;
    RectTransform tooltipBackgroundRect;
    Text tooltipText;
    Text FPSText;
    EventTrigger trigger;
    EventTrigger.Entry entry;
    float GUI_autoshowTimer;
    bool tooltipShow;
    float toolTipTimer;
    //public GraphicRaycaster raycaster;
    //PointerEventData pointerEventData;
    //public EventSystem eventSystem;
    //public StandaloneInputModule iModule;
    //public BaseInput bInput;
    //bool nearClipHackApplied;
    CursorLockMode cursorLockModeDefault;
#if URP
    UniversalAdditionalCameraData camData;
    //UniversalRenderPipelineAsset URPAsset;
    //UniversalRenderPipelineAsset lastURPAsset;
#elif HDRP
    //public HDAdditionalCameraData canvasCamData;
    //HDAdditionalCameraData HDCamData;
    //HDAdditionalCameraData HDCamDataCopy;
    //HDAdditionalCameraData HDCamDataCopy2;
    //HDCamera HDCam;
    //HDCamera HDCam2;
    //HDCamera HDCamCopy;
    HDAdditionalCameraData camData;
    //HDAdditionalCameraData leftCamData;
    //HDAdditionalCameraData rightCamData;
    HDRenderPipelineAsset HDRPAsset;
    RenderPipelineSettings HDRPSettings;
    RenderPipelineSettings defaultHDRPSettings;
    //RenderPipelineSettings.ColorBufferFormat defaultColorBufferFormat;
#endif
    //UniversalAdditionalCameraData camCloneData;
    //List<UniversalAdditionalCameraData> oldList;
    //List<UniversalAdditionalCameraData> newList;
    CameraDataStruct cameraDataStruct;
    List<Camera> cameraStack;
    List<Camera> leftCameraStack;
    List<Camera> rightCameraStack;
    //List<Camera> leftRightCameraStack;
    //Vector2 panel3DdepthMinMax;
    //Vector2 panelOverlayDepthMinMax;
    AdditionalS3DCamera[] additionalS3DCamerasStruct;
    //bool loaded;

#if UNITY_POST_PROCESSING_STACK_V2
    PostProcessLayer PPLayer;
    bool PPLayerEnabled;
#endif

#if CINEMACHINE
    bool cineBrain;
    Cinemachine.ICinemachineCamera vCam;
    Cinemachine.ICinemachineCamera defaultVCam;
    //Cinemachine.CinemachineBrain brain;
    //bool vCamSceneClipSetInProcess;
    bool vCamSceneClipIsReady;
    //bool vCamClipSetInProcess;
    //bool vCamSceneClipRestoreInProcess;
    bool nearClipHackApplied;
#endif

#if LookWithMouse
    LookWithMouse lookWithMouseScript;
#endif

#if SimpleCameraController
    UnityTemplateProjects.SimpleCameraController simpleCameraControllerScript;
#endif

    float GUI_Set_delay;

    public void Awake()
    {
        //Debug.Log("Awake " + name);
        GUI_Set_delay = Time.deltaTime * 2;

        if (!name.Contains("(Clone)"))
        {
#if ENABLE_INPUT_SYSTEM
            //Debug.Log(GUIAction.bindings.Count);

            if (GUIAction.bindings.Count == 0)
                GUIAction.AddBinding("<Keyboard>/tab");

            if (S3DAction.bindings.Count == 0)
                S3DAction.AddBinding("<Keyboard>/numpadMultiply");

            if (FOVAction.bindings.Count == 0)
                FOVAction.AddCompositeBinding("1DAxis")
                    .With("Positive", "<Keyboard>/numpadPlus")
                    .With("Negative", "<Keyboard>/numpadMinus");

            if (modifier1Action.bindings.Count == 0)
                modifier1Action.AddBinding("<Keyboard>/leftShift");

            if (modifier2Action.bindings.Count == 0)
                modifier2Action.AddBinding("<Keyboard>/leftCtrl");

            if (modifier3Action.bindings.Count == 0)
                modifier3Action.AddBinding("<Keyboard>/leftAlt");

            //Debug.Log(modifier3Action.bindings[0].effectivePath);

            GUIAction.performed += context => { OnGUIAction(context); };
            //S3DAction.performed += context => { OnS3DAction(context); };
            //FOVAction.performed += context => { OnVirtualIPDAction(context); };
            //modifier1Action.performed += context => { OnModifier1Action(context); };
            //modifier2Action.performed += context => { OnModifier2Action(context); };
#endif
        }
    }

    //GameObject selectedObject;

    void OnEnable()
    {
        //Debug.Log("OnEnable " + name);
        //Debug.Log("OnEnable panelDepth " + panelDepth);

        if (!name.Contains("(Clone)"))
        {
            Debug.Log("OnEnable");
            //Debug.Log("windowSize " + windowSize);
            //List<Type> types = GetAllTypesInAssembly(new string[] { "Assembly-CSharp" });
            //Debug.Log(types.Count);

            //foreach (var type in types)
            //{
            //    Debug.Log(type.Name);

            //    //if (type.Name.Contains("LookWithMouse", StringComparison.CurrentCultureIgnoreCase))
            //    //    Debug.Log(type.AssemblyQualifiedName);
            //}

            //Debug.Log(types.Exists(x => x.Name.Contains("mouse", StringComparison.CurrentCultureIgnoreCase)));
            //var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();

            //foreach (var assemble in assemblies)
            //{
            //    Debug.Log(assemble.name);

            //    if (assemble.name.Contains("mouse", StringComparison.CurrentCultureIgnoreCase))
            //        Debug.Log("***********************************");
            //}

            //if (Type.GetType("LookWithMouse") != null)
            //    Debug.Log("LookWithMouse");

            if (GraphicsSettings.defaultRenderPipeline == null)
                defaultRender = true;
            //else
            //    if (GraphicsSettings.defaultRenderPipeline.name.Contains("URP"))
            //        AddDefine("URP");

            S3DMaterial = new Material(Shader.Find("Stereo3D Screen Quad"));
            //S3DMaterial.SetColor("_LeftCol", anaglyphLeftColor);
            //S3DMaterial.SetColor("_RightCol", anaglyphRightColor);
            //S3DPanelMaterial = new Material(Shader.Find("UI/Default_Mod"));
            //S3DPanelMaterial = (Material)Resources.Load("3D_Panel", typeof(Material));
            S3DPanelMaterial = Resources.Load<Material>("3D_Panel");

            cam = GetComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cullingMask = cam.cullingMask;

#if CINEMACHINE
        //if (GetComponent<Cinemachine.CinemachineBrain>().enabled)
        //{
        //    //brain = GetComponent<Cinemachine.CinemachineBrain>();
        //    cineBrain = true;
        //    Invoke("GetVCam", Time.deltaTime);
        //}
        //else
        //    sceneNearClip = cam.nearClipPlane;

        if (GetComponent<Cinemachine.CinemachineBrain>() && GetComponent<Cinemachine.CinemachineBrain>().enabled)
        {
            //brain = GetComponent<Cinemachine.CinemachineBrain>();
            cineBrain = true;
            //vCamSceneClipSetInProcess = true;
            vCamSceneClipIsReady = false;
            Invoke("GetVCam", Time.deltaTime);
            //GetVCam();
        }
        else
#endif
            {
                sceneNearClip = cam.nearClipPlane;
                Debug.Log("********************************************************************************************************************sceneNearClip = " + sceneNearClip);
                sceneFarClip = cam.farClipPlane;
            }

            //Debug.Log("OnEnable sceneNearClip " + sceneNearClip);

#if UNITY_POST_PROCESSING_STACK_V2
        if (GetComponent<PostProcessLayer>())
        {
            PPLayer = GetComponent<PostProcessLayer>();
            PPLayerEnabled = PPLayer.enabled;

            if (PPLayerEnabled)
                setMatrixDirectly = false;
        }
#endif

            //#if ENABLE_INPUT_SYSTEM
            //        GUIAction.Enable();
            //        S3DAction.Enable();
            //        FOVAction.Enable();
            //        modifier1Action.Enable();
            //        modifier2Action.Enable();
            //        modifier3Action.Enable();
            //#endif

            //cullingMask = cam.cullingMask;
            //nearClip = cam.nearClipPlane;
            //sceneNearClip = cam.nearClipPlane;

            if (cameraPrefab)
            {
                leftCam = Instantiate(cameraPrefab, transform.position, transform.rotation).GetComponent<Camera>();
                leftCam.name = "leftCam";
                rightCam = Instantiate(cameraPrefab, transform.position, transform.rotation).GetComponent<Camera>();
                rightCam.name = "rightCam";
            }
            else
            {
                if (cloneCamera)
                {
                    //camClone = Instantiate(cam, transform.position, transform.rotation);
                    //camData = cam.GetUniversalAdditionalCameraData();
                    ////oldList = new List<UniversalAdditionalCameraData>();
                    //////oldList.Add(cam.GetComponent<UniversalAdditionalCameraData>());
                    ////oldList.Add(camClone.GetUniversalAdditionalCameraData());
                    ////newList = new List<UniversalAdditionalCameraData>();
                    ////newList.Add(cam.GetUniversalAdditionalCameraData());
                    //////Debug.Log(newList.Count);
                    //////Debug.Log(camCloneData.renderPostProcessing);
                    //camCloneData = camClone.GetUniversalAdditionalCameraData();
                    ////oldList[0] = newList[0];

                    ////var obj1 = new GameObject("obj1");
                    //////obj1.AddComponent<Camera>();
                    ////var obj2 = new GameObject("obj2");

                    //if (obj1 != obj2)
                    //    Debug.Log(obj1.GetInstanceID() + " " + obj2.GetInstanceID());

                    //leftCam = Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Camera>();
                    leftCam = Instantiate(cam, transform.position, transform.rotation);
                    leftCam.tag = "Untagged";
                    leftCam.name += "_leftCam";
                    //rightCam = Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Camera>();
                    rightCam = Instantiate(cam, transform.position, transform.rotation);
                    rightCam.tag = "Untagged";
                    rightCam.name += "_rightCam";

                    //for (int child = 0; child < leftCam.transform.childCount; child++)
                    //{
                    //    //Debug.Log(leftCam.transform.GetChild(child));
                    //    Destroy(leftCam.transform.GetChild(child).gameObject);
                    //}

                    //foreach (var component in leftCam.GetComponents(typeof(Component)))
                    //    if (!(component is Transform) && !(component is Camera) && !(component is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))
                    //        Destroy(component);

                    //DestroyComponents(leftCam);
                    //DestroyComponents(rightCam);

                    //void DestroyComponents(Camera camera)
                    //{
                    //    Debug.Log("destroyComponents");

                    //    foreach (var component in camera.GetComponents(typeof(Component)))
                    //        if (!(component is Transform) && !(component is Camera) && !(component is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))
                    //            Destroy(component);
                    //}

                    foreach (var component in cam.GetComponents(typeof(Component)))
                        if (!(component is Transform) && !(component is Camera)
#if URP
                            && !(component is UniversalAdditionalCameraData)
#elif HDRP
                            && !(component is HDAdditionalCameraData)
#endif
                            )
                        {
                            //Type componentType = component.GetType();
                            //Destroy(camClone.GetComponent(component.GetType()));
                            Destroy(leftCam.GetComponent(component.GetType()));
                            Destroy(rightCam.GetComponent(component.GetType()));
                        }

                    //rightCam = Instantiate(leftCam, transform.position, transform.rotation).GetComponent<Camera>();
                    //rightCam.name = "rightCam";

                    for (int child = 0; child < leftCam.transform.childCount; child++)
                    {
                        //Debug.Log(leftCam.transform.GetChild(child));
                        Destroy(leftCam.transform.GetChild(child).gameObject);
                        Destroy(rightCam.transform.GetChild(child).gameObject);
                    }

                    //universalAdditionalCameraData = new UniversalAdditionalCameraData();
                    //cameraDataStruct = new CameraDataStruct(cam.GetUniversalAdditionalCameraData().renderPostProcessing, cam.GetUniversalAdditionalCameraData().antialiasing);
//#if URP
//                    camData = cam.GetUniversalAdditionalCameraData();
//#elif HDRP
//                    //camData = cam.gameObject.GetComponent<HDAdditionalCameraData>();
//                    //camData = cam.GetComponent<HDAdditionalCameraData>();
//                    CamData_Get();

//                    //UnityEditor.Selection.activeGameObject = leftCam.gameObject;
//                    //HDAdditionalCameraData leftCamData = leftCam.GetComponent<HDAdditionalCameraData>();
//                    //leftCamData = leftCam.GetComponent<HDAdditionalCameraData>();
//                    //Debug.Log("camData = " + camData);
//                    //leftCamData = leftCam.gameObject.AddComponent<HDAdditionalCameraData>();
//                    //camData.CopyTo(leftCamData);
//                    ////UnityEditor.Selection.activeGameObject = rightCam.gameObject;
//                    ////HDAdditionalCameraData rightCamData = rightCam.GetComponent<HDAdditionalCameraData>();
//                    //rightCamData = rightCam.gameObject.AddComponent<HDAdditionalCameraData>();
//                    //camData.CopyTo(rightCamData);
//#endif
                    //cameraDataStruct = new CameraDataStruct(camData.renderPostProcessing, camData.antialiasing);
                    //SetCameraDataStruct();
                    //cameraDataStruct = new CameraDataStruct();
                    //cameraDataStruct = cam.GetUniversalAdditionalCameraData() as CameraDataStruct;
                    //cameraDataStruct = new GameObject("camHelper").AddComponent<UniversalAdditionalCameraData>();
                }
                else
                {
                    leftCam = new GameObject("leftCam").AddComponent<Camera>();
                    rightCam = new GameObject("rightCam").AddComponent<Camera>();
                    leftCam.CopyFrom(cam);
                    rightCam.CopyFrom(cam);
                    //GameObject lc = new GameObject("leftCam");
                    //GameObject rc = new GameObject("rightCam");
                    //leftCam = (Camera)CopyComponent(GetComponent<Camera>(), lc);
                    //rightCam = (Camera)CopyComponent(GetComponent<Camera>(), rc);
                    //leftCam.CopyFrom(cam);
                    //rightCam.CopyFrom (cam);
                    //leftCam.rect = rightCam.rect = Rect.MinMaxRect(0, 0, 1, 1);
                }
            }

            //DestroyImmediate(Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Stereo3D>());
            //GameObject go = cam.transform.Find("Plane").gameObject;
            //go.SetActive(false);
            //Instantiate(go, transform.position, transform.rotation);
            //Debug.Log(name);

            //if (!name.Contains("Clone"))
            //{
            //    GameObject clone = Instantiate(cam.gameObject, transform.position, transform.rotation);
            //    Destroy(clone.GetComponent<Stereo3D>());
            //    Destroy(clone.GetComponent<AudioListener>());
            //    //var components = clone.GetComponents(typeof(Component));

            //    foreach (var component in clone.GetComponents(typeof(Component)))
            //        if (!(component is Transform) && !(component is Camera) && !(component is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))
            //            Destroy(component);
            //}

            //GameObject clone = Instantiate(cam.transform.Find("Plane").gameObject, transform.position, transform.rotation);
            //clone.GetComponent<Test>().enabled = false;
            //DestroyImmediate(clone.GetComponent<Test>());
            //clone.SetActive(false);

            //Debug.Break();

            //leftCam.CopyFrom(cam);
            //rightCam.CopyFrom(cam);
            //var camData = cam.GetComponents<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //leftCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //rightCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //ComponentExtensions.GetCopyOf(leftCam, cam);
            //ComponentExtensions.GetCopyOf(rightCam, cam);
            //leftCam.name = "leftCam";
            //rightCam.name = "rightCam";

            //leftCam.rect = rightCam.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
            //leftCam.depth = rightCam.depth = cam.depth;
            leftCam.transform.parent = rightCam.transform.parent = transform;
            leftCam.stereoTargetEye = StereoTargetEyeMask.Left;
            rightCam.stereoTargetEye = StereoTargetEyeMask.Right;

            if (Screen.dpi != 0)
                PPI = Screen.dpi;

            canvas = Instantiate(Resources.Load<Canvas>("Canvas"), transform.position, transform.rotation);
            //canvas.transform.parent = transform;
            canvas.transform.SetParent(transform);
            canvasRayCam = canvas.gameObject.AddComponent<Camera>();
            canvasRayCam.enabled = false;
            //orthoCamSet(canvasRayCam);
            //canvasRayCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            canvasRayCam.orthographic = true;
            canvasRayCam.nearClipPlane = -1;
            canvasRayCam.farClipPlane = 1;
            //canvasRayCam.depth = cam.depth - 1;
            canvasRayCam.cullingMask = 0;
            canvasRayCam.useOcclusionCulling = false;
            canvas.worldCamera = canvasRayCam;

#if URP
            camData = cam.GetUniversalAdditionalCameraData();
            //canvasRayCam.GetUniversalAdditionalCameraData().clearDepth = false;
            canvasRayCam.GetUniversalAdditionalCameraData().renderShadows = false;
            //cameraStack = cam.GetUniversalAdditionalCameraData().cameraStack;
            cameraStack = camData.cameraStack;
            leftCameraStack = leftCam.GetUniversalAdditionalCameraData().cameraStack;
            rightCameraStack = rightCam.GetUniversalAdditionalCameraData().cameraStack;
            leftCameraStack.RemoveAll(t => t); //clean stacks after clone from the main camera
            rightCameraStack.RemoveAll(t => t);

            //Debug.Log(cameraStack.Count);

            //foreach (var cam in cameraStack)
            //    Debug.Log(cam);

            additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
            //int i = 0;

            //foreach (var c in additionalS3DCameras)
            //{
            //    if (c && !c.transform.Find(c.name + "(Clone)_leftCam"))
            //    {
            //        Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
            //        cloneLeft.tag = "Untagged";
            //        cloneLeft.name += "_leftCam";
            //        cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
            //        //cloneLeft.targetTexture = leftCamRT;
            //        Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
            //        cloneRight.tag = "Untagged";
            //        cloneRight.name += "_rightCam";
            //        cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
            //        //cloneRight.targetTexture = rightCamRT;

            //        cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
            //        //c.enabled = false;

            //        int index = additionalS3DCameras.IndexOf(c);
            //        additionalS3DCamerasStruct[index].camera = c;
            //        additionalS3DCamerasStruct[index].cameraLeft = cloneLeft;
            //        additionalS3DCamerasStruct[index].cameraRight = cloneRight;
            //        //i++;
            //    }
            //}

            foreach (var c in cameraStack)
                if (additionalS3DCameras.Contains(c) && !c.transform.Find(c.name + "(Clone)_leftCam"))
                {
                    Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
                    cloneLeft.tag = "Untagged";
                    cloneLeft.name += "_leftCam";
                    cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
                    //cloneLeft.targetTexture = leftCamRT;
                    Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
                    cloneRight.tag = "Untagged";
                    cloneRight.name += "_rightCam";
                    cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
                    //cloneRight.targetTexture = rightCamRT;

                    cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
                    //c.enabled = false;

                    int index = additionalS3DCameras.IndexOf(c);
                    //int index = cameraStack.IndexOf(c);
                    additionalS3DCamerasStruct[index].camera = c;
                    additionalS3DCamerasStruct[index].cameraLeft = cloneLeft;
                    additionalS3DCamerasStruct[index].cameraRight = cloneRight;
                    //i++;
                }

            //URPAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            ////lastURPAsset = new UniversalRenderPipelineAsset();
            //lastURPAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            ////lastURPAsset = URPAsset;
#elif HDRP
            camData = cam.GetComponent<HDAdditionalCameraData>();
            HDRPAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            defaultHDRPSettings = HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //Invoke("Test", 5);
#endif
            SetCameraDataStruct();

            if (GUIAsOverlay)
            {
                //panelDepthMinMax = new Vector2(0, 1);
                //panelDepth = Mathf.Clamp01(panelDepth);
                canvasCam = new GameObject("canvasCamera").AddComponent<Camera>();
                canvasCam.CopyFrom(canvasRayCam);
#if URP
                canvasCam.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                canvasCam.clearFlags = CameraClearFlags.Nothing;
#else
                canvasCam.clearFlags = CameraClearFlags.Color;
                canvasCam.backgroundColor = Color.clear;
#endif
                //canvasCam.transform.parent = canvas.transform;
                canvasCam.transform.SetParent(canvas.transform);
                //orthoCamSet(canvasCam);

                //leftCam.cullingMask = rightCam.cullingMask = cam.cullingMask = cullingMask & ~(1 << 5); //exclude UI layer
                canvasCam.cullingMask = canvasCam.cullingMask | (1 << 5); //add UI layer
                //canvasCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                //canvasCam.enabled = false;
                //cam.GetUniversalAdditionalCameraData().cameraStack.Add(canvasCam);

                //if (!cameraStack.Contains(canvasCam))
                //    cameraStack.Add(canvasCam);

                ////Invoke("CameraStackSet", Time.deltaTime * 8);
                //canvasCam.clearFlags = CameraClearFlags.Nothing;
                ////canvasCamMatrix = canvasCam.projectionMatrix;
                ////CameraStackSet();
                ////canvasCam.depth = cam.depth - 1;
                ////canvasCam.enabled = false;

#if HDRP
                //HDCamData = cam.gameObject.GetComponent<HDAdditionalCameraData>();
                ////HDCamDataCopy = new HDAdditionalCameraData();
                //GameObject HDRPCamCopy = new GameObject("HDRPCamDataCopy");
                //HDCamDataCopy = HDRPCamCopy.AddComponent<HDAdditionalCameraData>();
                //HDRPCamCopy.AddComponent<HDAdditionalCameraData>();
                //GameObject HDRPCamCopy2 = new GameObject("HDRPCamDataCopy2");
                //HDCamDataCopy2 = HDRPCamCopy2.AddComponent<HDAdditionalCameraData>();
                //HDRPCamCopy2.AddComponent<HDAdditionalCameraData>();
                ////HDCamDataCopy.gameObject.GetComponent<Camera>().enabled = false;
                //HDCamData.CopyTo(HDCamDataCopy);
                //HDCamData.CopyTo(HDCamDataCopy2);
                ////HDCamDataCopy.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                ////HDCamera HDCam = HDCamera.GetOrCreate(cam);
                ////HDCam = HDCamera.GetOrCreate(cam);
                //HDCam = HDCamera.GetOrCreate(HDRPCamCopy.GetComponent<Camera>());
                //HDCam2 = HDCamera.GetOrCreate(HDRPCamCopy2.GetComponent<Camera>());
                ////HDCamDataCopy = (HDAdditionalCameraData)typeof(HDCamera).GetField("m_AdditionalCameraData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HDCam);
                //FieldInfo[] fields = typeof(HDAdditionalCameraData).GetFields();
                //string str = File.ReadAllText(@"C:\Users\Vital\Desktop\New Text Document.txt");
                ////string[] lines = File.ReadAllLines(@"C:\Users\Vital\Desktop\New Text Document.txt");

                //foreach (FieldInfo field in fields)
                //{
                //    //if (field.Name.Contains("physicalParameters"))
                //    if (!str.Contains(field.Name))
                //        Debug.Log(field);
                //}

                //foreach (var line in lines)
                //{
                //    bool contains = false;

                //    foreach (FieldInfo field in fields)
                //    {
                //        if (line.Contains(field.Name))
                //            contains = true;
                //    }

                //    if (!contains)
                //        Debug.Log(line);
                //}

                ////HDCamDataCopy_Get();
                //selectedObject = UnityEditor.Selection.activeGameObject;
                //CanvasCamHDRPData_Set(); //get HDRP HDAdditionalCameraData by select runtime created canvasCam object in the editor
                HDAdditionalCameraData canvasCamData = canvasCam.gameObject.AddComponent<HDAdditionalCameraData>();
                canvasCamData.volumeLayerMask = 0;
                canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                canvasCamData.backgroundColorHDR = Color.clear;
                //canvasCamData.clearDepth = true;
                canvasCamData.probeLayerMask = 0;
                Debug.Log("canvasCamData.name " + canvasCamData.name);
                //HDCamera HDCam = HDCamera.GetOrCreate(canvasCam);
                //HDCam.frameSettings.
                //canvasCamData.volumeLayerMask = 1 << 1;

                //////HDRenderPipelineAsset HDRPAsset = (HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
                //HDRPAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                //////RenderPipelineSettings HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
                ////HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
                //defaultHDRPSettings = HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
                //////HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
                ////defaultColorBufferFormat = HDRPSettings.colorBufferFormat;

                //required for precompile? Without this alpha in Render Textures is not working
                //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
                //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);

                //Invoke("HDRPcolorBufferFormat1_Set", 5);
                //Invoke("HDRPcolorBufferFormat2_Set", 10);

                //if (!GUIOpened)
                //    Invoke("DefaultColorBufferFormat_Set", 5);

                //var pipelineAsset = Instantiate(GraphicsSettings.renderPipelineAsset) as HDRenderPipelineAsset;
                //pipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests = pipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests == 128 ? 1 : 128;
                //GraphicsSettings.renderPipelineAsset = pipelineAsset;

                ////var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                //////Debug.Log(typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline));
                ////RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
                ////RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
                //RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GraphicsSettings.currentRenderPipeline);
                //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
                ////Debug.Log(HDRPSettings.colorBufferFormat);
                ////typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
                //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
#endif
            }
            else
            {
            //    if (panelDepthMinMax.x < .5f)
            //        panelDepthMinMax.x = .5f;

            //    if (panelDepthMinMax.y < 100)
            //        panelDepthMinMax.y = 100;

            //    //panelDepthMinMax = new Vector2(1, 100);
            //    //panelDepth = Mathf.Clamp(panelDepth, panelDepthMinMax.x, panelDepthMinMax.y);
            //}
            //    canvasCam.enabled = false;

            //void orthoCamSet(Camera camera)
            //{
            //    camera.orthographic = true;
            //    camera.nearClipPlane = -1;
            //    camera.farClipPlane = 1;
            //    //camera.depth = cam.depth - 1;
            //    camera.cullingMask = 0;
            //    camera.useOcclusionCulling = false;
            //    camera.GetUniversalAdditionalCameraData().renderShadows = false;
            Invoke("HDRPSettings_Restore", GUI_Set_delay);
            }

            CameraStackSet();

            //cullingMask = cam.cullingMask;

            //if (inputSystem)
#if STARTER_ASSETS_PACKAGES_CHECKED
        {
            //Debug.Log(FindObjectOfType<StarterAssetsInputs>());
            starterAssetsInputs = FindObjectOfType<StarterAssetsInputs>();
            cursorLockedDefault = starterAssetsInputs.cursorLocked;
            cursorInputForLookDefault = starterAssetsInputs.cursorInputForLook;
            //S3DInputActionAsset = canvas.gameObject.AddComponent<PlayerInput>().actions = Resources.Load<InputActionAsset>("S3D");
            //gameObject.AddComponent<PlayerInput>().actions = Resources.Load<InputActionAsset>("S3D");
            //PlayerInput playerInput = gameObject.AddComponent<PlayerInput>();

            //playerInput = gameObject.AddComponent<PlayerInput>();
            //playerInput.enabled = false;
            //playerInput.actions = Resources.Load<InputActionAsset>("S3D");
            ////playerInput.DeactivateInput();
            ////playerInput.ActivateInput();
            ////playerInput.enabled = true;
            //Invoke("PlayerInputEnable", 1);
        }
#endif
            //else
            cursorLockModeDefault = Cursor.lockState;

            //        if (!EventSystem.current)
            //        {
            //            //Debug.Log("!EventSystem.current");
            //            //canvas.gameObject.AddComponent<EventSystem>();
            //            //canvas.gameObject.AddComponent<StandaloneInputModule>();

            //#if ENABLE_INPUT_SYSTEM
            //                new GameObject("UI_EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            //#else
            //                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            //#endif
            //        }
            //        //else
            //        //    //Debug.Log("EventSystem.current " + EventSystem.current.name);

#if ENABLE_INPUT_SYSTEM
        GUIAction.Enable();
        S3DAction.Enable();
        FOVAction.Enable();
        modifier1Action.Enable();
        modifier2Action.Enable();
        modifier3Action.Enable();

        if (!EventSystem.current)
            new GameObject("UI_EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
            if (!EventSystem.current)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif

            cursorRectTransform = canvas.transform.Find("Image_Cursor").GetComponent<RectTransform>();
            //cursorTransform = canvas.transform.Find("Image_Cursor");
            //Resize();
            //canvasSize = new Vector2(canvas.GetComponent<RectTransform>().rect.width, canvas.GetComponent<RectTransform>().rect.height);
            canvasDefaultSize = new Vector2(canvas.GetComponent<RectTransform>().rect.width, canvas.GetComponent<RectTransform>().rect.height);
            //enableS3D_toggle = GameObject.Find("Toggle_S3D_Enable").GetComponent<Toggle>();
            //enableS3D_toggle = canvas.transform.Find("Panel").transform.Find("Toggle_S3D_Enable").GetComponent<Toggle>();
            //raycaster = canvas.GetComponent<GraphicRaycaster>();
            //eventSystem = EventSystem.current;
            //eventSystem = canvas.gameObject.AddComponent<EventSystem>();
            //eventSystem = canvas.GetComponent<EventSystem>();
            //iModule = eventSystem.GetComponent<StandaloneInputModule>();
            //bInput = iModule.input;

            panel = canvas.transform.Find("Panel");
            enableS3D_toggle = panel.Find("Toggle_S3D_Enable").GetComponent<Toggle>();
            enableS3D_toggle.isOn = S3DEnabled;
            enableS3D_toggle.onValueChanged.AddListener(EnableS3D_Toggle);
            swapLR_toggle = panel.Find("Toggle_SwapLeftRight").GetComponent<Toggle>();
            swapLR_toggle.onValueChanged.AddListener(SwapLR_Toggle);
            optimize_toggle = panel.Find("Toggle_Optimize").GetComponent<Toggle>();
            optimize_toggle.onValueChanged.AddListener(Optimize_Toggle);
            vSync_toggle = panel.Find("Toggle_VSync").GetComponent<Toggle>();
            vSync_toggle.onValueChanged.AddListener(VSync_Toggle);
            //interlace_button = panel.Find("Button (Legacy)_Interlace").GetComponent<Button>();
            //interlace_button.onClick.AddListener(Interlace_Button);
            //vertical_button = panel.Find("Button (Legacy)_Vertical").GetComponent<Button>();
            //vertical_button.onClick.AddListener(Vertical_Button);
            //checkerboard_button = panel.Find("Button (Legacy)_Checkerboard").GetComponent<Button>();
            //checkerboard_button.onClick.AddListener(Checkerboard_Button);
            //sideBySide_button = panel.Find("Button (Legacy)_SideBySide").GetComponent<Button>();
            //sideBySide_button.onClick.AddListener(SideBySide_Button);
            //overUnder_button = panel.Find("Button (Legacy)_OverUnder").GetComponent<Button>();
            //overUnder_button.onClick.AddListener(OverUnder_Button);
            //anaglyph_button = panel.Find("Button (Legacy)_Anaglyph").GetComponent<Button>();
            //anaglyph_button.onClick.AddListener(Anaglyph_Button);
            outputMethod_dropdown = panel.Find("Dropdown (Legacy)_OutputMethod").GetComponent<Dropdown>();
            outputMethod_dropdown.onValueChanged.AddListener(OutputMethod_Dropdown);
            //DropdownClick(outputMethod_dropdown);
            List<Dropdown.OptionData> list = new List<Dropdown.OptionData>();
            //outputMethod_dropdown.options = Enum.GetValues(method.GetType().to);
            Array arr = Enum.GetValues(method.GetType());

            for (int i = 0; i < arr.Length; i++)
            {
                //Debug.Log(arr.GetValue(i).ToString());
                Dropdown.OptionData optionData = new Dropdown.OptionData();
                optionData.text = arr.GetValue(i).ToString().Replace("_", " ");
                list.Add(optionData);
            }

            outputMethod_dropdown.options = list;

            //foreach(var v in arr)
            //{
            //   //Debug.Log(v);
            //}

            PPI_inputField = panel.Find("InputField (Legacy)_PPI").GetComponent<InputField>();
            //PPI_inputField.text = Convert.ToString(PPI);
            PPI_inputField.onEndEdit.AddListener(PPI_InputField);
            InputFieldAutofocus(PPI_inputField);
            userIPD_slider = panel.Find("Slider_UserIPD").GetComponent<Slider>();
            userIPD_slider.onValueChanged.AddListener(UserIPD_Slider);
            userIPD_inputField = panel.Find("InputField (Legacy)_UserIPD").GetComponent<InputField>();
            //userIPD_inputField.text = Convert.ToString(userIPD);
            userIPD_inputField.onEndEdit.AddListener(UserIPD_InputField);
            InputFieldAutofocus(userIPD_inputField);
            virtualIPD_slider = panel.Find("Slider_VirtualIPD").GetComponent<Slider>();
            virtualIPD_slider.maxValue = virtualIPDMax;
            virtualIPD_slider.onValueChanged.AddListener(VirtualIPD_Slider);
            virtualIPD_inputField = panel.Find("InputField (Legacy)_VirtualIPD").GetComponent<InputField>();
            //virtualIPD_inputField.text = Convert.ToString(virtualIPD);
            virtualIPD_inputField.onEndEdit.AddListener(VirtualIPD_InputField);
            InputFieldAutofocus(virtualIPD_inputField);
            matchUserIPD_toggle = panel.Find("Toggle_User").GetComponent<Toggle>();
            matchUserIPD_toggle.onValueChanged.AddListener(MatchUserIPD_Toggle);
            FOV_slider = panel.Find("Slider_FieldOfView").GetComponent<Slider>();
            FOV_slider.minValue = FOVMinMax.x;
            FOV_slider.maxValue = FOVMinMax.y;
            FOV_slider.onValueChanged.AddListener(FOV_Slider);
            FOV_inputField = panel.Find("InputField (Legacy)_FieldOfView").GetComponent<InputField>();
            //FOV_inputField.text = Convert.ToString(virtualIPD);
            FOV_inputField.onEndEdit.AddListener(FOV_InputField);
            InputFieldAutofocus(FOV_inputField);
            panelDepth_slider = panel.Find("Slider_PanelDepth").GetComponent<Slider>();
            //panelDepth_slider.minValue = panelDepthMinMax.x;
            //panelDepth_slider.maxValue = panelDepthMinMax.y;
            panelDepth_slider.onValueChanged.AddListener(PanelDepth_Slider);
            trigger = panelDepth_slider.gameObject.AddComponent<EventTrigger>();
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.BeginDrag;
            entry.callback.AddListener((eventData) => { PanelDepthSlider_DragStart(); });
            trigger.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.EndDrag;
            entry.callback.AddListener((eventData) => { PanelDepthSlider_DragEnd(); });
            trigger.triggers.Add(entry);
            panelDepth_inputField = panel.Find("InputField (Legacy)_PanelDepth").GetComponent<InputField>();
            //panelDepth_inputField.text = Convert.ToString(panelDepth);
            //panelDepth_inputField.text = panelDepth.ToString();
            panelDepth_inputField.onEndEdit.AddListener(PanelDepth_InputField);
            InputFieldAutofocus(panelDepth_inputField);
            screenDistance_inputField = panel.Find("InputField (Legacy)_ScreenDistance").GetComponent<InputField>();
            //screenDistance_inputField.enabled = false;
            screenDistance_inputField.onEndEdit.AddListener(ScreenDistance_InputField);
            slotName_inputField = panel.Find("InputField (Legacy)_SlotName").GetComponent<InputField>();
            slotName_inputField.onEndEdit.AddListener(SlotName_InputField);
            InputFieldAutofocus(slotName_inputField);
            slotName_dropdown = panel.Find("Dropdown (Legacy)_SlotName").GetComponent<Dropdown>();
            slotName_dropdown.onValueChanged.AddListener(SlotName_Dropdown);
            //slotName_dropdown.template.sizeDelta = new Vector2(362, 30);
            //DropdownClick(slotName_dropdown);
            save_button = panel.Find("Button (Legacy)_Save").GetComponent<Button>();
            save_button.onClick.AddListener(Save_Button);
            load_button = panel.Find("Button (Legacy)_Load").GetComponent<Button>();
            load_button.onClick.AddListener(Load_Button);
            tooltip = cursorRectTransform.Find("Tooltip").gameObject;
            tooltip.SetActive(false);
            tooltipBackgroundRect = tooltip.transform.Find("Image_Background").GetComponent<RectTransform>();
            tooltipText = tooltip.transform.Find("Text (Legacy)").GetComponent<Text>();
            //TooltipShow("HellO \nTesT");
            FPSText = panel.Find("Text (Legacy)_FPS").GetComponent<Text>();

            //Invoke("InputFieldCaretMaterial_SetFields", Time.deltaTime);

            trigger = panel.gameObject.AddComponent<EventTrigger>();
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener((eventData) => { MouseDragStart(); });
            trigger.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener((eventData) => { MouseDrag(); });
            trigger.triggers.Add(entry);

            Tooltip(optimize_toggle.gameObject, "Half X/Y render resolution for certain output methods");
            Tooltip(vSync_toggle.gameObject, "Required for 'Over Under' S3D method");
            Tooltip(panel.Find("Text (Legacy)_ScreenDensity").gameObject, "Pixels Per Inch(PPI) density of the screen\nRequired to calculate the real physical size of the viewport");
            Tooltip(panel.Find("Text (Legacy)_UserIPD").gameObject, "Your interpupillary distance(IPD) in real millimeters on the screen if Screen Density is set correct\nRequired for realistic infinity depth perception and not overshoot as it causes abnormal eyes angle");
            Tooltip(panel.Find("Text (Legacy)_VirtualIPD").gameObject, "Your  interpupillary distance(IPD) in virtual 3D world millimeters\nUnlock from UserIPD and increase to simulate not user Stereo3D perception like toy size 3D world");
            Tooltip(matchUserIPD_toggle.gameObject, "Match Virtual IPD to User IPD for realistic view");
            Tooltip(panel.Find("Text (Legacy)_FieldOfView").gameObject, "Horizontal Field Of View(FOV) of the viewport");
            Tooltip(panel.Find("Text (Legacy)_PanelDepth").gameObject, "Depth of this panel as Screen Distance multiplier");
            Tooltip(panel.Find("Text (Legacy)_ScreenDistance").gameObject, "Distance from view point to the calculated size viewport in 3D world frustum based on PPI and FOV\nIf you set the correct PPI of your screen and real distance from eyes to screen match this value\nthen your real and virtual FOV will match and you get a 100% realistic view");
            Tooltip(panel.Find("Text (Legacy)_SlotName").gameObject, "Slot Name for Save/Load Stereo3D settings");
            Tooltip(slotName_dropdown.gameObject, "Available S3D settings files in folder:\n'" + Application.persistentDataPath + "'");

            DropdownSet();

            if (loadSettingsFromFile)
                LoadLastSave();

            //if (!loaded && loadSettingsFromFile)
            //{
            //    Debug.Log("!loaded && loadSettingsFromFile");
            //    loaded = true;
            //    LoadLastSave();
            //}

            swapLR_toggle.isOn = swapLR;
            optimize_toggle.isOn = optimize;
            vSync_toggle.isOn = vSync;
            outputMethod_dropdown.value = (int)method;
            matchUserIPD_toggle.isOn = matchUserIPD;
            slotName_inputField.text = slotName;

            //GUI_Set();
            //Invoke("GUI_Set", Time.deltaTime * 32);
            //aspect = cam.aspect;
            //FOV_Set();
            //Resize();
            CamRect_Set();
            PanelDepthMinMaxSet();
            Aspect_Set();
            VSyncSet();
            PPISet();
            UserIPDSet();
            VirtualIPDSet();
            //PanelDepthMinMaxSet();
            //PanelDepthSet();
            //PanelOverlayDepthMinMaxSet();
            //Panel3DdepthMinMaxSet();
            //CamSet();
            //RT_Set();
            //GUI_Set();
            Invoke("GUI_Set", GUI_Set_delay);

            lastGUIOpened = GUIOpened;
            lastS3DEnabled = S3DEnabled;
            lastEyePriority = eyePriority;
            lastSwapLR = swapLR;
            lastOptimize = optimize;
            lastVSync = vSync;
            lastMethod = method;
            lastPPI = PPI;
            lastUserIPD = userIPD;
            lastVirtualIPD = virtualIPD;
            lastVirtualIPDMax = virtualIPDMax;
            lastMatchUserIPD = matchUserIPD;
            //lastPixelPitch = pixelPitch;
            lastFOV = FOV;
            lastMinMaxFOV = FOVMinMax;
            lastPanelDepth = panelDepth;
            //lastPanelDepthMinMax = panelDepthMinMax;
            lastGUIAsOverlay = GUIAsOverlay;
            lastGUISizeKeep = GUISizeKeep;
            lastCameraPrefab = cameraPrefab;
            lastRTFormat = RTFormat;
            lastSetMatrixDirectly = setMatrixDirectly;
            //lastInterlaceType = interlaceType;
            //lastAnaglyphLeftColor = anaglyphLeftColor;
            //lastAnaglyphRightColor = anaglyphRightColor;
            lastSlotName = slotName;
            lastCamRect = cam.rect;
            lastNearClipHack = nearClipHack;
            lastMatrixKillHack = matrixKillHack;
            lastCanvasLocalPosZ = canvasLocalPosZ;
            lastCloneCamera = cloneCamera;
            lastCameraDataStruct = cameraDataStruct;
            lastLoadSettingsFromFile = loadSettingsFromFile;
            //lastAdditionalS3DCameras = additionalS3DCameras;
            //lastAdditionalS3DCameras = (Camera[])additionalS3DCameras.Clone();
            //lastAdditionalS3DCameras = additionalS3DCameras.Clone() as Camera[];
            //lastAdditionalS3DCameras = new Camera[additionalS3DCameras.Length];
            //lastAdditionalS3DCameras = new Camera[additionalS3DCameras.Count];
            //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras, 0);
            //lastAdditionalS3DCameras = new List<Camera>();
            //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);
            //lastCameraStack = cameraStack.ToArray();
            lastAdditionalS3DCameras = additionalS3DCameras.ToArray();

#if LookWithMouse
        bool lookWithMouseScriptEnabled = false;

        foreach (var script in FindObjectsByType<LookWithMouse>(FindObjectsSortMode.None))
        {
            if (script.enabled)
                if (!lookWithMouseScriptEnabled)
                {
                    lookWithMouseScriptEnabled = true;
                    lookWithMouseScript = script;
                    //Debug.Log(lookWithMouseScript);
                    //break;
                }
                else
                {
                    Debug.Log("More than one lookWithMouseScript enabled in the scene");
                    lookWithMouseScript = null;
                }
        }
#endif

#if SimpleCameraController
            bool simpleCameraControllerScriptEnabled = false;

            foreach (var script in FindObjectsByType<UnityTemplateProjects.SimpleCameraController>(FindObjectsSortMode.None))
            {
                if (script.enabled)
                {
                    //if (lookWithMouseScriptEnabled)
                    //    Debug.Log("More than one mouse control script enabled in the scene");

                    if (!simpleCameraControllerScriptEnabled)
                    {
                        simpleCameraControllerScriptEnabled = true;
                        simpleCameraControllerScript = script;
                        //Debug.Log(simpleCameraControllerScript);
                        //break;
                    }
                    else
                    {
                        Debug.Log("More than one simpleCameraControllerScript enabled in the scene");
                        simpleCameraControllerScript = null;
                    }
                }
            }
#endif
        }
    }

    void HDRPSettings_Restore()
    {
#if HDRP
        //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R11G11B10;
        //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
        HDRPSettings = defaultHDRPSettings;
        typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
#endif
    }

    //void CamData_Get()
    //{
    //    Debug.Log("CamData_Get");
    //    camData = cam.GetComponent<HDAdditionalCameraData>();

    //    if (!camData)
    //        Invoke("CamData_Get", Time.deltaTime);
    //}

    //void HDCamDataCopy_Get()
    //{
    //    Debug.Log("HDCamDataCopy_Get");
    //    //HDCamDataCopy = (HDAdditionalCameraData)typeof(HDCamera).GetField("m_AdditionalCameraData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HDCam);
    //    HDCamDataCopy = typeof(HDCamera).GetField("m_AdditionalCameraData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HDCam) as HDAdditionalCameraData;
    //    HDCamDataCopy2 = typeof(HDCamera).GetField("m_AdditionalCameraData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HDCam2) as HDAdditionalCameraData;

    //    //if (!HDCamDataCopy)
    //    if (!(HDCamDataCopy || HDCamDataCopy2))
    //        Invoke("HDCamDataCopy_Get", Time.deltaTime);
    //    else
    //    {
    //        HDCamData.CopyTo(HDCamDataCopy);
    //        HDCamData.CopyTo(HDCamDataCopy2);
    //    }
    //}

    //void DefaultColorBufferFormat_Set()
    //{
    //    Debug.Log("DefaultColorBufferFormat_Set");
    //    HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
    //    typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
    //}

    //UnityEngine.Object selectedObject;

    //    void CanvasCamHDRPData_Set()
    //    {
    //        //Debug.Log("CanvasCamHDRPData_Set()");

    //#if UNITY_EDITOR
    //        if (!selectedObject)
    //            selectedObject = UnityEditor.Selection.activeObject;

    //        UnityEditor.Selection.activeObject = canvasCam;
    //#endif

    //        if (canvasCam.GetComponent<HDAdditionalCameraData>())
    //        {
    //            //UnityEditor.Selection.activeObject = canvasCam;
    //            canvasCamData = canvasCam.GetComponent<HDAdditionalCameraData>();
    //            canvasCamData.volumeLayerMask = 0;
    //            canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
    //            //canvasCamData.clearDepth = true;
    //            canvasCamData.probeLayerMask = 0;
    //            //Debug.Log("canvasCamData.name " + canvasCamData.name);
    //#if UNITY_EDITOR
    //            //selectedObject = UnityEditor.Selection.activeGameObject;
    //            Debug.Log("selectedObject " + selectedObject.name);
    //            UnityEditor.Selection.activeObject = selectedObject;
    //#endif
    //        }
    //        else
    //            Invoke("CanvasCamHDRPData_Set", Time.deltaTime);
    //    }

    void CameraStackSet()
    {
#if URP
        //Debug.Break();

        if (S3DEnabled)
        {
            //if (GUIAsOverlay)
            //{
            //    //if (!leftCameraStack.Contains(canvasCam))
            //    //{
            //    //    leftCameraStack.Add(canvasCam);
            //    //    rightCameraStack.Add(canvasCam);
            //    //}

            //    ////leftCameraStack.Remove(canvasCam);
            //    ////rightCameraStack.Remove(canvasCam);
            //    ////leftCameraStack.Add(canvasCam);
            //    ////rightCameraStack.Add(canvasCam);
            //    //canvasCam.targetTexture = leftCamRT;

            //    cameraStack.Remove(canvasCam);
            //    canvasCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
            //}

            //additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
            //int i = 0;
            //if (cameraStack.Count != 0)
            foreach (var c in cameraStack)
            //for (int i = 0; i < cameraStack.Count; i++)
            {
                if (additionalS3DCameras.Contains(c))
                {
                    Debug.Log("additionalS3DCameras.Contains(c) " + c);
                    //Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
                    //cloneLeft.tag = "Untagged";
                    //cloneLeft.name += "_leftCam";
                    //cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
                    ////cloneLeft.targetTexture = leftCamRT;
                    //Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
                    //cloneRight.tag = "Untagged";
                    //cloneRight.name += "_rightCam";
                    //cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
                    ////cloneRight.targetTexture = rightCamRT;

                    //cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
                    ////c.enabled = false;

                    int index = additionalS3DCameras.IndexOf(c);
                    leftCameraStack.Add(additionalS3DCamerasStruct[index].cameraLeft);
                    rightCameraStack.Add(additionalS3DCamerasStruct[index].cameraRight);
                    //additionalS3DCamerasStruct[i].camera = c;
                    //additionalS3DCamerasStruct[i].cameraLeft = cloneLeft;
                    //additionalS3DCamerasStruct[i].cameraRight = cloneRight;
                    //i++;
                }
                else
                {
                    leftCameraStack.Add(c);
                    rightCameraStack.Add(c);
                }
            }

            //for (int i = 0; i < cameraStack.Count; i++)
            //{
            //    Camera c = cameraStack[i];
            //    //cameraStack.RemoveAt(i);
            //    //c.targetTexture = leftCamRT;
            //    leftCameraStack.Add(c);
            //    rightCameraStack.Add(c);
            //    //cameraStack.Remove(c);
            //}

            //Invoke("CameraStackRemove", Time.deltaTime * 8);
            cameraStack.RemoveAll(t => t);
        }
        else
        {
            //if (GUIAsOverlay)
            //{
            //    canvasCam.ResetProjectionMatrix();
            //    canvasCam.targetTexture = null;
            //    canvasCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            //    //if (!cameraStack.Contains(canvasCam))
            //    cameraStack.Add(canvasCam);

            //    //cameraStack.Remove(canvasCam);
            //    //cameraStack.Add(canvasCam);
            //    //canvasCam.targetTexture = null;

            //    //leftCameraStack.Remove(canvasCam);
            //    //rightCameraStack.Remove(canvasCam);
            //}

            //foreach (var c in leftCameraStack)
            //{
            //    //leftCameraStack.Remove(c);
            //    //rightCameraStack.Remove(c);

            //    //foreach (var d in additionalS3DCameras)
            //    //Debug.Log("CameraStackSet c.name.Replace("_leftCam", "")" + c.name.Replace("_leftCam", ""));

            //    //string cameraName = c.name.Replace("(Clone)_leftCam", "");
            //    //Debug.Log("CameraStackSet cameraName " + cameraName);
            //    Debug.Log("******************CameraStackSet var c in leftCameraStack " + c);

            //    //Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == cameraName);
            //    Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == c.name.Replace("(Clone)_leftCam", ""));
            //    //Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name.Contains(cameraName));

            //    //Debug.Log("CameraStackSet additionalS3DCamera " + additionalS3DCamera);

            //    //if (additionalS3DCameras.Find(t => t.name.Contains(c.name.Replace("_leftCam", ""))))
            //    if (additionalS3DCamera)
            //    //{
            //    //    if (!cameraStack.Contains(additionalS3DCamera))
            //            cameraStack.Add(additionalS3DCamera);
            //    //}
            //    else
            //            cameraStack.Add(c);
            //}

            CameraStackRestore();

            //for (int i = 0; i < leftCameraStack.Count; i++)
            //{
            //    Camera c = leftCameraStack[i];
            //    //leftCameraStack.Remove(c);
            //    //rightCameraStack.Remove(c);
            //    cameraStack.Add(c);
            //}

            //Invoke("S3DCameraStackRemove", 0);
            leftCameraStack.RemoveAll(t => t);
            rightCameraStack.RemoveAll(t => t);

            //if (GUIAsOverlay)
            //{
            //    canvasCam.ResetProjectionMatrix();
            //    canvasCam.targetTexture = null;
            //    canvasCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            //    //if (!cameraStack.Contains(canvasCam))
            //    cameraStack.Add(canvasCam);

            //    //cameraStack.Remove(canvasCam);
            //    //cameraStack.Add(canvasCam);
            //    //canvasCam.targetTexture = null;

            //    //leftCameraStack.Remove(canvasCam);
            //    //rightCameraStack.Remove(canvasCam);
            //}
        }

        lastCameraStack = cameraStack.ToArray();
        //Debug.Log("CameraStackSet cameraStack.Count " + cameraStack.Count);
        Debug.Log("lastCameraStack.Length " + lastCameraStack.Length + " cameraStack.Count " + cameraStack.Count);
#endif
    }

    void CameraStackRestore()
    {
#if URP
        foreach (var c in leftCameraStack)
        {
            ////Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == c.name.Replace("(Clone)_leftCam", ""));
            //Camera additionalS3DCamera = null;
            //string cameraName = c.name.Replace("(Clone)_leftCam", "");

            //foreach (var s in additionalS3DCamerasStruct)
            //    if (s.camera.name == cameraName)
            //        additionalS3DCamera = s.camera;

            //if (additionalS3DCamera)
            //    cameraStack.Add(additionalS3DCamera);
            //else
            //    cameraStack.Add(c);

            if (c.name.Contains("_leftCam"))
                cameraStack.Add(c.transform.parent.GetComponent<Camera>());
            else
                cameraStack.Add(c);
        }
#endif
    }

    //void CameraStackRemove()
    //{
    //    //for (int i = 0; i < cameraStack.Count; i++)
    //    //    cameraStack.RemoveAt(i);

    //    //cameraStack.RemoveAll(t => t.name.Contains("1"));
    //    cameraStack.RemoveAll(t => t);
    //}

    //void S3DCameraStackRemove()
    //{
    //    leftCameraStack.RemoveAll(t => t);
    //    rightCameraStack.RemoveAll(t => t);
    //}

    //public static List<Type> GetAllTypesInAssembly(string[] pAssemblyNames)
    //{
    //    List<Type> results = new List<Type>();
    //    foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    //    {
    //        foreach (string assemblyName in pAssemblyNames)
    //        {
    //            if (assembly.FullName.StartsWith(assemblyName))
    //            {
    //                Debug.Log(assembly.FullName);

    //                foreach (Type type in assembly.GetTypes())
    //                {
    //                    results.Add(type);
    //                }
    //                break;
    //            }
    //        }
    //    }
    //    return results;
    //}

    //void Start()
    //{
    //    Debug.Log("Start " + name);
    //}

    float vFOV;
    bool autoshow;
    float S3DKeyTimer;
    bool S3DKeyLoaded;
    bool camFOVChanged;
    bool GUIVisible;
    bool S3DActionPressed;
    bool FOVSetInProcess;
    //float canvasEdgeOffset;
    bool panelDepth_sliderIsDragging;
    //int time;
    bool oddFrame;
    //float canvasHalfSizeY;
    //float newPanelDepth;
    float setLastCameraDataStructTime;
    Vector2 cursorLocalPos;

    void Start()
    {
        Debug.Log("Start");

        //if (loadSettingsFromFile)
        //    LoadLastSave();

        //Invoke("AdditionalS3DCamerasListModify", 5);
        //camData = cam.GetUniversalAdditionalCameraData();
        //SetCameraDataStruct();
        //lastCameraDataStruct = cameraDataStruct;
        //Debug.Log("Time.deltaTime " + Time.deltaTime);
        //Invoke("SetLastCameraDataStruct", Time.deltaTime * 8);
        setLastCameraDataStructTime = Time.deltaTime * 32;
        Invoke("SetLastCameraDataStruct", setLastCameraDataStructTime);
        //Debug.Log("Start cameraDataStruct " + cameraDataStruct);
        //Invoke("CamData_Change", 5);
    }

    void SetLastCameraDataStruct()
    {
        lastCameraDataStruct = cameraDataStruct;
    }

    //void AdditionalS3DCamerasListModify()
    //{
    //    //additionalS3DCameras.cameras = null;
    //    //additionalS3DCameras.camera = null;
    //    //additionalS3DCameras.cameras[0] = null;
    //    //additionalS3DCameras.cameras = new Camera[1];
    //    //additionalS3DCameras.cameras[0] = cam;
    //    additionalS3DCameras[0] = cam;
    //    //additionalS3DCameras.cameras.Add(cam);
    //    //additionalS3DCameras.Add(cam);
    //    //additionalS3DCameras[0] = null;
    //}

    float averageTime;
    int frames;
    int averageFPS;

    void Update()
    {
        //Debug.Log("sceneNearClip = " + sceneNearClip);
        //Debug.Log("Update " + Time.time);
        //Debug.Log(camData + " " + leftCamData);
        ////UnityEditor.Selection.activeGameObject = leftCam.gameObject;
        ////camData.CopyTo(leftCam.GetComponent<HDAdditionalCameraData>());
        //leftCamData = leftCam.GetComponent<HDAdditionalCameraData>();
        //camData.CopyTo(leftCamData);
        ////UnityEditor.Selection.activeGameObject = rightCam.gameObject;
        ////camData.CopyTo(rightCam.GetComponent<HDAdditionalCameraData>());
        //rightCamData = rightCam.GetComponent<HDAdditionalCameraData>();
        //camData.CopyTo(rightCamData);

        //if (HDCamDataCopy)
        //{
        //    Debug.Log(HDCamDataCopy.antialiasing);
        //    Debug.Log(HDCamDataCopy2.antialiasing);
        //}

        ////if (!Equals(HDCamData, HDCamDataCopy))
        ////if (!HDCamData.Equals(HDCamDataCopy))
        ////if (!HDCamDataCopy.Equals(HDCamDataCopy2))
        //if (!Equals(HDCamDataCopy, HDCamDataCopy2))
        //{
        //    Debug.Log("!HDCamData.Equals(HDCamDataCopy)");
        //    //HDCamData.CopyTo(HDCamDataCopy);
        //    //HDCamDataCopy.CopyTo(HDCamDataCopy2);
        //}

        //////Debug.Log("HDRPSettings.colorBufferFormat " + (GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.colorBufferFormat);
        //////Debug.Log("typeof(HDRenderPipelineAsset) " + typeof(HDRenderPipelineAsset));
        //var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        //////Debug.Log(typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline));
        //RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
        //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;

        //if (!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings))
        //{
        //    HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
        //    Debug.Log("HDRPSettings.colorBufferFormat != HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat");
        //}

        //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
        //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);

        int FPS = (int)(1 / Time.deltaTime);
        frames += 1;
        averageTime += Time.deltaTime;

        if (averageTime > 1) //calculate average FPS per 1 second
        {
            averageFPS = (int)(frames / averageTime);
            averageTime = frames = 0;
        }

        //FPSText.text = "test";
        //FPSText.text = FPS.ToString();
        FPSText.text = averageFPS.ToString() + " FPS";
        //canvasCamData = canvasCam.GetComponent<HDAdditionalCameraData>();
        //Debug.Log("OnEnable panelDepth as screen distance = " + 1 / (1 - panelDepth));
        //canvasCam.enabled = true;
        oddFrame = !oddFrame;
        //Debug.Log(oddFrame + " Update " + Time.time);

        //if (canvasCam && S3DEnabled)
        //{
        //    if (oddFrame)
        //    {
        //        if (method == Method.SideBySide_HMD)
        //            canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //        else
        //            canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //        if (method == Method.Interlace_Horizontal)
        //            canvasCamMatrix[1, 3] = -oneRowShift;

        //        canvasCam.projectionMatrix = canvasCamMatrix;
        //        //canvasCam.targetTexture = leftCamRT;
        //        canvasCam.targetTexture = leftCamCanvasRT;
        //        //canvasCam.Render();
        //        //Graphics.CopyTexture(leftCamCanvasRT, leftCamRT);
        //    }
        //    else
        //    {
        //        if (method == Method.SideBySide_HMD)
        //            canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //        else
        //            canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //        if (method == Method.Interlace_Horizontal)
        //            canvasCamMatrix[1, 3] = 0;

        //        canvasCam.projectionMatrix = canvasCamMatrix;
        //        //canvasCam.targetTexture = rightCamRT;
        //        canvasCam.targetTexture = rightCamCanvasRT;
        //        //canvasCam.Render();
        //        //Graphics.CopyTexture(rightCamCanvasRT, rightCamRT);
        //    }

        //    //UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);

        //    //if (oddFrame)
        //    //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);

        //    //canvasCam.Render();
        //    //Debug.Break();
        //    //canvasCam.enabled = false;
        //    //Debug.Log(camera);
        //    //Debug.Log(oddFrame + " RenderQuad camera == canvasCam " + Time.time);
        //    //UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);
        //}

        //Matrix4x4 canvasCamMatrix = canvasCam.projectionMatrix;
        //float canvasHalfSizeX = canvasSize.x * .5f;
        ////Matrix4x4 canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
        ////Matrix4x4 canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * shift, canvasHalfSizeX - canvasHalfSizeX * shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
        //Matrix4x4 canvasCamMatrix = canvasCam.projectionMatrix;

        ////if (oddFrame)
        ////    canvasCamMatrix[0, 3] = -shift;
        ////else
        ////    canvasCamMatrix[0, 3] = shift;

        //if (oddFrame)
        //{
        //    canvasCamMatrix[0, 3] = -shift;
        //    canvasCam.projectionMatrix = canvasCamMatrix;
        //    canvasCam.targetTexture = rightCamRT;
        //}
        //else
        //{
        //    canvasCamMatrix[0, 3] = shift;
        //    canvasCam.projectionMatrix = canvasCamMatrix;
        //    canvasCam.targetTexture = leftCamRT;
        //}

        //canvasCamMatrix[0, 2] = shift;
        //canvasCam.projectionMatrix = canvasCamMatrix;
        //canvasCam.ResetProjectionMatrix();
        //Debug.Log(canvasCam.projectionMatrix[0, 3] + " " + shift);
        //canvasCam.projectionMatrix = MatrixSet(canvasCam.projectionMatrix, -shift, 0);

        //if (oddFrame)
        //    canvasCam.projectionMatrix = MatrixSet(canvasCam.projectionMatrix, -shift, 0);
        //else
        //    canvasCam.projectionMatrix = MatrixSet(canvasCam.projectionMatrix, shift, 0);

        //if ((int)Time.time != time)
        //{
        //    time = (int)Time.time;
        //    //Debug.Log(time);

        //    canvasCam.enabled = true;
        //    canvasCam.targetTexture = leftCamRT;
        //    canvasCam.Render();
        //    //Debug.Break();
        //    canvasCam.enabled = false;
        //}

        //canvasCam.targetTexture = leftCamRT;
        //canvasCam.Render();
        //Debug.Log(canvasCam.GetUniversalAdditionalCameraData().clearDepth);
        //Debug.Log("Update " + Time.time);
#if CINEMACHINE
        //if (S3DEnabled && nearClipHack && cam.nearClipPlane != -1)
        //{
        //    //Debug.Log("S3DEnabled && nearClipHack && cam.nearClipPlane != -1");
        //    VCamNearClipHack();
        //}

        //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //if (vCam == GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera)
        //if (Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //{
        //    Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //    //Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam");
        //}

        if (cineBrain && Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //if (vCam != null && Cinemachine.CinemachineBrain.SoloCamera != vCam)
        {
            Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam, Cinemachine.CinemachineBrain.SoloCamera = " + Cinemachine.CinemachineBrain.SoloCamera + ", vCam = " + vCam);

            //if (Cinemachine.CinemachineBrain.SoloCamera == null)
            if (Cinemachine.CinemachineBrain.SoloCamera == null || Cinemachine.CinemachineBrain.SoloCamera.ToString() == "null") //virtual camera lost
                Cinemachine.CinemachineBrain.SoloCamera = vCam;
            else //virtual camera changed
                if (vCamSceneClipIsReady)
                {
                    //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
                    VCamClipRestore();

                    vCam = Cinemachine.CinemachineBrain.SoloCamera;

                    //if (!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane)
                    //{
                    //    sceneNearClip = cam.nearClipPlane;
                    //    //Debug.Log("!S3DEnabled && !GUIVisible && sceneNearClip != cam.nearClipPlane sceneNearClip= " + cam.nearClipPlane);
                    //    ClipSet();
                    //}

                    VCamSceneClipSet();
                    //Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != null sceneNearClip " + sceneNearClip);
                    FOV_Set();
                    RT_Set();
                    ClipSet();
                }
        }
        //else
        //    if (Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //{
        //    vCam = Cinemachine.CinemachineBrain.SoloCamera;
        //    //Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam " + vCam);
        //}

        if (nearClipHackApplied && cam.nearClipPlane != -1)
        //if (nearClipHackApplied && ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane != -1)
        {
            Debug.Log("nearClipHackApplied && cam.nearClipPlane != -1 cam.nearClipPlane " + cam.nearClipPlane);
            VCamNearClipHack();
        }
#endif

        ////if (!S3DEnabled && !GUIVisible && sceneNearClip != cam.nearClipPlane)
        //if (!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane)
        //{
        //    sceneNearClip = cam.nearClipPlane;
        //    Debug.Log("********************************************************************************************************************sceneNearClip = " + sceneNearClip);
        //    //Debug.Log("!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane sceneNearClip= " + sceneNearClip);
        //    //RT_Set();
        //    ClipSet();
        //}

        if (!GUIVisible && cam.farClipPlane >= 0 && sceneFarClip != cam.farClipPlane)
        {
            sceneFarClip = cam.farClipPlane;
            ClipSet();
        }

#if URP
        ////if (!lastURPAsset.Equals(GraphicsSettings.currentRenderPipeline))
        //if (lastURPAsset.supportsMainLightShadows != ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows)
        //{
        //    Debug.Log("!lastURPAsset.Equals(URPAsset)");
        //    //OnOffToggle();
        //    //lastURPAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        //    lastURPAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        //}

        //Debug.Log("((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows" + ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows);
#elif HDRP
        //HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;

        //if (!GUIOpened && !GUIVisible && HDRPSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (!GUIOpened && !GUIVisible && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (Time.time > GUI_Set_delay && !GUIVisible && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (Time.time > GUI_Set_delay && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != HDRPSettings.colorBufferFormat)
        //{
        //    ////defaultColorBufferFormat = HDRPSettings.colorBufferFormat;
        //    //defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;
        //    //Debug.Log("!GUIVisible && HDRPSettings.colorBufferFormat != defaultColorBufferFormat " + defaultColorBufferFormat);
        //    Debug.Log("HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat");

        //    if (!GUIVisible)
        //        defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;

        //    HDRPSettings_Set();
        //}

        //if (!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings))
        if (!HDRPSettings.Equals(HDRPAsset.currentPlatformRenderPipelineSettings))
        {
            Debug.Log("!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings)");
            //HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            //defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;
            defaultHDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            OnOffToggle();
        }

        if (canvasCam && S3DEnabled)
        {
            //if (oddFrame)
            //{
            //    if (method == Method.SideBySide_HMD)
            //        canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
            //    else
            //        canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

            //    if (method == Method.Interlace_Horizontal)
            //        canvasCamMatrix[1, 3] = -oneRowShift;

            //    canvasCam.projectionMatrix = canvasCamMatrix;
            //    canvasCam.targetTexture = leftCamCanvasRT;
            //}
            //else
            //{
            //    if (method == Method.SideBySide_HMD)
            //        canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
            //    else
            //        canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

            //    if (method == Method.Interlace_Horizontal)
            //        canvasCamMatrix[1, 3] = 0;

            //    canvasCam.projectionMatrix = canvasCamMatrix;
            //    canvasCam.targetTexture = rightCamCanvasRT;
            //}

            CanvasCamS3DRender_Set();
        }
#endif

        //Debug.Log(vCam);
        //Debug.Log(Cinemachine.CinemachineCore.Instance.IsLive(vCam));

        //if (brain.ActiveVirtualCamera == null)
        //    //Debug.Log(brain.ActiveVirtualCamera);

#if ENABLE_INPUT_SYSTEM
        //if (inputSystem)
        //{
        //Debug.Log("inputSystem");

        //if (FOVAction.ReadValue<float>() != 0)
        //    virtualIPD += FOVAction.ReadValue<float>();
        //    //Debug.Log(FOVAction.ReadValue<float>());

        if (S3DAction.ReadValue<float>() != 0)
        {
            S3DActionPressed = true;
            //S3DKeyTimer += Time.deltaTime;
            ////Debug.Log(S3DKeyTimer);

            //if (S3DKeyTimer > 1 && !S3DKeyLoaded)
            //{
            //    Load(slotName);
            //    //S3DKeyTimer = 0;
            //    S3DKeyLoaded = true;
            //    //Debug.Log("S3DKeyLoaded");
            //}

            //GUI_Autoshow();
            S3DKeyHold();
        }

        if (S3DActionPressed && S3DAction.ReadValue<float>() == 0)
        {
            S3DActionPressed = false;
            //Debug.Log("S3DActionPressed && S3DAction.ReadValue<float>() == 0");

            //if (S3DKeyTimer < 1)
            //    if (modifier1Action.ReadValue<float>() != 0)
            //        swapLR = !swapLR;
            //    else
            //        if (modifier2Action.ReadValue<float>() != 0)
            //        Save(slotName);
            //    else
            //            if (modifier3Action.ReadValue<float>() != 0)
            //        optimize = !optimize;
            //    else
            //        S3DEnabled = !S3DEnabled;
            ////else
            ////    //Debug.Log("S3DActionPressed && S3DAction.ReadValue<float>() == 0 S3DKeyTimer > 1");

            //S3DKeyTimer = 0;
            //S3DKeyLoaded = false;
            S3DKeyUp(modifier1Action.ReadValue<float>(), modifier2Action.ReadValue<float>(), modifier3Action.ReadValue<float>());
        }

        if (FOVAction.ReadValue<float>() != 0)
        {
            //if (modifier2Action.ReadValue<float>() != 0)
            //    if (modifier1Action.ReadValue<float>() != 0)
            //        virtualIPD += 10 * FOVAction.ReadValue<float>();
            //    else
            //        virtualIPD += FOVAction.ReadValue<float>();
            //else
            //    if (FOVControl)
            //        if (modifier1Action.ReadValue<float>() != 0)
            //            FOV -= FOVAction.ReadValue<float>();
            //        else
            //            FOV -= .1f * FOVAction.ReadValue<float>();

            //GUI_Autoshow();
            FOVAxis(FOVAction.ReadValue<float>(), modifier1Action.ReadValue<float>(), modifier2Action.ReadValue<float>());
        }
        //}
#else
        //else
        //if (!inputSystem)
        //{
        if (Input.GetKeyDown(GUIKey))
            GUIOpened = !GUIOpened;

        //if (Input.GetKeyDown(S3DKey) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
        //{
        //    S3DEnabled = !S3DEnabled;
        //    //enableS3D_toggle.isOn = S3DEnabled;
        //}

        //if (Input.GetKeyDown(S3DKey) && Input.GetKey(KeyCode.LeftShift))
        //{
        //    swapLR = !swapLR;
        //    //swapLR_toggle.isOn = swapLR;
        //}

        //if (Input.GetKeyDown(S3DKey) && Input.GetKey(KeyCode.LeftAlt))
        //{
        //    optimize = !optimize;
        //    //optimize_toggle.isOn = optimize;
        //}

        //if (FOVControl)
        //{
        //    if (Input.GetKey(increaseKey) && !Input.GetKey(KeyCode.LeftControl))
        //        if (Input.GetKey(KeyCode.LeftShift))
        //            FOV += 1;
        //        else
        //            FOV += .1f;

        //    if (Input.GetKey(decreaseKey) && !Input.GetKey(KeyCode.LeftControl))
        //        if (Input.GetKey(KeyCode.LeftShift))
        //            FOV -= 1;
        //        else
        //            FOV -= .1f;
        //}

        //if (Input.GetKey(decreaseKey) && Input.GetKey(KeyCode.LeftControl))
        //    if (Input.GetKey(KeyCode.LeftShift))
        //        virtualIPD += 10;
        //    else
        //        virtualIPD += 1;

        //if (Input.GetKey(increaseKey) && Input.GetKey(KeyCode.LeftControl))
        //    if (Input.GetKey(KeyCode.LeftShift))
        //        virtualIPD -= 10;
        //    else
        //        virtualIPD -= 1;

        if (Input.GetKey(S3DKey))
        {
            //S3DKeyTimer += Time.deltaTime;
            ////Debug.Log(S3DKeyTimer);

            //if (S3DKeyTimer > 1 && !S3DKeyLoaded)
            //{
            //    Load(slotName);
            //    //S3DKeyTimer = 0;
            //    S3DKeyLoaded = true;
            //    //Debug.Log("S3DKeyLoaded");
            //}

            //GUI_Autoshow();
            S3DKeyHold();
        }

        if (Input.GetKeyUp(S3DKey))
        {
            //if (S3DKeyTimer < 1)
            //    if (Input.GetKey(KeyCode.LeftShift))
            //        swapLR = !swapLR;
            //    else
            //        if (Input.GetKey(KeyCode.LeftControl))
            //            Save(slotName);
            //        else
            //            if (Input.GetKey(KeyCode.LeftAlt))
            //                optimize = !optimize;
            //            else
            //                S3DEnabled = !S3DEnabled;
            ////else
            ////    Load(slotName);

            ////if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
            ////    S3DEnabled = !S3DEnabled;

            //S3DKeyTimer = 0;
            //S3DKeyLoaded = false;
            ////GUI_Autoshow();
            S3DKeyUp(Input.GetKey(KeyCode.LeftShift) ? 1 : 0, Input.GetKey(KeyCode.LeftControl) ? 1 : 0, Input.GetKey(KeyCode.LeftAlt) ? 1 : 0);

        }

        if (Input.GetKey(increaseKey))
        {
            //if (Input.GetKey(KeyCode.LeftControl))
            //    if (Input.GetKey(KeyCode.LeftShift))
            //        virtualIPD += 10;
            //    else
            //        virtualIPD += 1;
            //else
            //    if (FOVControl)
            //        if (Input.GetKey(KeyCode.LeftShift))
            //            FOV -= 1;
            //        else
            //            FOV -= .1f;

            //GUI_Autoshow();
            FOVAxis(1, Input.GetKey(KeyCode.LeftShift) ? 1 : 0, Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
        }

        if (Input.GetKey(decreaseKey))
        {
            //if (Input.GetKey(KeyCode.LeftControl))
            //    if (Input.GetKey(KeyCode.LeftShift))
            //        virtualIPD -= 10;
            //    else
            //        virtualIPD -= 1;
            //else
            //    if (FOVControl)
            //        if (Input.GetKey(KeyCode.LeftShift))
            //            FOV += 1;
            //        else
            //            FOV += .1f;

            //GUI_Autoshow();
            FOVAxis(-1, Input.GetKey(KeyCode.LeftShift) ? 1 : 0, Input.GetKey(KeyCode.LeftControl) ? 1 : 0);
        }
        //}
#endif

        SetCameraDataStruct();

        //if (canvas.gameObject.activeSelf && caret == null)
        //{
        //    if (PPI_inputField.transform.Find("InputField (Legacy)_PPI Input Caret"))
        //    {
        //       //Debug.Log("PPI_inputField.transform.Find");
        //        InputFieldCaretMaterial_Set(PPI_inputField);
        //        InputFieldCaretMaterial_Set(userIPD_inputField);
        //        InputFieldCaretMaterial_Set(virtualIPD_inputField);
        //        InputFieldCaretMaterial_Set(FOV_inputField);
        //        InputFieldCaretMaterial_Set(panelDepth_inputField);
        //        InputFieldCaretMaterial_Set(screenDistance_inputField);
        //        InputFieldCaretMaterial_Set(slotName_inputField);
        //    }

        //   //Debug.Log("caret == null");
        //}

        if (Screen.width != windowSize.x || Screen.height != windowSize.y)
            Resize();

        if (GUIOpened)
        {
            //Debug.Log(Input.mousePosition);
            //cursorRectTransform.anchoredPosition = new Vector2(Input.mousePosition.x / windowSize.x * canvasSize.x - canvasSize.x * 0.5f, Input.mousePosition.y / windowSize.y * canvasSize.y - canvasSize.y);
            //cursorLocalPos = new Vector2(Input.mousePosition.x / windowSize.x * canvasSize.x - canvasSize.x * 0.5f, Input.mousePosition.y / windowSize.y * canvasSize.y - canvasSize.y);
            //cursorLocalPos = new Vector2((Input.mousePosition.x - cam.rect.x * windowSize.x) / cam.pixelWidth * canvasSize.x - canvasSize.x * 0.5f, (Input.mousePosition.y - cam.rect.y * windowSize.y) / cam.pixelHeight * canvasSize.y - canvasSize.y);
            //cursorLocalPos.x = ((Input.mousePosition.x - cam.rect.x * windowSize.x) / cam.pixelWidth - .5f) * canvasSize.x * (1 + canvasEdgeOffset);
            //cursorLocalPos.x = (Input.mousePosition.x / windowSize.x - .5f) * canvasSize.x * (1 + canvasEdgeOffset);
            //cursorLocalPos.x = (Input.mousePosition.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.x = ((Input.mousePosition.x - cam.rect.x * windowSize.x) / cam.pixelWidth - .5f) * canvasSize.x + virtualIPD * .0005f * ((int)eyePriority - 1) / canvas.GetComponent<RectTransform>().lossyScale.x;
            //cursorLocalPos.y = ((Input.mousePosition.y - cam.rect.y * windowSize.y) / cam.pixelHeight - 1) * canvasSize.y;
            //cursorLocalPos.y = (Input.mousePosition.y / windowSize.y - 1) * canvasSize.y;

            Vector2 pointerPosition;

#if ENABLE_INPUT_SYSTEM
            //cursorLocalPos.x = (Pointer.current.position.value.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (Pointer.current.position.value.y / windowSize.y - 1) * canvasSize.y;
            //pointerPosition.x = Pointer.current.position.value.x;
            //pointerPosition.y = Pointer.current.position.value.y;
            pointerPosition.x = UnityEngine.InputSystem.Pointer.current.position.value.x;
            pointerPosition.y = UnityEngine.InputSystem.Pointer.current.position.value.y;
#else
            //cursorLocalPos.x = (Input.mousePosition.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (Input.mousePosition.y / windowSize.y - 1) * canvasSize.y;
            pointerPosition.x = Input.mousePosition.x;
            pointerPosition.y = Input.mousePosition.y;
#endif

            //cursorLocalPos.x = (pointerPosition.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (pointerPosition.y / windowSize.y - 1) * canvasSize.y;
            //Debug.Log((pointerPosition.y / windowSize.y - cam.rect.y) / cam.rect.height - 1);
            //Camera canvasWorldCam = canvas.worldCamera;
            //cursorLocalPos.x = ((pointerPosition.x / windowSize.x - canvasWorldCam.rect.x) / canvasWorldCam.rect.width - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = ((pointerPosition.y / windowSize.y - canvasWorldCam.rect.y) / canvasWorldCam.rect.height - 1) * canvasSize.y;
            //cursorLocalPos.x = ((pointerPosition.x / windowSize.x - canvas.worldCamera.rect.x) / canvas.worldCamera.rect.width - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = ((pointerPosition.y / windowSize.y - canvas.worldCamera.rect.y) / canvas.worldCamera.rect.height - 1) * canvasSize.y;

            Vector2 viewportLeftBottomPos = new Vector2(Mathf.Clamp01(canvas.worldCamera.rect.x), Mathf.Clamp01(canvas.worldCamera.rect.y)); //viewport LeftBottom & RightTop corner coordinates inside render window
            Vector2 viewportRightTopPos = new Vector2(Mathf.Clamp01(canvas.worldCamera.rect.xMax), Mathf.Clamp01(canvas.worldCamera.rect.yMax));
            Rect viewportRect = new Rect(viewportLeftBottomPos.x, viewportLeftBottomPos.y, viewportRightTopPos.x - viewportLeftBottomPos.x, viewportRightTopPos.y - viewportLeftBottomPos.y);
            //Debug.Log("viewportRect.x " + viewportRect.x + " viewportRect.width " + viewportRect.width);
            //Debug.Log("canvas.worldCamera.rect.x " + canvas.worldCamera.rect.x + " canvas.worldCamera.rect.xMin " + canvas.worldCamera.rect.xMin);
            cursorLocalPos.x = ((pointerPosition.x / windowSize.x - viewportRect.x) / viewportRect.width - .5f) * canvasWidthWithOffset;
            cursorLocalPos.y = ((pointerPosition.y / windowSize.y - viewportRect.y) / viewportRect.height - 1) * canvasSize.y;

            cursorRectTransform.anchoredPosition = cursorLocalPos;
            //cursorTransform.localPosition = new Vector2(Input.mousePosition.x / windowSize.x * canvasSize.x - canvasSize.x * 0.5f, Input.mousePosition.y / windowSize.y * canvasSize.y - canvasSize.y * 0.5f);
            //canvas.worldCamera.ViewportPointToRay(Vector3.zero);
            //EventSystem eSys = EventSystem.current;
            //eSys.

            if (toolTipTimer < toolTipShowDelay)
            {
                toolTipTimer += Time.deltaTime;

                if (tooltipShow && toolTipTimer >= toolTipShowDelay)
                    tooltip.SetActive(true);

                //Debug.Log("toolTipTimer");
            }

            //pointerEventData = new PointerEventData(eventSystem);
            //pointerEventData.position = new Vector2(0, 0);
            //RaycastResult rayRes = pointerEventData.pointerCurrentRaycast;
            //rayRes.screenPosition = Vector2.zero;
            //rayRes.worldPosition = Vector3.zero;
            //pointerEventData.pointerCurrentRaycast = rayRes;
            //pointerEventData.Reset();
            //pointerEventData = new PointerEventData(eventSystem) { position = new Vector2(Input.mousePosition.x - 100, Input.mousePosition.y) };

            //List<RaycastResult> results = new List<RaycastResult>();
            //raycaster.Raycast(pointerEventData, results);

            //eventSystem.RaycastAll(pointerEventData, results);
            ////StandaloneInputModule iModule = eventSystem.GetComponent<StandaloneInputModule>();
            ////BaseInput bInput = iModule.inputOverride;
            //bInput.compositionCursorPos = Vector2.zero;
            ////bInput.enabled = false;
            //iModule.inputOverride = bInput;

            //if (results.Count > 0)
            //   //Debug.Log("Hit " + results[0].gameObject.name);

            //EventSystem.current = eventSystem;

            //Debug.Log(pointerEventData.position);
            //Debug.Log(panelDepth_sliderIsDragging);
            //Debug.Log(Input.mousePosition.x);
        }
        //Debug.Log("Update " + Time.time);

        //check variable changes after Keys pressed
        if (lastGUIOpened != GUIOpened)
        {
            if (!lastGUIOpened)
            {
                //if (inputSystem)
#if STARTER_ASSETS_PACKAGES_CHECKED
                {
                    cursorLockedDefault = starterAssetsInputs.cursorLocked;
                    cursorInputForLookDefault = starterAssetsInputs.cursorInputForLook;
                }
#endif
                //else
                cursorLockModeDefault = Cursor.lockState;
            }

            lastGUIOpened = GUIOpened;
            GUI_Set();
        }

        if (GUI_autoshowTimer > 0)
        {
            autoshow = true;
            GUI_autoshowTimer -= Time.deltaTime;
            GUI_autoshowTimer *= GUIOpened ? 0 : 1;
            //Debug.Log(GUI_autoshowTimer);

            if (!canvas.gameObject.activeSelf)
            {
                canvas.gameObject.SetActive(true);
                GUIVisible = true;
                ClipSet();
                HDRPSettings_Set();
                //Debug.Log("canvas.gameObject.SetActive(true)");
            }
        }
        else
            if (autoshow)
        {
            autoshow = false;

            if (!GUIOpened)
            {
                canvas.gameObject.SetActive(false);
                GUIVisible = false;
                ClipSet();
                HDRPSettings_Set();
            }

            //Debug.Log("autoshow = false");
        }

        if (lastS3DEnabled != S3DEnabled)
        {
            lastS3DEnabled = S3DEnabled;
            enableS3D_toggle.isOn = S3DEnabled;
            Aspect_Set();
            ClipSet();
            CameraStackSet();
        }

        if (lastEyePriority != eyePriority)
        {
            lastEyePriority = eyePriority;
            CamSet();
            //Canvas_Set();
        }

        if (lastSwapLR != swapLR)
        {
            lastSwapLR = swapLR;
            swapLR_toggle.isOn = swapLR;
            CamSet();
        }

        if (lastOptimize != optimize)
        {
            lastOptimize = optimize;
            optimize_toggle.isOn = optimize;
            //ViewSet();
            //RT_Set();
            Aspect_Set();
        }

        if (lastVSync != vSync)
        {
            lastVSync = vSync;
            vSync_toggle.isOn = vSync;

            VSyncSet();
        }

        if (lastMethod != method)
        {
            lastMethod = method;

            //if (method == Method.SideBySide_HMD)
            //{
            //    cam.aspect *= .5f;
            //}
            //else
            //{
            //    cam.aspect = windowSize.x / windowSize.y;
            //}

            //Resize();
            Aspect_Set();
            //ViewSet();
            //RT_Set();
            outputMethod_dropdown.value = (int)method;
        }

        if (lastPPI != PPI)
        {
            //lastPPI = PPI;
            PPISet();
        }

        if (lastUserIPD != userIPD)
        {
            //lastUserIPD = userIPD;
            UserIPDSet();
        }

        if (lastVirtualIPD != virtualIPD)
        {
            //lastVirtualIPD = virtualIPD;
            VirtualIPDSet();
        }

        if (lastVirtualIPDMax != virtualIPDMax)
        {
            lastVirtualIPDMax = virtualIPDMax;
            virtualIPD_slider.maxValue = virtualIPDMax;
            virtualIPD_slider.value = virtualIPD;
        }

        if (lastMatchUserIPD != matchUserIPD)
        {
            lastMatchUserIPD = matchUserIPD;
            matchUserIPD_toggle.isOn = matchUserIPD;
            VirtualIPDSet();
        }

        //if (lastPixelPitch != pixelPitch)
        //{
        //    lastPixelPitch = pixelPitch;
        //    PixelPitchSet();
        //}

        if (!FOVSetInProcess && cam.fieldOfView != vFOV) //check camera FOV changes to set FOV from other scripts
        {
            //Debug.Log("cam.fieldOfView != vFOV");
            //Debug.Log("camFOVChanged");
            camFOVChanged = true;
            FOV_Set();
        }

        if (lastFOV != FOV)
        {
            //lastFOV = FOV;
            FOV_Set();
        }

        if (lastMinMaxFOV != FOVMinMax)
        {
            lastMinMaxFOV = FOVMinMax;
            FOV_slider.minValue = FOVMinMax.x;
            FOV_slider.maxValue = FOVMinMax.y;
        }

        if (lastPanelDepth != panelDepth)
        {
            PanelDepthSet();
            Canvas_Set();
        }

        if (lastPanelDepthMinMax != panelDepthMinMax)
        {
            //panelDepthMinMax.x = Mathf.Max(panelDepthMinMax.x, .5f);
            //lastPanelDepthMinMax = panelDepthMinMax;
            //panelDepth_slider.minValue = panelDepthMinMax.x;
            //panelDepth_slider.maxValue = panelDepthMinMax.y;
            PanelDepthMinMaxSet();
        }

        //newList = new List<UniversalAdditionalCameraData>();
        //newList.Add(camClone.GetComponent<UniversalAdditionalCameraData>());

        //cameraDataStruct = new CameraDataStruct(cam.GetUniversalAdditionalCameraData().renderPostProcessing, cam.GetUniversalAdditionalCameraData().antialiasing);
        //cameraDataStruct = new CameraDataStruct(camData.renderType, camData.renderPostProcessing, camData.antialiasing);
        //SetCameraDataStruct();

        //if (cloneCamera)
        //{
        //if (camData)
        //SetCameraDataStruct();

        //cameraDataStruct.renderPostProcessing = camData.renderPostProcessing;
        //cameraDataStruct.antialiasing = camData.antialiasing;
        //Debug.Log(camData.scriptableRenderer);
        //Debug.Log("lastCameraDataStruct.Equals(cameraDataStruct) " + lastCameraDataStruct.Equals(cameraDataStruct));
        //Debug.Log("lastCameraDataStruct " + lastCameraDataStruct.ToString() + " cameraDataStruct " + cameraDataStruct.ToString());

        //Debug.Log(lastCameraDataStruct.cameraRenderType + " " + cameraDataStruct.cameraRenderType);
        //Debug.Log(lastCameraDataStruct.scriptableRenderer + " " + cameraDataStruct.scriptableRenderer);
        //Debug.Log(lastCameraDataStruct.renderPostProcessing + " " + cameraDataStruct.renderPostProcessing);
        //Debug.Log(lastCameraDataStruct.antialiasing + " " + cameraDataStruct.antialiasing);
        //Debug.Log(lastCameraDataStruct.antialiasingQuality + " " + cameraDataStruct.antialiasingQuality);
        //Debug.Log(lastCameraDataStruct.stopNaN + " " + cameraDataStruct.stopNaN);
        //Debug.Log(lastCameraDataStruct.dithering + " " + cameraDataStruct.dithering);
        //Debug.Log(lastCameraDataStruct.renderShadows + " " + cameraDataStruct.renderShadows);
        //Debug.Log(lastCameraDataStruct.depth + " " + cameraDataStruct.depth);
        //Debug.Log(lastCameraDataStruct.useOcclusionCulling + " " + cameraDataStruct.useOcclusionCulling);

        //Debug.Break();

        //if (lastInterlaceType != interlaceType)
        //{
        //    lastInterlaceType = interlaceType;
        //    RT_Set();
        //}

        //  if (lastAnaglyphLeftColor != anaglyphLeftColor)
        //  {
        //      lastAnaglyphLeftColor = anaglyphLeftColor;
        //S3DMaterial.SetColor("_LeftCol", anaglyphLeftColor);
        //  }

        //  if (lastAnaglyphRightColor != anaglyphRightColor)
        //  {
        //      lastAnaglyphRightColor = anaglyphRightColor;
        //S3DMaterial.SetColor("_RightCol", anaglyphRightColor);
        //  }

        if (lastSlotName != slotName)
        {
            slotName_inputField.text = slotName;
            //DropdownSet();
            lastSlotName = slotName;
        }

        if (lastCamRect != cam.rect)
        {
            lastCamRect = cam.rect;
            //leftCam.rect = rightCam.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
            CamRect_Set();
            //Resize();
            Aspect_Set();
            //ViewSet();
            //RT_Set();
        }

        if (lastNearClipHack != nearClipHack)
        {
            lastNearClipHack = nearClipHack;
            //RT_Set();
            ClipSet();
        }

        if (lastMatrixKillHack != matrixKillHack)
        {
            lastMatrixKillHack = matrixKillHack;
            RT_Set();
        }

        if (!GUIAsOverlay && lastCanvasLocalPosZ != canvasLocalPosZ)
        {
            lastCanvasLocalPosZ = canvasLocalPosZ;
            ClipSet();
        }

        ////if (lastAdditionalS3DCameras != additionalS3DCameras)
        ////if (lastAdditionalS3DCameras.cameras != additionalS3DCameras.cameras)
        //if (!lastAdditionalS3DCameras.Equals(additionalS3DCameras))
        ////if (!lastAdditionalS3DCameras.cameras.Equals(additionalS3DCameras.cameras))
        ////if (!ReferenceEquals(lastAdditionalS3DCameras, additionalS3DCameras.ToArray()))
        //{
        //    //lastAdditionalS3DCameras = new Camera[additionalS3DCameras.Count];
        //    additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);
        //    //lastAdditionalS3DCameras = additionalS3DCameras;
        //    //lastAdditionalS3DCameras = (Camera[])additionalS3DCameras.Clone();
        //    //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);
        //    //Debug.Log("lastAdditionalS3DCameras != additionalS3DCameras");
        //    //Debug.Log("lastAdditionalS3DCameras != additionalS3DCameras " + lastAdditionalS3DCameras.Length + " " + additionalS3DCameras.Length);
        //    //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);

        //    for (int i = 0; i < additionalS3DCameras.Count; i++)
        //    {
        //        Debug.Log("component " + additionalS3DCameras[i].name + " " + lastAdditionalS3DCameras[i].name);
        //    }
        //}

        if (lastGUIAsOverlay != GUIAsOverlay)
        {
            //if (GUIAsOverlay)
            //{
            //    //panelDepth /= panelDepthMinMax.y;
            //    newPanelDepth = panelDepth / panelDepthMinMax.y;
            //    panelDepthMinMax = new Vector2(0, 1);
            //}
            //else
            //{
            //    panelDepthMinMax = new Vector2(1, 100);
            //    //panelDepth *= panelDepthMinMax.y;
            //    newPanelDepth = panelDepth * panelDepthMinMax.y;
            //}

            ////Debug.Log("panelDepthMinMax " + panelDepthMinMax + " panelDepth " + panelDepth);
            //Debug.Log("panelDepthMinMax " + panelDepthMinMax + " newPanelDepth " + newPanelDepth);

            ////panelDepth_slider.minValue = panelDepthMinMax.x;
            ////panelDepth_slider.maxValue = panelDepthMinMax.y;
            ////PanelDepth_InputField(panelDepth.ToString());
            ////panelDepth_slider.value = panelDepth;
            ////PanelDepth_Slider(panelDepth);
            ////panelDepth_inputField.text = panelDepth.ToString();
            ////Debug.Log("panelDepth_slider.value " + panelDepth_slider.value);
            lastGUIAsOverlay = GUIAsOverlay;
            OnOffToggle();
            //Invoke("DelayedPanelDepthSet", Time.deltaTime);
        }

        if (lastGUISizeKeep != GUISizeKeep)
        {
            lastGUISizeKeep = GUISizeKeep;
            Aspect_Set();
        }

        if (lastCameraPrefab != cameraPrefab)
        {
            lastCameraPrefab = cameraPrefab;
            //this.enabled = false;
            //this.enabled = true;
            //OnDisable();
            //OnEnable();
            OnOffToggle();
        }

        if (lastRTFormat != RTFormat)
        {
            lastRTFormat = RTFormat;
            //this.enabled = false;
            //this.enabled = true;
            OnOffToggle();
        }

        if (lastSetMatrixDirectly != setMatrixDirectly)
        {
            lastSetMatrixDirectly = setMatrixDirectly;
            //this.enabled = false;
            //this.enabled = true;
            OnOffToggle();
        }

        if (lastCloneCamera != cloneCamera)
        {
            lastCloneCamera = cloneCamera;
            OnOffToggle();
        }

        if (lastLoadSettingsFromFile != loadSettingsFromFile)
        {
            lastLoadSettingsFromFile = loadSettingsFromFile;
            OnOffToggle();
        }

#if URP
        if (lastCameraStack.Length != cameraStack.Count)
        {
            Debug.Log("lastCameraStack.Length != cameraStack.Count " + lastCameraStack.Length + " " + cameraStack.Count);
            OnOffToggle();
        }
        else
            for (int i = 0; i < lastCameraStack.Length; i++)
                if (lastCameraStack[i] != cameraStack[i])
                {
                    Debug.Log("lastCameraStack[i] != cameraStack[i] " + lastCameraStack[i] + " " + cameraStack[i]);
                    OnOffToggle();
                    break;
                }
#endif

        if (lastAdditionalS3DCameras.Length != additionalS3DCameras.Count)
        {
            Debug.Log("lastAdditionalS3DCameras.Length != additionalS3DCameras.Count " + lastAdditionalS3DCameras.Length + " " + additionalS3DCameras.Count);
            //AdditionalS3DCamerasUpdate();
            OnOffToggle();
        }
        else
            for (int i = 0; i < lastAdditionalS3DCameras.Length; i++)
                if (lastAdditionalS3DCameras[i] != additionalS3DCameras[i])
                {
                    Debug.Log("lastAdditionalS3DCameras[i] != additionalS3DCameras[i] " + lastAdditionalS3DCameras[i] + " " + additionalS3DCameras[i]);
                    //Debug.Break();
                    //AdditionalS3DCamerasUpdate();
                    OnOffToggle();
                    break;
                }

        //void AdditionalS3DCamerasUpdate()
        //{
        //    Debug.Log("AdditionalS3DCamerasUpdate");
        //    //lastAdditionalS3DCameras = new Camera[additionalS3DCameras.Count];
        //    //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);
        //    //lastAdditionalS3DCameras = additionalS3DCameras.ToArray();
        //    OnOffToggle();
        //    //Debug.Break();
        //}

        //if (!Equals(lastCameraDataStruct, cameraDataStruct))
        if (Time.time > setLastCameraDataStructTime && !lastCameraDataStruct.Equals(cameraDataStruct))
        {
            //Debug.Break();
            Debug.Log("!lastCameraDataStruct.Equals(cameraDataStruct)");
            //universalAdditionalCameraData = cam.GetUniversalAdditionalCameraData();
            lastCameraDataStruct = cameraDataStruct;
            //cameraDataStruct = cam.GetUniversalAdditionalCameraData();
            //lastUniversalAdditionalCameraData = universalAdditionalCameraData;
            //universalAdditionalCameraData.renderPostProcessing = cam.GetUniversalAdditionalCameraData().renderPostProcessing;
            OnOffToggle();
        }

        //Debug.Log(universalAdditionalCameraData.Equals(cam.GetUniversalAdditionalCameraData()));
        //}

        void OnOffToggle()
        {
            Debug.Log("OnOffToggle");
            enabled = false;
            //enabled = true;
            Invoke("Enable", 0); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
            //Invoke("Enable", Time.deltaTime * 4); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
        }
    }

    void Enable()
    {
        enabled = true;
    }

    void PanelDepthMinMaxSet()
    {
        //panelOverlayDepthMinMax.x = Mathf.Min(panelDepthMinMax.x, 1);
        //panelOverlayDepthMinMax.y = Mathf.Clamp(panelDepthMinMax.y, panelDepthMinMax.x, 1);

        //panel3DdepthMinMax.x = Mathf.Max(panelDepthMinMax.x, .5f);
        //panel3DdepthMinMax.y = Mathf.Max(panelDepthMinMax.y, panelDepthMinMax.x);

        //if (GUIAsOverlay)
        //{
            //panelDepthMinMax = new Vector2(Mathf.Min(panelDepthMinMax.x, 1), Mathf.Clamp(panelDepthMinMax.y, Mathf.Min(panelDepthMinMax.x, 1), 1));
            panelDepthMinMax.x = Mathf.Min(panelDepthMinMax.x, 1);
            panelDepthMinMax.y = Mathf.Clamp(panelDepthMinMax.y, panelDepthMinMax.x, 1);
            //PanelOverlayDepthMinMaxSet();
            //panelDepthMinMax = panelOverlayDepthMinMax;
        //}
        //else
        //{
        //    //panelDepthMinMax = new Vector2(Mathf.Max(panelDepthMinMax.x, .5f), Mathf.Max(panelDepthMinMax.y, Mathf.Max(panelDepthMinMax.x, .5f)));
        //    panelDepthMinMax.x = Mathf.Max(panelDepthMinMax.x, .5f);
        //    panelDepthMinMax.y = Mathf.Max(panelDepthMinMax.y, panelDepthMinMax.x);
        //    //panelDepthMinMax = Panel3DdepthMinMaxSet(panelDepthMinMax);
        //    //Panel3DdepthMinMaxSet();
        //    //panelDepthMinMax = panel3DdepthMinMax;
        //}

        //Vector2 Panel3DdepthMinMaxSet(Vector2 vector2)
        //{
        //    vector2.x = Mathf.Max(vector2.x, .5f);
        //    vector2.y = Mathf.Max(vector2.y, vector2.x);
        //    return vector2;
        //}

        lastPanelDepthMinMax = panelDepthMinMax;
        PanelDepthSet(); //to prevent panelDepth reset by slider it must be set before
        panelDepth_slider.minValue = panelDepthMinMax.x;
        panelDepth_slider.maxValue = panelDepthMinMax.y;
        Debug.Log("PanelDepthMinMaxSet" + panelDepthMinMax);

        //PanelDepthSet();
    }

    void PanelDepthSet()
    {
        panelDepth = Mathf.Clamp(panelDepth, panelDepthMinMax.x, panelDepthMinMax.y);
        Debug.Log("PanelDepthSet " + panelDepth);
        panelDepth_slider.value = panelDepth;
        //panelDepth_inputField.text = Convert.ToString(panelDepth);
        panelDepth_inputField.text = panelDepth.ToString();
        lastPanelDepth = panelDepth;

        //Canvas_Set();
    }

    //void DelayedPanelDepthSet()
    //{
    //    panelDepth = newPanelDepth;
    //}

    //void PanelOverlayDepthMinMaxSet()
    //{
    //    panelOverlayDepthMinMax.x = Mathf.Min(panelDepthMinMax.x, 1);
    //    panelOverlayDepthMinMax.y = Mathf.Clamp(panelDepthMinMax.y, panelDepthMinMax.x, 1);
    //}

    //void Panel3DdepthMinMaxSet()
    //{
    //    panel3DdepthMinMax.x = Mathf.Max(panelDepthMinMax.x, .5f);
    //    panel3DdepthMinMax.y = Mathf.Max(panelDepthMinMax.y, panelDepthMinMax.x);
    //}

    void HDRPSettings_Set()
    {
#if HDRP
        Debug.Log("HDRPSettings_Set");

        if (GUIAsOverlay)
        {
            if (GUIVisible)
                HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            else
                //HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
                HDRPSettings.colorBufferFormat = defaultHDRPSettings.colorBufferFormat;

            typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //Invoke("RT_Set", 5);
        }
#endif
    }

    //void HDRPcolorBufferFormat1_Set()
    //{
    //    //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R11G11B10;
    //    RenderPipelineSettings settings = HDRPAsset.currentPlatformRenderPipelineSettings;
    //    settings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R11G11B10;
    //    typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, settings);
    //}

    //void HDRPcolorBufferFormat2_Set()
    //{
    //    //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
    //    RenderPipelineSettings settings = HDRPAsset.currentPlatformRenderPipelineSettings;
    //    settings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
    //    typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, settings);
    //}

#if ENABLE_INPUT_SYSTEM
    //void PlayerInputEnable()
    //{
    //    playerInput.enabled = true;
    //}

    public void OnGUIAction(InputAction.CallbackContext context)
    {
        //Debug.Log("OnGUIAction value.isPressed " + value.isPressed);
        GUIOpened = !GUIOpened;
    }

    //public void OnS3DAction(InputAction.CallbackContext context)
    //{
    //    S3DEnabled = !S3DEnabled;
    //}

    //public void OnVirtualIPDAction(InputAction.CallbackContext context)
    //{
    //    //if (Input.GetKey(increaseKey))
    //    //{
    //    //    if (Input.GetKey(KeyCode.LeftControl))
    //    //        if (Input.GetKey(KeyCode.LeftShift))
    //    //            virtualIPD += 10;
    //    //        else
    //    //            virtualIPD += 1;
    //    //    else
    //    //        if (FOVControl)
    //    //        if (Input.GetKey(KeyCode.LeftShift))
    //    //            FOV -= 1;
    //    //        else
    //    //            FOV -= .1f;

    //    //    GUI_Autoshow();
    //    //}

    //    //Debug.Log(FOVAction.ReadValue<float>());
    //    ////virtualIPD += context.ReadValue<float>();
    //    //virtualIPD += FOVAction.ReadValue<float>();
    //}

    //public void OnModifier1Action(InputAction.CallbackContext context)
    //{
    //    //Debug.Log("modifier1Action");
    //    //Debug.Log(modifier1Action.ReadValue<float>());
    //}

    //public void OnModifier2Action(InputAction.CallbackContext context)
    //{
    //    //Debug.Log("modifier2Action");
    //}

    //public void OnMove(InputValue value)
    //{
    //    //Debug.Log("OnMove value " + value.Get<Vector2>());
    //}

    //public void Test()
    //{
    //    //Debug.Log("Test");
    //}
#endif

    //Component CopyComponent(Component original, GameObject destination)
    //{
    //    System.Type type = original.GetType();
    //    Component copy = destination.AddComponent(type);
    //    // Copied fields can be restricted with BindingFlags
    //    System.Reflection.FieldInfo[] fields = type.GetFields();
    //    foreach (System.Reflection.FieldInfo field in fields)
    //    {
    //        field.SetValue(copy, field.GetValue(original));
    //    }
    //    return copy;
    //}

#if CINEMACHINE
    void GetVCam()
    {
        Debug.Log("GetVCam");
        defaultVCam = vCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera;
        //Debug.Log("GetVCam() " + vCam);

        //if (vCam == null)
        //    Invoke("GetVCam", Time.deltaTime);
        //else
        //    sceneNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
        ////{
        ////    //cam.cullingMask = 0;

        ////    if (nearClipHack)
        ////    {
        ////        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
        ////        //Cinemachine.CinemachineVirtualCamera vc = (Cinemachine.CinemachineVirtualCamera)vCam;
        ////        //vc.m_Lens.NearClipPlane = -1;
        ////    }

        ////    //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        ////    //Invoke("VCamActivate", 1);
        ////}
        //vCamSceneClipSetInProcess = true;

        if (vCam == null)
            Invoke("GetVCam", Time.deltaTime);
        else
            VCamSceneClipSet();

        //Invoke("VCamActivate", 1);
        //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //Debug.Log("GetVCam sceneNearClip " + sceneNearClip);
    }

    //void VCamActivate()
    //{
    //    //Cinemachine.CinemachineBrain.SoloCamera = vCam;
    //}

    void VCamCullingOff()
    {
        Debug.Log("VCamCullingOff");

        if (vCam != null)
        {
            //cam.cullingMask = 2147483647;
            cam.cullingMask = 0;
            //Debug.Log(cam.cullingMask);
            //cam.Reset();
            //cam.nearClipPlane = -1; //Hack for more fps in SRP(Scriptable Render Pipeline)
            //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
        }
        else
            Invoke("VCamCullingOff", Time.deltaTime);
    }

    void VCamFOVSet()
    {
        Debug.Log("VCamFOVSet");
        FOVSetInProcess = true;

        if (vCam != null)
        {
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FieldOfView = vFOV;
            //FOVSetInProcess = false;
            CheckCamFOVSet();
        }
        else
            Invoke("VCamFOVSet", Time.deltaTime);
    }

    void VCamSceneClipSet()
    {
        Debug.Log("VCamSceneClipSet cam.nearClipPlane " + cam.nearClipPlane);
        //vCamSceneClipSetInProcess = true;
        //vCamSceneClipIsReady = false;

        ////if (!nearClipHackApplied && vCam != null)
        //if (vCam != null && !nearClipHackApplied && !vCamSceneClipRestoreInProcess)
        //{
            sceneNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
            Debug.Log("VCamSceneClipSet sceneNearClip " + sceneNearClip);
            sceneFarClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane;
            //vCamSceneClipSetInProcess = false;
            vCamSceneClipIsReady = true;
        //}
        //else
        //{
        //    if (nearClipHackApplied)
        //        VCamClipRestore();

        //    Invoke("VCamSceneClipSet", Time.deltaTime);
        //}
    }

    void VCamClipSet()
    {
        Debug.Log("VCamClipSet");
        //vCamClipSetInProcess = true;
        nearClipHackApplied = false;

        //if (vCam != null)
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
        //else
        //    Invoke("VCamClipSet", Time.deltaTime);

        //if (vCam != null && !vCamSceneClipSetInProcess)
        if (vCamSceneClipIsReady)
        {
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane = farClip;
            Debug.Log("VCamClipSet nearClip " + nearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
            //vCamClipSetInProcess = false;
        }
        else
            //if (!vCamSceneClipRestoreInProcess)
                Invoke("VCamClipSet", Time.deltaTime);
            //else
            //    vCamClipSetInProcess = false;
    }

    void VCamNearClipHack()
    {
        Debug.Log("VCamNearClipHack " + nearClipHackApplied);
        //nearClipHackApplied = true;

        //if (vCam != null && !vCamSceneClipSetInProcess && !vCamSceneClipRestoreInProcess)
        if (vCamSceneClipIsReady)
        {
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
            Debug.Log("VCamNearClipHack ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane + " " + Time.time);
            nearClipHackApplied = true;
        }
        else
            Invoke("VCamNearClipHack", Time.deltaTime);
    }

    void VCamClipRestore()
    {
        Debug.Log("VCamClipRestore sceneNearClip " + sceneNearClip);
        //vCamSceneClipRestoreInProcess = true;
        nearClipHackApplied = false;

        //if (vCam != null)
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
        //else
        //    Invoke("VCamClipRestore", Time.deltaTime);

        //if (vCam != null && !vCamClipSetInProcess)
        if (vCamSceneClipIsReady)
        {
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane = sceneFarClip; //restore vCam default FarClipPlane
            Debug.Log("VCamClipRestore ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
            //vCamSceneClipRestoreInProcess = false;
        }
        else
            Invoke("VCamClipRestore", Time.deltaTime);
    }
#endif

    void Tooltip(GameObject gObject, string text)
    {
        trigger = gObject.AddComponent<EventTrigger>();
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { TooltipShow(text); });
        trigger.triggers.Add(entry);
        trigger = gObject.AddComponent<EventTrigger>();
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerExit;
        entry.callback.AddListener((eventData) => { TooltipHide(); });
        trigger.triggers.Add(entry);
    }

    Vector2 posDiff; //position difference between panel and cursor
    //Vector2 cursorLocalPos;

    void MouseDragStart()
    {
        posDiff = panel.GetComponent<RectTransform>().anchoredPosition - cursorLocalPos;
    }

    void MouseDrag()
    {
        //Debug.Log(cursorLocalPos);
        panel.GetComponent<RectTransform>().anchoredPosition = cursorLocalPos + posDiff;
    }

    void PanelDepthSlider_DragStart()
    {
        panelDepth_sliderIsDragging = true;
    }

    void PanelDepthSlider_DragEnd()
    {
        panelDepth_sliderIsDragging = false;
        CanvasOffset_Set();
    }

    void InputFieldAutofocus(InputField field)
    {
        trigger = field.gameObject.AddComponent<EventTrigger>();
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => { FieldPointerEnter(field); });
        trigger.triggers.Add(entry);
        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerExit;
        entry.callback.AddListener((eventData) => { FieldPointerExit(field); });
        trigger.triggers.Add(entry);
    }

    //void DropdownClick(Dropdown dropdown)
    //{
    //    trigger = dropdown.gameObject.AddComponent<EventTrigger>();
    //    entry = new EventTrigger.Entry();
    //    entry.eventID = EventTriggerType.PointerClick;
    //    entry.callback.AddListener((eventData) => { PointerClick(dropdown); });
    //    trigger.triggers.Add(entry);
    //}

    //void DropdownListHide(Dropdown dropdown)
    //{
    //    trigger = dropdown.template.gameObject.AddComponent<EventTrigger>();
    //    entry = new EventTrigger.Entry();
    //    entry.eventID = EventTriggerType.PointerExit;
    //    entry.callback.AddListener((eventData) => { ListPointerExit(dropdown); });
    //    trigger.triggers.Add(entry);
    //}

    void TooltipShow(string text)
    {
        tooltipShow = true;
        toolTipTimer = 0;
        //tooltip.SetActive(true);
        tooltipText.text = text;
        tooltipBackgroundRect.sizeDelta = new Vector2(tooltipText.preferredWidth, tooltipText.preferredHeight);
    }

    void TooltipHide()
    {
        tooltipShow = false;
        tooltip.SetActive(false);
    }

    float imageOffset;

    void Canvas_Set()
    {
        //canvasLocalPosZ = screenDistance * panelDepth * virtualIPD / userIPD * 0.001f;
        //float panelDepthAsScreenDistance = 1 / (1 - panelDepth);
        //canvasLocalPosZ = screenDistance * panelDepthAsScreenDistance * virtualIPD / userIPD * 0.001f;

        if (GUIAsOverlay)
        {
            //canvas.GetComponent<RectTransform>().localScale = Vector3.one;

            if (S3DEnabled)
            {
                canvas.GetComponent<RectTransform>().localPosition = Vector3.zero;
                canvas.GetComponent<RectTransform>().localScale = Vector3.one;
                float canvasHalfSizeX = canvasSize.x * .5f;
                float canvasHalfSizeY = canvasSize.y * .5f;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = canvasRayCam;
                //canvas.transform.localRotation = Quaternion.identity;
                //canvasCam.targetTexture = leftCamRT; //required to clear the screen window when the main camera viewport rectangle is not fully occupied

                if (canvasCam.targetTexture == null)
                {
                    Debug.Log("canvasCam.targetTexture == null");
                    canvasCam.targetTexture = leftCamRT; //required to clear the screen window when the main camera viewport rectangle is not fully occupied
                }

                canvasCam.enabled = true;
                canvasCam.orthographicSize = canvasHalfSizeY;
                //canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
                //canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, canvasCam.nearClipPlane, canvasCam.farClipPlane);
                canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX, canvasHalfSizeX, -canvasHalfSizeY, canvasHalfSizeY, canvasCam.nearClipPlane, canvasCam.farClipPlane);
                //canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY - canvasHalfSizeY * .5f, canvasHalfSizeY - canvasHalfSizeY * .5f, canvasCam.nearClipPlane, canvasCam.farClipPlane);
                //Debug.Log(canvasCamMatrix);
            }
            else
            {
                //canvasCam.targetTexture = null;
                canvasCam.enabled = false;
                //canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
            }
        }
        else
        {
            //canvasLocalPosZ = screenDistance * panelDepth * virtualIPD / userIPD * 0.001f;
            float panelDepthAsScreenDistance = 1 / (1 - panelDepth);
            panelDepthAsScreenDistance = Mathf.Min(panelDepthAsScreenDistance, 100);
            //Debug.Log("panelDepthAsScreenDistance " + panelDepthAsScreenDistance);
            canvasLocalPosZ = screenDistance * panelDepthAsScreenDistance * virtualIPD / userIPD * 0.001f;
            //Debug.Log("canvasLocalPosZ " + canvasLocalPosZ);
            float canvasScale = canvasLocalPosZ * Mathf.Tan(FOV * Mathf.PI / 360) * 2 / canvasSize.x;
            //Debug.Log("canvasScale " + canvasScale);
            //canvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, canvasLocalPosZ);
            canvas.GetComponent<RectTransform>().localPosition = new Vector3(virtualIPD * .0005f * -((int)eyePriority - 1) * (S3DEnabled ? 1 : 0), 0, canvasLocalPosZ);
            canvas.GetComponent<RectTransform>().localScale = new Vector3(canvasScale, canvasScale, canvasScale);

            //canvasRayCam.orthographicSize = canvasSize.y * canvas.transform.lossyScale.y * .5f;

            //if (!panelDepth_sliderIsDragging) //prevent flatter due canvas resize while slided dragging by pointer
            //    CanvasOffset_Set();
        }

        Debug.Log("canvas.transform.lossyScale.y " + canvas.transform.lossyScale.y);
        canvasRayCam.orthographicSize = canvasSize.y * canvas.transform.lossyScale.y * .5f;
        //canvasHalfSizeY = canvasSize.y * canvas.transform.lossyScale.y * .5f;
        //canvasRayCam.orthographicSize = canvasHalfSizeY;
        //canvasCamMatrix = canvasCam.projectionMatrix;

        if (!panelDepth_sliderIsDragging) //prevent flatter due canvas resize while slided dragging by pointer
            CanvasOffset_Set();

        //Debug.Log(canvas.GetComponent<RectTransform>().localPosition);
        //Debug.Log("Canvas_Set canvasLocalPosZ " + canvasLocalPosZ);
        //Debug.Log("Canvas_Set canvasRayCam.orthographicSize " + canvasRayCam.orthographicSize);
    }

    float canvasWidthWithOffset;

    void CanvasOffset_Set()
    {
        float canvasEdgeOffset;

        if (method == Method.SideBySide_HMD)
        {
            //if (GUIAsOverlay)
            //    canvasEdgeOffset = 1 - imageOffset * panelDepth;
            //else
            //    canvasEdgeOffset = 1 - imageOffset * (1 - (1 / panelDepth));

            canvasEdgeOffset = 1 - imageOffset * panelDepth;

            //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasSize.x * 2 - canvasSize.x * canvasEdgeOffset * Convert.ToUInt16(S3DEnabled), canvasSize.y);
            //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasSize.x * 2 - canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0), canvasSize.y);
            //canvasWidthWithOffset = canvasSize.x * 2 - canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0);
            //canvasWidthWithOffset = canvasSize.x + canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0);
        }
        else
        {
            //if (GUIAsOverlay)
            //    canvasEdgeOffset = imageOffset * panelDepth;
            //else
            //    canvasEdgeOffset = imageOffset * Mathf.Abs(1 / panelDepth - 1);

            canvasEdgeOffset = imageOffset * panelDepth;

            //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasSize.x + canvasSize.x * canvasEdgeOffset * Convert.ToSingle(S3DEnabled), canvasSize.y);
            //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasSize.x + canvasSize.x * canvasEdgeOffset * Convert.ToUInt16(S3DEnabled), canvasSize.y);
            //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasSize.x + canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0), canvasSize.y);
            //canvasWidthWithOffset = canvasSize.x + canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0);
        }

        canvasWidthWithOffset = canvasSize.x + canvasSize.x * canvasEdgeOffset * (S3DEnabled ? 1 : 0);
        canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(canvasWidthWithOffset, canvasSize.y);
        canvasRayCam.aspect = canvas.GetComponent<RectTransform>().sizeDelta.x / canvasSize.y;

        //Debug.Log(canvasRayCam.aspect);
        Debug.Log("Canvas_Set canvasEdgeOffset " + canvasWidthWithOffset);
    }

    float aspect;

    void Resize()
    {
        //Debug.Log("Resize");
        windowSize = new Vector2(Screen.width, Screen.height);
        //viewportSize = new Vector2(windowSize.x * cam.rect.width, windowSize.y * cam.rect.height);
        //viewportSize = new Vector2(cam.pixelWidth, cam.pixelHeight);
        //aspect = viewportSize.x / viewportSize.y;
        //aspect = windowSize.x / windowSize.y;
        //aspect = viewportSize.x / viewportSize.y;
        //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(windowSize.x, windowSize.y);
        //canvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, windowSize.x * 0.5f);
        Aspect_Set();
    }

    void Aspect_Set()
    {
        //aspect = cam.aspect;
        aspect = cam.pixelWidth / (float)cam.pixelHeight;

        if (S3DEnabled && method == Method.SideBySide_HMD)
            aspect *= .5f;

        //cam.aspect = aspect;
        leftCam.aspect = rightCam.aspect = cam.aspect = aspect;

        if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                    c.cameraLeft.aspect = c.cameraRight.aspect = c.camera.aspect = aspect;

        //if (GUISizeKeep)
        //    canvasSize = new Vector2(cam.pixelWidth, cam.pixelWidth / aspect);
        //else
        //    canvasSize = new Vector2(canvasSize.y * aspect, canvasSize.y);

        if (GUISizeKeep)
            canvasSize = new Vector2(cam.pixelWidth, cam.pixelWidth / aspect);
        else
            canvasSize = new Vector2(canvasDefaultSize.y * aspect, canvasDefaultSize.y);

        //Debug.Log("Aspect_Set Resize cam.rect " + cam.rect);
        //Debug.Log("Aspect_Set aspect " + aspect);
        //Debug.Log("Aspect_Set cam.pixelWidth " + cam.pixelWidth + " cam.pixelHeight " + cam.pixelHeight);

        //ViewSet();
        FOV_Set();
        RT_Set();
        //Canvas_Set();
        //CanvasOffset_Set();
    }

    void S3DKeyHold()
    {
        S3DKeyTimer += Time.deltaTime;
        //Debug.Log(S3DKeyTimer);

        if (S3DKeyTimer > 1 && !S3DKeyLoaded)
        {
            Load(slotName);
            //S3DKeyTimer = 0;
            S3DKeyLoaded = true;
            //Debug.Log("S3DKeyLoaded");
        }

        GUI_Autoshow();
    }

    void S3DKeyUp(float modifier1, float modifier2, float modifier3)
    {
        if (S3DKeyTimer < 1)
            if (modifier1 != 0)
                swapLR = !swapLR;
            else
                if (modifier2 != 0)
                Save(slotName);
            else
                    if (modifier3 != 0)
                optimize = !optimize;
            else
                S3DEnabled = !S3DEnabled;
        //else
        //    Load(slotName);

        //if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt))
        //    S3DEnabled = !S3DEnabled;

        S3DKeyTimer = 0;
        S3DKeyLoaded = false;
        //GUI_Autoshow();
    }

    void FOVAxis(float value, float modifier1, float modifier2)
    {
        if (modifier2 != 0)
            if (modifier1 != 0)
                virtualIPD += 10 * value;
            else
                virtualIPD += value;
        else
            if (FOVControl)
                if (modifier1 != 0)
                    FOV -= value;
                else
                    FOV -= .1f * value;

        GUI_Autoshow();
    }

    //void InputFieldCaretMaterial_SetFields()
    //{
    //    InputFieldCaretMaterial_Set(PPI_inputField);
    //    InputFieldCaretMaterial_Set(userIPD_inputField);
    //    InputFieldCaretMaterial_Set(virtualIPD_inputField);
    //    InputFieldCaretMaterial_Set(FOV_inputField);
    //    InputFieldCaretMaterial_Set(panelDepth_inputField);
    //    InputFieldCaretMaterial_Set(screenDistance_inputField);
    //    InputFieldCaretMaterial_Set(slotName_inputField);

    //    GUI_Set();
    //}

    void GUI_Autoshow()
    {
        GUI_autoshowTimer = GUIAutoshowTime;
    }

    void GUI_Set()
    {
        Debug.Log("GUI_Set");
        //lastGUIOpened = GUIOpened;
        Cursor.visible = false;
        //CursorLockMode cursorLockMode = Cursor.lockState;

        if (GUIOpened)
        {
            //if (inputSystem)
#if STARTER_ASSETS_PACKAGES_CHECKED
            {
                starterAssetsInputs.cursorLocked = false;
                starterAssetsInputs.cursorInputForLook = false;
            }
#endif
            //else
                Cursor.lockState = CursorLockMode.None;

            ////Cursor.visible = true;
            //canvas.enabled = true;
            //cursorRectTransform.GetComponent<Canvas>().enabled = true;
            canvas.gameObject.SetActive(true);
            GUIVisible = true;
            cursorRectTransform.gameObject.SetActive(true);
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = true;

            if (PPI_inputField.transform.Find(PPI_inputField.name + " Input Caret"))
            {
                InputFieldCaretMaterial_Set(PPI_inputField);
                InputFieldCaretMaterial_Set(userIPD_inputField);
                InputFieldCaretMaterial_Set(virtualIPD_inputField);
                InputFieldCaretMaterial_Set(FOV_inputField);
                InputFieldCaretMaterial_Set(panelDepth_inputField);
                InputFieldCaretMaterial_Set(screenDistance_inputField);
                InputFieldCaretMaterial_Set(slotName_inputField);
            }
            else
                Invoke("GUI_Set", Time.deltaTime); //try to get Caret in the next frame

#if LookWithMouse
            if (lookWithMouseScript)
                lookWithMouseScript.enabled = false;
#endif

#if SimpleCameraController
            if (simpleCameraControllerScript)
                simpleCameraControllerScript.enabled = false;
#endif
        }
        else
        {
            //if (inputSystem)
            //{
            //    starterAssetsInputs.cursorLocked = cursorLockedDefault;
            //    starterAssetsInputs.cursorInputForLook = cursorInputForLookDefault;
            //}
            //else
            //    Cursor.lockState = cursorLockModeDefault;
            GUIClose();

            //Cursor.lockState = CursorLockMode.Locked;
            ////Cursor.visible = false;
            //canvas.enabled = false;
            //cursorRectTransform.GetComponent<Canvas>().enabled = false;
            canvas.gameObject.SetActive(false);
            GUIVisible = false;
            cursorRectTransform.gameObject.SetActive(false);
            canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;

#if LookWithMouse
            if (lookWithMouseScript)
                lookWithMouseScript.enabled = true;
#endif

#if SimpleCameraController
            if (simpleCameraControllerScript)
                simpleCameraControllerScript.enabled = true;
#endif
        }

        ClipSet();
        HDRPSettings_Set();
    }

    void GUIClose()
    {
        //if (inputSystem)
#if STARTER_ASSETS_PACKAGES_CHECKED
        {
            starterAssetsInputs.cursorLocked = cursorLockedDefault;
            starterAssetsInputs.cursorInputForLook = cursorInputForLookDefault;
        }
#endif
        //else
            Cursor.lockState = cursorLockModeDefault;
    }

    void ClipSet()
    {
        //Debug.Log("ClipSet canvasLocalPosZ " + canvasLocalPosZ);

        //if (canvasLocalPosZ < nearClip * 2)
        if (!GUIAsOverlay && GUIVisible && canvasLocalPosZ < sceneNearClip * 2)
        {
            nearClip = canvasLocalPosZ * .5f;
            //Debug.Log("ClipSet canvasLocalPosZ < nearClip");
        }
        else
            //if (!cineBrain || cineBrain && vCam != null)
            nearClip = sceneNearClip;
            //else
            //{
            //    Invoke("ClipSet", Time.deltaTime);
            //    return;
            //}

        if (!GUIAsOverlay && GUIVisible && canvasLocalPosZ > sceneFarClip * .8f)
        {
            farClip = canvasLocalPosZ * 1.25f;
            //farClip = canvasLocalPosZ;
            //Debug.Log("ClipSet canvasLocalPosZ < nearClip");
        }
        else
            farClip = sceneFarClip;

        //Debug.Log("ClipSet sceneNearClip " + sceneNearClip);
        //Debug.Log("ClipSet sceneFarClip " + sceneFarClip);

        //if (S3DEnabled)
        //{
        //    leftCam.nearClipPlane = rightCam.nearClipPlane = nearClip;
        //    leftCam.ResetProjectionMatrix();
        //    rightCam.ResetProjectionMatrix();
        //}
        //else
        //{
        //    if (cineBrain)
        //        VCamClipSet();
        //    else
        //        cam.nearClipPlane = nearClip;
        //}

        leftCam.nearClipPlane = rightCam.nearClipPlane = nearClip;
        leftCam.farClipPlane = rightCam.farClipPlane = farClip;
        leftCam.ResetProjectionMatrix();
        rightCam.ResetProjectionMatrix();

        //if (!S3DEnabled)
        //    if (cineBrain)
        //        VCamClipSet();
        //    else
        //        cam.nearClipPlane = nearClip;

        if (S3DEnabled)
        {
            if (nearClipHack)
            {
#if CINEMACHINE
                if (cineBrain)
                    VCamNearClipHack();
                else
#endif
                    cam.nearClipPlane = -1;
            }
            else
            {
#if CINEMACHINE
                if (cineBrain)
                    VCamClipRestore();
                else
#endif
                    cam.nearClipPlane = sceneNearClip;
            }
        }
        else
        {
            //nearClipHackApplied = false;
            //VCamClipRestore();

#if CINEMACHINE
            if (cineBrain)
                VCamClipSet();
            else
#endif
            {
                cam.nearClipPlane = nearClip;
                cam.farClipPlane = farClip;
            }
        }

        ViewSet();
    }

    void InputFieldCaretMaterial_Set(InputField field)
    {
        if (field.transform.Find(field.name + " Input Caret"))
        {
            caret = field.transform.Find(field.name + " Input Caret").GetComponent<CanvasRenderer>();
            //caret.GetMaterial(0).shader = Shader.Find("UI/Default_Mod");
            caret.SetMaterial(S3DPanelMaterial, 0);
        }
        else
        {
            //Debug.Log(field + " Input Caret is not set");
            Invoke("GUI_Set", Time.deltaTime); //try to get Caret in the next frame
        }
    }

    void EnableS3D_Toggle(bool isOn)
    {
        S3DEnabled = isOn;
        //RT_Set();
    }

    void SwapLR_Toggle(bool isOn)
    {
        swapLR = isOn;
        //CamSet();
    }

    void Optimize_Toggle(bool isOn)
    {
        optimize = isOn;

        //ViewSet();
        //RT_Set();
    }

    void VSync_Toggle(bool isOn)
    {
        vSync = isOn;

        ////Application.targetFrameRate = 60;
        //QualitySettings.vSyncCount = vSync ? 1 : 0;
    }

    //void Interlace_Button()
    //{
    //    method = Method.Interlace_Horizontal;

    //    //ViewSet();
    //    //RT_Set();
    //}

    //void Vertical_Button()
    //{
    //    method = Method.Interlace_Vertical;

    //    //ViewSet();
    //    //RT_Set();
    //}

    //void Checkerboard_Button()
    //{
    //    method = Method.Interlace_Checkerboard;

    //    //ViewSet();
    //    //RT_Set();
    //}

    //void SideBySide_Button()
    //{
    //    method = Method.SideBySide;

    //    //ViewSet();
    //    //RT_Set();
    //}

    //void OverUnder_Button()
    //{
    //    method = Method.OverUnder;

    //    //ViewSet();
    //    //RT_Set();
    //}

    //void Anaglyph_Button()
    //{
    //    method = Method.Anaglyph_RedCyan;

    //    //ViewSet();
    //    //RT_Set();
    //}

    void PPI_InputField(string fieldString)
    {
        //float value = Convert.ToSingle(fieldString);
        //value = Mathf.Clamp(value, 1, 1000);
        //Debug.Log(value);
        //PPI_inputField.text = value.ToString();
        //PPI = Mathf.Clamp(Convert.ToSingle(fieldString), 1, 1000);
        //PPI = Convert.ToSingle(fieldString);
        PPI = float.Parse(fieldString);
        //PPI_inputField.text = PPI.ToString();

        //ViewSet();
    }

    void UserIPD_Slider(float value)
    {
        userIPD = value;
    }

    void UserIPD_InputField(string fieldString)
    {
        //userIPD = Convert.ToSingle(fieldString);
        userIPD = float.Parse(fieldString);
    }

    void VirtualIPD_Slider(float value)
    {
        virtualIPD = value;
    }

    void VirtualIPD_InputField(string fieldString)
    {
        //virtualIPD = Convert.ToSingle(fieldString);
        virtualIPD = float.Parse(fieldString);
    }

    void MatchUserIPD_Toggle(bool isOn)
    {
        matchUserIPD = isOn;
    }

    void FOV_Slider(float value)
    {
        FOV = value;
    }

    void FOV_InputField(string fieldString)
    {
        //FOV = Convert.ToSingle(fieldString);
        FOV = float.Parse(fieldString);
    }

    void PanelDepth_Slider(float value)
    {
        //Debug.Log("PanelDepth_Slider " + value);
        panelDepth = value;
    }

    void PanelDepth_InputField(string fieldString)
    {
        //Debug.Log("PanelDepth_InputField " + fieldString);
        //panelDepth = Convert.ToSingle(fieldString);
        panelDepth = float.Parse(fieldString);
    }

    void ScreenDistance_InputField(string fieldString)
    {
        screenDistance_inputField.text = screenDistance.ToString();
    }

    void SlotName_InputField(string fieldString)
    {
        slotName = fieldString;
    }

    void SlotName_Dropdown(int itemNumber)
    {
        //Debug.Log(itemNumber);
        //slotName = slotName_dropdown.options[itemNumber].text;
        slotName = slotName_dropdown.captionText.text;
        //Load(slotName);
    }

    void OutputMethod_Dropdown(int itemNumber)
    {
       //Debug.Log("OutputMethod_Dropdown");
        method = (Method)itemNumber;
    }

    void Save_Button()
    {
        Save(slotName);
        SaveUserName(slotName);
    }

    void Load_Button()
    {
        Load(slotName);
    }

    void FieldPointerEnter(InputField field)
    {
       //Debug.Log("Pointer Enter");
        field.ActivateInputField();
    }

    void FieldPointerExit(InputField field)
    {
       //Debug.Log("Pointer Exit");
        field.DeactivateInputField();
    }

    //void PointerClick(Dropdown dropdown)
    //{
    //   //Debug.Log("Pointer Click");
    //    trigger = dropdown.transform.Find("Dropdown List").gameObject.AddComponent<EventTrigger>();
    //    EventTrigger.Entry entry = new EventTrigger.Entry();
    //    entry.eventID = EventTriggerType.PointerExit;
    //    entry.callback.AddListener((eventData) => { ListPointerExit(dropdown); });
    //    trigger.triggers.Add(entry);
    //}

    //void ListPointerExit(Dropdown dropdown)
    //{
    //   //Debug.Log("Dropdown Pointer Exit");
    //    dropdown.Hide();
    //}

    void VSyncSet()
    {
        //Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        //int targetFPS = -1 + (vSync ? Screen.currentResolution.refreshRate + 1 : 0);
        //int targetFPS = vSync ? Screen.currentResolution.refreshRate : -1;
        //Application.targetFrameRate = targetFPS;
        Application.targetFrameRate = vSync ? Screen.currentResolution.refreshRate : -1;
        //Debug.Log("VSyncSet " + Application.targetFrameRate);
    }

    void PPISet()
    {
        //PPI = Mathf.Max(PPI, 1);
        PPI = Mathf.Clamp(PPI, 1, 1000);
        //pixelPitch = 25.4f / PPI;
        PPI_inputField.text = PPI.ToString();
        lastPPI = PPI;

        ViewSet();
    }

    //  void PixelPitchSet()
    //  {
    //pixelPitch = Mathf.Max(pixelPitch, .001f);
    //      PPI = 25.4f / pixelPitch;

    //      ViewSet();
    //  }

    void UserIPDSet()
    {
        //userIPD = Mathf.Max(userIPD, 0);
        userIPD = Mathf.Clamp(userIPD, 0.01f, 100);
        userIPD_slider.value = userIPD;
        //userIPD_inputField.text = Convert.ToString(userIPD);
        userIPD_inputField.text = userIPD.ToString();
        lastUserIPD = userIPD;

        if (matchUserIPD)
            VirtualIPDSet();
        else
            CamSet();
    }

    void VirtualIPDSet()
    {
		if (matchUserIPD)
			virtualIPD = userIPD;
        else
            virtualIPD = Mathf.Clamp(virtualIPD, 0.01f, virtualIPDMax);

        virtualIPD_slider.value = virtualIPD;
        //virtualIPD_inputField.text = Convert.ToString(virtualIPD);
        virtualIPD_inputField.text = virtualIPD.ToString();
        lastVirtualIPD = virtualIPD;

        CamSet();
    }

    float scaleX;
    float scaleY;

    void FOV_Set()
    {
        //FOV = Mathf.Clamp(FOV, 1, 179);
        //aspect = cam.aspect;

        if (FOVControl && !camFOVChanged)
        {
            FOV = Mathf.Clamp(FOV, FOVMinMax.x, FOVMinMax.y);
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * aspect;
            vFOV = 360 * Mathf.Atan(1 / scaleY) / Mathf.PI;

            //cam.fieldOfView = vFOV;
            //Debug.Log("FOV_Set FOVControl && !camFOVChanged FOV " + FOV + " vFOV " + vFOV);

#if CINEMACHINE
            if (cineBrain)
                VCamFOVSet();
            else
#endif
                cam.fieldOfView = vFOV;

            //Debug.Log("FOV_Set FOVControl && !camFOVChanged FOV " + FOV + " vFOV " + vFOV + " cineBrain " + cineBrain + " cam.fieldOfView " + cam.fieldOfView);
        }
        else
        {
            camFOVChanged = false;
            vFOV = cam.fieldOfView;
            FOV = Mathf.Atan(aspect * Mathf.Tan(vFOV * Mathf.PI / 360)) * 360 / Mathf.PI;
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * aspect;
            //Debug.Log("!(FOV_Set FOVControl && !camFOVChanged) FOV " + FOV + " vFOV " + vFOV);
        }

        //Debug.Log("FOV_Set " + FOV + " vFOV " + vFOV);
        //Debug.Break();
        //Debug.Log("FOV_Set cam.fieldOfView " + cam.fieldOfView + " vFOV " + vFOV);
        //leftCam.fieldOfView = rightCam.fieldOfView = vFOV;

        FOV_slider.value = FOV;
        //FOV_inputField.text = Convert.ToString(FOV);
        FOV_inputField.text = FOV.ToString();
        lastFOV = FOV;

        ViewSet();
    }

    void CheckCamFOVSet()
    {
        Debug.Log("CheckCamFOVSet");

        if (cam.fieldOfView == vFOV)
            FOVSetInProcess = false;
        else
            Invoke("CheckCamFOVSet", Time.deltaTime);
    }

    //void GetFOVFromCam()
    //{
    //    //Debug.Log("GetFOVFromCam");
    //    vFOV = cam.fieldOfView;
    //    FOV = Mathf.Atan(aspect * Mathf.Tan(vFOV * Mathf.PI / 360)) * 360 / Mathf.PI;
    //    scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
    //    scaleY = scaleX * aspect;
    //}

    void CamSet()
    {	
        Vector3 leftCamPos;
        Vector3 rightCamPos;

        if (eyePriority == EyePriority.Left)
        {
            leftCamPos = Vector3.zero;
            rightCamPos = Vector3.right * virtualIPD * .001f;
		}
        else 
            if (eyePriority == EyePriority.Right)
            {
			    leftCamPos = Vector3.left * virtualIPD * .001f;
			    rightCamPos = Vector3.zero;
		    }
            else
            {
			    leftCamPos = Vector3.left * virtualIPD * .0005f;
			    rightCamPos = Vector3.right * virtualIPD * .0005f;
		    }

        if (swapLR)
        {
            leftCam.transform.localPosition = rightCamPos;
            rightCam.transform.localPosition = leftCamPos;

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.cameraLeft.transform.localPosition = rightCamPos;
                        c.cameraRight.transform.localPosition = leftCamPos;
                    }
        }
        else
        {
            leftCam.transform.localPosition = leftCamPos;
            rightCam.transform.localPosition = rightCamPos;

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.cameraLeft.transform.localPosition = leftCamPos;
                        c.cameraRight.transform.localPosition = rightCamPos;
                    }
        }

        ViewSet();
    }

    //Camera clearCamera;

    void CamRect_Set()
    {
        leftCam.rect = rightCam.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
       //Debug.Log("CamRect_Set");

        //if (cam.rect.x > 0 || cam.rect.xMax > 1 || cam.rect.y > 0 || cam.rect.yMax > 1)
        //{

        //    if (!clearCamera)
        //    {
        //        clearCamera = new GameObject("clearCamera").AddComponent<Camera>();
        //        clearCamera.CopyFrom(canvasRayCam);
        //    }
        //}
    }

    float shift;
    float screenDistance;
    float imageWidth;
    float oneRowShift;

    void ViewSet()
    {
        //imageWidth = cam.pixelWidth * pixelPitch; //real size of rendered image on screen
        imageWidth = cam.pixelWidth * 25.4f / PPI; //real size of rendered image on screen

        if (method == Method.SideBySide_HMD)
            imageWidth *= .5f;

        shift = imageOffset = userIPD / imageWidth; //shift optic axis relative to the screen size (UserIPD/screenSize)

        if (method == Method.SideBySide_HMD)
            shift = -1 + shift;

        //float oneRowShift = 0;
        //oneRowShift = 0;

        if (!optimize && method == Method.Interlace_Horizontal)
            oneRowShift = 2f / cam.pixelHeight;
        else
            oneRowShift = 0;

        //Debug.Log(oneRowShift);

        screenDistance = scaleX * imageWidth * .5f; //calculated distance to screen from user eyes where real FOV will match to virtual for realistic view
        screenDistance_inputField.text = screenDistance.ToString();

        if (setMatrixDirectly)
        {
            //set "shift" via matrix give fps gain from 304 to 308
            if (swapLR)
            {
		        leftCam.projectionMatrix = MatrixSet(leftCam.projectionMatrix, -shift, oneRowShift);
		        rightCam.projectionMatrix = MatrixSet(rightCam.projectionMatrix, shift, 0);

            if (additionalS3DCamerasStruct != null)
                    foreach (var c in additionalS3DCamerasStruct)
                        if (c.camera)
                        {
                            c.cameraLeft.projectionMatrix = MatrixSet(c.cameraLeft.projectionMatrix, -shift, oneRowShift);
                            c.cameraRight.projectionMatrix = MatrixSet(c.cameraRight.projectionMatrix, shift, 0);
                        }
            }
            else
            {
		        leftCam.projectionMatrix = MatrixSet(leftCam.projectionMatrix, shift, oneRowShift);
		        rightCam.projectionMatrix = MatrixSet(rightCam.projectionMatrix, -shift, 0);

            if (additionalS3DCamerasStruct != null)
                    foreach (var c in additionalS3DCamerasStruct)
                        if (c.camera)
                        {
                            c.cameraLeft.projectionMatrix = MatrixSet(c.cameraLeft.projectionMatrix, shift, oneRowShift);
                            c.cameraRight.projectionMatrix = MatrixSet(c.cameraRight.projectionMatrix, -shift, 0);
                        }
            }
        }
        else
        {
            //return matrix control to camera settings
            leftCam.ResetProjectionMatrix();
            rightCam.ResetProjectionMatrix();

            //set "shift" via cam "lensShift" required set "physical" cam
            leftCam.usePhysicalProperties = rightCam.usePhysicalProperties = true; //need set again after ResetProjectionMatrix
            leftCam.sensorSize = rightCam.sensorSize = cam.sensorSize = new Vector2(imageWidth, imageWidth / aspect);
            leftCam.gateFit = rightCam.gateFit = cam.gateFit = Camera.GateFitMode.None;
            //cam.focalLength = leftCam.focalLength = rightCam.focalLength = screenDistance;

            Vector2 lensShift = new Vector2(-shift * .5f, 0);

            if (swapLR)
                lensShift.x *= -1;

            if (!optimize && method == Method.Interlace_Horizontal)
                lensShift.y = -oneRowShift * .5f;

            leftCam.lensShift = -lensShift;
		    rightCam.lensShift = new Vector2(lensShift.x, 0);

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.cameraLeft.ResetProjectionMatrix();
                        c.cameraRight.ResetProjectionMatrix();
                        c.cameraLeft.usePhysicalProperties = c.cameraRight.usePhysicalProperties = true;
                        c.cameraLeft.sensorSize = c.cameraRight.sensorSize = c.camera.sensorSize = new Vector2(imageWidth, imageWidth / aspect);
                        c.cameraLeft.gateFit = c.cameraRight.gateFit = c.camera.gateFit = Camera.GateFitMode.None;
                        c.cameraLeft.lensShift = -lensShift;
                        c.cameraRight.lensShift = new Vector2(lensShift.x, 0);
                    }
        }

        //Debug.Log("ViewSet vFOV " + vFOV);
        //leftCam.fieldOfView = rightCam.fieldOfView = cam.fieldOfView = vFOV;
        leftCam.fieldOfView = rightCam.fieldOfView = vFOV;

        if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                    c.cameraLeft.fieldOfView = c.cameraRight.fieldOfView = c.camera.fieldOfView = vFOV;

        Canvas_Set();
    }

    Matrix4x4 MatrixSet(Matrix4x4 matrix, float shiftX, float shiftY)
    {	
        matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
        matrix[0, 2] = shiftX; //shift whole image projection in X axis of screen clip space
        matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
        matrix[1, 2] = shiftY;

        return matrix;
    }

    RenderTexture leftCamRT;   
    RenderTexture rightCamRT;
    RenderTexture leftCamCanvasRT;
    RenderTexture rightCamCanvasRT;
    int pass;
    ComputeBuffer verticesPosBuffer;
    ComputeBuffer verticesUVBuffer;

    void RT_Set()
    {
        Debug.Log("RT_Set");
        ReleaseRT(); //remove RT before adding new to avoid duplication

	    if (S3DEnabled)
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (PPLayer)
                PPLayer.enabled = false; //disabling Post Process Layer if exist due it heavily eats fps even when the camera doesn't render the scene
#endif

            //if (GetComponent<Cinemachine.CinemachineBrain>())
            //    Invoke("GetVCam", Time.deltaTime);

            //cam.cullingMask = 0;

#if CINEMACHINE
            if (cineBrain)
                VCamCullingOff();
            else
#endif
                cam.cullingMask = 0;

            //Invoke("VCamCullingOff", 10);
            //leftCam.enabled = true;
		    //rightCam.enabled = true;
            leftCam.enabled = rightCam.enabled = true;

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.camera.enabled = false;
                        c.cameraLeft.enabled = c.cameraRight.enabled = true;
                    }

            int rtWidth = cam.pixelWidth;
		    int rtHeight = cam.pixelHeight;
            Vertices();

            int columns = 1;
            int rows = 1;

            switch (method)
            {
                case Method.Interlace_Horizontal:

                    //int columns = 1;
                    //int rows = 1;

                  // switch (interlaceType)
                  //  {
		                //case InterlaceType.Horizontal:
   		             //       rows = rtHeight;

                  //          if (optimize)
			               //     rtHeight /= 2;

                  //          pass = 0;
	   	             //   break;

   		             //   case InterlaceType.Vertical:
   		             //       columns = rtWidth;

                  //          if (optimize)
                  //              rtWidth /= 2;

                  //          pass = 1;
	   	             //   break;

   		             //   case InterlaceType.Checkerboard:
   		             //       columns = rtWidth;
   		             //       rows = rtHeight;
                  //          pass = 2;
	   	             //   break;
  	               // }

   		            rows = rtHeight;

                    if (optimize)
			            rtHeight /= 2;

                    pass = 0;

		            S3DMaterial.SetInt("_Columns", columns);
		            S3DMaterial.SetInt("_Rows", rows);
                break;

   		        case Method.Interlace_Vertical:
   		            columns = rtWidth;

                    if (optimize)
                        rtWidth /= 2;

                    pass = 1;

		            S3DMaterial.SetInt("_Columns", columns);
		            S3DMaterial.SetInt("_Rows", rows);
	   	        break;

   		        case Method.Interlace_Checkerboard:
   		            columns = rtWidth;
   		            rows = rtHeight;
                    pass = 2;

		            S3DMaterial.SetInt("_Columns", columns);
		            S3DMaterial.SetInt("_Rows", rows);
	   	        break;

                case Method.SideBySide:

                    if (optimize)
                        rtWidth /= 2;

                    verticesUV[2] = new Vector2(2, 1);
                    verticesUV[3] = new Vector2(2, 0);
                    pass = 3;
                break;

                case Method.SideBySide_HMD:

                    if (optimize)
                        rtWidth /= 2;

                    verticesUV[2] = new Vector2(2, 1);
                    verticesUV[3] = new Vector2(2, 0);
                    pass = 3;
                    break;

                case Method.OverUnder:

                    if (optimize)
                        rtHeight /= 2;

                    verticesUV[1] = new Vector2(0, 2);
                    verticesUV[2] = new Vector2(1, 2);
                    pass = 4;
                break;

                case Method.Anaglyph_RedCyan:
                    S3DMaterial.SetColor("_LeftCol", Color.red);
                    S3DMaterial.SetColor("_RightCol", Color.cyan);
                    pass = 5;
                break;

                case Method.Anaglyph_RedBlue:
                    S3DMaterial.SetColor("_LeftCol", Color.red);
                    S3DMaterial.SetColor("_RightCol", Color.blue);
                    pass = 5;
                break;

                case Method.Anaglyph_GreenMagenta:
                    S3DMaterial.SetColor("_LeftCol", Color.green);
                    S3DMaterial.SetColor("_RightCol", Color.magenta);
                    pass = 5;
                    break;

                case Method.Anaglyph_AmberBlue:
                    S3DMaterial.SetColor("_LeftCol", new Color(1, 1, 0, 1));
                    S3DMaterial.SetColor("_RightCol", Color.blue);
                    pass = 5;
                    break;
            }

            //Debug.Log("RT_Set Width: " + rtWidth + " Height: " + rtHeight);
            //Debug.Log("RT_Set columns: " + columns + " rows: " + rows);

            leftCamRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
	        rightCamRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
            leftCamCanvasRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
            rightCamCanvasRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
            //leftCamRT.filterMode = FilterMode.Point;
            //rightCamRT.filterMode = FilterMode.Point;
            leftCamRT.filterMode = rightCamRT.filterMode = leftCamCanvasRT.filterMode = rightCamCanvasRT.filterMode = FilterMode.Point;
            //leftCamRT.wrapMode = TextureWrapMode.Repeat; //need for OpenGL SBS & OU to work with the only one 4 vertex screen quad and UV coordinates over 1
            //rightCamRT.wrapMode = TextureWrapMode.Repeat;
            leftCamRT.wrapMode = rightCamRT.wrapMode = leftCamCanvasRT.wrapMode = rightCamCanvasRT.wrapMode = TextureWrapMode.Repeat;

            leftCam.targetTexture = leftCamRT;
            rightCam.targetTexture = rightCamRT;

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.cameraLeft.targetTexture = leftCamRT;
                        c.cameraRight.targetTexture = rightCamRT;
                    }

            S3DMaterial.SetTexture("_LeftTex", leftCamRT);
	        S3DMaterial.SetTexture("_RightTex", rightCamRT);

           //Debug.Log("RT_Set leftCamRT.height: " + leftCamRT.height + " leftCamRT.width: " + leftCamRT.width);

            verticesPosBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(Vector2)));
            verticesPosBuffer.SetData(verticesPos);
            S3DMaterial.SetBuffer("verticesPosBuffer", verticesPosBuffer);

            verticesUVBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(Vector2)));
            verticesUVBuffer.SetData(verticesUV);
            S3DMaterial.SetBuffer("verticesUVBuffer", verticesUVBuffer);

#if UNITY_2019_1_OR_NEWER
            if (!defaultRender)
            {
                RenderPipelineManager.endCameraRendering += RenderQuad; //add render context

//                if (nearClipHack)
//#if CINEMACHINE
//                    if (cineBrain)
//                    {
//                        VCamNearClipHack();
//                        //nearClipHackApplied = true;
//                    }
//                    else
//#endif
//                        cam.nearClipPlane = -1;

//                //nearClipHackApplied = true;
//                //Invoke("VCamCullingOff", 10);
            }
#endif

            //if (!Application.isEditor)
            if (matrixKillHack)
                cam.projectionMatrix = Matrix4x4.zero;
        }
    }

    CommandBuffer commandBuffer;
    float frameTime;
    //private HDAdditionalCameraData.ClearColorMode clearColorMode;
    //private Color backgroundColorHDR;
    //private bool clearDepth;
    //private bool customRenderingSettings;
    //private LayerMask volumeLayerMask;
    //private Transform volumeAnchorOverride;
    //private HDAdditionalCameraData.AntialiasingMode antialiasing;
    //private bool dithering;
    //private bool xrRendering;
    //private bool stopNaNs;
    //private float taaSharpenStrength;
    //private float taaHistorySharpening;
    //private float taaAntiFlicker;
    //private float taaMotionVectorRejection;
    //private bool taaAntiHistoryRinging;
    //private float taaBaseBlendFactor;
    //private float taaJitterScale;
    //private HDAdditionalCameraData.FlipYMode flipYMode;
    //private bool fullscreenPassthrough;
    //private bool allowDynamicResolution;
    //private bool invertFaceCulling;
    //private LayerMask probeLayerMask;
    //private bool hasPersistentHistory;
    //private GameObject exposureTarget;
    //private HDPhysicalCamera physicalParameters;
    //private FrameSettings renderingPathCustomFrameSettings;
    //private FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
    //private FrameSettingsRenderType defaultFrameSettings;
    //private object probeCustomFixedExposure;
    //private bool allowDeepLearningSuperSampling;
    //private bool deepLearningSuperSamplingUseCustomQualitySettings;
    //private uint deepLearningSuperSamplingQuality;
    //private bool deepLearningSuperSamplingUseCustomAttributes;
    //private bool deepLearningSuperSamplingUseOptimalSettings;
    //private float deepLearningSuperSamplingSharpening;
    //private float materialMipBias;

    //public HDAdditionalCameraData.SMAAQualityLevel SMAAQuality { get; private set; }
    //public HDAdditionalCameraData.TAAQualityLevel TAAQuality { get; private set; }

#if UNITY_2019_1_OR_NEWER
    void RenderQuad(ScriptableRenderContext context, Camera camera) //render context for SRP
    {
        if (S3DEnabled && camera == canvasCam)
        {
#if URP
            //Debug.Log(Time.time);
            //canvasCam.enabled = true;
            //canvasCam.enabled = false;

            //if (oddFrame)
            //{
            //    canvasCam.targetTexture = leftCamRT;
            //    //canvasCam.Render();
            //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);
            //}
            //else
            //{
            //    canvasCam.targetTexture = rightCamRT;
            //    //canvasCam.Render();
            //}

            if (frameTime != Time.time) //set targetTexture cause call this 5 times per frame so use condition to execute below commands only once per frame
            {
                frameTime = Time.time;
                //Debug.Log("frameTime != Time.time " + frameTime);
                //Matrix4x4 canvasCamMatrix = canvasCam.projectionMatrix;
                //canvasCamMatrix = canvasCam.projectionMatrix;

                if (oddFrame)
                {
                    if (method == Method.SideBySide_HMD)
                        canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
                    else
                        canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

                    if (method == Method.Interlace_Horizontal)
                        canvasCamMatrix[1, 3] = -oneRowShift;

                    canvasCam.projectionMatrix = canvasCamMatrix;
                    canvasCam.targetTexture = leftCamRT;
                    //canvasCam.targetTexture = leftCamCanvasRT;
                    //canvasCam.Render();
                    //Graphics.CopyTexture(leftCamCanvasRT, leftCamRT);
                }
                else
                {
                    if (method == Method.SideBySide_HMD)
                        canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
                    else
                        canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

                    if (method == Method.Interlace_Horizontal)
                        canvasCamMatrix[1, 3] = 0;

                    canvasCam.projectionMatrix = canvasCamMatrix;
                    canvasCam.targetTexture = rightCamRT;
                    //canvasCam.targetTexture = rightCamCanvasRT;
                    //canvasCam.Render();
                    //Graphics.CopyTexture(rightCamCanvasRT, rightCamRT);
                }

            //Graphics.CopyTexture(leftCamCanvasRT, leftCamRT);
            //Graphics.CopyTexture(rightCamCanvasRT, rightCamRT);
            //Graphics.Blit(leftCamCanvasRT, leftCamRT, S3DPanelMaterial);
            //Graphics.Blit(rightCamCanvasRT, rightCamRT, S3DPanelMaterial);
            //canvasCam.enabled = false;


            //canvasCam.projectionMatrix = canvasCamMatrix;
//#if URP
            UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);
//#endif
            }

            //UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);

            //if (oddFrame)
            //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);

            //canvasCam.Render();
            //Debug.Break();
            //canvasCam.enabled = false;
            //Debug.Log(camera);
            //Debug.Log(oddFrame + " RenderQuad camera == canvasCam " + Time.time);
            //UniversalRenderPipeline.RenderSingleCamera(context, canvasCam);
#elif HDRP
            Graphics.Blit(leftCamCanvasRT, leftCamRT, S3DPanelMaterial);
            Graphics.Blit(rightCamCanvasRT, rightCamRT, S3DPanelMaterial);
#endif
        }

        if (camera == cam)
        {
            //Debug.Log("camera == cam " + Time.time);
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "screenQuad";

            //render clip space screen quad using S3DMaterial preset vertices buffer with:
            commandBuffer.DrawProcedural(Matrix4x4.identity, S3DMaterial, pass, MeshTopology.Quads, 4); //this need "nearClipPlane = -1" for same quad position as using Blit with custom camera Rect coordinates
            //commandBuffer.Blit(null, cam.activeTexture, S3DMaterial, pass); //or this

            context.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Release();
            context.Submit();
        }
    }
#endif

            void Vertices() //set clip space vertices and texture coordinates for render fullscreen quad via shader buffer
    {
        if (defaultRender)
        {
            verticesPos[0] = new Vector2(-1, -1);
            verticesPos[1] = new Vector2(-1, 1);
            verticesPos[2] = new Vector2(1, 1);
            verticesPos[3] = new Vector2(1, -1);
           //Debug.Log("defaultRender");
        }
        else
        {
            //translate camera rect coordinates to clip coordinates
            //need when using custom camera rect with Blit or DrawProcedural & "nearClipPlane = -1" hack
            Vector4 clipRect = new Vector4(Mathf.Max(cam.rect.min.x, 0), Mathf.Max(cam.rect.min.y, 0), Mathf.Min(cam.rect.max.x, 1), Mathf.Min(cam.rect.max.y, 1)) * 2 - Vector4.one;

            verticesPos[0] = new Vector2(clipRect.x, clipRect.y);
            verticesPos[1] = new Vector2(clipRect.x, clipRect.w);
            verticesPos[2] = new Vector2(clipRect.z, clipRect.w);
            verticesPos[3] = new Vector2(clipRect.z, clipRect.y);

            //Debug.Log("Vertices() cam.rect.max.y " + cam.rect.max.y);
           //Debug.Log("Vertices() clipRect " + clipRect);
        }

        verticesUV[0] = new Vector2(0, 0);
            verticesUV[1] = new Vector2(0, 1);
            verticesUV[2] = new Vector2(1, 1);
            verticesUV[3] = new Vector2(1, 0);
    }

    void OnDisable()
    {
        //Debug.Log("OnDisable " + name);

        if (!name.Contains("(Clone)"))
        {
            Debug.Log("OnDisable");
            GUIClose();
            ReleaseRT();
            Destroy(leftCam.gameObject);
            Destroy(rightCam.gameObject);

            //if (cameraStack != null)
            //    cameraStack.Remove(canvasCam);

            //foreach (var c in leftCameraStack)
            //    cameraStack.Add(c);

            CameraStackRestore();

            if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        Destroy(c.cameraLeft.gameObject);
                        Destroy(c.cameraRight.gameObject);
                    }

            if (canvasCam)
                Destroy(canvasCam.gameObject);

#if CINEMACHINE
            VCamClipRestore();

            //if (cineBrain && defaultVCam != null && Cinemachine.CinemachineBrain.SoloCamera.ToString() != "null")
            //    Cinemachine.CinemachineBrain.SoloCamera = defaultVCam;

            cineBrain = false;
            vCam = null;
#else
                    cam.nearClipPlane = sceneNearClip;
                    cam.farClipPlane = sceneFarClip;
#endif

            Destroy(S3DMaterial);
            Destroy(canvas.gameObject);
            Resources.UnloadUnusedAssets(); //free memory
            cam.ResetAspect();
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * cam.aspect;
            cam.fieldOfView = 360 * Mathf.Atan(1 / scaleY) / Mathf.PI;

#if ENABLE_INPUT_SYSTEM
            GUIAction.Disable();
            S3DAction.Disable();
            FOVAction.Disable();
            modifier1Action.Disable();
            modifier2Action.Disable();
            modifier3Action.Disable();
#endif

//#if HDRP
            //HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
            //HDRPSettings = defaultHDRPSettings;
            //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            HDRPSettings_Restore();
//#endif
        }
    }

    void ReleaseRT()
    {
        //Debug.Log("ReleaseRT " + name);
#if UNITY_2019_1_OR_NEWER
        if (!defaultRender)
            RenderPipelineManager.endCameraRendering -= RenderQuad; //remove render context
#endif

        //leftCam.targetTexture = null;
        //rightCam.targetTexture = null;
        leftCam.targetTexture = rightCam.targetTexture = null;
        //leftCam.enabled = false;
		//rightCam.enabled = false;
        leftCam.enabled = rightCam.enabled = false;

        if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                {
                    c.cameraLeft.targetTexture = c.cameraRight.targetTexture = null;
                    c.cameraLeft.enabled = c.cameraRight.enabled = false;
                    c.camera.enabled = true;
                }

        cam.cullingMask = cullingMask;
        //cam.nearClipPlane = nearClip;
        //Debug.Log("ReleaseRT nearClip " + nearClip);

        //if (cineBrain)
        //    VCamClipRestore();
        //else
        //    cam.nearClipPlane = sceneNearClip;

//#if CINEMACHINE
//        VCamClipRestore();
//        //nearClipHackApplied = false;
//#else
//        cam.nearClipPlane = sceneNearClip;
//        cam.farClipPlane = sceneFarClip;
//#endif

        //nearClipHackApplied = false;

        //vCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera;

        //if (vCam == null)
        //    cam.nearClipPlane = nearClip;
        //else
        //{
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
        //    //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //}

        //if (vCam != null)
        //{
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
        //    Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //}
        //else
        //    cam.nearClipPlane = nearClip;

        //if (vCam != null)
        //{
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
        //    //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //    //Invoke("VCamActivate", 1);
        //}

        //cam.nearClipPlane = nearClip;

        //ClipSet();
        //cam.ResetProjectionMatrix();

        if (verticesPosBuffer != null)
        {
            verticesPosBuffer.Release();
            verticesUVBuffer.Release();
            leftCamRT.Release();
	        rightCamRT.Release();
        }

#if UNITY_POST_PROCESSING_STACK_V2
        if (PPLayer)
            PPLayer.enabled = PPLayerEnabled;
#endif
    }

    //void VCamNearClipRestore()
    //{
    //    if (vCam != null)
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
    //    else
    //        Invoke("VCamNearClipRestore", Time.deltaTime);
    //}

    //ignored in SRP(URP or HDRP) but in default render via cam buffer even empty function give fps gain from 294 to 308
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Debug.Log("OnRenderImage");
        //if (defaultRender) //commented till SRP don't go here
        if (S3DEnabled)
        {
            if (canvasCam)
            {
                CanvasCamS3DRender_Set();

                Graphics.Blit(leftCamCanvasRT, leftCamRT, S3DPanelMaterial);
                Graphics.Blit(rightCamCanvasRT, rightCamRT, S3DPanelMaterial);
            }
            //else
            Graphics.Blit(null, destination, S3DMaterial, pass);
        }
        else
            Graphics.Blit(source, destination);
    }

    void CanvasCamS3DRender_Set()
    {
        if (oddFrame)
        {
            if (method == Method.SideBySide_HMD)
                canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
            else
                canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

            if (method == Method.Interlace_Horizontal)
                canvasCamMatrix[1, 3] = -oneRowShift;

            canvasCam.projectionMatrix = canvasCamMatrix;
            canvasCam.targetTexture = leftCamCanvasRT;
        }
        else
        {
            if (method == Method.SideBySide_HMD)
                canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
            else
                canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

            if (method == Method.Interlace_Horizontal)
                canvasCamMatrix[1, 3] = 0;

            canvasCam.projectionMatrix = canvasCamMatrix;
            canvasCam.targetTexture = rightCamCanvasRT;
        }
    }

    ////Immediate GUI Stereo3D settings panel (remove next three functions to delete IMGUI)
    //Rect guiWindow = new Rect(20, 20, 640, 290);

    //void OnGUI()
    //{
    //    if (GUIOpened)
    //    {
    //	    Cursor.lockState = CursorLockMode.None;
    //        Cursor.visible = true;

    //	    guiWindow = GUILayout.Window(0, guiWindow, GuiWindowContent, "Stereo3D Settings");

    //  if (!guiWindow.Contains(Event.current.mousePosition) && !Input.GetMouseButton(0))
    //   GUI.UnfocusWindow();
    // }
    //    else
    //    {
    //	    Cursor.lockState = CursorLockMode.Locked;
    //        Cursor.visible = false;
    // }
    //}

    //void GuiWindowContent (int windowID)
    //{
    // GUILayout.BeginHorizontal();
    //  GUILayout.BeginVertical();
    //   GUILayout.BeginHorizontal();
    //   GUILayout.EndHorizontal();

    //          S3DEnabled = GUILayout.Toggle(S3DEnabled, " Enable S3D");

    //                string[] methodStrings = { "Interlace", "Side By Side", "Over Under", "Anaglyph" };
    //    method = (Method)GUILayout.SelectionGrid((int)method, methodStrings, 1, GUILayout.Width(100));

    //        GUILayout.EndVertical();
    //  GUILayout.BeginVertical();
    //   GUILayout.BeginHorizontal();

    //          swapLR = GUILayout.Toggle(swapLR, " Swap Left-Right Cameras");

    //   GUILayout.EndHorizontal();
    //   GUILayout.BeginHorizontal();

    //                string[] interlaceTypeStrings = { "Horizontal", "Vertical", "Checkerboard" };
    //    //interlaceType = (InterlaceType)GUILayout.Toolbar((int)interlaceType, interlaceTypeStrings, GUILayout.Width(298));

    //    GUILayout.FlexibleSpace();
    //    GUILayout.Label ("PPI", GUILayout.Width(30));

    //    if (GUILayout.Button("-", GUILayout.Width(20)))
    //	    PPI -= 1;

    //                string PPIString = StringCheck(PPI.ToString());
    //                string fieldString = GUILayout.TextField(PPIString, 5, GUILayout.Width(40));

    //    if (fieldString != PPIString)
    //                    PPI = Convert.ToSingle(fieldString);

    //    if (GUILayout.Button("+", GUILayout.Width(20)))
    //	    PPI += 1;

    //    GUILayout.Label (" pix");
    //    GUILayout.Space(20);

    //   GUILayout.EndHorizontal();
    //   //GUILayout.BeginHorizontal();

    //   // GUILayout.FlexibleSpace();
    //   // GUILayout.Label ("Pixel pitch", GUILayout.Width(70));

    //   // if (GUILayout.Button("-", GUILayout.Width(20)))
    //	  //  pixelPitch -= .001f;

    //   //             string pixelPitchString = StringCheck(pixelPitch.ToString());
    //   // fieldString = GUILayout.TextField(pixelPitchString, 5, GUILayout.Width(40));

    //   // if (fieldString != pixelPitchString)
    //	  //  pixelPitch = Convert.ToSingle(fieldString);


    //   // if (GUILayout.Button("+", GUILayout.Width(20)))
    //	  //  pixelPitch += .001f;

    //   // GUILayout.Label (" mm");
    //   // GUILayout.Space(15);

    //   //GUILayout.EndHorizontal();
    //  GUILayout.EndVertical();
    // GUILayout.EndHorizontal();
    // GUILayout.BeginHorizontal();
    //  GUILayout.Space(4);
    //  GUILayout.BeginVertical();

    //   GUILayout.Label ("          User IPD", GUILayout.Width(100));
    //   GUILayout.Label ("        Virtual IPD", GUILayout.Width(100));
    //   GUILayout.Label ("  Horizontal FOV", GUILayout.Width(100));
    //   GUILayout.Label ("     Vertical FOV", GUILayout.Width(100));
    //   GUILayout.Label ("Screen distance", GUILayout.Width(100));

    //  GUILayout.EndVertical();
    //  GUILayout.Space(4);
    //  GUILayout.BeginVertical();

    //   GUILayout.Space(10);
    //            userIPD = GUILayout.HorizontalSlider(userIPD, 0, 100, GUILayout.Width(300));

    //   GUILayout.Space(9);
    //   virtualIPD = GUILayout.HorizontalSlider(virtualIPD, 0, 1000, GUILayout.Width(300));

    //   GUILayout.Space(9);
    //   //FOV = GUILayout.HorizontalSlider(FOV, .1f, 179.9f, GUILayout.Width(300)); //179.9f will cause the slider stuck in the standalone player but OK in the editor
    //   FOV = GUILayout.HorizontalSlider(FOV, 1, 179, GUILayout.Width(300));

    //  GUILayout.EndVertical();
    //  GUILayout.BeginVertical();
    //   GUILayout.BeginHorizontal();

    //    if (GUILayout.Button("-", GUILayout.Width(20)))
    //	    userIPD -= .1f;

    //          string userIPDString = StringCheck(userIPD.ToString());
    //    fieldString = GUILayout.TextField(userIPDString, 5, GUILayout.Width(40));

    //                if (fieldString != userIPDString)
    //        userIPD = Convert.ToSingle(fieldString);

    //    if (GUILayout.Button("+", GUILayout.Width(20)))
    //	    userIPD += .1f;

    //    GUILayout.Label (" mm");

    //   GUILayout.EndHorizontal();
    //   GUILayout.BeginHorizontal();

    //    if (GUILayout.Button("-", GUILayout.Width(20)))
    //	    virtualIPD -= 1f;

    //          string virtualIPDString = StringCheck(virtualIPD.ToString());
    //    fieldString = GUILayout.TextField(virtualIPDString, 5, GUILayout.Width(40));

    //                if (fieldString != virtualIPDString)
    //        virtualIPD = Convert.ToSingle(fieldString);

    //    if (GUILayout.Button("+", GUILayout.Width(20)))
    //	    virtualIPD += 1f;

    //    GUILayout.Label (" mm");

    //          matchUserIPD = GUILayout.Toggle(matchUserIPD, " Match User IPD");

    //   GUILayout.EndHorizontal();
    //   GUILayout.BeginHorizontal();

    //    if (GUILayout.Button("-", GUILayout.Width(20)))
    //	    FOV -= .1f;

    //                string hFOVString = StringCheck(FOV.ToString());
    //    fieldString = GUILayout.TextField(hFOVString, 5, GUILayout.Width(40));

    //                if (fieldString != hFOVString)
    //        FOV = Convert.ToSingle(fieldString);

    //    if (GUILayout.Button("+", GUILayout.Width(20)))
    //	    FOV += .1f;

    //    GUILayout.Label (" deg");

    //   GUILayout.EndHorizontal();
    //   GUILayout.BeginHorizontal();

    //       GUILayout.Space(28);
    //    GUILayout.TextField(vFOV.ToString(), 5, GUILayout.Width(40));

    //    GUILayout.Label (" deg");

    //   GUILayout.EndHorizontal();
    //   GUILayout.BeginHorizontal();

    //       GUILayout.Space(28);
    //    GUILayout.TextField(screenDistance.ToString(), 5, GUILayout.Width(40));

    //    GUILayout.Label (" mm");

    //   GUILayout.EndHorizontal();
    //  GUILayout.EndVertical();
    //    GUILayout.EndHorizontal();

    //    GUI.DragWindow(new Rect(0, 0, 640, 20)); //make GUI window draggable by top
    //}

    //string StringCheck(string str)
    //{
    //    if (str.Length > 5)
    //        str = str.Substring(0, 5);

    //    return str;
    //}

    void Save(string name)
    {
        //Debug.Log(Application.persistentDataPath);
        string filePath = Application.persistentDataPath + "/S3D_Settings_" + name;
        FileStream file = File.Create(filePath);
        //SaveLoad data = new SaveLoad(swapLR, optimize, vSync, method, PPI, userIPD, virtualIPD, matchUserIPD, FOV, panelDepth, panel.GetComponent<RectTransform>().anchoredPosition);
        SavableVector2 panelPos = new SavableVector2(panel.GetComponent<RectTransform>().anchoredPosition.x, panel.GetComponent<RectTransform>().anchoredPosition.y);
        //pos.panelPosX = panel.GetComponent<RectTransform>().anchoredPosition.x;
        //pos.panelPosY = panel.GetComponent<RectTransform>().anchoredPosition.y;
        SaveLoad data = new SaveLoad(swapLR, optimize, vSync, method, PPI, userIPD, virtualIPD, matchUserIPD, FOV, panelDepth, panelPos);
        BinaryFormatter binF = new BinaryFormatter();
        binF.Serialize(file, data);
        file.Close();

        DropdownSet();
        DropdownNameSet(name);
    }

    void Load(string name)
    {
       //Debug.Log("Load");

        string filePath = Application.persistentDataPath + "/S3D_Settings_" + name;

        if (File.Exists(filePath))
        {
            FileStream file = File.OpenRead(filePath);
            BinaryFormatter binF = new BinaryFormatter();
            SaveLoad data = (SaveLoad)binF.Deserialize(file);
            file.Close();

            //Debug.Log(data.PPI);
            swapLR = data.swapLR;
            optimize = data.optimize;
            vSync = data.vSync;
            method = data.method;
            PPI = data.PPI;
            userIPD = data.userIPD;
            virtualIPD = data.virtualIPD;
            matchUserIPD = data.matchUserIPD;
            FOV = data.FOV;
            panelDepth = data.panelDepth;
            //panelDepth = Mathf.Clamp(data.panelDepth, panelDepthMinMax.x, panelDepthMinMax.y);
            //panel.GetComponent<RectTransform>().anchoredPosition = data.panelPos;
            panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(data.panelPos.x, data.panelPos.y);

            DropdownNameSet(name);
        }
    }

    void SaveUserName(string name)
    {
        string filePath = Application.persistentDataPath + "/S3D_Settings_LastSaveUserName";
        FileStream file = File.Create(filePath);
        SaveLoadUserName data = new SaveLoadUserName(name);
        BinaryFormatter binF = new BinaryFormatter();
        binF.Serialize(file, data);
        file.Close();
    }

    void LoadLastSave()
    {
        string filePath = Application.persistentDataPath + "/S3D_Settings_LastSaveUserName";

        if (File.Exists(filePath))
        {
            FileStream file = File.OpenRead(filePath);
            BinaryFormatter binF = new BinaryFormatter();
            SaveLoadUserName data = (SaveLoadUserName)binF.Deserialize(file);
            file.Close();

            //Debug.Log(data.userName);
            slotName = data.userName;
        }

        Load(slotName);
    }

    void DropdownSet()
    {
        List<Dropdown.OptionData> slotNames = new List<Dropdown.OptionData>();
        DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
        FileInfo[] files = dir.GetFiles("S3D_Settings_*");
        //Dropdown.OptionData firstOptionData = new Dropdown.OptionData();
        //firstOptionData.text = slotName;
        //slotNames.Add(firstOptionData);

        foreach (FileInfo f in files)
        {
            string itemName = f.Name.Replace("S3D_Settings_", "");
            Dropdown.OptionData optionData = new Dropdown.OptionData();

            //if (itemName != slotName && itemName != "LastSaveUserName")
            if (itemName != "LastSaveUserName")
            {
                //Debug.Log(itemName);
                optionData.text = itemName;
                slotNames.Add(optionData);
            }
        }

        slotName_dropdown.options = slotNames;
    }

    void DropdownNameSet(string name)
    {
        //for (int i = 0; i < slotName_dropdown.options.Count; i++)
        //{
        //   //Debug.Log(slotName_dropdown.options[i].text);

        //    if (slotName_dropdown.options[i].text == name)
        //    {
        //        slotName_dropdown.value = i;
        //    }
        //}

        foreach (Dropdown.OptionData optionData in slotName_dropdown.options)
        {
           //Debug.Log(optionData.text);

            if (optionData.text == name)
            {
                slotName_dropdown.value = slotName_dropdown.options.IndexOf(optionData);
            }
        }
    }

    //void CamData_Change()
    //{
    //    camData.requiresColorOption = CameraOverrideOption.Off;
    //}

    void SetCameraDataStruct()
    {
        cameraDataStruct = new CameraDataStruct(
#if URP
            //camData.renderType,
            //camData.scriptableRenderer,
            //camData.renderPostProcessing,
            //camData.antialiasing,
            //camData.antialiasingQuality,
            //camData.stopNaN,
            //camData.dithering,
            //camData.renderShadows,
            // //camData.cameraStack,

             camData.requiresDepthTexture,
             camData.stopNaN,
             camData.antialiasingQuality,
             camData.antialiasing,
             camData.renderPostProcessing,
             camData.volumeStack,
             camData.volumeTrigger,
             camData.volumeLayerMask,
             camData.requiresColorTexture,
             camData.allowXRRendering,
             camData.renderType,
             camData.requiresColorOption,
             camData.requiresDepthOption,
             camData.renderShadows,
             camData.dithering,
#elif HDRP
            camData.clearColorMode,
            camData.backgroundColorHDR,
            camData.clearDepth,
            camData.customRenderingSettings,
            camData.volumeLayerMask,
            camData.volumeAnchorOverride,
            camData.antialiasing,
            camData.dithering,
            camData.xrRendering,
            camData.SMAAQuality,
            camData.stopNaNs,
            camData.taaSharpenStrength,
            camData.TAAQuality,
            camData.taaHistorySharpening,
            camData.taaAntiFlicker,
            camData.taaMotionVectorRejection,
            camData.taaAntiHistoryRinging,
            camData.taaBaseBlendFactor,
            camData.taaJitterScale,
            camData.flipYMode,
            camData.fullscreenPassthrough,
            camData.allowDynamicResolution,
            camData.invertFaceCulling,
            camData.probeLayerMask,
            camData.hasPersistentHistory,
            camData.exposureTarget,
            camData.physicalParameters,
            camData.renderingPathCustomFrameSettings,
            camData.renderingPathCustomFrameSettingsOverrideMask,
            camData.defaultFrameSettings,
            //camData.probeCustomFixedExposure,
            camData.allowDeepLearningSuperSampling,
            camData.deepLearningSuperSamplingUseCustomQualitySettings,
            camData.deepLearningSuperSamplingQuality,
            camData.deepLearningSuperSamplingUseCustomAttributes,
            camData.deepLearningSuperSamplingUseOptimalSettings,
            camData.deepLearningSuperSamplingSharpening,
            camData.materialMipBias,
#endif
            cam.depth,
            cam.useOcclusionCulling
            );
    }

    [Serializable]
    public struct CameraDataStruct
    {
#if URP
        //public CameraRenderType cameraRenderType;
        //public ScriptableRenderer scriptableRenderer;
        //public bool renderPostProcessing;
        //public AntialiasingMode antialiasing;
        //public AntialiasingQuality antialiasingQuality;
        //public bool stopNaN;
        //public bool dithering;
        //public bool renderShadows;
        ////public List<Camera> cameraStack; //compare not working

        public bool requiresDepthTexture;
        public bool stopNaN;
        public AntialiasingQuality antialiasingQuality;
        public AntialiasingMode antialiasing;
        public bool renderPostProcessing;
        public VolumeStack volumeStack;
        public Transform volumeTrigger;
        public LayerMask volumeLayerMask;
        public bool requiresColorTexture;
        public bool allowXRRendering;
        public CameraRenderType renderType;
        public CameraOverrideOption requiresColorOption;
        public CameraOverrideOption requiresDepthOption;
        public bool renderShadows;
        public bool dithering;
#elif HDRP
        HDAdditionalCameraData.ClearColorMode clearColorMode;
        Color backgroundColorHDR;
        bool clearDepth;
        bool customRenderingSettings;
        LayerMask volumeLayerMask;
        Transform volumeAnchorOverride;
        HDAdditionalCameraData.AntialiasingMode antialiasing;
        bool dithering;
        bool xrRendering;
        HDAdditionalCameraData.SMAAQualityLevel SMAAQuality;
        bool stopNaNs;
        float taaSharpenStrength;
        HDAdditionalCameraData.TAAQualityLevel TAAQuality;
        float taaHistorySharpening;
        float taaAntiFlicker;
        float taaMotionVectorRejection;
        bool taaAntiHistoryRinging;
        float taaBaseBlendFactor;
        float taaJitterScale;
        HDAdditionalCameraData.FlipYMode flipYMode;
        bool fullscreenPassthrough;
        bool allowDynamicResolution;
        bool invertFaceCulling;
        LayerMask probeLayerMask;
        bool hasPersistentHistory;
        GameObject exposureTarget;
        HDPhysicalCamera physicalParameters;
        FrameSettings renderingPathCustomFrameSettings;
        FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        FrameSettingsRenderType defaultFrameSettings;
        //object probeCustomFixedExposure;
        bool allowDeepLearningSuperSampling;
        bool deepLearningSuperSamplingUseCustomQualitySettings;
        uint deepLearningSuperSamplingQuality;
        bool deepLearningSuperSamplingUseCustomAttributes;
        bool deepLearningSuperSamplingUseOptimalSettings;
        float deepLearningSuperSamplingSharpening;
        float materialMipBias;
#endif
        public float depth;
        public bool useOcclusionCulling;

        public CameraDataStruct(
#if URP
            //CameraRenderType cameraRenderType,
            //ScriptableRenderer scriptableRenderer,
            //bool renderPostProcessing,
            //AntialiasingMode antialiasing,
            //AntialiasingQuality antialiasingQuality,
            //bool stopNaN,
            //bool dithering,
            //bool renderShadows,
            ////List<Camera> cameraStack,

            bool requiresDepthTexture,
            bool stopNaN,
            AntialiasingQuality antialiasingQuality,
            AntialiasingMode antialiasing,
            bool renderPostProcessing,
            VolumeStack volumeStack,
            Transform volumeTrigger,
            LayerMask volumeLayerMask,
            bool requiresColorTexture,
            bool allowXRRendering,
            CameraRenderType renderType,
            CameraOverrideOption requiresColorOption,
            CameraOverrideOption requiresDepthOption,
            bool renderShadows,
            bool dithering,
#elif HDRP
            HDAdditionalCameraData.ClearColorMode clearColorMode,
            Color backgroundColorHDR,
            bool clearDepth,
            bool customRenderingSettings,
            LayerMask volumeLayerMask,
            Transform volumeAnchorOverride,
            HDAdditionalCameraData.AntialiasingMode antialiasing,
            bool dithering,
            bool xrRendering,
            HDAdditionalCameraData.SMAAQualityLevel SMAAQuality,
            bool stopNaNs,
            float taaSharpenStrength,
            HDAdditionalCameraData.TAAQualityLevel TAAQuality,
            float taaHistorySharpening,
            float taaAntiFlicker,
            float taaMotionVectorRejection,
            bool taaAntiHistoryRinging,
            float taaBaseBlendFactor,
            float taaJitterScale,
            HDAdditionalCameraData.FlipYMode flipYMode,
            bool fullscreenPassthrough,
            bool allowDynamicResolution,
            bool invertFaceCulling,
            LayerMask probeLayerMask,
            bool hasPersistentHistory,
            GameObject exposureTarget,
            HDPhysicalCamera physicalParameters,
            FrameSettings renderingPathCustomFrameSettings,
            FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask,
            FrameSettingsRenderType defaultFrameSettings,
            //object probeCustomFixedExposure,
            bool allowDeepLearningSuperSampling,
            bool deepLearningSuperSamplingUseCustomQualitySettings,
            uint deepLearningSuperSamplingQuality,
            bool deepLearningSuperSamplingUseCustomAttributes,
            bool deepLearningSuperSamplingUseOptimalSettings,
            float deepLearningSuperSamplingSharpening,
            float materialMipBias,
#endif
            float depth,
            bool useOcclusionCulling
            )
        {
#if URP
            //this.cameraRenderType = cameraRenderType;
            //this.scriptableRenderer = scriptableRenderer;
            //this.renderPostProcessing = renderPostProcessing;
            //this.antialiasing = antialiasing;
            //this.antialiasingQuality = antialiasingQuality;
            //this.stopNaN = stopNaN;
            //this.dithering = dithering;
            //this.renderShadows = renderShadows;
            ////this.cameraStack = cameraStack;

             this.requiresDepthTexture = requiresDepthTexture;
             this.stopNaN = stopNaN;
             this.antialiasingQuality = antialiasingQuality;
             this.antialiasing = antialiasing;
             this.renderPostProcessing = renderPostProcessing;
             this.volumeStack = volumeStack;
             this.volumeTrigger = volumeTrigger;
             this.volumeLayerMask = volumeLayerMask;
             this.requiresColorTexture = requiresColorTexture;
             this.allowXRRendering = allowXRRendering;
             this.renderType = renderType;
             this.requiresColorOption = requiresColorOption;
             this.requiresDepthOption = requiresDepthOption;
             this.renderShadows = renderShadows;
             this.dithering = dithering;
#elif HDRP
            this.clearColorMode = clearColorMode;
            this.backgroundColorHDR = backgroundColorHDR;
            this.clearDepth = clearDepth;
            this.customRenderingSettings = customRenderingSettings;
            this.volumeLayerMask = volumeLayerMask;
            this.volumeAnchorOverride = volumeAnchorOverride;
            this.antialiasing = antialiasing;
            this.dithering = dithering;
            this.xrRendering = xrRendering;
            this.SMAAQuality = SMAAQuality;
            this.stopNaNs = stopNaNs;
            this.taaSharpenStrength = taaSharpenStrength;
            this.TAAQuality = TAAQuality;
            this.taaHistorySharpening = taaHistorySharpening;
            this.taaAntiFlicker = taaAntiFlicker;
            this.taaMotionVectorRejection = taaMotionVectorRejection;
            this.taaAntiHistoryRinging = taaAntiHistoryRinging;
            this.taaBaseBlendFactor = taaBaseBlendFactor;
            this.taaJitterScale = taaJitterScale;
            this.flipYMode = flipYMode;
            this.fullscreenPassthrough = fullscreenPassthrough;
            this.allowDynamicResolution = allowDynamicResolution;
            this.invertFaceCulling = invertFaceCulling;
            this.probeLayerMask = probeLayerMask;
            this.hasPersistentHistory = hasPersistentHistory;
            this.exposureTarget = exposureTarget;
            this.physicalParameters = physicalParameters;
            this.renderingPathCustomFrameSettings = renderingPathCustomFrameSettings;
            this.renderingPathCustomFrameSettingsOverrideMask = renderingPathCustomFrameSettingsOverrideMask;
            this.defaultFrameSettings = defaultFrameSettings;
            //this.probeCustomFixedExposure = probeCustomFixedExposure;
            this.allowDeepLearningSuperSampling = allowDeepLearningSuperSampling;
            this.deepLearningSuperSamplingUseCustomQualitySettings = deepLearningSuperSamplingUseCustomQualitySettings;
            this.deepLearningSuperSamplingQuality = deepLearningSuperSamplingQuality;
            this.deepLearningSuperSamplingUseCustomAttributes = deepLearningSuperSamplingUseCustomAttributes;
            this.deepLearningSuperSamplingUseOptimalSettings = deepLearningSuperSamplingUseOptimalSettings;
            this.deepLearningSuperSamplingSharpening = deepLearningSuperSamplingSharpening;
            this.materialMipBias = materialMipBias;
#endif
            this.depth = depth;
            this.useOcclusionCulling = useOcclusionCulling;
        }
    }

    //[Serializable]
    public struct AdditionalS3DCamera
    {
        //public Camera[] cameras;
        //public List<Camera> cameras;
        public Camera camera;
        public Camera cameraLeft;
        public Camera cameraRight;
    }

    [Serializable]
    struct SavableVector2
    { 
        public float x;
        public float y;

        public SavableVector2(float inX, float inY)
        {
            x = inX;
            y = inY;
        }
    }

    [Serializable]
    class SaveLoad
    {
        public bool swapLR;
        public bool optimize;
        public bool vSync;
        public Method method;
        public float PPI;
        public float userIPD;
        public float virtualIPD;
        public bool matchUserIPD;
        public float FOV;
        public float panelDepth;
        //public Vector2 panelPos;
        public SavableVector2 panelPos;

        public SaveLoad(
            bool inSwapLR,
            bool inOptimize,
            bool inVSync,
            Method inMethod,
            float inPPI,
            float inUserIPD,
            float inVirtualIPD,
            bool inMatchUserIPD,
            float inFOV,
            float inPanelDepth,
            //Vector2 inPanelPos
            SavableVector2 inPanelPos
            )
        {
            swapLR = inSwapLR;
            optimize = inOptimize;
            vSync = inVSync;
            method = inMethod;
            PPI = inPPI;
            userIPD = inUserIPD;
            virtualIPD = inVirtualIPD;
            matchUserIPD = inMatchUserIPD;
            FOV = inFOV;
            panelDepth = inPanelDepth;
            panelPos = inPanelPos;
        }
    }

    [Serializable]
    class SaveLoadUserName
    {
        public string userName;

        public SaveLoadUserName(string inUserName)
        {
            userName = inUserName;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {
            //Debug.Log("Up and running");

            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                if (package.name == "com.unity.cinemachine")
                {
                    ////Debug.Log(UnityEditor.EditorUserBuildSettings.selectedStandaloneTarget);
                    //var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
                    //UnityEditor.PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
                    ////Debug.Log(defines.Length);

                    //bool cinemachineDefine = false;

                    //foreach (string define in defines)
                    //    if (define.Contains("CINEMACHINE"))
                    //        cinemachineDefine = true;

                    //if (!cinemachineDefine)
                    //{
                    //    string[] newDefines = new string[defines.Length + 1];
                    //    defines.CopyTo(newDefines, 0);
                    //    newDefines.SetValue("CINEMACHINE", defines.Length);
                    //    UnityEditor.PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);

                    //    //foreach (string define in newDefines)
                    //        //Debug.Log(define);
                    //}

                    AddDefine("CINEMACHINE");
                }

            foreach (var assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies())
            {
                if (assembly.name.StartsWith("Assembly-CSharp"))
                {

                    foreach (var fileName in assembly.sourceFiles)
                    {
                        if (fileName.Contains("LookWithMouse"))
                            AddDefine("LookWithMouse");

                        if (fileName.Contains("SimpleCameraController"))
                            AddDefine("SimpleCameraController");
                    }
                }
            }

            //if (GraphicsSettings.defaultRenderPipeline.name.Contains("Universal"))
            //    AddDefine("URP");

            //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
            //    AddDefine("URP");

            //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset"))
            //    AddDefine("HDRP");

            if (GraphicsSettings.defaultRenderPipeline)
            {
                if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
                    AddDefine("URP");
                else
                    if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset"))
                        AddDefine("HDRP");
            }

            //Debug.Log(GraphicsSettings.defaultRenderPipeline.GetType().ToString());
            //Debug.Log(GraphicsSettings.defaultRenderPipeline);
        }
    }

    static void AddDefine(string defineName)
    {
        //Debug.Log("AddDefine");
        var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
        UnityEditor.PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);

        bool alreadyDefined = false;

        foreach (string define in defines)
            if (define.Contains(defineName))
            {
                //Debug.Log(defineName + " already Defined");
                alreadyDefined = true;
            }

        if (!alreadyDefined)
        {
            string[] newDefines = new string[defines.Length + 1];
            defines.CopyTo(newDefines, 0);
            newDefines.SetValue(defineName, defines.Length);

            //foreach(var def in newDefines)
            //    Debug.Log(def);

            UnityEditor.PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
        }
    }
#endif
} 