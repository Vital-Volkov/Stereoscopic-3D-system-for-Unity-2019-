
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
//using static Unity.VisualScripting.Member;

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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

//#if STARTER_ASSETS_PACKAGES_CHECKED
//#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using StarterAssets;
#endif

#if POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

public class Stereo3D : MonoBehaviour
{
    public enum Method {
        Interlace_Horizontal, 
        Interlace_Vertical, 
        Interlace_Checkerboard, 
        SideBySide, 
        SideBySide_Full, 
        SideBySide_HMD, 
        OverUnder, 
        OverUnder_Full, 
        Two_Displays, 
        Two_Displays_MirrorX, 
        Two_Displays_MirrorY, 
        Sequential, 
        D3D11,
        Anaglyph_RedCyan, 
        Anaglyph_RedBlue, 
        Anaglyph_GreenMagenta, 
        Anaglyph_AmberBlue
    };
    //public enum InterlaceType {Horizontal, Vertical, Checkerboard};
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
    public bool hide2DCursor;
    //public Color anaglyphLeftColor = Color.red; //tweak colors at runtime to best match different goggles
    //public Color anaglyphRightColor = Color.cyan;
    public bool cloneCamera = true;
    public GameObject cameraPrefab; //if empty, Stereo3D cameras are copies of the main cam. Set prefab if need custom settings &/or components
    public RenderTextureFormat RTFormat;
    //public RenderTextureFormat RTFormat = RenderTextureFormat.DefaultHDR; //DefaultHDR(16bitFloat) be able to contain Post Process Effects and give fps gain from 328 to 343. In my case RGB111110Float is fastest - 346fps.
    //public bool setMatrixDirectly = true; //shift image Vanish points to User IPD directly via camera Matrix(gives fps boost) or via camera's "physically" settings "lensShift"(required for Post Processing Stack V2 pack as it resets matrix and yields incorrect aspect)
    public string slotName = "User1";
    public bool loadSettingsFromFile = true; //autoload settings at start from last saved file
    //public Canvas canvas;
    //public CameraDataStruct cameraDataStruct;
    public bool detectCameraSettingChange = true;
    public bool debug;

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
    public InputAction GUIAction;
    public InputAction S3DAction;
    public InputAction FOVAction;
    public InputAction modifier1Action;
    public InputAction modifier2Action;
    public InputAction modifier3Action;
    //bool inputSystem = true;
    //bool cursorLockedDefault;
    //bool cursorInputForLookDefault;
    //InputActionAsset S3DInputActionAsset;
    PlayerInput playerInput;
    string GuiActionPath;
    string S3DActionPath;
    string FOVActionPath1;
    string FOVActionPath2;
    string modifier1ActionPath;
    string modifier2ActionPath;
    string modifier3ActionPath;
    //ReadOnlyArray<InputBinding> GUIActionBindings;
    IDisposable inputSystem_KeyListener;
    //StarterAssetsInputs starterAssetsInputs;
#else
    //public KeyCode GUIKey = KeyCode.Tab; //GUI window show/hide Key
    public KeyCode GUIKey = KeyCode.Keypad0; //GUI window show/hide Key
    public KeyCode S3DKey = KeyCode.KeypadMultiply; //S3D enable/disable shortcut Key and hold "LeftControl" Key to swap left-right cameras
    public KeyCode increaseKey = KeyCode.KeypadPlus; //increase Field Of View shortcut Key + hold "Shift" Key to faster change + hold "LeftControl" Key to increase virtual IPD if "matchUserIPD" unchecked
    public KeyCode decreaseKey = KeyCode.KeypadMinus; //decrease Field Of View shortcut Key + hold "Shift" Key to faster change + hold "LeftControl" Key to decrease virtual IPD if "matchUserIPD" unchecked
#endif

    //public Camera[] additionalS3DCameras;
    public List<Camera> additionalS3DCameras;
    //public AdditionalS3DCamera[] additionalS3DCamerasStruct;

    [Header("FPS Boost")]
    public bool disableCullingMask; //disable CullingMask for main camera in S3D mode to FPS boost
    public bool nearClipHack; //Hack for FPS gain in SRP(Scriptable Render Pipeline) and required for custom Viewport Rect but can cause terrain gaps(Test Track Demo in Unity 2021)
    public bool matrixKillHack; //Hack for FPS gain from 308 to 328 and required to fix terrain gaps caused by nearClipHack(Test Track Demo in Unity 2021)

    [Header("Info")]
    public Material S3DMaterial; //generated material
    //public RenderTexture rta;
    Material S3DPanelMaterial; //generated material
    //Material RenderTextureFlipMaterial; //generated material
    //Material RenderTextureFlipMaterial2; //generated material

    Camera cam;
    //Camera camClone;
    Camera camera_left;
    Camera camera_right;
    int sceneCullingMask;
    float nearClip;
    float sceneNearClip;
    float farClip;
    float sceneFarClip;
    float cameraNearClip;
    //float canvasNearClip;
    Vector2[] verticesPos = new Vector2[4];
    Vector2[] verticesUV = new Vector2[4];

    //bool defaultRender;
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
    Vector2 lastPanelPosition;
    bool lastGUIAsOverlay;
    bool lastGUISizeKeep;
    GameObject lastCameraPrefab;
    RenderTextureFormat lastRTFormat;
    //bool lastSetMatrixDirectly;
    //InterlaceType lastInterlaceType;
    //Color lastAnaglyphLeftColor;
    //Color lastAnaglyphRightColor;
    string lastSlotName;
    Rect lastCamRect;
    bool lastDisableCullingMask;
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
    Camera canvasCamera;
    Camera canvasCamera_left;
    Camera canvasCamera_right;
    //Matrix4x4 canvasCamMatrix;
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
    //bool nearClipHackApplyed;
    CursorLockMode cursorLockModeDefault;
#if URP
    UniversalAdditionalCameraData camData;
    UniversalRenderPipelineAsset URPAsset;
    //UniversalRenderPipelineAsset lastURPAsset;
    ScriptableRenderer lastScriptableRenderer;
    //int lastMSAASampleCount;
    bool URPAssetIsReady;
    List<Camera> cameraStack;
    List<Camera> leftCameraStack;
    List<Camera> rightCameraStack;
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
    HDAdditionalCameraData canvasCamData;
    HDAdditionalCameraData canvasCamData_left;
    HDAdditionalCameraData canvasCamData_right;
    //bool GUIAsOverlayState;
    //RenderTexture canvasRenderTexture;
    //RenderTexture canvasRenderTexture_left;
    //RenderTexture canvasRenderTexture_right;
    LayerMask volumeLayerMask;
    LayerMask probeLayerMask;
#endif
    //UniversalAdditionalCameraData camCloneData;
    //List<UniversalAdditionalCameraData> oldList;
    //List<UniversalAdditionalCameraData> newList;
    CameraDataStruct cameraDataStruct;
    //List<Camera> leftRightCameraStack;
    //Vector2 panel3DdepthMinMax;
    //Vector2 panelOverlayDepthMinMax;
    AdditionalS3DCamera[] additionalS3DCamerasStruct;
    //bool loaded;

#if POST_PROCESSING_STACK_V2
    PostProcessLayer PPLayer;
    bool PPLayerDefaultStatus;
    bool PPLayerLastStatus;
#endif

#if CINEMACHINE
    bool cineMachineEnabled;
    Cinemachine.ICinemachineCamera vCam;
    //Cinemachine.ICinemachineCamera defaultVCam;
    Cinemachine.CinemachineBrain cineMachineBrain;
    //bool vCamSceneClipSetInProcess;
    //bool vCamSceneClipIsReady;
    //bool vCamClipSetInProcess;
    //bool vCamSceneClipRestoreInProcess;
    //bool nearClipHackApplyed;
    //bool vCamChanged;
    bool vCamSelected;
    float vCamSelectedTimer;
#endif

#if LookWithMouse
    LookWithMouse lookWithMouseScript;
#endif

//#if SimpleCameraController //by default it using right mouse button to look around and not conflict with S3D panel
//    UnityTemplateProjects.SimpleCameraController simpleCameraControllerScript;
//#endif

//#if STARTER_ASSETS_PACKAGES_CHECKED
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
    StarterAssetsInputs starterAssetsInputs;
    bool cursorLockedDefault;
    bool cursorInputForLookDefault;
#endif

    float GUI_Set_delay;
    //float setLastCameraDataStructTime;
    bool cameraDataStructIsReady;
    bool physicalCamera;
    Vector2 camVanishPoint;
    //RenderTextureFormat defaultRTFormat;
    bool lastHide2DCursor;

    public void Awake()
    {
        if (!name.Contains("(Clone)")) //avoid code execution of this script copy from camera clones before removing unwanted components
        {
            if (debug) Debug.Log("Awake");
            /* Screen.width is not correct OnEnable so viewportWidth need to be set here or at variable declaration and update only on changing
             GameObject.Find("SceneCamera").GetComponent<Camera>().pixelRect also not correct OnEnable
             EditorWindow.GetWindow<SceneView>().camera.pixelRect works but changing active window in player to scene view
             */

            GUI_Set_delay = Time.deltaTime * 2;

            //for (int i = 0; i < 4; i++)
            //{
            //    int val = (i ^ 3);
            //    if (debug) Debug.Log(i + " ^ 3 = " + val);
            //}

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            //if (debug) Debug.Log(GUIAction.bindings.Count);

            if (GUIAction.bindings.Count == 0)
                //GUIAction.AddBinding("<Keyboard>/tab");
                GUIAction.AddBinding("<Keyboard>/numpad0");

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

            //if (debug) Debug.Log(modifier3Action.bindings[0].effectivePath);
            //Debug.Log(GUIAction.bindings[0].effectivePath);

            GUIAction.performed += context => { OnGUIAction(context); };
            //S3DAction.performed += context => { OnS3DAction(context); };
            //FOVAction.performed += context => { OnVirtualIPDAction(context); };
            //modifier1Action.performed += context => { OnModifier1Action(context); };
            //modifier2Action.performed += context => { OnModifier2Action(context); };

            //GUIActionBindings = GUIAction.bindings;
            GuiActionPath = GUIAction.bindings[0].path;
            S3DActionPath = S3DAction.bindings[0].path;
            FOVActionPath1 = FOVAction.bindings[1].path;
            FOVActionPath2 = FOVAction.bindings[2].path;
            modifier1ActionPath = modifier1Action.bindings[0].path;
            modifier2ActionPath = modifier2Action.bindings[0].path;
            modifier3ActionPath = modifier3Action.bindings[0].path;
#endif

#if !HDRP
            if(RTFormat == RenderTextureFormat.ARGB32 && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                RTFormat = RenderTextureFormat.RGB111110Float; //preffered fastest format as default ARGB32 but supported HDR post process(like lamp glow in 3D Sample Extra Unity 2019)
#endif
        }
    }

    //GameObject selectedObject;
    int additionalS3DTopmostCameraIndex;
    Camera lastAdditionalS3DTopmostCamera;

    void OnEnable()
    {
        //if (debug) Debug.Log("OnEnable " + name);
        //if (debug) Debug.Log("OnEnable panelDepth " + panelDepth);

        if (!name.Contains("(Clone)"))
        {
            if (debug) Debug.Log("OnEnable cameraDataStructIsReady " + cameraDataStructIsReady);
            //if (debug) Debug.Log("windowSize " + windowSize);
            //List<Type> types = GetAllTypesInAssembly(new string[] { "Assembly-CSharp" });
            //if (debug) Debug.Log(types.Count);

            //foreach (var type in types)
            //{
            //    if (debug) Debug.Log(type.Name);

            //    //if (type.Name.Contains("LookWithMouse", StringComparison.CurrentCultureIgnoreCase))
            //    //    if (debug) Debug.Log(type.AssemblyQualifiedName);
            //}

            //if (debug) Debug.Log(types.Exists(x => x.Name.Contains("mouse", StringComparison.CurrentCultureIgnoreCase)));
            //var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();

            //foreach (var assemble in assemblies)
            //{
            //    if (debug) Debug.Log(assemble.name);

            //    if (assemble.name.Contains("mouse", StringComparison.CurrentCultureIgnoreCase))
            //        if (debug) Debug.Log("***********************************");
            //}

            //if (Type.GetType("LookWithMouse") != null)
            //    if (debug) Debug.Log("LookWithMouse");

            //if (GraphicsSettings.defaultRenderPipeline == null)
            //    defaultRender = true;
            ////else
            ////    if (GraphicsSettings.defaultRenderPipeline.name.Contains("URP"))
            ////        AddDefine("URP");

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                S3DMaterial = new Material(Shader.Find("Stereo3D Screen Quad GLES2"));
            else
                S3DMaterial = new Material(Shader.Find("Stereo3D Screen Quad"));

            //S3DMaterial.SetColor("_LeftCol", anaglyphLeftColor);
            //S3DMaterial.SetColor("_RightCol", anaglyphRightColor);
            //S3DPanelMaterial = new Material(Shader.Find("UI/Default_Mod"));
            //S3DPanelMaterial = (Material)Resources.Load("3D_Panel", typeof(Material));
            S3DPanelMaterial = Resources.Load<Material>("3D_Panel");
            //RenderTextureFlipMaterial = new Material(Shader.Find("Render Texture Flip"));
            //RenderTextureFlipMaterial2 = new Material(Shader.Find("Render Texture Flip"));

            cam = GetComponent<Camera>();

#if POST_PROCESSING_STACK_V2
        if (GetComponent<PostProcessLayer>())
        {
            PPLayer = GetComponent<PostProcessLayer>();
            PPLayer.finalBlitToCameraTarget = false;
            PPLayerLastStatus = PPLayerDefaultStatus = PPLayer.enabled;

            //if (PPLayerDefaultStatus)
            //    //setMatrixDirectly = false;
            //    cam.usePhysicalProperties = true;
        }
#endif

            cam.orthographic = false; //S3D is not possible in orthographic
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            sceneCullingMask = cam.cullingMask;
            if (debug) Debug.Log("sceneCullingMask " + sceneCullingMask);
            physicalCamera = cam.usePhysicalProperties;

            //if (cam.projectionMatrix != Matrix4x4.zero)
                camMatrix = cam.projectionMatrix;

            if (debug) Debug.Log("OnEnable camMatrix\n" + camMatrix);
            camVanishPoint = new Vector2(cam.projectionMatrix[0, 2], cam.projectionMatrix[1, 2]);
            //nearClip = sceneNearClip = cam.nearClipPlane;

            //if (additionalS3DCameras.Count != 0)
            //{
            //    //nearClip = sceneNearClip = additionalS3DCameras[additionalS3DCameras.Count - 1].nearClipPlane;

            //    //for (int i = additionalS3DCameras.Count - 1; i >= 0; i--)
            //    //{
            //    //    if (additionalS3DCameras[i] != null)
            //    //    {
            //    //        //if (debug) Debug.Log("i " + i);
            //    //        additionalS3DTopmostCameraIndex = i;
            //    //        if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);
            //    //        break;
            //    //    }
            //    //}

            //    ClosestCameraIndex_Set();
            //    //additionalS3DTopmostCameraIndex = additionalS3DCameras.Count - 2;
            //    nearClip = sceneNearClip = additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane;
            //}
            //else
            //    nearClip = sceneNearClip = cam.nearClipPlane;

#if CINEMACHINE
            //if (GetComponent<Cinemachine.CinemachineBrain>().enabled)
            //{
            //    //cineMachineBrain = GetComponent<Cinemachine.CinemachineBrain>();
            //    cineMachineEnabled = true;
            //    Invoke("GetVCam", Time.deltaTime);
            //}
            //else
            //    sceneNearClip = cam.nearClipPlane;

            if (GetComponent<Cinemachine.CinemachineBrain>() && GetComponent<Cinemachine.CinemachineBrain>().enabled)
            {
                cineMachineBrain = GetComponent<Cinemachine.CinemachineBrain>();
                cineMachineEnabled = true;
                //nearClipHack = true; //required to kill main camera rendering in S3D mode instead of using cam.cullingMask = 0 via VCamCullingOff which cause Cinemachine not working
                //vCamSceneClipSetInProcess = true;
                //vCamSceneClipIsReady = false;
                Invoke("GetVCam", Time.deltaTime);
                //GetVCam();
            }
            //else
#endif
            ////cameraNearClip = cam.nearClipPlane;
            //ClosestCameraIndex_Set();
            //SceneNearClip_Set();
            //SceneFarClip_Set();
            //nearClip = sceneNearClip; //set nearClip & farClip to non zero before Clip_Set call delayed after GUI_Set
            ////farClip = sceneFarClip = cam.farClipPlane;
            //farClip = sceneFarClip;

//#if CINEMACHINE
//            //if (GetComponent<Cinemachine.CinemachineBrain>().enabled)
//            //{
//            //    //cineMachineBrain = GetComponent<Cinemachine.CinemachineBrain>();
//            //    cineMachineEnabled = true;
//            //    Invoke("GetVCam", Time.deltaTime);
//            //}
//            //else
//            //    sceneNearClip = cam.nearClipPlane;

//            if (GetComponent<Cinemachine.CinemachineBrain>() && GetComponent<Cinemachine.CinemachineBrain>().enabled)
//            {
//                //cineMachineBrain = GetComponent<Cinemachine.CinemachineBrain>();
//                cineMachineEnabled = true;
//                //vCamSceneClipSetInProcess = true;
//                vCamSceneClipIsReady = false;
//                Invoke("GetVCam", Time.deltaTime);
//                //GetVCam();
//            }
//            //else
//#endif
            //{
            //    //sceneNearClip = cam.nearClipPlane;
            //    //sceneFarClip = cam.farClipPlane;
            //    nearClip = sceneNearClip = cam.nearClipPlane;
            //    farClip = sceneFarClip = cam.farClipPlane;
            //}

            //if (debug) Debug.Log("OnEnable sceneNearClip " + sceneNearClip);
            //if (debug) Debug.Log("OnEnable sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);

//#if POST_PROCESSING_STACK_V2
//        if (GetComponent<PostProcessLayer>())
//        {
//            PPLayer = GetComponent<PostProcessLayer>();
//            PPLayerDefaultStatus = PPLayer.enabled;

//            if (PPLayerDefaultStatus)
//                //setMatrixDirectly = false;
//                cam.usePhysicalProperties = true;
//        }
//#endif

            //#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
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
                camera_left = Instantiate(cameraPrefab, transform.position, transform.rotation).GetComponent<Camera>();
                camera_left.name = "prefabCamera_left";
                camera_right = Instantiate(cameraPrefab, transform.position, transform.rotation).GetComponent<Camera>();
                camera_right.name = "prefabCamera_right";
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
                    //////if (debug) Debug.Log(newList.Count);
                    //////if (debug) Debug.Log(camCloneData.renderPostProcessing);
                    //camCloneData = camClone.GetUniversalAdditionalCameraData();
                    ////oldList[0] = newList[0];

                    ////var obj1 = new GameObject("obj1");
                    //////obj1.AddComponent<Camera>();
                    ////var obj2 = new GameObject("obj2");

                    //if (obj1 != obj2)
                    //    if (debug) Debug.Log(obj1.GetInstanceID() + " " + obj2.GetInstanceID());

                    //camera_left = Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Camera>();
                    camera_left = Instantiate(cam, transform.position, transform.rotation);
                    //camera_left.tag = "Untagged";
                    camera_left.name += "_left";
                    //camera_right = Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Camera>();
                    camera_right = Instantiate(cam, transform.position, transform.rotation);
                    //camera_right.tag = "Untagged";
                    camera_right.name += "_right";

                    //for (int child = 0; child < camera_left.transform.childCount; child++)
                    //{
                    //    //if (debug) Debug.Log(camera_left.transform.GetChild(child));
                    //    Destroy(camera_left.transform.GetChild(child).gameObject);
                    //}

                    //foreach (var component in camera_left.GetComponents(typeof(Component)))
                    //    if (!(component is Transform) && !(component is Camera) && !(component is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData))
                    //        Destroy(component);

                    //DestroyComponents(camera_left);
                    //DestroyComponents(camera_right);

                    //void DestroyComponents(Camera camera)
                    //{
                    //    if (debug) Debug.Log("destroyComponents");

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
#elif POST_PROCESSING_STACK_V2
                            && !(component is PostProcessLayer)
#endif
                            )
                        {
                            //Type componentType = component.GetType();
                            //Destroy(camClone.GetComponent(component.GetType()));
                            Destroy(camera_left.GetComponent(component.GetType()));
                            Destroy(camera_right.GetComponent(component.GetType()));
                        }

                    //camera_right = Instantiate(camera_left, transform.position, transform.rotation).GetComponent<Camera>();
                    //camera_right.name = "camera_right";

                    for (int child = 0; child < camera_left.transform.childCount; child++)
                    {
                        //if (debug) Debug.Log(camera_left.transform.GetChild(child));
                        Destroy(camera_left.transform.GetChild(child).gameObject);
                        Destroy(camera_right.transform.GetChild(child).gameObject);
                    }

                    //universalAdditionalCameraData = new UniversalAdditionalCameraData();
                    //cameraDataStruct = new CameraDataStruct(cam.GetUniversalAdditionalCameraData().renderPostProcessing, cam.GetUniversalAdditionalCameraData().antialiasing);
//#if URP
//                    camData = cam.GetUniversalAdditionalCameraData();
//#elif HDRP
//                    //camData = cam.gameObject.GetComponent<HDAdditionalCameraData>();
//                    //camData = cam.GetComponent<HDAdditionalCameraData>();
//                    CamData_Get();

//                    //UnityEditor.Selection.activeGameObject = camera_left.gameObject;
//                    //HDAdditionalCameraData leftCamData = camera_left.GetComponent<HDAdditionalCameraData>();
//                    //leftCamData = camera_left.GetComponent<HDAdditionalCameraData>();
//                    //if (debug) Debug.Log("camData = " + camData);
//                    //leftCamData = camera_left.gameObject.AddComponent<HDAdditionalCameraData>();
//                    //camData.CopyTo(leftCamData);
//                    ////UnityEditor.Selection.activeGameObject = camera_right.gameObject;
//                    ////HDAdditionalCameraData rightCamData = camera_right.GetComponent<HDAdditionalCameraData>();
//                    //rightCamData = camera_right.gameObject.AddComponent<HDAdditionalCameraData>();
//                    //camData.CopyTo(rightCamData);
//#endif
                    //cameraDataStruct = new CameraDataStruct(camData.renderPostProcessing, camData.antialiasing);
                    //CameraDataStruct_Set();
                    //cameraDataStruct = new CameraDataStruct();
                    //cameraDataStruct = cam.GetUniversalAdditionalCameraData() as CameraDataStruct;
                    //cameraDataStruct = new GameObject("camHelper").AddComponent<UniversalAdditionalCameraData>();
                }
                else
                {
                    camera_left = new GameObject("camera_left").AddComponent<Camera>();
                    camera_right = new GameObject("camera_right").AddComponent<Camera>();
                    camera_left.CopyFrom(cam);
                    camera_right.CopyFrom(cam);
                    //GameObject lc = new GameObject("camera_left");
                    //GameObject rc = new GameObject("camera_right");
                    //camera_left = (Camera)CopyComponent(GetComponent<Camera>(), lc);
                    //camera_right = (Camera)CopyComponent(GetComponent<Camera>(), rc);
                    //camera_left.CopyFrom(cam);
                    //camera_right.CopyFrom (cam);
                    //camera_left.rect = camera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);
                }
            }

            camera_left.tag = camera_right.tag = "MainCamera";

            //DestroyImmediate(Instantiate(cam.gameObject, transform.position, transform.rotation).GetComponent<Stereo3D>());
            //GameObject go = cam.transform.Find("Plane").gameObject;
            //go.SetActive(false);
            //Instantiate(go, transform.position, transform.rotation);
            //if (debug) Debug.Log(name);

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

            //camera_left.CopyFrom(cam);
            //camera_right.CopyFrom(cam);
            //var camData = cam.GetComponents<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //camera_left.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //camera_right.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            //ComponentExtensions.GetCopyOf(camera_left, cam);
            //ComponentExtensions.GetCopyOf(camera_right, cam);
            //camera_left.name = "camera_left";
            //camera_right.name = "camera_right";

            //camera_left.rect = camera_right.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
            //camera_left.depth = camera_right.depth = cam.depth;
            camera_left.transform.parent = camera_right.transform.parent = transform;
            camera_left.usePhysicalProperties = camera_right.usePhysicalProperties = cam.usePhysicalProperties;
            camera_left.stereoTargetEye = StereoTargetEyeMask.Left;
            camera_right.stereoTargetEye = StereoTargetEyeMask.Right;
            //camera_left.allowMSAA = camera_right.allowMSAA = false;
            //if (debug) Debug.Log("sceneCullingMask " + sceneCullingMask);

            if (Screen.dpi != 0)
                PPI = Screen.dpi;

            canvas = Instantiate(Resources.Load<Canvas>("Canvas"), transform.position, transform.rotation);
            canvas.gameObject.SetActive(false);
            //canvas.transform.parent = transform;
            canvas.transform.SetParent(transform);
            canvasRayCam = canvas.gameObject.AddComponent<Camera>();
            canvasRayCam.stereoTargetEye = StereoTargetEyeMask.None;
            canvasRayCam.enabled = false;
            //orthoCamSet(canvasRayCam);
            //canvasRayCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            canvasRayCam.orthographic = true;
            canvasRayCam.nearClipPlane = -1;
            canvasRayCam.farClipPlane = 1;
            //canvasRayCam.depth = cam.depth - 1;
            canvasRayCam.cullingMask = 0;
            canvasRayCam.useOcclusionCulling = false;
            //canvasRayCam.allowMSAA = false; //fixed black background when render to texture with antialiasing is same as in quality settings and blit to screen by same camera in default render
            canvas.worldCamera = canvasRayCam;
            //canvas.pixelPerfect = true;
            additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
            additionalS3DTopmostCameraIndex = -1; //if no additionalS3DCameras
            lastAdditionalS3DTopmostCamera = null;

#if URP
            camData = cam.GetUniversalAdditionalCameraData();
            //canvasRayCam.GetUniversalAdditionalCameraData().clearDepth = false;
            canvasRayCam.GetUniversalAdditionalCameraData().renderShadows = false;
            //cameraStack = cam.GetUniversalAdditionalCameraData().cameraStack;
            cameraStack = camData.cameraStack;
            leftCameraStack = camera_left.GetUniversalAdditionalCameraData().cameraStack;
            rightCameraStack = camera_right.GetUniversalAdditionalCameraData().cameraStack;
            leftCameraStack.RemoveAll(t => t); //clean stacks after clone from the main camera
            rightCameraStack.RemoveAll(t => t);

            //if (debug) Debug.Log(cameraStack.Count);

            //foreach (var cam in cameraStack)
            //    if (debug) Debug.Log(cam);

            //additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
            //int i = 0;

            //foreach (var c in additionalS3DCameras)
            //{
            //    if (c && !c.transform.Find(c.name + "(Clone)_left"))
            //    {
            //        Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
            //        cloneLeft.tag = "Untagged";
            //        cloneLeft.name += "_left";
            //        cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
            //        //cloneLeft.targetTexture = renderTexture_left;
            //        Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
            //        cloneRight.tag = "Untagged";
            //        cloneRight.name += "_right";
            //        cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
            //        //cloneRight.targetTexture = renderTexture_right;

            //        cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
            //        //c.enabled = false;

            //        int index = additionalS3DCameras.IndexOf(c);
            //        additionalS3DCamerasStruct[index].camera = c;
            //        additionalS3DCamerasStruct[index].camera_left = cloneLeft;
            //        additionalS3DCamerasStruct[index].camera_right = cloneRight;
            //        //i++;
            //    }
            //}

            //foreach (var c in cameraStack)
            //    //if (additionalS3DCameras.Contains(c) && !c.transform.Find(c.name + "(Clone)_left"))
            //    if (additionalS3DCameras.Contains(c))
            //    {
            //        int index = additionalS3DCameras.IndexOf(c);

            //        if (c.transform.Find(c.name + "(Clone)_left"))
            //        {
            //            additionalS3DCamerasStruct[index].camera = c;
            //            additionalS3DCamerasStruct[index].camera_left = c.transform.Find(c.name + "(Clone)_left").GetComponent<Camera>();
            //            additionalS3DCamerasStruct[index].camera_right = c.transform.Find(c.name + "(Clone)_right").GetComponent<Camera>();
            //        }
            //        else
            //        {
            //            Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
            //            cloneLeft.tag = "Untagged";
            //            cloneLeft.name += "_left";
            //            cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
            //            //cloneLeft.targetTexture = renderTexture_left;
            //            Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
            //            cloneRight.tag = "Untagged";
            //            cloneRight.name += "_right";
            //            cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
            //            //cloneRight.targetTexture = renderTexture_right;

            //            cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
            //            //c.enabled = false;

            //            //int index = additionalS3DCameras.IndexOf(c);
            //            //int index = cameraStack.IndexOf(c);
            //            additionalS3DCamerasStruct[index].camera = c;
            //            additionalS3DCamerasStruct[index].camera_left = cloneLeft;
            //            additionalS3DCamerasStruct[index].camera_right = cloneRight;
            //            //i++;
            //        }
            //    }

            //additionalS3DTopmostCameraIndex = -1; //if no additionalS3DCameras
            //lastAdditionalS3DTopmostCamera = null;

            //for (int i = 0; i < additionalS3DCameras.Count; i++) //foreach not working if member is duplicate
            //{
            //    Camera c = additionalS3DCameras[i];

            //    //if (!cameraStack.Contains(c))
            //    //    cameraStack.Add(c);

            //    //if (c == null)
            //    //    if (debug) Debug.Log("int i = 0; i < additionalS3DCameras.Count; i++ c == null");
            //    //else
            //    //    additionalS3DTopmostCameraIndex = i;

            //    //if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);

            //    if (c != null)
            //    {
            //        additionalS3DTopmostCameraIndex = i;
            //        lastAdditionalS3DTopmostCamera = c;

            //        if (c.transform.Find(c.name + "(Clone)_left"))
            //        {
            //            if (debug) Debug.Log("c.transform.Find(c.name + '(Clone)_left')");
            //            additionalS3DCamerasStruct[i].camera = c;
            //            additionalS3DCamerasStruct[i].camera_left = c.transform.Find(c.name + "(Clone)_left").GetComponent<Camera>();
            //            additionalS3DCamerasStruct[i].camera_right = c.transform.Find(c.name + "(Clone)_right").GetComponent<Camera>();
            //        }
            //        else
            //        {
            //            Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
            //            cloneLeft.tag = "Untagged";
            //            cloneLeft.name += "_left";
            //            cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
            //            //cloneLeft.targetTexture = renderTexture_left;
            //            Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
            //            cloneRight.tag = "Untagged";
            //            cloneRight.name += "_right";
            //            cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
            //            //cloneRight.targetTexture = renderTexture_right;

            //            cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
            //            //c.enabled = false;

            //            //int i = additionalS3DCameras.IndexOf(c);
            //            //int i = cameraStack.IndexOf(c);
            //            additionalS3DCamerasStruct[i].camera = c;
            //            additionalS3DCamerasStruct[i].camera_left = cloneLeft;
            //            additionalS3DCamerasStruct[i].camera_right = cloneRight;
            //            //i++;
            //        }
            //    }

            //    if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);
            //}

            ////lastAdditionalS3DTopmostCamera = additionalS3DCameras[additionalS3DTopmostCameraIndex];
            //SceneNearClip_Set();
            //SceneFarClip_Set();
            //nearClip = sceneNearClip; //set nearClip & farClip to non zero before Clip_Set call delayed after GUI_Set
            //farClip = sceneFarClip;
            //if (debug) Debug.Log("OnEnable sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);

            URPAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            //URPAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            ////lastURPAsset = new UniversalRenderPipelineAsset();
            //lastURPAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            //lastURPAsset = URPAsset;
            //lastScriptableRenderer = URPAsset.scriptableRenderer;
            //lastMSAASampleCount = URPAsset.msaaSampleCount;
            //Invoke("URPAsset_Get", GUI_Set_delay);
            Invoke("URPAssetSettings_Get", GUI_Set_delay);
//#else
//            //additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
//            float depth = cam.depth;

//            foreach (var c in additionalS3DCameras)
//            {
//                if (c && !c.transform.Find(c.name + "(Clone)_left"))
//                {
//                    if (c.depth <= depth)
//                        c.depth = depth += 1;
//                    else
//                        depth = c.depth;

//#if HDRP
//                    HDAdditionalCameraData additionalCamData = c.gameObject.GetComponent<HDAdditionalCameraData>();

//                    if (additionalCamData.clearColorMode != HDAdditionalCameraData.ClearColorMode.Color)
//                    {
//                        if (debug) Debug.Log("additionalCamData.clearColorMode != HDAdditionalCameraData.ClearColorMode.Color");
//                        additionalCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
//                        additionalCamData.backgroundColorHDR = Color.clear;
//                    }

//                    //additionalCamData.volumeLayerMask = 0;
//                    ////additionalCamData.clearDepth = true;
//                    //additionalCamData.probeLayerMask = 0;
//#endif

//                    Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
//                    cloneLeft.tag = "Untagged";
//                    cloneLeft.name += "_left";
//                    cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
//                    cloneLeft.rect = Rect.MinMaxRect(0, 0, 1, 1);
//                    Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
//                    cloneRight.tag = "Untagged";
//                    cloneRight.name += "_right";
//                    cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
//                    cloneRight.rect = Rect.MinMaxRect(0, 0, 1, 1);

//                    cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
//                    cloneLeft.usePhysicalProperties = cloneRight.usePhysicalProperties = c.usePhysicalProperties = cam.usePhysicalProperties;

//                    int index = additionalS3DCameras.IndexOf(c);
//                    additionalS3DCamerasStruct[index].camera = c;
//                    additionalS3DCamerasStruct[index].camera_left = cloneLeft;
//                    additionalS3DCamerasStruct[index].camera_right = cloneRight;

//                    additionalS3DTopmostCameraIndex = index;
//                    lastAdditionalS3DTopmostCamera = c;

//                    if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);
//                }
//            }
#endif

            float depth = cam.depth;

            for (int i = 0; i < additionalS3DCameras.Count; i++) //foreach not go throught all iterations if member is copy and not set correct additionalS3DCamerasStruct
            {
                Camera c = additionalS3DCameras[i];

                //if (c != null)
                if (c)
                {
                    c.stereoTargetEye = StereoTargetEyeMask.None;
                    additionalS3DTopmostCameraIndex = i;
                    lastAdditionalS3DTopmostCamera = c;

                    if (c.transform.Find(c.name + "(Clone)_left"))
                    {
                        if (debug) Debug.Log("c.transform.Find(c.name + '(Clone)_left')");
                        additionalS3DCamerasStruct[i].camera = c;
                        additionalS3DCamerasStruct[i].camera_left = c.transform.Find(c.name + "(Clone)_left").GetComponent<Camera>();
                        additionalS3DCamerasStruct[i].camera_right = c.transform.Find(c.name + "(Clone)_right").GetComponent<Camera>();
                    }
                    else
                    {
                        if (c.depth <= depth)
                            c.depth = depth += 1;
                        else
                            depth = c.depth;

#if HDRP
                        HDAdditionalCameraData additionalCamData = c.gameObject.GetComponent<HDAdditionalCameraData>();

                        if (additionalCamData.clearColorMode != HDAdditionalCameraData.ClearColorMode.Color)
                        {
                            if (debug) Debug.Log("additionalCamData.clearColorMode != HDAdditionalCameraData.ClearColorMode.Color");
                            additionalCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                            additionalCamData.backgroundColorHDR = Color.clear;
                        }
//#elif URP
//                    c.clearFlags = CameraClearFlags.Nothing;
//#else
#elif !URP
                        c.clearFlags = CameraClearFlags.Depth;
#endif

#if POST_PROCESSING_STACK_V2
                        if (c.GetComponent<PostProcessLayer>())
                        {
                            additionalS3DCamerasStruct[i].PPLayer = c.GetComponent<PostProcessLayer>();
                            additionalS3DCamerasStruct[i].PPLayer.finalBlitToCameraTarget = false;
                            //additionalS3DCamerasStruct[i].PPLayerDefaultStatus = additionalS3DCamerasStruct[i].PPLayer.enabled;
                            additionalS3DCamerasStruct[i].PPLayerLastStatus = additionalS3DCamerasStruct[i].PPLayer.enabled;

                            //if (PPLayerDefaultStatus)
                            //    //setMatrixDirectly = false;
                            //    cam.usePhysicalProperties = true;
                        }
#endif

                        Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
                        cloneLeft.tag = "Untagged";
                        cloneLeft.name += "_left";
                        cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
                        //cloneLeft.rect = Rect.MinMaxRect(0, 0, 1, 1);
                        Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
                        cloneRight.tag = "Untagged";
                        cloneRight.name += "_right";
                        cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
                        //cloneRight.rect = Rect.MinMaxRect(0, 0, 1, 1);
                        //cloneLeft.allowMSAA = cloneRight.allowMSAA = false;

                        cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
                        c.tag = cloneLeft.tag = cloneRight.tag = "MainCamera";

                        additionalS3DCamerasStruct[i].camera = c;
                        additionalS3DCamerasStruct[i].camera_left = cloneLeft;
                        additionalS3DCamerasStruct[i].camera_right = cloneRight;
                    }
                }

                if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);
            }

            SceneNearClip_Set();
            SceneFarClip_Set();
            nearClip = sceneNearClip; //set nearClip & farClip to non zero before Clip_Set call delayed after GUI_Set
            farClip = sceneFarClip;
            if (debug) Debug.Log("OnEnable sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);
//#elif HDRP
#if HDRP
            camData = cam.GetComponent<HDAdditionalCameraData>();
            volumeLayerMask = camData.volumeLayerMask;
            probeLayerMask = camData.probeLayerMask;
            HDRPAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            defaultHDRPSettings = HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //Invoke("Test", 5);

            //if (method != Method.Two_Displays)
            //    GUIAsOverlayState = GUIAsOverlay;
#endif

            //            if (GUIAsOverlay)
            //            {
            //                //panelDepthMinMax = new Vector2(0, 1);
            //                //panelDepth = Mathf.Clamp01(panelDepth);
            //                canvasCamera = new GameObject("canvasCamera").AddComponent<Camera>();
            //                canvasCamera.CopyFrom(canvasRayCam);
            //#if URP
            //                canvasCamera.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
            //                //canvasCamera.clearFlags = CameraClearFlags.Nothing;
            //#else
            //                //canvasCamera.clearFlags = CameraClearFlags.Color;
            //                canvasCamera.backgroundColor = Color.clear;
            //#endif
            //                //canvasCamera.transform.parent = canvas.transform;
            //                canvasCamera.transform.SetParent(canvas.transform);
            //                //orthoCamSet(canvasCamera);

            //                //camera_left.cullingMask = camera_right.cullingMask = cam.cullingMask = sceneCullingMask & ~(1 << 5); //exclude UI layer
            //                canvasCamera.cullingMask = canvasCamera.cullingMask | (1 << 5); //add UI layer
            //                //canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            //                //canvasCamera.enabled = false;
            //                //cam.GetUniversalAdditionalCameraData().cameraStack.Add(canvasCamera);

            //                //if (!cameraStack.Contains(canvasCamera))
            //                //    cameraStack.Add(canvasCamera);

            //                ////Invoke("CameraStackSet", Time.deltaTime * 8);
            //                //canvasCamera.clearFlags = CameraClearFlags.Nothing;
            //                ////canvasCamMatrix = canvasCamera.projectionMatrix;
            //                ////CameraStackSet();
            //                ////canvasCamera.depth = cam.depth - 1;
            //                ////canvasCamera.enabled = false;

            //#if HDRP
            //                //HDCamData = cam.gameObject.GetComponent<HDAdditionalCameraData>();
            //                ////HDCamDataCopy = new HDAdditionalCameraData();
            //                //GameObject HDRPCamCopy = new GameObject("HDRPCamDataCopy");
            //                //HDCamDataCopy = HDRPCamCopy.AddComponent<HDAdditionalCameraData>();
            //                //HDRPCamCopy.AddComponent<HDAdditionalCameraData>();
            //                //GameObject HDRPCamCopy2 = new GameObject("HDRPCamDataCopy2");
            //                //HDCamDataCopy2 = HDRPCamCopy2.AddComponent<HDAdditionalCameraData>();
            //                //HDRPCamCopy2.AddComponent<HDAdditionalCameraData>();
            //                ////HDCamDataCopy.gameObject.GetComponent<Camera>().enabled = false;
            //                //HDCamData.CopyTo(HDCamDataCopy);
            //                //HDCamData.CopyTo(HDCamDataCopy2);
            //                ////HDCamDataCopy.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            //                ////HDCamera HDCam = HDCamera.GetOrCreate(cam);
            //                ////HDCam = HDCamera.GetOrCreate(cam);
            //                //HDCam = HDCamera.GetOrCreate(HDRPCamCopy.GetComponent<Camera>());
            //                //HDCam2 = HDCamera.GetOrCreate(HDRPCamCopy2.GetComponent<Camera>());
            //                ////HDCamDataCopy = (HDAdditionalCameraData)typeof(HDCamera).GetField("m_AdditionalCameraData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(HDCam);
            //                //FieldInfo[] fields = typeof(HDAdditionalCameraData).GetFields();
            //                //string str = File.ReadAllText(@"C:\Users\Vital\Desktop\New Text Document.txt");
            //                ////string[] lines = File.ReadAllLines(@"C:\Users\Vital\Desktop\New Text Document.txt");

            //                //foreach (FieldInfo field in fields)
            //                //{
            //                //    //if (field.Name.Contains("physicalParameters"))
            //                //    if (!str.Contains(field.Name))
            //                //        if (debug) Debug.Log(field);
            //                //}

            //                //foreach (var line in lines)
            //                //{
            //                //    bool contains = false;

            //                //    foreach (FieldInfo field in fields)
            //                //    {
            //                //        if (line.Contains(field.Name))
            //                //            contains = true;
            //                //    }

            //                //    if (!contains)
            //                //        if (debug) Debug.Log(line);
            //                //}

            //                ////HDCamDataCopy_Get();
            //                //selectedObject = UnityEditor.Selection.activeGameObject;
            //                //CanvasCamHDRPData_Set(); //get HDRP HDAdditionalCameraData by select runtime created canvasCamera object in the editor
            //                //HDAdditionalCameraData canvasCamData = canvasCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            //                canvasCamData = canvasCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            //                canvasCamData.volumeLayerMask = 0;
            //                //canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            //                canvasCamData.backgroundColorHDR = Color.clear;
            //                //canvasCamData.clearDepth = true;
            //                canvasCamData.probeLayerMask = 0;
            //                if (debug) Debug.Log("canvasCamData.name " + canvasCamData.name);
            //                //HDCamera HDCam = HDCamera.GetOrCreate(canvasCamera);
            //                //HDCam.frameSettings.
            //                //canvasCamData.volumeLayerMask = 1 << 1;

            //                //////HDRenderPipelineAsset HDRPAsset = (HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            //                //HDRPAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            //                //////RenderPipelineSettings HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            //                ////HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            //                //defaultHDRPSettings = HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            //                //////HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            //                ////defaultColorBufferFormat = HDRPSettings.colorBufferFormat;

            //                //required for precompile? Without this alpha in Render Textures is not working
            //                //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            //                //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);

            //                //Invoke("HDRPcolorBufferFormat1_Set", 5);
            //                //Invoke("HDRPcolorBufferFormat2_Set", 10);

            //                //if (!GUIOpened)
            //                //    Invoke("DefaultColorBufferFormat_Set", 5);

            //                //var pipelineAsset = Instantiate(GraphicsSettings.renderPipelineAsset) as HDRenderPipelineAsset;
            //                //pipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests = pipelineAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests == 128 ? 1 : 128;
            //                //GraphicsSettings.renderPipelineAsset = pipelineAsset;

            //                ////var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            //                //////if (debug) Debug.Log(typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline));
            //                ////RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
            //                ////RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
            //                //RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GraphicsSettings.currentRenderPipeline);
            //                //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            //                ////if (debug) Debug.Log(HDRPSettings.colorBufferFormat);
            //                ////typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //                //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //#endif
            //            }
            //            else
            //            {
            //            //    if (panelDepthMinMax.x < .5f)
            //            //        panelDepthMinMax.x = .5f;

            //            //    if (panelDepthMinMax.y < 100)
            //            //        panelDepthMinMax.y = 100;

            //            //    //panelDepthMinMax = new Vector2(1, 100);
            //            //    //panelDepth = Mathf.Clamp(panelDepth, panelDepthMinMax.x, panelDepthMinMax.y);
            //            //}
            //            //    canvasCamera.enabled = false;

            //            //void orthoCamSet(Camera camera)
            //            //{
            //            //    camera.orthographic = true;
            //            //    camera.nearClipPlane = -1;
            //            //    camera.farClipPlane = 1;
            //            //    //camera.depth = cam.depth - 1;
            //            //    camera.cullingMask = 0;
            //            //    camera.useOcclusionCulling = false;
            //            //    camera.GetUniversalAdditionalCameraData().renderShadows = false;
            //                Invoke("HDRPSettings_Restore", GUI_Set_delay);
            //            }

            //            CameraStackSet();

            //cullingMask = cam.cullingMask;

            //if (inputSystem)
//#if STARTER_ASSETS_PACKAGES_CHECKED
//#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            {
                //if (debug) Debug.Log(FindObjectOfType<StarterAssetsInputs>());
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
            //            //if (debug) Debug.Log("!EventSystem.current");
            //            //canvas.gameObject.AddComponent<EventSystem>();
            //            //canvas.gameObject.AddComponent<StandaloneInputModule>();

            //#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            //                new GameObject("UI_EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            //#else
            //                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            //#endif
            //        }
            //        //else
            //        //    //if (debug) Debug.Log("EventSystem.current " + EventSystem.current.name);

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        GUIAction.Enable();
        S3DAction.Enable();
        FOVAction.Enable();
        modifier1Action.Enable();
        modifier2Action.Enable();
        modifier3Action.Enable();

        //inputSystem_KeyListener = InputSystem.onAnyButtonPress.Call(AnyKeyPress);

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
                //if (debug) Debug.Log(arr.GetValue(i).ToString());
                Dropdown.OptionData optionData = new Dropdown.OptionData();
                optionData.text = arr.GetValue(i).ToString().Replace("_", " ");
                list.Add(optionData);
            }

            outputMethod_dropdown.options = list;

            //foreach(var v in arr)
            //{
            //   //if (debug) Debug.Log(v);
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

            trigger = slotName_dropdown.gameObject.AddComponent<EventTrigger>();
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.AddListener((eventData) => { SlotName_DropdownSelect(); });
            trigger.triggers.Add(entry);

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
            //Tooltip(vSync_toggle.gameObject, "Required for 'Over Under' S3D method");
            Tooltip(vSync_toggle.gameObject, "Required for 'Over Under'\nand Sequential S3D methods");
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

            //if (method != Method.Two_Displays)
            //    GUIAsOverlayState = GUIAsOverlay;

            if (loadSettingsFromFile)
                LoadLastSave(); //must be after panel setup
            else
                if (Time.time == 0)
                    lastPanelPosition = panel.GetComponent<RectTransform>().anchoredPosition;
                else
                    panel.GetComponent<RectTransform>().anchoredPosition = lastPanelPosition;

            //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
            //    cam.usePhysicalProperties = false;

            //physicalCamera = cam.usePhysicalProperties;
//#if HDRP
//            if (method == Method.Two_Displays) //must be after load variables
//            {
//                //cam.enabled = false;

//                //if (GUIAsOverlay)
//                    //GUIAsOverlayState = GUIAsOverlay;

//                GUIAsOverlay = false;
//                //camera_left.targetDisplay = 0;
//                //camera_right.targetDisplay = 1;
//            }
//            //else
//            //    GUIAsOverlayState = GUIAsOverlay;
//#endif

            CameraDataStruct_Set();

            if (GUIAsOverlay)
            {
                canvasCamera = new GameObject("canvasCamera").AddComponent<Camera>();
                canvasCamera_left = new GameObject("canvasCamera_left").AddComponent<Camera>();
                canvasCamera_right = new GameObject("canvasCamera_right").AddComponent<Camera>();
                canvasCamera.CopyFrom(canvasRayCam);
                canvasCamera.stereoTargetEye = StereoTargetEyeMask.None;
                canvasCamera.cullingMask = canvasCamera.cullingMask | (1 << 5); //add UI layer

                //if (additionalS3DCameras.Count != 0)
                if (additionalS3DTopmostCameraIndex != -1)
                    //canvasCamera.depth = additionalS3DCameras[additionalS3DCameras.Count - 1].depth + 1;
                    canvasCamera.depth = additionalS3DCameras[additionalS3DTopmostCameraIndex].depth + 1;
                else
                    canvasCamera.depth = cam.depth + 1;

                //canvasCamera_left.CopyFrom(canvasRayCam);
                //canvasCamera_right.CopyFrom(canvasRayCam);
                canvasCamera_left.CopyFrom(canvasCamera);
                canvasCamera_right.CopyFrom(canvasCamera);
                canvasCamera_left.stereoTargetEye = StereoTargetEyeMask.Left;
                canvasCamera_right.stereoTargetEye = StereoTargetEyeMask.Right;
                canvasCamera.transform.SetParent(canvas.transform);
                //canvasCamera_left.transform.SetParent(canvas.transform);
                //canvasCamera_right.transform.SetParent(canvas.transform);
                //canvasCamera_left.transform.parent = canvasCamera_right.transform.parent = canvas.transform;
                canvasCamera_left.transform.parent = canvasCamera_right.transform.parent = canvasCamera.transform;
                canvasCamera.tag = canvasCamera_left.tag = canvasCamera_right.tag = "MainCamera";

                ////canvasCamera.cullingMask = canvasCamera.cullingMask | (1 << 5); //add UI layer
                ////canvasCamera_left.cullingMask = canvasCamera_left.cullingMask | (1 << 5); //add UI layer
                ////canvasCamera_right.cullingMask = canvasCamera_right.cullingMask | (1 << 5); //add UI layer
                ////canvasCamera_left.cullingMask = canvasCamera_right.cullingMask = canvasCamera_left.cullingMask | (1 << 5); //add UI layer
                //canvasCamera.cullingMask = canvasCamera_left.cullingMask = canvasCamera_right.cullingMask = canvasCamera.cullingMask | (1 << 5); //add UI layer
                //#if URP
                //                //canvasCamera.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                //                //canvasCamera_left.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                //                //canvasCamera_right.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                //                canvasCamera_left.GetUniversalAdditionalCameraData().renderShadows = canvasCamera_right.GetUniversalAdditionalCameraData().renderShadows = canvasCamera.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                //                //canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = CameraClearFlags.Nothing;
                //                //canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = canvasCamera.clearFlags = CameraClearFlags.Nothing;
                //                canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = canvasCamera.clearFlags = CameraClearFlags.Depth;
                //                canvasCamera_left.backgroundColor = canvasCamera_right.backgroundColor = Color.clear;
                //#else
                //                ////canvasCamera.backgroundColor = Color.clear;
                //                //canvasCamera_left.backgroundColor = Color.clear;
                //                //canvasCamera_right.backgroundColor = Color.clear;
                //                canvasCamera_left.backgroundColor = canvasCamera_right.backgroundColor = Color.clear;
                //#endif

                //#if HDRP
                //                canvasCamData = canvasCamera.gameObject.AddComponent<HDAdditionalCameraData>();
                //                //canvasCamData.volumeLayerMask = 0;
                //                //canvasCamData.backgroundColorHDR = Color.clear;
                //                //canvasCamData.probeLayerMask = 0;
                //                canvasCamData_left = canvasCamera_left.gameObject.AddComponent<HDAdditionalCameraData>();
                //                canvasCamData_right = canvasCamera_right.gameObject.AddComponent<HDAdditionalCameraData>();
                //                //canvasCamData_left.volumeLayerMask = canvasCamData_right.volumeLayerMask = 0;
                //                //canvasCamData_left.backgroundColorHDR = canvasCamData_right.backgroundColorHDR = Color.clear;
                //                //canvasCamData_left.probeLayerMask = canvasCamData_right.probeLayerMask = 0;
                //                canvasCamData.volumeLayerMask = canvasCamData_left.volumeLayerMask = canvasCamData_right.volumeLayerMask = 0;
                //                canvasCamData.backgroundColorHDR = canvasCamData_left.backgroundColorHDR = canvasCamData_right.backgroundColorHDR = Color.clear;
                //                canvasCamData.probeLayerMask = canvasCamData_left.probeLayerMask = canvasCamData_right.probeLayerMask = 0;
                //                //if (debug) Debug.Log("canvasCamData.name " + canvasCamData.name);
                //                //canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
                //                //canvasCamData_left.clearColorMode = canvasCamData_right.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                //                canvasCamData.clearColorMode = canvasCamData_left.clearColorMode = canvasCamData_right.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                ////#endif
                //#elif !URP
                //                //canvasCamera.clearFlags = CameraClearFlags.Depth;
                //                //canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = CameraClearFlags.Color;
                //                canvasCamera.clearFlags = canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = CameraClearFlags.Depth;
                //#endif
#if HDRP
                canvasCamData = canvasCamera.gameObject.AddComponent<HDAdditionalCameraData>();
                canvasCamData_left = canvasCamera_left.gameObject.AddComponent<HDAdditionalCameraData>();
                canvasCamData_right = canvasCamera_right.gameObject.AddComponent<HDAdditionalCameraData>();
                canvasCamData.volumeLayerMask = canvasCamData_left.volumeLayerMask = canvasCamData_right.volumeLayerMask = 0;
                canvasCamData.probeLayerMask = canvasCamData_left.probeLayerMask = canvasCamData_right.probeLayerMask = 0;
                canvasCamData.backgroundColorHDR = canvasCamData_left.backgroundColorHDR = canvasCamData_right.backgroundColorHDR = Color.clear;
                canvasCamData.clearColorMode = canvasCamData_left.clearColorMode = canvasCamData_right.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
//#else
//#if URP
#elif URP
                canvasCamera_left.GetUniversalAdditionalCameraData().renderShadows = canvasCamera_right.GetUniversalAdditionalCameraData().renderShadows = canvasCamera.GetUniversalAdditionalCameraData().renderShadows = false; //not copied
                canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                //canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = canvasCamera.clearFlags = CameraClearFlags.Nothing;
//#endif
#else
                canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = canvasCamera.clearFlags = CameraClearFlags.Depth;
                //canvasCamera_left.backgroundColor = canvasCamera_right.backgroundColor = canvasCamera.backgroundColor = Color.clear;
#endif
                //if (additionalS3DCameras.Count != 0)
                //    canvasCamera.depth = additionalS3DCameras[additionalS3DCameras.Count - 1].depth + 1;
                //else
                //    canvasCamera.depth = cam.depth + 1;

                //if (method == Method.Two_Displays)
                //    canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            }
            else
                Invoke("HDRPSettings_Restore", GUI_Set_delay);

#if URP
            CameraStackSet();
#endif
            //if (!loaded && loadSettingsFromFile)
            //{
            //    if (debug) Debug.Log("!loaded && loadSettingsFromFile");
            //    loaded = true;
            //    LoadLastSave();
            //}

            swapLR_toggle.isOn = swapLR;
            optimize_toggle.isOn = optimize;
            vSync_toggle.isOn = vSync;
            outputMethod_dropdown.value = (int)method;
            matchUserIPD_toggle.isOn = matchUserIPD;
            slotName_inputField.text = slotName;
            //defaultRTFormat = lastRTFormat = RTFormat;

            //GUI_Set();
            //Invoke("GUI_Set", Time.deltaTime * 32);
            //aspect = cam.aspect;
            //FOV_Set();
            //Resize();
            //CamRect_Set();
            //PanelDepthMinMaxSet();
            VSyncSet();
            PPISet();
            UserIPDSet();
            VirtualIPDSet();
            Aspect_Set();
            //CamRect_Set();
            CamSet();
            //VSyncSet();
            //PPISet();
            //UserIPDSet();
            //VirtualIPDSet();
            //PanelDepthMinMaxSet();
            //PanelDepthSet();
            //PanelOverlayDepthMinMaxSet();
            //Panel3DdepthMinMaxSet();
            //CamSet();
            //Render_Set();
            PanelDepthMinMaxSet();
            //GUI_Set();
            Invoke("GUI_Set", GUI_Set_delay);

            //lastGUIOpened = GUIOpened;
            lastGUIOpened = GUIVisible = GUIOpened;
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
            //lastSetMatrixDirectly = setMatrixDirectly;
            //lastInterlaceType = interlaceType;
            //lastAnaglyphLeftColor = anaglyphLeftColor;
            //lastAnaglyphRightColor = anaglyphRightColor;
            lastSlotName = slotName;
            lastCamRect = cam.rect;
            lastDisableCullingMask = disableCullingMask;
            lastNearClipHack = nearClipHack;
            lastMatrixKillHack = matrixKillHack;
            lastCanvasLocalPosZ = canvasLocalPosZ;
            lastCloneCamera = cloneCamera;
            //lastCameraDataStruct = cameraDataStruct;
            //setLastCameraDataStructTime = Time.deltaTime * 32;
            //float setLastCameraDataStructTime = Time.deltaTime * 32;
            //float setLastCameraDataStructTime = 1f / Screen.currentResolution.refreshRate * 32;
            //float setLastCameraDataStructTime = .021f;
            //if (debug) Debug.Log("OnEnable setLastCameraDataStructTime " + setLastCameraDataStructTime);
            //Invoke("SetLastCameraDataStruct", setLastCameraDataStructTime);
            //if (debug) Debug.Log("OnEnable before Invoke('SetLastCameraDataStruct'', setLastCameraDataStructTime) " + setLastCameraDataStructTime);
            //Invoke("SetLastCameraDataStruct", setLastCameraDataStructTime);
            Invoke("SetLastCameraDataStruct", .021f);

            //for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
            //        additionalS3DCamerasStruct[i].lastCameraDataStruct = additionalS3DCamerasStruct[i].cameraDataStruct;

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
            Cursor.visible = !(lastHide2DCursor = hide2DCursor);

#if LookWithMouse
        bool lookWithMouseScriptEnabled = false;

        //foreach (var script in FindObjectsByType<LookWithMouse>(FindObjectsSortMode.None))
        foreach (var script in FindObjectsOfType<LookWithMouse>())
        {
            if (script.enabled)
                if (!lookWithMouseScriptEnabled)
                {
                    lookWithMouseScriptEnabled = true;
                    lookWithMouseScript = script;
                    //if (debug) Debug.Log(lookWithMouseScript);
                    //break;
                }
                else
                {
                    if (debug) Debug.Log("More than one lookWithMouseScript enabled in the scene");
                    lookWithMouseScript = null;
                }
        }
#endif

//#if SimpleCameraController
//            bool simpleCameraControllerScriptEnabled = false;

//            //foreach (var script in FindObjectsByType<UnityTemplateProjects.SimpleCameraController>(FindObjectsSortMode.None))
//            foreach (var script in FindObjectsOfType<UnityTemplateProjects.SimpleCameraController>())
//            {
//                if (debug) Debug.Log("FindObjectsOfType<UnityTemplateProjects.SimpleCameraController>()");

//                if (script.enabled)
//                {
//                    //if (lookWithMouseScriptEnabled)
//                    //    if (debug) Debug.Log("More than one mouse control script enabled in the scene");

//                    if (!simpleCameraControllerScriptEnabled)
//                    {
//                        simpleCameraControllerScriptEnabled = true;
//                        simpleCameraControllerScript = script;
//                        //if (debug) Debug.Log(simpleCameraControllerScript);
//                        //break;
//                    }
//                    else
//                    {
//                        if (debug) Debug.Log("More than one simpleCameraControllerScript enabled in the scene");
//                        simpleCameraControllerScript = null;
//                    }
//                }
//            }
//#endif
        }
    }

    //void ClosestCameraIndex_Set()
    //{
    //    for (int i = additionalS3DCameras.Count - 1; i >= 0; i--)
    //    {
    //        if (additionalS3DCameras[i] != null)
    //        {
    //            //if (debug) Debug.Log("i " + i);
    //            additionalS3DTopmostCameraIndex = i;
    //            if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);
    //            break;
    //        }
    //    }
    //}

    void SceneNearClip_Set()
    {
        //if (debug) Debug.Log("cineMachineEnabled " + cineMachineEnabled);
        //if (debug) Debug.Log("SceneNearClip_Set ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        cameraNearClip = cam.nearClipPlane;
        //if (debug) Debug.Log("SceneNearClip_Set cameraNearClip " + cameraNearClip);

#if CINEMACHINE
        if (cineMachineEnabled)
            if (vCam != null)
            {
                cameraNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
                cam.nearClipPlane = cameraNearClip;

                //if (cameraDataStructIsReady)
                //    CameraDataStruct_Change();
            }
            //else
            //    Invoke("SceneNearClip_Set", Time.deltaTime);
#endif
        //nearClip = sceneNearClip = cam.nearClipPlane;
        //cameraNearClip = cam.nearClipPlane;

        //if (additionalS3DCameras.Count != 0)
        if (additionalS3DTopmostCameraIndex != -1)
            //nearClip = sceneNearClip = additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane;
            sceneNearClip = additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane;
        else
            sceneNearClip = cameraNearClip;

        if (debug) Debug.Log("SceneNearClip_Set sceneNearClip " + sceneNearClip);
    }

    void SceneFarClip_Set()
    {
        //if (debug) Debug.Log("cineMachineEnabled " + cineMachineEnabled);
        //if (debug) Debug.Log("SceneFarClip_Set ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane);
        sceneFarClip = cam.farClipPlane;

#if CINEMACHINE
            if (cineMachineEnabled)
                if (vCam != null)
                {
                    sceneFarClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane;
                    cam.farClipPlane = sceneFarClip;
                }
                //else
                //    Invoke("SceneFarClip_Set", Time.deltaTime);
#endif

        if (debug) Debug.Log("SceneFarClip_Set sceneFarClip " + sceneFarClip);
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
    //    if (debug) Debug.Log("CamData_Get");
    //    camData = cam.GetComponent<HDAdditionalCameraData>();

    //    if (!camData)
    //        Invoke("CamData_Get", Time.deltaTime);
    //}

    //void HDCamDataCopy_Get()
    //{
    //    if (debug) Debug.Log("HDCamDataCopy_Get");
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
    //    if (debug) Debug.Log("DefaultColorBufferFormat_Set");
    //    HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
    //    typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
    //}

    //UnityEngine.Object selectedObject;

    //    void CanvasCamHDRPData_Set()
    //    {
    //        //if (debug) Debug.Log("CanvasCamHDRPData_Set()");

    //#if UNITY_EDITOR
    //        if (!selectedObject)
    //            selectedObject = UnityEditor.Selection.activeObject;

    //        UnityEditor.Selection.activeObject = canvasCamera;
    //#endif

    //        if (canvasCamera.GetComponent<HDAdditionalCameraData>())
    //        {
    //            //UnityEditor.Selection.activeObject = canvasCamera;
    //            canvasCamData = canvasCamera.GetComponent<HDAdditionalCameraData>();
    //            canvasCamData.volumeLayerMask = 0;
    //            canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
    //            //canvasCamData.clearDepth = true;
    //            canvasCamData.probeLayerMask = 0;
    //            //if (debug) Debug.Log("canvasCamData.name " + canvasCamData.name);
    //#if UNITY_EDITOR
    //            //selectedObject = UnityEditor.Selection.activeGameObject;
    //            if (debug) Debug.Log("selectedObject " + selectedObject.name);
    //            UnityEditor.Selection.activeObject = selectedObject;
    //#endif
    //        }
    //        else
    //            Invoke("CanvasCamHDRPData_Set", Time.deltaTime);
    //    }

#if URP
    void CameraStackSet()
    {
//#if URP
        //Debug.Break();

        if (S3DEnabled)
        {
            //if (GUIAsOverlay)
            //{
            //    //if (!leftCameraStack.Contains(canvasCamera))
            //    //{
            //    //    leftCameraStack.Add(canvasCamera);
            //    //    rightCameraStack.Add(canvasCamera);
            //    //}

            //    ////leftCameraStack.Remove(canvasCamera);
            //    ////rightCameraStack.Remove(canvasCamera);
            //    ////leftCameraStack.Add(canvasCamera);
            //    ////rightCameraStack.Add(canvasCamera);
            //    //canvasCamera.targetTexture = renderTexture_left;

            //    cameraStack.Remove(canvasCamera);
            //    canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
            //}

            //additionalS3DCamerasStruct = new AdditionalS3DCamera[additionalS3DCameras.Count];
            //int i = 0;
            //if (cameraStack.Count != 0)
            foreach (var c in cameraStack)
            //for (int i = 0; i < cameraStack.Count; i++)
            {
                if (c != canvasCamera)
                    if (additionalS3DCameras.Contains(c))
                    {
                        if (debug) Debug.Log("additionalS3DCameras.Contains(c) " + c);
                        //Camera cloneLeft = Instantiate(c, c.transform.position, c.transform.rotation);
                        //cloneLeft.tag = "Untagged";
                        //cloneLeft.name += "_left";
                        //cloneLeft.stereoTargetEye = StereoTargetEyeMask.Left;
                        ////cloneLeft.targetTexture = renderTexture_left;
                        //Camera cloneRight = Instantiate(c, c.transform.position, c.transform.rotation);
                        //cloneRight.tag = "Untagged";
                        //cloneRight.name += "_right";
                        //cloneRight.stereoTargetEye = StereoTargetEyeMask.Right;
                        ////cloneRight.targetTexture = renderTexture_right;

                        //cloneLeft.transform.parent = cloneRight.transform.parent = c.transform;
                        ////c.enabled = false;

                        int index = additionalS3DCameras.IndexOf(c);
                        leftCameraStack.Add(additionalS3DCamerasStruct[index].camera_left);
                        rightCameraStack.Add(additionalS3DCamerasStruct[index].camera_right);
                        //additionalS3DCamerasStruct[i].camera = c;
                        //additionalS3DCamerasStruct[i].camera_left = cloneLeft;
                        //additionalS3DCamerasStruct[i].camera_right = cloneRight;
                        //i++;
                    }
                    else
                    {
                        leftCameraStack.Add(c);
                        rightCameraStack.Add(c);
                    }
            }

            //if (GUIAsOverlay)
            if (GUIAsOverlay && GUIVisible)
            {
                if (!leftCameraStack.Contains(canvasCamera_left))
                {
                    leftCameraStack.Add(canvasCamera_left);
                    rightCameraStack.Add(canvasCamera_right);
                }
            }
            else
            {
                leftCameraStack.Remove(canvasCamera_left);
                rightCameraStack.Remove(canvasCamera_right);
            }

            //for (int i = 0; i < cameraStack.Count; i++)
            //{
            //    Camera c = cameraStack[i];
            //    //cameraStack.RemoveAt(i);
            //    //c.targetTexture = renderTexture_left;
            //    leftCameraStack.Add(c);
            //    rightCameraStack.Add(c);
            //    //cameraStack.Remove(c);
            //}

            //Invoke("CameraStackRemove", Time.deltaTime * 8);
            cameraStack.RemoveAll(t => t);

            //if (method == Method.Two_Displays)
            //{
            //    leftCameraStack.Add(canvasCamera_left);
            //    rightCameraStack.Add(canvasCamera_right);
            //}
        }
        else
        {
            //if (GUIAsOverlay)
            //{
            //    canvasCamera.ResetProjectionMatrix();
            //    canvasCamera.targetTexture = null;
            //    canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            //    //if (!cameraStack.Contains(canvasCamera))
            //    cameraStack.Add(canvasCamera);

            //    //cameraStack.Remove(canvasCamera);
            //    //cameraStack.Add(canvasCamera);
            //    //canvasCamera.targetTexture = null;

            //    //leftCameraStack.Remove(canvasCamera);
            //    //rightCameraStack.Remove(canvasCamera);
            //}

            //foreach (var c in leftCameraStack)
            //{
            //    //leftCameraStack.Remove(c);
            //    //rightCameraStack.Remove(c);

            //    //foreach (var d in additionalS3DCameras)
            //    //if (debug) Debug.Log("CameraStackSet c.name.Replace("_left", "")" + c.name.Replace("_left", ""));

            //    //string cameraName = c.name.Replace("(Clone)_left", "");
            //    //if (debug) Debug.Log("CameraStackSet cameraName " + cameraName);
            //    if (debug) Debug.Log("******************CameraStackSet var c in leftCameraStack " + c);

            //    //Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == cameraName);
            //    Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == c.name.Replace("(Clone)_left", ""));
            //    //Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name.Contains(cameraName));

            //    //if (debug) Debug.Log("CameraStackSet additionalS3DCamera " + additionalS3DCamera);

            //    //if (additionalS3DCameras.Find(t => t.name.Contains(c.name.Replace("_left", ""))))
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
            //leftCameraStack.RemoveAll(t => t);
            //rightCameraStack.RemoveAll(t => t);

            //if (GUIAsOverlay)
            //{
            //    //canvasCamera.ResetProjectionMatrix();
            //    //canvasCamera.targetTexture = null;
            //    //canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

            //if (GUIAsOverlay && !cameraStack.Contains(canvasCamera))
            if (GUIAsOverlay && GUIVisible)
            {
                if (!cameraStack.Contains(canvasCamera))
                    cameraStack.Add(canvasCamera);
            }
            else
                cameraStack.Remove(canvasCamera);

            //    //cameraStack.Remove(canvasCamera);
            //    //cameraStack.Add(canvasCamera);
            //    //canvasCamera.targetTexture = null;

            //    //leftCameraStack.Remove(canvasCamera);
            //    //rightCameraStack.Remove(canvasCamera);
            //}
        }

        lastCameraStack = cameraStack.ToArray();
        //if (debug) Debug.Log("CameraStackSet cameraStack.Count " + cameraStack.Count);
        if (debug) Debug.Log("CameraStackSet lastCameraStack.Length " + lastCameraStack.Length + " cameraStack.Count " + cameraStack.Count);
//#endif
    }

    void CameraStackRestore()
    {
//#if URP
        foreach (var c in leftCameraStack)
        {
            ////Camera additionalS3DCamera = additionalS3DCameras.Find(t => t.name == c.name.Replace("(Clone)_left", ""));
            //Camera additionalS3DCamera = null;
            //string cameraName = c.name.Replace("(Clone)_left", "");

            //foreach (var s in additionalS3DCamerasStruct)
            //    if (s.camera.name == cameraName)
            //        additionalS3DCamera = s.camera;

            //if (additionalS3DCamera)
            //    cameraStack.Add(additionalS3DCamera);
            //else
            //    cameraStack.Add(c);

            if (c.name.Contains("_left"))
                cameraStack.Add(c.transform.parent.GetComponent<Camera>());
            else
                cameraStack.Add(c);
        }

        leftCameraStack.RemoveAll(t => t);
        rightCameraStack.RemoveAll(t => t);
        if (debug) Debug.Log("CameraStackRestore cameraStack.Count " + cameraStack.Count);
//#endif
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
    //                if (debug) Debug.Log(assembly.FullName);

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

    //void URPAsset_Get()
    void URPAssetSettings_Get()
    {
        //URPAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        ////lastURPAsset = new UniversalRenderPipelineAsset();
        //lastURPAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        //lastURPAsset = URPAsset;
        lastScriptableRenderer = URPAsset.scriptableRenderer;
        //lastMSAASampleCount = URPAsset.msaaSampleCount;
        URPAssetIsReady = true;
    }
#endif

    //void Start()
    //{
    //    if (debug) Debug.Log("Start " + name);
    //}

    float vFOV;
    bool autoshow;
    float S3DKeyTimer;
    bool S3DKeyLoaded;
    bool camFOVChangedExternal;
    bool GUIVisible;
    bool S3DActionPressed;
    //bool FOVSetInProcess;
    //float canvasEdgeOffset;
    bool panelDepth_sliderIsDragging;
    //int time;
    //bool oddFrame;
    //float canvasHalfSizeY;
    //float newPanelDepth;
    //float setLastCameraDataStructTime;
    Vector2 cursorLocalPos;

	// Native plugin rendering events are only called if a plugin is used
	// by some script. This means we have to DllImport at least
	// one function in some active script.
	// For this example, we'll call into plugin's SetTimeFromUnity
	// function and pass the current time so the plugin can animate.

#if !UNITY_EDITOR
//#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
//	[DllImport ("__Internal")]
//#else
//	[DllImport ("RenderingPlugin")]
//#endif
//	private static extern void SetTimeFromUnity(float t);


	// We'll also pass native pointer to a texture in Unity.
	// The plugin will fill texture data from native code.
#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
	//private static extern void SetTextureFromUnity(System.IntPtr texture, int w, int h);
	private static extern void SetTextureFromUnity(System.IntPtr textureLeft, System.IntPtr textureRight, int w, int h);

	// We'll pass native pointer to the mesh vertex buffer.
	// Also passing source unmodified mesh data.
	// The plugin will fill vertex data from native code.
#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
	private static extern void SetMeshBuffersFromUnity (IntPtr vertexBuffer, int vertexCount, IntPtr sourceVertices, IntPtr sourceNormals, IntPtr sourceUVs);

#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport("RenderingPlugin")]
#endif
	private static extern IntPtr GetRenderEventFunc();

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport ("__Internal")]
	private static extern void RegisterPlugin();
#endif
#endif

    //void Start()
    IEnumerator Start()
    {
        if (debug) Debug.Log("Start");
        lastLoadSettingsFromFile = loadSettingsFromFile = false; //prevent reload settings by OnOffToggle

        //if (loadSettingsFromFile)
        //    LoadLastSave();

        ////Invoke("AdditionalS3DCamerasListModify", 5);
        ////camData = cam.GetUniversalAdditionalCameraData();
        ////CameraDataStruct_Set();
        ////lastCameraDataStruct = cameraDataStruct;
        ////if (debug) Debug.Log("Time.deltaTime " + Time.deltaTime);
        ////Invoke("SetLastCameraDataStruct", Time.deltaTime * 8);
        //setLastCameraDataStructTime = Time.deltaTime * 32;
        //Invoke("SetLastCameraDataStruct", setLastCameraDataStructTime);
        ////if (debug) Debug.Log("Start cameraDataStruct " + cameraDataStruct);
        ////Invoke("CamData_Change", 5);

        //if (method == Method.Two_Displays) //prevent disabled post process with Method.Two_Displays at start
        //    OnOffToggle();

#if UNITY_WEBGL && !UNITY_EDITOR
		RegisterPlugin();
#endif
        //Camera.onPostRender += CamEndRenderCallback;
		//CreateTextureAndPassToPlugin();
		//SendMeshBuffersToPlugin();
		yield return StartCoroutine("CallPluginAtEndOfFrames");
    }

	private IEnumerator CallPluginAtEndOfFrames()
	{
		while (true) {
			// Wait until all frame rendering is done
			yield return new WaitForEndOfFrame();

            //Camera.SetupCurrent(camera_left);
            //camera_left.targetTexture = null;
            ////RenderTexture.active = null;
            ////RenderTextureFlipMaterial.SetTexture("_MainTex", renderTexture_left);
            ////RenderTextureFlipMaterial.SetPass(0);
            ////Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
            ////Graphics.Blit(renderTexture_left, null as RenderTexture);
            //Graphics.Blit(RenderTexture.active, null as RenderTexture);
            ////camera_left.targetDisplay = 0;
            ////if (debug) Debug.Log("WaitForEndOfFrame Camera.current: " + Camera.current);

            //Camera.SetupCurrent(camera_right);
            //camera_right.targetTexture = null;
            ////RenderTexture.active = null;
            ////RenderTexture.active = null;
            ////RenderTextureFlipMaterial2.SetTexture("_MainTex", renderTexture_right);
            ////RenderTextureFlipMaterial2.SetTextureScale("_MainTex", new Vector2(-1, 1)); //not working
            ////RenderTextureFlipMaterial2.SetPass(0);
            ////Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
            ////Graphics.Blit(renderTexture_right, null, RenderTextureFlipMaterial);
            //Graphics.Blit(RenderTexture.active, null, RenderTextureFlipMaterial);
            ////camera_right.targetDisplay = 1;
            ////if (debug) Debug.Log("WaitForEndOfFrame Camera.current: " + Camera.current);

            ////Camera.SetupCurrent(cam);
            ////Camera.SetupCurrent(camera_right);

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
#if !UNITY_EDITOR
            if (method == Method.D3D11)
            {
			    //SetTimeFromUnity (Time.timeSinceLevelLoad);
			    GL.IssuePluginEvent(GetRenderEventFunc(), 1);
            }
#endif
        }
    }

    void SetLastCameraDataStruct()
    {
        //if (debug) Debug.Log("SetLastCameraDataStruct " + Time.time);
        lastCameraDataStruct = cameraDataStruct;
        //if (debug) Debug.Log("SetLastCameraDataStruct after lastCameraDataStruct = cameraDataStruct " + Time.time);

        for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
            additionalS3DCamerasStruct[i].lastCameraDataStruct = additionalS3DCamerasStruct[i].cameraDataStruct;

        //if (debug) Debug.Log("SetLastCameraDataStruct after additionalS3DCamerasStruct[i].lastCameraDataStruct = additionalS3DCamerasStruct[i].cameraDataStruct " + Time.time);
        cameraDataStructIsReady = true;
        //CameraDataStruct_Check();
    }

    //void CameraDataStruct_Check()
    //{
    //    if (debug) Debug.Log("CameraDataStruct_Check");
    //    bool notEqual = false;

    //    if (!lastCameraDataStruct.Equals(cameraDataStruct))
    //    {
    //        //Debug.Break();
    //        if (debug) Debug.Log("CameraDataStruct_Check !lastCameraDataStruct.Equals(cameraDataStruct)--------------------------------------------------------------");

    //        //lastCameraDataStruct = cameraDataStruct;
    //        //Invoke("CameraDataStruct_Check", Time.deltaTime);
    //        notEqual = true;
    //    }
    //    else
    //        for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
    //        {
    //            if (!additionalS3DCamerasStruct[i].lastCameraDataStruct.Equals(additionalS3DCamerasStruct[i].cameraDataStruct))
    //            {
    //                if (debug) Debug.Log("CameraDataStruct_Check !additionalS3DCamerasStruct[i].lastCameraDataStruct.Equals(additionalS3DCamerasStruct[i].cameraDataStruct)---------------------------------------------------------");
    //                //additionalS3DCamerasStruct[i].lastCameraDataStruct = additionalS3DCamerasStruct[i].cameraDataStruct;
    //                //OnOffToggle();
    //                notEqual = true;
    //            }
    //        }

    //    if (notEqual)
    //        Invoke("CameraDataStruct_Check", Time.deltaTime);
    //    else
    //        cameraDataStructIsReady = true;
    //}

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
    bool done;
    //float lastCamNearClip;
    //float camNearClip;
    bool onOffToggle;
    //float lastVCamNearClip;
    //bool vCamSelected;
    //float vCamSelectedTimer;
    bool oddFrame;
    //RenderTexture nullRT;

    //void VCamUnselect()
    //{
    //    if (vCam != null && UnityEditor.Selection.activeObject != vCam.VirtualCameraGameObject)
    //        vCamSelected = false;
    //}

    void Update()
    {
        //if (debug) Debug.Log("lastAdditionalS3DTopmostCamera " + lastAdditionalS3DTopmostCamera);
        //if (debug) Debug.Log("additionalS3DCameras.Count " + additionalS3DCameras.Count + " additionalS3DCamerasStruct.Length " + additionalS3DCamerasStruct.Length);
        //if (debug) Debug.Log("cam.nearClipPlane " + cam.nearClipPlane);
        //if (debug) Debug.Log("cam.projectionMatrix " + cam.projectionMatrix);
        //if (debug) Debug.Log(Cinemachine.CinemachineBrain.SoloCamera);
        oddFrame = !oddFrame;

        if (S3DEnabled && method == Method.Sequential)
        {
            //oddFrame = !oddFrame;
            S3DMaterial.SetInt("_OddFrame", oddFrame ? 1 : 0);

//#if !HDRP
#if !URP // GUI panel & additional cameras disappear in URP
            if (optimize)
            {
                //if (Time.time < 10)
                if (oddFrame)
                {
                    camera_left.Render();
                    camera_right.Render();

//#if !URP
                    foreach (var c in additionalS3DCamerasStruct)
                        if (c.camera)
                        {
                            c.camera_left.Render();
                            c.camera_right.Render();
                        }
//#endif

                    if (GUIAsOverlay && GUIVisible)
                    {
                        //if (debug) Debug.Log("canvasCamera_left && canvasCamera_left.isActiveAndEnabled");
                        canvasCamera_left.Render();
                        canvasCamera_right.Render();
                    }
//#endif
                }
            }
#endif
            //else
            //{
            //    if (oddFrame)
            //    {
            //        camera_left.targetTexture = renderTexture_left;
            //        camera_right.targetTexture = renderTexture_right;
            //    }
            //    else
            //    {
            //        camera_left.targetTexture = nullRT;
            //        camera_right.targetTexture = nullRT;
            //    }
            //}
            //#endif
        }

        //if (cam.nearClipPlane == -1 && vCam != null && UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject)
        //    //if (debug) Debug.Log("UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject");
        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;

        //if (vCam != null && UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject)
        //{
        //    //if (lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane)
        //    //{
        //    //    if (debug) Debug.Log("lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        //    //    lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
        //    //    if (debug) Debug.Log("lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        //    //}
        //    //else
        //    //{
        //    //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
        //    //    cam.nearClipPlane = -1;
        //    //}

        //    vCamSelected = true;
        //    vCamSelectedTimer = 1;
        //}
        //else
        //{
        //    //vCamSelected = false;
        //    //Invoke("VCamUnselect", 1);
        //    vCamSelectedTimer -= Time.deltaTime;

        //    if (vCamSelectedTimer <= 0)
        //        vCamSelected = false;
        //}

        //if (vCam != null && lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane)
        //{
        //    if (((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane > 0)
        //    {
        //        SceneNearClip_Set();
        //        //Clip_Set();
        //        onOffToggle = true;
        //    }

        //    if (debug) Debug.Log("lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        //    lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
        //}

        //if (debug) Debug.Log("Update " + Time.time);
        //if (debug) Debug.Log("aspect " + aspect);
        //if (debug) Debug.Log("cam.aspect " + cam.aspect + " camera_left.aspect " + camera_left.aspect + " camera_right.aspect " + camera_right.aspect);

        //foreach (var c in additionalS3DCamerasStruct)
        //    if (debug) Debug.Log("c.camera.aspect " + c.camera.aspect + " c.camera_left.aspect " + c.camera_left.aspect + " c.camera_right.aspect " + c.camera_right.aspect);

        //rtWidth = cam.pixelWidth;
        //rtHeight = cam.pixelHeight;
        //renderTexture_left.Release();
        //renderTexture_right.Release();
        //renderTexture_left = RT_Make();
        //renderTexture_right = RT_Make();
        //camera_left.targetTexture = camera_right.targetTexture = canvasCamera_left.targetTexture = canvasCamera_right.targetTexture = cam.targetTexture;

        //if (method == Method.Two_Displays)
        //    camera_left.rect = camera_right.rect = canvasCamera_left.rect = canvasCamera_right.rect = cam.rect;

        //if (debug) Debug.Log(cullingMask);
        //if (debug) Debug.Log(additionalS3DCamerasStruct[0].camera_left.targetTexture);

        //if (canvasCamera_left && canvasCamera_left.isActiveAndEnabled)
        //{
        //    canvasCamera_left.targetTexture = renderTexture_left;
        //    canvasCamera_right.targetTexture = renderTexture_right;
        //}

        CameraDataStruct_Set();
//#if UNITY_EDITOR
//        //if (S3DEnabled && method == Method.Two_Displays)
//        if (S3DEnabled)
//        {
//            if (method == Method.Two_Displays)
//            {
//                camera_left.targetTexture = renderTexture_left;
//                camera_right.targetTexture = renderTexture_right;
//            }
//        }
//        else
//            if (additionalS3DCameras.Count != 0 || canvasCamera && canvasCamera.isActiveAndEnabled)
//                cam.targetTexture = renderTexture;
//#endif

        //if (!done && Time.time > 5) //executed once
        //{
        //    done = true;

        //    //cam.cullingMask = 0;
        //    //if (debug) Debug.Log("cam.cullingMask " + cam.cullingMask);

        //    Matrix4x4 m = cam.projectionMatrix;
        //    m[0, 2] = .1f;
        //    m[1, 2] = .1f;
        //    cam.projectionMatrix = m;

        //    foreach (var c in additionalS3DCamerasStruct)
        //        if (c.camera)
        //        {
        //            Matrix4x4 m2 = c.camera.projectionMatrix;
        //            m2[0, 2] = m[0, 2];
        //            m2[1, 2] = m[1, 2];
        //            c.camera.projectionMatrix = m2;
        //        }

        //    //cam.fieldOfView = 88;

        //    //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = .001f;
        //    //cam.nearClipPlane = .001f;
        //}

#if UNITY_EDITOR && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        //if (debug) Debug.Log(GUIAction.bindings[0].path);
        //if (debug) Debug.Log(GUIAction.bindings.Count);

        //if (!done && GUIAction.bindings[0].path != "<Keyboard>/tab")
        //    if (!done && Time.time > 5)
        //    {
        //        if (debug) Debug.Log("GUIAction.bindings[0].path != ' < Keyboard >/ tab'");
        //        if (debug) Debug.Log("!done && Time.time > 5");
        //        done = true;
        //        GUIAction.Disable();
        //        GUIAction.Reset();
        //        GUIAction.AddBinding("<Keyboard>/backspace");
        //        GUIAction.ChangeBinding(0).WithPath("<Keyboard>/backspace");
        //        if (debug) Debug.Log("GUIAction.bindings[0].path " + GUIAction.bindings[0].path);
        //        if (debug) Debug.Log("GUIAction.bindings " + GUIAction.bindings[1]);
        //        GUIAction.Enable();
        //        if (debug) Debug.Log(GUIAction.actionMap.actions);
        //    }

        if (GuiActionPath != GUIAction.bindings[0].path)
        //if (!GUIActionBindings.Equals(GUIAction.bindings))
        //if (!Equals(GUIActionBindings, GUIAction.bindings))
        //if (GUIActionBindings.Count != GUIAction.bindings.Count)
        {
            if (debug) Debug.Log("GuiActionPath != GUIAction.bindings[0].path");
            //if (debug) Debug.Log("!GUIActionBindings.Equals(GUIAction.bindings)");
            GuiActionPath = GUIAction.bindings[0].path;
            GUIAction.ChangeBinding(0).WithPath(GUIAction.bindings[0].path);
            //GUIActionBindings = GUIAction.bindings;
        }

        if (S3DActionPath != S3DAction.bindings[0].path)
        {
            S3DActionPath = S3DAction.bindings[0].path;
            S3DAction.ChangeBinding(0).WithPath(S3DAction.bindings[0].path);
        }

        if (FOVActionPath1 != FOVAction.bindings[1].path)
        {
            //if (debug) Debug.Log("FOVActionPath1 != FOVAction.bindings[1].path");
            FOVActionPath1 = FOVAction.bindings[1].path;
            FOVAction.ChangeBinding(1).WithPath(FOVAction.bindings[1].path);
        }

        if (FOVActionPath2 != FOVAction.bindings[2].path)
        {
            //if (debug) Debug.Log("FOVActionPath2 != FOVAction.bindings[2].path");
            FOVActionPath2 = FOVAction.bindings[2].path;
            FOVAction.ChangeBinding(2).WithPath(FOVAction.bindings[2].path);
        }

        if (modifier1ActionPath != modifier1Action.bindings[0].path)
        {
            modifier1ActionPath = modifier1Action.bindings[0].path;
            modifier1Action.ChangeBinding(0).WithPath(modifier1Action.bindings[0].path);
        }

        if (modifier2ActionPath != modifier2Action.bindings[0].path)
        {
            modifier2ActionPath = modifier2Action.bindings[0].path;
            modifier2Action.ChangeBinding(0).WithPath(modifier2Action.bindings[0].path);
        }

        if (modifier3ActionPath != modifier3Action.bindings[0].path)
        {
            modifier3ActionPath = modifier3Action.bindings[0].path;
            modifier3Action.ChangeBinding(0).WithPath(modifier3Action.bindings[0].path);
        }
#endif

        //if (debug) Debug.Log("sceneNearClip = " + sceneNearClip);
        //if (debug) Debug.Log("Update " + Time.time);
        //if (debug) Debug.Log(camData + " " + leftCamData);
        ////UnityEditor.Selection.activeGameObject = camera_left.gameObject;
        ////camData.CopyTo(camera_left.GetComponent<HDAdditionalCameraData>());
        //leftCamData = camera_left.GetComponent<HDAdditionalCameraData>();
        //camData.CopyTo(leftCamData);
        ////UnityEditor.Selection.activeGameObject = camera_right.gameObject;
        ////camData.CopyTo(camera_right.GetComponent<HDAdditionalCameraData>());
        //rightCamData = camera_right.GetComponent<HDAdditionalCameraData>();
        //camData.CopyTo(rightCamData);

        //if (HDCamDataCopy)
        //{
        //    if (debug) Debug.Log(HDCamDataCopy.antialiasing);
        //    if (debug) Debug.Log(HDCamDataCopy2.antialiasing);
        //}

        ////if (!Equals(HDCamData, HDCamDataCopy))
        ////if (!HDCamData.Equals(HDCamDataCopy))
        ////if (!HDCamDataCopy.Equals(HDCamDataCopy2))
        //if (!Equals(HDCamDataCopy, HDCamDataCopy2))
        //{
        //    if (debug) Debug.Log("!HDCamData.Equals(HDCamDataCopy)");
        //    //HDCamData.CopyTo(HDCamDataCopy);
        //    //HDCamDataCopy.CopyTo(HDCamDataCopy2);
        //}

        //////if (debug) Debug.Log("HDRPSettings.colorBufferFormat " + (GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.colorBufferFormat);
        //////if (debug) Debug.Log("typeof(HDRenderPipelineAsset) " + typeof(HDRenderPipelineAsset));
        //var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        //////if (debug) Debug.Log(typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline));
        //RenderPipelineSettings HDRPSettings = (RenderPipelineSettings)typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", flags).GetValue(GraphicsSettings.currentRenderPipeline);
        //HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;

        //if (!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings))
        //{
        //    HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
        //    if (debug) Debug.Log("HDRPSettings.colorBufferFormat != HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat");
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
        //canvasCamData = canvasCamera.GetComponent<HDAdditionalCameraData>();
        //if (debug) Debug.Log("OnEnable panelDepth as screen distance = " + 1 / (1 - panelDepth));
        //canvasCamera.enabled = true;
        //oddFrame = !oddFrame;
        //if (debug) Debug.Log(oddFrame + " Update " + Time.time);

        //if (canvasCamera && S3DEnabled)
        //{
        //    if (oddFrame)
        //    {
        //        if (method == Method.SideBySide_HMD)
        //            canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //        else
        //            canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //        if (method == Method.Interlace_Horizontal)
        //            canvasCamMatrix[1, 3] = -oneRowShift;

        //        canvasCamera.projectionMatrix = canvasCamMatrix;
        //        //canvasCamera.targetTexture = renderTexture_left;
        //        canvasCamera.targetTexture = canvasRenderTexture_left;
        //        //canvasCamera.Render();
        //        //Graphics.CopyTexture(canvasRenderTexture_left, renderTexture_left);
        //    }
        //    else
        //    {
        //        if (method == Method.SideBySide_HMD)
        //            canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //        else
        //            canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //        if (method == Method.Interlace_Horizontal)
        //            canvasCamMatrix[1, 3] = 0;

        //        canvasCamera.projectionMatrix = canvasCamMatrix;
        //        //canvasCamera.targetTexture = renderTexture_right;
        //        canvasCamera.targetTexture = canvasRenderTexture_right;
        //        //canvasCamera.Render();
        //        //Graphics.CopyTexture(canvasRenderTexture_right, renderTexture_right);
        //    }

        //    //UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);

        //    //if (oddFrame)
        //    //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);

        //    //canvasCamera.Render();
        //    //Debug.Break();
        //    //canvasCamera.enabled = false;
        //    //if (debug) Debug.Log(camera);
        //    //if (debug) Debug.Log(oddFrame + " PostRenderContext camera == canvasCamera " + Time.time);
        //    //UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);
        //}

        //Matrix4x4 canvasCamMatrix = canvasCamera.projectionMatrix;
        //float canvasHalfSizeX = canvasSize.x * .5f;
        ////Matrix4x4 canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
        ////Matrix4x4 canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * shift, canvasHalfSizeX - canvasHalfSizeX * shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
        //Matrix4x4 canvasCamMatrix = canvasCamera.projectionMatrix;

        ////if (oddFrame)
        ////    canvasCamMatrix[0, 3] = -shift;
        ////else
        ////    canvasCamMatrix[0, 3] = shift;

        //if (oddFrame)
        //{
        //    canvasCamMatrix[0, 3] = -shift;
        //    canvasCamera.projectionMatrix = canvasCamMatrix;
        //    canvasCamera.targetTexture = renderTexture_right;
        //}
        //else
        //{
        //    canvasCamMatrix[0, 3] = shift;
        //    canvasCamera.projectionMatrix = canvasCamMatrix;
        //    canvasCamera.targetTexture = renderTexture_left;
        //}

        //canvasCamMatrix[0, 2] = shift;
        //canvasCamera.projectionMatrix = canvasCamMatrix;
        //canvasCamera.ResetProjectionMatrix();
        //if (debug) Debug.Log(canvasCamera.projectionMatrix[0, 3] + " " + shift);
        //canvasCamera.projectionMatrix = Matrix_Set(canvasCamera.projectionMatrix, -shift, 0);

        //if (oddFrame)
        //    canvasCamera.projectionMatrix = Matrix_Set(canvasCamera.projectionMatrix, -shift, 0);
        //else
        //    canvasCamera.projectionMatrix = Matrix_Set(canvasCamera.projectionMatrix, shift, 0);

        //if ((int)Time.time != time)
        //{
        //    time = (int)Time.time;
        //    //if (debug) Debug.Log(time);

        //    canvasCamera.enabled = true;
        //    canvasCamera.targetTexture = renderTexture_left;
        //    canvasCamera.Render();
        //    //Debug.Break();
        //    canvasCamera.enabled = false;
        //}

        //canvasCamera.targetTexture = renderTexture_left;
        //canvasCamera.Render();
        //if (debug) Debug.Log(canvasCamera.GetUniversalAdditionalCameraData().clearDepth);
        //if (debug) Debug.Log("Update " + Time.time);

        ////if (!S3DEnabled && !GUIVisible && sceneNearClip != cam.nearClipPlane)
        //if (!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane)
        //{
        //    sceneNearClip = cam.nearClipPlane;
        //    if (debug) Debug.Log("********************************************************************************************************************sceneNearClip = " + sceneNearClip);
        //    //if (debug) Debug.Log("!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane sceneNearClip= " + sceneNearClip);
        //    //Render_Set();
        //    Clip_Set();
        //}

        //if (Time.time > GUI_Set_delay && lastCamNearClip != cam.nearClipPlane)
        //{
        //    if (debug) Debug.Log("Update cam.nearClipPlane external change");
        //    sceneNearClip = cam.nearClipPlane;
        //    Clip_Set();
        //}

        //if (!GUIVisible && cam.farClipPlane >= 0 && sceneFarClip != cam.farClipPlane)
        //if (sceneFarClip != cam.farClipPlane)
        //{
        //    if (debug) Debug.Log("Update sceneFarClip != cam.farClipPlane");
        //    sceneFarClip = cam.farClipPlane;
        //    Clip_Set();
        //}

        //if (debug) Debug.Log(vCam);
        //if (debug) Debug.Log(Cinemachine.CinemachineCore.Instance.IsLive(vCam));

        //if (cineMachineBrain.ActiveVirtualCamera == null)
        //    //if (debug) Debug.Log(cineMachineBrain.ActiveVirtualCamera);

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        //if (inputSystem)
        //{
        //if (debug) Debug.Log("inputSystem");

        //if (FOVAction.ReadValue<float>() != 0)
        //    virtualIPD += FOVAction.ReadValue<float>();
        //    //if (debug) Debug.Log(FOVAction.ReadValue<float>());

        if (S3DAction.ReadValue<float>() != 0)
        {
            S3DActionPressed = true;
            //S3DKeyTimer += Time.deltaTime;
            ////if (debug) Debug.Log(S3DKeyTimer);

            //if (S3DKeyTimer > 1 && !S3DKeyLoaded)
            //{
            //    Load(slotName);
            //    //S3DKeyTimer = 0;
            //    S3DKeyLoaded = true;
            //    //if (debug) Debug.Log("S3DKeyLoaded");
            //}

            //GUI_Autoshow();
            S3DKeyHold();
        }

        if (S3DActionPressed && S3DAction.ReadValue<float>() == 0)
        {
            S3DActionPressed = false;
            //if (debug) Debug.Log("S3DActionPressed && S3DAction.ReadValue<float>() == 0");

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
            ////    //if (debug) Debug.Log("S3DActionPressed && S3DAction.ReadValue<float>() == 0 S3DKeyTimer > 1");

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
            ////if (debug) Debug.Log(S3DKeyTimer);

            //if (S3DKeyTimer > 1 && !S3DKeyLoaded)
            //{
            //    Load(slotName);
            //    //S3DKeyTimer = 0;
            //    S3DKeyLoaded = true;
            //    //if (debug) Debug.Log("S3DKeyLoaded");
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

        if (displaySelectWaitInput)
        {
            //if (debug) Debug.Log("displaySelectWaitInput");

            if (Input.anyKeyDown)
            {
                //if (debug) Debug.Log("Input.inputString " + Input.inputString);

                //if (display_left == null)
                //{
                //    //string inputString = Input.inputString.Replace(" ", "");
                //    string inputString = Input.inputString.Trim();

                //    //if (Input.inputString != "" && displays.Contains(Input.inputString))
                //    if (inputString != "" && displays.Contains(inputString))
                //    {
                //        if (debug) Debug.Log("displays.Contains(Input.inputString) " + Input.inputString);
                //        //if (debug) Debug.Log("displays.Contains(Input.inputString)" + Input.inputString + "end");
                //        displayIndex_left = Convert.ToInt32(inputString);
                //        //display_left = Display.displays[Convert.ToInt32(inputString)];
                //        //display_left = fakeDisplays[Convert.ToInt32(inputString)];
                //        //display_left = Display.displays[displayIndex_left];
                //        display_left = fakeDisplays[displayIndex_left];
                //        display_left.Activate();
                //        if (debug) Debug.Log("display_left " + display_left);
                //        //DisplayRight_Set();
                //        displays = displays.Replace(inputString, "");
                //        //if (debug) Debug.Log("displays " + displays);
                //        //Destroy(staticTooltip);
                //        StaticTooltip_Make("Press the number key to select the display for Right camera:" + displays);
                //    }
                //    else
                //        if (debug) Debug.Log("!displays.Contains(Input.inputString) " + Input.inputString);
                //}
                //else
                //{
                //    string inputString = Input.inputString.Trim();

                //    if (inputString != "" && displays.Contains(inputString))
                //    {
                //        if (debug) Debug.Log("displays.Contains(Input.inputString) " + Input.inputString);
                //        displayIndex_right = Convert.ToInt32(inputString);
                //        //display_right = Display.displays[Convert.ToInt32(inputString)];
                //        //display_right = fakeDisplays[Convert.ToInt32(inputString)];
                //        //display_right = Display.displays[displayIndex_right];
                //        display_right = fakeDisplays[displayIndex_right];
                //        display_right.Activate();
                //        if (debug) Debug.Log("display_right " + display_right);
                //        displays = displays.Replace(inputString, "");
                //        Destroy(staticTooltip);
                //        displaySelectWaitInput = false;
                //        //TargetDisplays_Set();
                //        TargetDisplays_Set(displayIndex_left, displayIndex_right);
                //    }
                //}

                //TargetDisplays_Input();
                TargetDisplays_Input(Input.inputString);
            }
        }
#endif

        //CameraDataStruct_Set();

        //if (canvas.gameObject.activeSelf && caret == null)
        //{
        //    if (PPI_inputField.transform.Find("InputField (Legacy)_PPI Input Caret"))
        //    {
        //       //if (debug) Debug.Log("PPI_inputField.transform.Find");
        //        InputFieldCaretMaterial_Set(PPI_inputField);
        //        InputFieldCaretMaterial_Set(userIPD_inputField);
        //        InputFieldCaretMaterial_Set(virtualIPD_inputField);
        //        InputFieldCaretMaterial_Set(FOV_inputField);
        //        InputFieldCaretMaterial_Set(panelDepth_inputField);
        //        InputFieldCaretMaterial_Set(screenDistance_inputField);
        //        InputFieldCaretMaterial_Set(slotName_inputField);
        //    }

        //   //if (debug) Debug.Log("caret == null");
        //}

        if (Screen.width != windowSize.x || Screen.height != windowSize.y)
            Resize();

        if (GUIOpened)
        {
            //if (debug) Debug.Log(Input.mousePosition);
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

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            //cursorLocalPos.x = (Pointer.current.position.value.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (Pointer.current.position.value.y / windowSize.y - 1) * canvasSize.y;
            //pointerPosition.x = Pointer.current.position.value.x;
            //pointerPosition.y = Pointer.current.position.value.y;
            //pointerPosition.x = UnityEngine.InputSystem.Pointer.current.position.value.x;
            //pointerPosition.y = UnityEngine.InputSystem.Pointer.current.position.value.y;
            //pointerPosition = UnityEngine.InputSystem.Pointer.current.position.value; //only for Input System 1.5 and above
            pointerPosition = UnityEngine.InputSystem.Pointer.current.position.ReadValue(); //for Input System below and above 1.5
            //pointerPosition = Mouse.current.position.ReadValue();
#else
            //cursorLocalPos.x = (Input.mousePosition.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (Input.mousePosition.y / windowSize.y - 1) * canvasSize.y;
            pointerPosition.x = Input.mousePosition.x;
            pointerPosition.y = Input.mousePosition.y;
#endif

            //cursorLocalPos.x = (pointerPosition.x / windowSize.x - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = (pointerPosition.y / windowSize.y - 1) * canvasSize.y;
            //if (debug) Debug.Log((pointerPosition.y / windowSize.y - cam.rect.y) / cam.rect.height - 1);
            //Camera canvasWorldCam = canvas.worldCamera;
            //cursorLocalPos.x = ((pointerPosition.x / windowSize.x - canvasWorldCam.rect.x) / canvasWorldCam.rect.width - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = ((pointerPosition.y / windowSize.y - canvasWorldCam.rect.y) / canvasWorldCam.rect.height - 1) * canvasSize.y;
            //cursorLocalPos.x = ((pointerPosition.x / windowSize.x - canvas.worldCamera.rect.x) / canvas.worldCamera.rect.width - .5f) * canvasWidthWithOffset;
            //cursorLocalPos.y = ((pointerPosition.y / windowSize.y - canvas.worldCamera.rect.y) / canvas.worldCamera.rect.height - 1) * canvasSize.y;

            Vector2 viewportLeftBottomPos = new Vector2(Mathf.Clamp01(canvas.worldCamera.rect.x), Mathf.Clamp01(canvas.worldCamera.rect.y)); //viewport LeftBottom & RightTop corner coordinates inside render window
            Vector2 viewportRightTopPos = new Vector2(Mathf.Clamp01(canvas.worldCamera.rect.xMax), Mathf.Clamp01(canvas.worldCamera.rect.yMax));
            Rect viewportRect = new Rect(viewportLeftBottomPos.x, viewportLeftBottomPos.y, viewportRightTopPos.x - viewportLeftBottomPos.x, viewportRightTopPos.y - viewportLeftBottomPos.y);
            //if (debug) Debug.Log("viewportRect.x " + viewportRect.x + " viewportRect.width " + viewportRect.width);
            //if (debug) Debug.Log("canvas.worldCamera.rect.x " + canvas.worldCamera.rect.x + " canvas.worldCamera.rect.xMin " + canvas.worldCamera.rect.xMin);
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

                //if (debug) Debug.Log("toolTipTimer" + toolTipTimer);
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
            //   //if (debug) Debug.Log("Hit " + results[0].gameObject.name);

            //EventSystem.current = eventSystem;

            //if (debug) Debug.Log(pointerEventData.position);
            //if (debug) Debug.Log(panelDepth_sliderIsDragging);
            //if (debug) Debug.Log(Input.mousePosition.x);
        }
        //if (debug) Debug.Log("Update " + Time.time);

        //check variable changes after Keys pressed
        if (lastGUIOpened != GUIOpened)
        {
//            if (!lastGUIOpened)
//            {
////if (inputSystem)
////#if STARTER_ASSETS_PACKAGES_CHECKED
////#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
//#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
//                {
//                    cursorLockedDefault = starterAssetsInputs.cursorLocked;
//                    cursorInputForLookDefault = starterAssetsInputs.cursorInputForLook;
//                }
//#endif
//                //else
//                cursorLockModeDefault = Cursor.lockState;
//            }

            //lastGUIOpened = GUIOpened;
            //lastGUIOpened = GUIVisible = GUIOpened;
            GUIVisible = GUIOpened;
            GUI_Set();
//#if HDRP
//            Render_Set();
//#endif
        }

        if (GUI_autoshowTimer > 0)
        {
            autoshow = true;
            GUI_autoshowTimer -= Time.deltaTime;
            GUI_autoshowTimer *= GUIOpened ? 0 : 1;
            //if (debug) Debug.Log(GUI_autoshowTimer);

            //if (!canvas.gameObject.activeSelf)
            if (!GUIVisible)
            {
                //canvas.gameObject.SetActive(true);
                //if (debug) Debug.Log("canvas.gameObject.SetActive(true)");
                GUIVisible = true;
                //Clip_Set();
                //HDRPSettings_Set();
                GUI_Set();
            }
        }
        else
            if (autoshow)
            {
                autoshow = false;
                //if (debug) Debug.Log("autoshow = false");

                if (!GUIOpened)
                {
                    //canvas.gameObject.SetActive(false);
                    GUIVisible = false;
                    //Clip_Set();
                    //HDRPSettings_Set();
                    GUI_Set();
                }
            }

        if (lastS3DEnabled != S3DEnabled)
        {
            //if (cam.projectionMatrix == Matrix4x4.zero)
            //    cam.projectionMatrix = camMatrix;

            if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
#if URP || HDRP
                if (lastS3DEnabled)
                    RenderPipelineManager.beginCameraRendering += PreRenderClearScreen;
#else
                Camera.onPreRender += PreRenderClearScreen;
#endif
            lastS3DEnabled = S3DEnabled;
            enableS3D_toggle.isOn = S3DEnabled;
            Aspect_Set();
            Clip_Set();
#if URP
            CameraStackSet();
#endif
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
            //Render_Set();
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
            if (method == Method.D3D11 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11)
                method = lastMethod;
            else
            {
                lastMethod = method;

//#if HDRP
//              if (lastMethod == Method.Two_Displays)
//                  GUIAsOverlay = GUIAsOverlayState;
//#endif

                if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
#if URP || HDRP
                    if (method == Method.Two_Displays)
                        RenderPipelineManager.beginCameraRendering += PreRenderClearScreen;
#else
                    Camera.onPreRender += PreRenderClearScreen;
#endif

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
                //Render_Set();
                //outputMethod_dropdown.value = (int)method;
            }

            outputMethod_dropdown.value = (int)method;
        }

        if (lastPPI != PPI)
        {
            //lastPPI = PPI;
            PPISet();
            ViewSet();
        }

        if (lastUserIPD != userIPD)
        {
            //lastUserIPD = userIPD;
            UserIPDSet();
            CamSet();
        }

        if (lastVirtualIPD != virtualIPD)
        {
            //lastVirtualIPD = virtualIPD;
            VirtualIPDSet();
            CamSet();
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
            CamSet();
        }

        //if (lastPixelPitch != pixelPitch)
        //{
        //    lastPixelPitch = pixelPitch;
        //    PixelPitchSet();
        //}

        //if (!FOVSetInProcess && cam.fieldOfView != vFOV) //check camera FOV changes to set FOV from other scripts
        //{
        //    //if (debug) Debug.Log("cam.fieldOfView != vFOV");
        //    //if (debug) Debug.Log("camFOVChangedExternal");
        //    camFOVChangedExternal = true;
        //    FOV_Set();
        //}

        //if (lastFOV != FOV)
        //{
        //    //lastFOV = FOV;
        //    FOV_Set();
        //}

        //if (!FOVSetInProcess) //check camera FOV changes to set FOV from other scripts
        //{
        if (cam.fieldOfView != vFOV)
            {
                if (debug) Debug.Log("camFOVChangedExternal");
                camFOVChangedExternal = true;
                FOV_Set();
            }
            else
                if (lastFOV != FOV)
                {
                    if (debug) Debug.Log("camFOVChanged Internal");
                    FOV_Set();
                }
        //}

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
        //CameraDataStruct_Set();

        //if (cloneCamera)
        //{
        //if (camData)
        //CameraDataStruct_Set();

        //cameraDataStruct.renderPostProcessing = camData.renderPostProcessing;
        //cameraDataStruct.antialiasing = camData.antialiasing;
        //if (debug) Debug.Log(camData.scriptableRenderer);
        //if (debug) Debug.Log("lastCameraDataStruct.Equals(cameraDataStruct) " + lastCameraDataStruct.Equals(cameraDataStruct));
        //if (debug) Debug.Log("lastCameraDataStruct " + lastCameraDataStruct.ToString() + " cameraDataStruct " + cameraDataStruct.ToString());

        //if (debug) Debug.Log(lastCameraDataStruct.cameraRenderType + " " + cameraDataStruct.cameraRenderType);
        //if (debug) Debug.Log(lastCameraDataStruct.scriptableRenderer + " " + cameraDataStruct.scriptableRenderer);
        //if (debug) Debug.Log(lastCameraDataStruct.renderPostProcessing + " " + cameraDataStruct.renderPostProcessing);
        //if (debug) Debug.Log(lastCameraDataStruct.antialiasing + " " + cameraDataStruct.antialiasing);
        //if (debug) Debug.Log(lastCameraDataStruct.antialiasingQuality + " " + cameraDataStruct.antialiasingQuality);
        //if (debug) Debug.Log(lastCameraDataStruct.stopNaN + " " + cameraDataStruct.stopNaN);
        //if (debug) Debug.Log(lastCameraDataStruct.dithering + " " + cameraDataStruct.dithering);
        //if (debug) Debug.Log(lastCameraDataStruct.renderShadows + " " + cameraDataStruct.renderShadows);
        //if (debug) Debug.Log(lastCameraDataStruct.depth + " " + cameraDataStruct.depth);
        //if (debug) Debug.Log(lastCameraDataStruct.useOcclusionCulling + " " + cameraDataStruct.useOcclusionCulling);

        //Debug.Break();

        //if (lastInterlaceType != interlaceType)
        //{
        //    lastInterlaceType = interlaceType;
        //    Render_Set();
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
            //camera_left.rect = camera_right.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
            //CamRect_Set();
            //Resize();
            Aspect_Set();
            //ViewSet();
            //Render_Set();
#if URP || HDRP
            RenderPipelineManager.beginCameraRendering += PreRenderClearScreen;
#else
            Camera.onPreRender += PreRenderClearScreen;
#endif
        }

        if (lastDisableCullingMask != disableCullingMask)
        {
            lastDisableCullingMask = disableCullingMask;
            onOffToggle = true;
        }

        if (lastNearClipHack != nearClipHack)
        {
            lastNearClipHack = nearClipHack;
            //Render_Set();
            Clip_Set();
        }

        if (lastMatrixKillHack != matrixKillHack)
        {
            //if (lastMatrixKillHack)
            //    cam.ResetProjectionMatrix();

            //if (cam.projectionMatrix == Matrix4x4.zero)
            //    cam.projectionMatrix = camMatrix;

            lastMatrixKillHack = matrixKillHack;
            //Render_Set();
            ViewSet();
        }

        if (!GUIAsOverlay && lastCanvasLocalPosZ != canvasLocalPosZ)
        {
            lastCanvasLocalPosZ = canvasLocalPosZ;
            Clip_Set();
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
        //    //if (debug) Debug.Log("lastAdditionalS3DCameras != additionalS3DCameras");
        //    //if (debug) Debug.Log("lastAdditionalS3DCameras != additionalS3DCameras " + lastAdditionalS3DCameras.Length + " " + additionalS3DCameras.Length);
        //    //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);

        //    for (int i = 0; i < additionalS3DCameras.Count; i++)
        //    {
        //        if (debug) Debug.Log("component " + additionalS3DCameras[i].name + " " + lastAdditionalS3DCameras[i].name);
        //    }
        //}

        if (lastGUISizeKeep != GUISizeKeep)
        {
            lastGUISizeKeep = GUISizeKeep;
            Aspect_Set();
        }

        if (physicalCamera != cam.usePhysicalProperties)
        {
            if (debug) Debug.Log("physicalCamera != cam.usePhysicalProperties");
            physicalCamera = cam.usePhysicalProperties;
            //OnOffToggle();
            onOffToggle = true;
        }

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

            ////if (debug) Debug.Log("panelDepthMinMax " + panelDepthMinMax + " panelDepth " + panelDepth);
            //if (debug) Debug.Log("panelDepthMinMax " + panelDepthMinMax + " newPanelDepth " + newPanelDepth);

            ////panelDepth_slider.minValue = panelDepthMinMax.x;
            ////panelDepth_slider.maxValue = panelDepthMinMax.y;
            ////PanelDepth_InputField(panelDepth.ToString());
            ////panelDepth_slider.value = panelDepth;
            ////PanelDepth_Slider(panelDepth);
            ////panelDepth_inputField.text = panelDepth.ToString();
            ////if (debug) Debug.Log("panelDepth_slider.value " + panelDepth_slider.value);
            
            if (lastGUIAsOverlay) //prevent Infinity error on disable if UserIPD = 0
            {
                UserIPDSet();
                VirtualIPDSet();
            }

            lastGUIAsOverlay = GUIAsOverlay;
            //UserIPDSet();
            //VirtualIPDSet();
            //OnOffToggle();
            onOffToggle = true;
            //Invoke("DelayedPanelDepthSet", Time.deltaTime);
        }

        if (lastCameraPrefab != cameraPrefab)
        {
            lastCameraPrefab = cameraPrefab;
            //this.enabled = false;
            //this.enabled = true;
            //OnDisable();
            //OnEnable();
            //OnOffToggle();
            onOffToggle = true;
        }

        if (lastRTFormat != RTFormat)
        {
            //lastRTFormat = RTFormat;
            //defaultRTFormat = RTFormat;
            //this.enabled = false;
            //this.enabled = true;
            //OnOffToggle();
            onOffToggle = true;
        }

        //if (lastSetMatrixDirectly != setMatrixDirectly)
        //{
        //    lastSetMatrixDirectly = setMatrixDirectly;
        //    //this.enabled = false;
        //    //this.enabled = true;
        //    OnOffToggle();
        //}

        if (lastCloneCamera != cloneCamera)
        {
            lastCloneCamera = cloneCamera;
            //OnOffToggle();
            onOffToggle = true;
        }

        if (lastLoadSettingsFromFile != loadSettingsFromFile)
        {
            lastLoadSettingsFromFile = loadSettingsFromFile;
            //OnOffToggle();
            onOffToggle = true;
        }

        if (lastAdditionalS3DCameras.Length != additionalS3DCameras.Count)
        {
            if (debug) Debug.Log("lastAdditionalS3DCameras.Length != additionalS3DCameras.Count " + lastAdditionalS3DCameras.Length + " " + additionalS3DCameras.Count);
            //AdditionalS3DCamerasUpdate();
            //OnOffToggle();
            //ClosestCameraIndex_Set();
            onOffToggle = true;
        }
        else
            for (int i = 0; i < lastAdditionalS3DCameras.Length; i++)
                if (lastAdditionalS3DCameras[i] != additionalS3DCameras[i])
                {
                    if (debug) Debug.Log("lastAdditionalS3DCameras[i] != additionalS3DCameras[i] " + lastAdditionalS3DCameras[i] + " " + additionalS3DCameras[i]);
                    //Debug.Break();
                    //AdditionalS3DCamerasUpdate();
                    //OnOffToggle();
                    onOffToggle = true;
                    break;
                }

        //void AdditionalS3DCamerasUpdate()
        //{
        //    if (debug) Debug.Log("AdditionalS3DCamerasUpdate");
        //    //lastAdditionalS3DCameras = new Camera[additionalS3DCameras.Count];
        //    //additionalS3DCameras.CopyTo(lastAdditionalS3DCameras);
        //    //lastAdditionalS3DCameras = additionalS3DCameras.ToArray();
        //    OnOffToggle();
        //    //Debug.Break();
        //}

        if (lastHide2DCursor != hide2DCursor
            || lastHide2DCursor == Cursor.visible //prevent external script restore cursor like simpleCameraControllerScript
            )
        {
            if (debug) Debug.Log("lastHide2DCursor != hide2DCursor");
            Cursor.visible = !(lastHide2DCursor = hide2DCursor);
            //lastHide2DCursor = hide2DCursor;
            //Cursor.visible = !hide2DCursor;
        }

#if HDRP
        //HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;

        //if (!GUIOpened && !GUIVisible && HDRPSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (!GUIOpened && !GUIVisible && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (Time.time > GUI_Set_delay && !GUIVisible && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat)
        //if (Time.time > GUI_Set_delay && HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != HDRPSettings.colorBufferFormat)
        //{
        //    ////defaultColorBufferFormat = HDRPSettings.colorBufferFormat;
        //    //defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;
        //    //if (debug) Debug.Log("!GUIVisible && HDRPSettings.colorBufferFormat != defaultColorBufferFormat " + defaultColorBufferFormat);
        //    if (debug) Debug.Log("HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat != defaultColorBufferFormat");

        //    if (!GUIVisible)
        //        defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;

        //    HDRPSettings_Set();
        //}

        //if (!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings))
        if (!HDRPSettings.Equals(HDRPAsset.currentPlatformRenderPipelineSettings))
        {
            if (debug) Debug.Log("!Equals(HDRPSettings, HDRPAsset.currentPlatformRenderPipelineSettings)");
            HDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;
            //defaultColorBufferFormat = HDRPAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;
            HDRPSettings.colorBufferFormat = defaultHDRPSettings.colorBufferFormat;

            if (!HDRPSettings.Equals(defaultHDRPSettings))
                defaultHDRPSettings = HDRPSettings;
            else
                defaultHDRPSettings = HDRPAsset.currentPlatformRenderPipelineSettings;

            //OnOffToggle();
            onOffToggle = true;
        }

        //if (canvasCamera && S3DEnabled)
        //{
        //    //if (oddFrame)
        //    //{
        //    //    if (method == Method.SideBySide_HMD)
        //    //        canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //    //    else
        //    //        canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //    //    if (method == Method.Interlace_Horizontal)
        //    //        canvasCamMatrix[1, 3] = -oneRowShift;

        //    //    canvasCamera.projectionMatrix = canvasCamMatrix;
        //    //    canvasCamera.targetTexture = canvasRenderTexture_left;
        //    //}
        //    //else
        //    //{
        //    //    if (method == Method.SideBySide_HMD)
        //    //        canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
        //    //    else
        //    //        canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

        //    //    if (method == Method.Interlace_Horizontal)
        //    //        canvasCamMatrix[1, 3] = 0;

        //    //    canvasCamera.projectionMatrix = canvasCamMatrix;
        //    //    canvasCamera.targetTexture = canvasRenderTexture_right;
        //    //}

        //    CanvasCamS3DRender_Set();
        //}
#elif URP
        if (lastCameraStack.Length != cameraStack.Count)
        {
            if (debug) Debug.Log("lastCameraStack.Length != cameraStack.Count " + lastCameraStack.Length + " " + cameraStack.Count);
            //OnOffToggle();
            onOffToggle = true;
        }
        else
            for (int i = 0; i < lastCameraStack.Length; i++)
                if (lastCameraStack[i] != cameraStack[i])
                {
                    if (debug) Debug.Log("lastCameraStack[i] != cameraStack[i] " + lastCameraStack[i] + " " + cameraStack[i]);
                    //OnOffToggle();
                    onOffToggle = true;
                    break;
                }

        //if (!lastURPAsset.Equals(GraphicsSettings.currentRenderPipeline))
        //if (lastURPAsset.supportsMainLightShadows != ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows)
        //if (!lastURPAsset.Equals(URPAsset))
        //if (URPAssetIsReady && !lastScriptableRenderer.Equals(URPAsset.scriptableRenderer))
        //if (lastMSAASampleCount != URPAsset.msaaSampleCount)
        //if (!lastScriptableRenderer.Equals(URPAsset.scriptableRenderer))
        if (URPAsset != GraphicsSettings.currentRenderPipeline || URPAssetIsReady && !lastScriptableRenderer.Equals(URPAsset.scriptableRenderer))
        {
            if (debug) Debug.Log("!lastScriptableRenderer.Equals(URPAsset.scriptableRenderer) " + lastScriptableRenderer);
            //if (debug) Debug.Log("lastMSAASampleCount != URPAsset.msaaSampleCount");
            //OnOffToggle();
            onOffToggle = true;
            //lastURPAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            //lastURPAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            //lastURPAsset = URPAsset;
            //lastScriptableRenderer = URPAsset.scriptableRenderer;
            //lastMSAASampleCount = URPAsset.msaaSampleCount;
        }

        //if (debug) Debug.Log("((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows" + ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsMainLightShadows);
#endif

#if CINEMACHINE
        //if (S3DEnabled && nearClipHack && cam.nearClipPlane != -1)
        //{
        //    //if (debug) Debug.Log("S3DEnabled && nearClipHack && cam.nearClipPlane != -1");
        //    VCamNearClipHack();
        //}

        //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //if (vCam == GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera)
        //if (Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //{
        //    Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //    //if (debug) Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam");
        //}

#if UNITY_EDITOR
        if (vCam != null && UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject)
        {
            //if (lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane)
            //{
            //    if (debug) Debug.Log("lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
            //    lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
            //    if (debug) Debug.Log("lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
            //}
            //else
            //{
            //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
            //    cam.nearClipPlane = -1;
            //}

            vCamSelected = true;
            vCamSelectedTimer = 1;
        }
        else
        {
            //vCamSelected = false;
            //Invoke("VCamUnselect", 1);
            vCamSelectedTimer -= Time.deltaTime;

            if (vCamSelectedTimer <= 0)
                vCamSelected = false;
        }
#endif
        //if (cineMachineEnabled && Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //if (vCam != null && Cinemachine.CinemachineBrain.SoloCamera != vCam)
        if (vCam != null && cineMachineBrain.ActiveVirtualCamera != null && vCam != cineMachineBrain.ActiveVirtualCamera)
        {
            //if (debug) Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam, Cinemachine.CinemachineBrain.SoloCamera = " + Cinemachine.CinemachineBrain.SoloCamera + ", vCam = " + vCam);

            ////if (Cinemachine.CinemachineBrain.SoloCamera == null)
            //if (Cinemachine.CinemachineBrain.SoloCamera == null || Cinemachine.CinemachineBrain.SoloCamera.ToString() == "null") //virtual camera lost
            //    Cinemachine.CinemachineBrain.SoloCamera = vCam;
            //else //virtual camera changed
            //    //if (vCamSceneClipIsReady)
            //    {
                    //if (debug) Debug.Log("virtual camera changed");
                    if (debug) Debug.Log("Cinemachine virtual camera changed, vCam: " + vCam + ", Cinemachine.CinemachineBrain.SoloCamera: " + Cinemachine.CinemachineBrain.SoloCamera);
                    ////((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
                    //VCamClipRestore();

                    //vCam = Cinemachine.CinemachineBrain.SoloCamera;

                    ////if (!GUIVisible && cam.nearClipPlane >= 0 && sceneNearClip != cam.nearClipPlane)
                    ////{
                    ////    sceneNearClip = cam.nearClipPlane;
                    ////    //if (debug) Debug.Log("!S3DEnabled && !GUIVisible && sceneNearClip != cam.nearClipPlane sceneNearClip= " + cam.nearClipPlane);
                    ////    Clip_Set();
                    ////}

                    //VCamSceneClipSet();
                    ////if (debug) Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != null sceneNearClip " + sceneNearClip);
                    //FOV_Set();
                    //Render_Set();
                    //Clip_Set();

                    //onOffToggle = vCamChanged = true;
                    onOffToggle = true;
                    CameraDataStruct_Change(); //prevent change current sceneNearClip and sceneFarClip by virtual camera changed and required to restore previous virtual camera settings OnDisable
            //}
                //OnOffToggle();
                //onOffToggle = true;
        }
        //else
        //    if (Cinemachine.CinemachineBrain.SoloCamera != vCam)
        //{
        //    vCam = Cinemachine.CinemachineBrain.SoloCamera;
        //    //if (debug) Debug.Log("Cinemachine.CinemachineBrain.SoloCamera != vCam " + vCam);
        //}

        //if (nearClipHackApplyed && cam.nearClipPlane != -1)
        ////if (nearClipHackApplyed && ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane != -1)
        //{
        //    if (debug) Debug.Log("nearClipHackApplyed && cam.nearClipPlane != -1 cam.nearClipPlane " + cam.nearClipPlane);
        //    VCamNearClipHack();
        //}
#endif

#if POST_PROCESSING_STACK_V2
        if (PPLayer && PPLayer.enabled != PPLayerLastStatus)
        {
            Debug.Log("PPLayer.enabled != PPLayerLastStatus");
            PPLayerDefaultStatus = PPLayer.enabled;
            onOffToggle = true;
        }

        //foreach (var c in additionalS3DCamerasStruct)
        //    if (c.PPLayer && c.camera && c.PPLayer.enabled != c.PPLayerLastStatus)
        //    {
        //        Debug.Log("c.PPLayer.enabled != c.PPLayerLastStatus");
        //        additionalS3DCamerasStruct[additionalS3DCameras.IndexOf(c.camera)].PPLayerDefaultStatus = c.PPLayer.enabled;
        //        onOffToggle = true;
        //    }

        for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
            if (additionalS3DCamerasStruct[i].camera && additionalS3DCamerasStruct[i].PPLayer && additionalS3DCamerasStruct[i].PPLayer.enabled != additionalS3DCamerasStruct[i].PPLayerLastStatus)
            {
                Debug.Log("c.PPLayer.enabled != c.PPLayerLastStatus");
                //additionalS3DCamerasStruct[i].PPLayerDefaultStatus = additionalS3DCamerasStruct[i].PPLayer.enabled;
                onOffToggle = true;
            }

#endif

        //if (Time.time > setLastCameraDataStructTime)
        //if (cameraDataStructIsReady)
        //if (cameraDataStructIsReady
        if (detectCameraSettingChange && cameraDataStructIsReady
//#if CINEMACHINE
//            && vCamSceneClipIsReady
//#endif
            )
        {
            //if (debug) Debug.Log("cameraDataStructIsReady");
            //bool notEqual = false; //prevent multiple OnOffToggle() call

            //if (!Equals(lastCameraDataStruct, cameraDataStruct))
            //if (Time.time > setLastCameraDataStructTime && !lastCameraDataStruct.Equals(cameraDataStruct))
            if (!lastCameraDataStruct.Equals(cameraDataStruct))
            {
                onOffToggle = true;
                //Debug.Break();
                if (debug) Debug.Log("!lastCameraDataStruct.Equals(cameraDataStruct)");
                //onOffToggle = true;

                if (lastCameraDataStruct.cullingMask != cameraDataStruct.cullingMask)
                {
                    //if (debug) Debug.Log("lastCameraDataStruct.cullingMask != cameraDataStruct.cullingMask " + lastCameraDataStruct.cullingMask + " " + cameraDataStruct.cullingMask);
                    sceneCullingMask = cameraDataStruct.cullingMask;
                }

                //if (additionalS3DCameras.Count == 0 && cam.nearClipPlane != sceneNearClip)
                //    //if (debug) Debug.Log("additionalS3DCameras.Count == 0 && cam.nearClipPlane != sceneNearClip");
                //    SceneNearClip_Set();

                //if (cam.nearClipPlane != sceneNearClip)
                //{
                //    if (additionalS3DCameras.Count == 0)
                //        SceneNearClip_Set();
                //    else
                //        cameraNearClip = cam.nearClipPlane;

                //}
                //if (debug) Debug.Log("additionalS3DCameras.Count == 0 && cam.nearClipPlane != sceneNearClip");
                //SceneNearClip_Set();

                //if (cam.nearClipPlane > .01f && cam.nearClipPlane != camera_left.nearClipPlane) //detect external changes in cam.nearClipPlane for set it on disable
                //        cameraNearClip = cam.nearClipPlane;

                //if (cam.farClipPlane != camera_left.farClipPlane)
                //    sceneFarClip = cam.farClipPlane;

                //if (cam.farClipPlane != camera_left.farClipPlane)
                //    sceneFarClip = cam.farClipPlane;
                //else
                //    if (cam.nearClipPlane != camera_left.nearClipPlane) //detect external changes in cam.nearClipPlane for set it on disable
                //    cameraNearClip = cam.nearClipPlane;

                if (lastCameraDataStruct.farClipPlane != cameraDataStruct.farClipPlane)
                {
                    sceneFarClip = cam.farClipPlane;
                    //VCamClip_Sync();
                    //Clip_Set();
                    //onOffToggle = false;
                }
                else
                    if (lastCameraDataStruct.nearClipPlane != cameraDataStruct.nearClipPlane) //detect external changes in cam.nearClipPlane for set it on disable
                    {
                        if (debug) Debug.Log("lastCameraDataStruct.nearClipPlane != cameraDataStruct.nearClipPlane lastCameraDataStruct.nearClipPlane " + lastCameraDataStruct.nearClipPlane + " cameraDataStruct.nearClipPlane " + cameraDataStruct.nearClipPlane);
                        //cameraNearClip = cam.nearClipPlane;

                        ////if (cam.nearClipPlane > .001f)
                        ////if (!(nearClipHack && cam.nearClipPlane == .001f))
                        ////if (S3DEnabled && nearClipHack && vCam != null && UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject)
                        //if (S3DEnabled && nearClipHack && cam.nearClipPlane <= .001f && vCamSelected) //detect reset nearClipHack from cam.nearClipPlane by selecting active virtual camera in the editor
                        //{
                        //    if (debug) Debug.Log("cam.nearClipPlane reset " + cam.nearClipPlane + " vCamSelected " + vCamSelected);
                        //    onOffToggle = false;
                        //    cam.nearClipPlane = -1;
                        //    //VCamClip_Sync();
                        //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
                        //    //CameraDataStruct_Change();
                        //}
                        //else
                        //{
                        //    if (debug) Debug.Log("cam.nearClipPlane set " + cam.nearClipPlane + " vCamSelected " + vCamSelected);
                        //    //cameraNearClip = sceneNearClip = cam.nearClipPlane;
                        //    cameraNearClip = cam.nearClipPlane;

                        //    if (additionalS3DCameras.Count == 0)
                        //        sceneNearClip = cameraNearClip;
                        //}

                        //if (!vCamSelected && cam.nearClipPlane > 0 || vCamSelected && cam.nearClipPlane > .001f || vCamSelected && !(S3DEnabled && nearClipHack) && cam.nearClipPlane > 0)
                        if (
#if CINEMACHINE
                        vCamSelected && cam.nearClipPlane > .001f || vCamSelected && !(S3DEnabled && nearClipHack) && cam.nearClipPlane > 0 || !vCamSelected && 
#endif
                        cam.nearClipPlane > 0
                        )
                        {
                            cameraNearClip = cam.nearClipPlane;

                            //if (additionalS3DCameras.Count == 0)
                            if (additionalS3DTopmostCameraIndex == -1)
                                sceneNearClip = cameraNearClip;

                            if (debug) Debug.Log("!vCamSelected && cam.nearClipPlane > 0 || vCamSelected && cam.nearClipPlane > .001f || vCamSelected && !(S3DEnabled && nearClipHack) && cam.nearClipPlane > 0 sceneNearClip " + sceneNearClip);
                        }
                        else
                            {
                                onOffToggle = false;

                                if (S3DEnabled && nearClipHack)
                                {
                                    //onOffToggle = false;
                                    cam.nearClipPlane = -1;
#if CINEMACHINE
                                    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
#endif
                                }
                                else
                                {
                                    cam.nearClipPlane = lastCameraDataStruct.nearClipPlane;
#if CINEMACHINE
                                    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = lastCameraDataStruct.nearClipPlane;
#endif
                                }
                            }

                    //VCamClip_Sync();
                    //SceneNearClip_Set();
                    //Clip_Set();
                    //onOffToggle = false;
                    }

#if !CINEMACHINE
                if (lastCameraDataStruct.projectionMatrix != cameraDataStruct.projectionMatrix)
                //{
                    //if (debug) Debug.Log("lastCameraDataStruct.projectionMatrix != cameraDataStruct.projectionMatrix");
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                            if (lastCameraDataStruct.projectionMatrix[i, j] != cameraDataStruct.projectionMatrix[i, j])
                                //if (debug) Debug.Log("i " + i + " j " + j);
                                camMatrix[i, j] = cameraDataStruct.projectionMatrix[i, j]; //add external changes in projectionMatrix to camMatrix while current projectionMatrix can be zero and then restore from camMatrix on disable.
#endif
                //    if (S3DEnabled && matrixKillHack)
                //        cam.projectionMatrix = Matrix4x4.zero;
                //}

                //universalAdditionalCameraData = cam.GetUniversalAdditionalCameraData();
                lastCameraDataStruct = cameraDataStruct;
                //cameraDataStruct = cam.GetUniversalAdditionalCameraData();
                //lastUniversalAdditionalCameraData = universalAdditionalCameraData;
                //universalAdditionalCameraData.renderPostProcessing = cam.GetUniversalAdditionalCameraData().renderPostProcessing;
                //OnOffToggle();
                //notEqual = true;
                //Invoke("OnOffToggle", 1);
                //onOffToggle = true;
            }

            for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
            {
                if (!additionalS3DCamerasStruct[i].lastCameraDataStruct.Equals(additionalS3DCamerasStruct[i].cameraDataStruct))
                {
                    if (debug) Debug.Log("!additionalS3DCamerasStruct[i].lastCameraDataStruct.Equals(additionalS3DCamerasStruct[i].cameraDataStruct)");

                    //if (additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera.nearClipPlane != sceneNearClip)
                    //if (i == additionalS3DTopmostCameraIndex && additionalS3DCamerasStruct[i].camera.nearClipPlane != additionalS3DCamerasStruct[i].camera_left.nearClipPlane)
                    if (i == additionalS3DTopmostCameraIndex && additionalS3DCamerasStruct[i].lastCameraDataStruct.nearClipPlane != additionalS3DCamerasStruct[i].cameraDataStruct.nearClipPlane)
                    {
                        //if (debug) Debug.Log("----------sceneNearClip " + sceneNearClip + " additionalS3DCamerasStruct[i].camera.nearClipPlane " + additionalS3DCamerasStruct[i].camera.nearClipPlane + " additionalS3DCamerasStruct[i].camera_left.nearClipPlane " + additionalS3DCamerasStruct[i].camera_left.nearClipPlane);
                        //Debug.Break();
                        SceneNearClip_Set();
                    }

                    additionalS3DCamerasStruct[i].lastCameraDataStruct = additionalS3DCamerasStruct[i].cameraDataStruct;
                    //OnOffToggle();
                    //notEqual = true;
                    onOffToggle = true;
                }
            }

            //if (notEqual)
            //    OnOffToggle();
        }

        //if (vCam != null && lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane)
        //{
        //    if (debug) Debug.Log("lastVCamNearClip != ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        //    //lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;

        //    if (((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane > .001f)
        //    {
        //        SceneNearClip_Set();
        //        //Clip_Set();
        //        onOffToggle = true;
        //    }
        //    else
        //        if (S3DEnabled && nearClipHack && vCam != null && UnityEditor.Selection.activeObject == vCam.VirtualCameraGameObject)
        //        {
        //            //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
        //            //cam.nearClipPlane = -1;
        //            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane = -1;
        //            //lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane = -1;

        //            if (((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane != -1)
        //                if (debug) Debug.Log("((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane != -1-----------------------------------------------------------------------------");
        //        }

        //    if (debug) Debug.Log("lastVCamNearClip " + lastVCamNearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
        //    lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
        //}

        if (onOffToggle)
        //void OnOffToggle()
        {
            if (!lastCameraDataStruct.Equals(cameraDataStruct))
                if (debug) Debug.Log("!lastCameraDataStruct.Equals(cameraDataStruct)**************************");

            if (debug) Debug.Log("OnOffToggle");
            onOffToggle = false;
            enabled = false;
            //Invoke("Disable", 1);
            //enabled = true;
            Invoke("Enable", 0); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
            //Invoke("Enable", Time.deltaTime * 4); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
        }

//#if UNITY_EDITOR
//        //if (S3DEnabled && method == Method.Two_Displays)
//        if (S3DEnabled)
//        {
//            if (method == Method.Two_Displays)
//            {
//                camera_left.targetTexture = renderTexture_left;
//                camera_right.targetTexture = renderTexture_right;
//            }
//        }
//        else
//            if (additionalS3DCameras.Count != 0 || canvasCamera && canvasCamera.isActiveAndEnabled)
//                cam.targetTexture = renderTexture;
//#endif

//        if (S3DEnabled && method == Method.Sequential)
//        {
//            //oddFrame = !oddFrame;
//            S3DMaterial.SetInt("_OddFrame", oddFrame ? 1 : 0);

//            //if (Time.time < 10)
//            if (oddFrame)
//            {
//                camera_left.Render();
//                camera_right.Render();

//#if !URP
//                foreach (var c in additionalS3DCamerasStruct)
//                    if (c.camera)
//                    {
//                        c.camera_left.Render();
//                        c.camera_right.Render();
//                    }
//#endif

//                if (GUIAsOverlay && GUIVisible)
//                {
//                    //if (debug) Debug.Log("canvasCamera_left && canvasCamera_left.isActiveAndEnabled");
//                    canvasCamera_left.Render();
//                    canvasCamera_right.Render();
//                }
//            }
//        }

        //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //    if (!(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
        //    {
        //        camera_left.GetComponent<BlitToScreen>().enabled = true;
        //        camera_right.GetComponent<BlitToScreen>().enabled = true;
        //    }
        //    else
        //    {
        //        camera_left.GetComponent<BlitToScreen>().enabled = false;
        //        camera_right.GetComponent<BlitToScreen>().enabled = false;
        //    }
    }

    //void OnOffToggle()
    //{
    //    if (!lastCameraDataStruct.Equals(cameraDataStruct))
    //        if (debug) Debug.Log("!lastCameraDataStruct.Equals(cameraDataStruct)**************************");

    //    if (debug) Debug.Log("OnOffToggle");
    //    enabled = false;
    //    //Invoke("Disable", 1);
    //    //enabled = true;
    //    Invoke("Enable", 0); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
    //                         //Invoke("Enable", Time.deltaTime * 4); //prevent additionalS3DCameras clone duplicates on reenable this script in same frame as previous clones can't be destroyed instantly before creating new ones
    //}

    //void Disable()
    //{
    //    enabled = false;
    //}

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
        if (debug) Debug.Log("PanelDepthMinMaxSet" + panelDepthMinMax);

        //PanelDepthSet();
    }

    void PanelDepthSet()
    {
        panelDepth = Mathf.Clamp(panelDepth, panelDepthMinMax.x, panelDepthMinMax.y);
        if (debug) Debug.Log("PanelDepthSet " + panelDepth);
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
        if (debug) Debug.Log("HDRPSettings_Set");

        //if (GUIAsOverlay)
        //if (GUIAsOverlay || additionalS3DCameras.Count != 0)
        //{
            //if (GUIVisible || additionalS3DCameras.Count != 0)
            //if (GUIAsOverlay && GUIVisible || additionalS3DCameras.Count != 0)
            if (GUIAsOverlay && GUIVisible || additionalS3DTopmostCameraIndex != -1)
            {
                HDRPSettings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
                //cam.targetTexture = renderTexture;
            }
            else
            {
                //if (additionalS3DCameras.Count == 0)
                //HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
                HDRPSettings.colorBufferFormat = defaultHDRPSettings.colorBufferFormat;
                //cam.targetTexture = null;
            }

            typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
            //Invoke("Render_Set", 5);
            //Render_Set();
        //}
#endif
        //Render_Set();
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

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
    //void PlayerInputEnable()
    //{
    //    playerInput.enabled = true;
    //}

    public void OnGUIAction(InputAction.CallbackContext context)
    {
        //if (debug) Debug.Log("OnGUIAction value.isPressed " + value.isPressed);
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

    //    //if (debug) Debug.Log(FOVAction.ReadValue<float>());
    //    ////virtualIPD += context.ReadValue<float>();
    //    //virtualIPD += FOVAction.ReadValue<float>();
    //}

    //public void OnModifier1Action(InputAction.CallbackContext context)
    //{
    //    //if (debug) Debug.Log("modifier1Action");
    //    //if (debug) Debug.Log(modifier1Action.ReadValue<float>());
    //}

    //public void OnModifier2Action(InputAction.CallbackContext context)
    //{
    //    //if (debug) Debug.Log("modifier2Action");
    //}

    //public void OnMove(InputValue value)
    //{
    //    //if (debug) Debug.Log("OnMove value " + value.Get<Vector2>());
    //}

    //public void Test()
    //{
    //    //if (debug) Debug.Log("Test");
    //}

    void AnyKeyPress(InputControl key)
    {
        if (debug) Debug.Log("AnyKeyPress key.ToString() " + key.name);
        string keyName = key.name.Replace("numpad", "");

        if (displaySelectWaitInput)
        {
            //TargetDisplays_Input();
            //TargetDisplays_Input(key.ToString());
            //TargetDisplays_Input(key.name);
            TargetDisplays_Input(keyName);
        }
    }
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
        //if (debug) Debug.Log("GetVCam");
        //defaultVCam = vCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera;
        vCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera;
        if (debug) Debug.Log("GetVCam() " + vCam);

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

        if (vCam == null || vCam.ToString() == "null")
            Invoke("GetVCam", Time.deltaTime);
        else
        {
        //    lastVCamNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
            //VCamSceneClipSet();
            SceneNearClip_Set();
            SceneFarClip_Set();
            //vCamChanged = false;
        }

        //Invoke("VCamActivate", 1);
        //Cinemachine.CinemachineBrain.SoloCamera = vCam;
        //if (debug) Debug.Log("GetVCam sceneNearClip " + sceneNearClip);
    }

    //void VCamActivate()
    //{
    //    //Cinemachine.CinemachineBrain.SoloCamera = vCam;
    //}

    void VCamCullingOff()
    {
        if (debug) Debug.Log("VCamCullingOff");

        //if (vCam != null)
        if (vCam != null && vCam.ToString() != "null")
        {
            //if (debug) Debug.Log("vCam != null vCam " + vCam);
            //cam.cullingMask = 2147483647;
            cam.cullingMask = 0;
            //cam.cullingMask = 1;
            //if (debug) Debug.Log(cam.cullingMask);
            //cam.Reset();
            //cam.nearClipPlane = -1; //Hack for more fps in SRP(Scriptable Render Pipeline)
            //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = -1;
            CameraDataStruct_Change();
        }
        else
            Invoke("VCamCullingOff", Time.deltaTime);
    }

    void VCamFOVSet()
    {
        if (debug) Debug.Log("VCamFOVSet");
        //FOVSetInProcess = true;

        if (vCam != null)
        {
            //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FieldOfView = vFOV;
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FieldOfView = cam.fieldOfView = vFOV;
            ////FOVSetInProcess = false;
            //CheckCamFOVSet();
        }
        else
            Invoke("VCamFOVSet", Time.deltaTime);
    }

    //void VCamSceneClipSet()
    //{
    //    if (debug) Debug.Log("VCamSceneClipSet cam.nearClipPlane " + cam.nearClipPlane);
    //    //vCamSceneClipSetInProcess = true;
    //    //vCamSceneClipIsReady = false;

    //    ////if (!nearClipHackApplyed && vCam != null)
    //    //if (vCam != null && !nearClipHackApplyed && !vCamSceneClipRestoreInProcess)
    //    //{
    //        sceneNearClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane;
    //        if (debug) Debug.Log("VCamSceneClipSet sceneNearClip " + sceneNearClip);
    //        sceneFarClip = ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane;
    //        //vCamSceneClipSetInProcess = false;
    //        vCamSceneClipIsReady = true;
    //    //}
    //    //else
    //    //{
    //    //    if (nearClipHackApplyed)
    //    //        VCamClipRestore();

    //    //    Invoke("VCamSceneClipSet", Time.deltaTime);
    //    //}
    //}

    //void VCamClipSet()
    //{
    //    if (debug) Debug.Log("VCamClipSet");
    //    //vCamClipSetInProcess = true;
    //    nearClipHackApplyed = false;

    //    //if (vCam != null)
    //    //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
    //    //else
    //    //    Invoke("VCamClipSet", Time.deltaTime);

    //    //if (vCam != null && !vCamSceneClipSetInProcess)
    //    if (vCamSceneClipIsReady)
    //    {
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane = nearClip; //todo
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane = cam.farClipPlane = farClip;
    //        if (debug) Debug.Log("VCamClipSet nearClip " + nearClip + " ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
    //        //vCamClipSetInProcess = false;
    //        ViewSet();
    //    }
    //    else
    //        //if (!vCamSceneClipRestoreInProcess)
    //            Invoke("VCamClipSet", Time.deltaTime);
    //        //else
    //        //    vCamClipSetInProcess = false;
    //}

    //void VCamNearClipHack()
    //{
    //    //if (debug) Debug.Log("VCamNearClipHack " + nearClipHackApplyed);
    //    if (debug) Debug.Log("VCamNearClipHack");
    //    //nearClipHackApplyed = true;

    //    //if (vCam != null && !vCamSceneClipSetInProcess && !vCamSceneClipRestoreInProcess)
    //    //if (vCamSceneClipIsReady)
    //    //{
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane = -1;
    //        if (debug) Debug.Log("VCamNearClipHack ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane + " " + Time.time);
    //        //nearClipHackApplyed = true;
    //        ViewSet();
    //    //}
    //    //else
    //    //    Invoke("VCamNearClipHack", Time.deltaTime);
    //}

    //void VCamClipRestore()
    //{
    //    if (debug) Debug.Log("VCamClipRestore sceneNearClip " + sceneNearClip);
    //    //vCamSceneClipRestoreInProcess = true;
    //    //nearClipHackApplyed = false;

    //    //if (vCam != null)
    //    //    ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
    //    //else
    //    //    Invoke("VCamClipRestore", Time.deltaTime);

    //    //if (vCam != null && !vCamClipSetInProcess)
    //    if (vCamSceneClipIsReady)
    //    {
    //        //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane = sceneNearClip; //restore vCam default NearClipPlane
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip; //restore vCam default NearClipPlane todo
    //        ClosestCamera_SceneNearClipSet();
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane = cam.farClipPlane = sceneFarClip; //restore vCam default FarClipPlane
    //        if (debug) Debug.Log("VCamClipRestore ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane);
    //        //vCamSceneClipRestoreInProcess = false;
    //        ViewSet();
    //    }
    //    else
    //        Invoke("VCamClipRestore", Time.deltaTime);
    //}
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
        //if (debug) Debug.Log(cursorLocalPos);
        //panel.GetComponent<RectTransform>().anchoredPosition = cursorLocalPos + posDiff;
        lastPanelPosition = panel.GetComponent<RectTransform>().anchoredPosition = cursorLocalPos + posDiff;
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

    void SlotName_DropdownSelect()
    {
        //if (debug) Debug.Log("SlotName_DropdownSelect");
        //SlotName_Dropdown(0);
        slotName = slotName_dropdown.captionText.text;

        if (slotName == "")
            slotName = "User1";

        //slotName = slotName_dropdown.options[slotName_dropdown.value].text;
    }

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

        //if (GUIAsOverlay && canvasCamera)
        if (GUIAsOverlay && canvasCamera)
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
                //canvasCamera.targetTexture = renderTexture_left; //required to clear the screen window when the main camera viewport rectangle is not fully occupied
//#if HDRP
//                //canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
//                canvasCamData_left.clearColorMode = canvasCamData_right.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
//#elif URP
//                //canvasCamera.clearFlags = CameraClearFlags.Nothing;
//                canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = CameraClearFlags.Nothing;
//#else
//                //canvasCamera.clearFlags = CameraClearFlags.Color;
//                canvasCamera_left.clearFlags = canvasCamera_right.clearFlags = CameraClearFlags.Color;
//#endif
                //canvasCamera.rect = Rect.MinMaxRect(0, 0, 1, 1);
                //canvasCamera_left.rect = canvasCamera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);

                //if (canvasCamera.targetTexture == null)
                //{
                //    if (debug) Debug.Log("canvasCamera.targetTexture == null");
                //    canvasCamera.targetTexture = renderTexture_left; //required to clear the screen window when the main camera viewport rectangle is not fully occupied
                //}

                //canvasCamera.enabled = true;
                canvasCamera.enabled = false;
                //canvasCamera_left.enabled = canvasCamera_right.enabled = true;

//#if !HDRP
#if !URP
                if (method == Method.Sequential && optimize)
                    canvasCamera_left.enabled = canvasCamera_right.enabled = false;
                else
#endif
                    canvasCamera_left.enabled = canvasCamera_right.enabled = true;
//#endif

                //canvasCamera.orthographicSize = canvasHalfSizeY;
                canvasCamera_left.orthographicSize = canvasCamera_right.orthographicSize = canvasHalfSizeY;
                ////canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, -1, 1);
                ////canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY, canvasHalfSizeY, canvasCamera.nearClipPlane, canvasCamera.farClipPlane);
                //canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX, canvasHalfSizeX, -canvasHalfSizeY, canvasHalfSizeY, canvasCamera.nearClipPlane, canvasCamera.farClipPlane);
                ////canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX - canvasHalfSizeX * -shift, canvasHalfSizeX - canvasHalfSizeX * -shift, -canvasHalfSizeY - canvasHalfSizeY * .5f, canvasHalfSizeY - canvasHalfSizeY * .5f, canvasCamera.nearClipPlane, canvasCamera.farClipPlane);

                //Matrix4x4 canvasCamMatrix_left = canvasCamera_left.projectionMatrix;
                //Matrix4x4 canvasCamMatrix_right = canvasCamera_right.projectionMatrix;
                //Matrix4x4 canvasCamMatrix = Matrix4x4.Ortho(-canvasHalfSizeX, canvasHalfSizeX, -canvasHalfSizeY, canvasHalfSizeY, canvasCamera.nearClipPlane, canvasCamera.farClipPlane);
                Matrix4x4 canvasCamMatrix_left;
                Matrix4x4 canvasCamMatrix_right;
                //canvasCamMatrix_left = canvasCamMatrix_right = canvasCamMatrix;
                canvasCamMatrix_left = canvasCamMatrix_right = Matrix4x4.Ortho(-canvasHalfSizeX, canvasHalfSizeX, -canvasHalfSizeY, canvasHalfSizeY, canvasCamera.nearClipPlane, canvasCamera.farClipPlane);

                if (method == Method.SideBySide_HMD)
                {
                    //canvasCamMatrix_left[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
                    //canvasCamMatrix_right[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
                    float shift = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
                    canvasCamMatrix_left[0, 3] = shift;
                    canvasCamMatrix_right[0, 3] = -shift;
                }
                else
                {
                    //canvasCamMatrix_left[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;
                    //canvasCamMatrix_right[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;
                    float shift = imageOffset * panelDepth * (swapLR ? -1 : 1);
                    canvasCamMatrix_left[0, 3] = -shift;
                    canvasCamMatrix_right[0, 3] = shift;

                  //  if (method == Method.Two_Displays_MirrorX)
                  //  {
                  //      canvasCamMatrix_right[0, 3] *= -1;
		                //canvasCamMatrix_right *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
                  //  }
                  //  else
                  //      if (method == Method.Two_Displays_MirrorY)
		                //    canvasCamMatrix_right *= Matrix4x4.Scale(new Vector3(1, -1, 1));
                }

                if (method == Method.Interlace_Horizontal)
                    canvasCamMatrix_left[1, 3] = -oneRowShift;
                else
                    canvasCamMatrix_left[1, 3] = 0;

                canvasCamera_left.projectionMatrix = canvasCamMatrix_left;
                canvasCamera_right.projectionMatrix = canvasCamMatrix_right;

                //if (debug) Debug.Log(canvasCamMatrix);
            }
            else
            {
                //canvasCamera.ResetProjectionMatrix();
                //canvasCamera.targetTexture = null;
                canvasCamera.enabled = true;
                canvasCamera_left.enabled = canvasCamera_right.enabled = false;
//#if HDRP
//                canvasCamData.clearColorMode = HDAdditionalCameraData.ClearColorMode.None;
//#elif !URP
//                //canvasCamera.clearFlags = CameraClearFlags.Nothing;
//                //#else
//                canvasCamera.clearFlags = CameraClearFlags.Depth;
//#endif
                canvasCamera.rect = cam.rect;
                //canvasCamera.enabled = false;
                //canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                //canvas.worldCamera = cam;
                canvas.worldCamera = canvasCamera;
                //canvas.GetComponent<CanvasScaler>().scaleFactor = windowSize.y / canvasSize.y;
                canvas.GetComponent<CanvasScaler>().scaleFactor = windowSize.y / canvasSize.y * canvasCamera.rect.height;
                //canvas.GetComponent<CanvasScaler>().scaleFactor = windowSize.y / canvasSize.y * cam.rect.height;
                //canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                //canvas.GetComponent<CanvasScaler>().referenceResolution = canvasSize;
                //if (debug) Debug.Log("canvasSize " + canvasSize + " windowSize " + windowSize);
                //canvas.planeDistance = cam.farClipPlane * .5f;
                canvas.planeDistance = canvasCamera.farClipPlane;
            }
        }
        else
        {
            //canvasLocalPosZ = screenDistance * panelDepth * virtualIPD / userIPD * 0.001f;
            float panelDepthAsScreenDistance = 1 / (1 - panelDepth);
            panelDepthAsScreenDistance = Mathf.Min(panelDepthAsScreenDistance, 100);
            //if (debug) Debug.Log("panelDepthAsScreenDistance " + panelDepthAsScreenDistance);
            canvasLocalPosZ = screenDistance * panelDepthAsScreenDistance * virtualIPD / userIPD * 0.001f;
            if (debug) Debug.Log("virtualIPD " + virtualIPD + " userIPD " + userIPD);
            if (debug) Debug.Log("canvasLocalPosZ " + canvasLocalPosZ);
            float canvasScale = canvasLocalPosZ * Mathf.Tan(FOV * Mathf.PI / 360) * 2 / canvasSize.x;
            //if (debug) Debug.Log("canvasScale " + canvasScale);
            //canvas.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, canvasLocalPosZ);
            //canvas.GetComponent<RectTransform>().localPosition = new Vector3(virtualIPD * .0005f * -((int)eyePriority - 1) * (S3DEnabled ? 1 : 0), 0, canvasLocalPosZ);
            canvas.GetComponent<RectTransform>().localPosition = new Vector3(virtualIPD * .0005f * -((int)eyePriority - 1) * (S3DEnabled ? 1 : 0) + camVanishPoint.x * .5f * canvasSize.x * canvas.transform.lossyScale.x, camVanishPoint.y * .5f * canvasSize.y * canvas.transform.lossyScale.y, canvasLocalPosZ);
            canvas.GetComponent<RectTransform>().localScale = new Vector3(canvasScale, canvasScale, canvasScale);

            //canvasRayCam.orthographicSize = canvasSize.y * canvas.transform.lossyScale.y * .5f;

            //if (!panelDepth_sliderIsDragging) //prevent flatter due canvas resize while slided dragging by pointer
            //    CanvasOffset_Set();
        }

        if (debug) Debug.Log("canvas.transform.lossyScale.y " + canvas.transform.lossyScale.y);
        canvasRayCam.orthographicSize = canvasSize.y * canvas.transform.lossyScale.y * .5f;
        //canvasHalfSizeY = canvasSize.y * canvas.transform.lossyScale.y * .5f;
        //canvasRayCam.orthographicSize = canvasHalfSizeY;
        //canvasCamMatrix = canvasCamera.projectionMatrix;

        if (!panelDepth_sliderIsDragging) //prevent flatter due canvas resize while slided dragging by pointer
            CanvasOffset_Set();

        //if (debug) Debug.Log(canvas.GetComponent<RectTransform>().localPosition);
        //if (debug) Debug.Log("Canvas_Set canvasLocalPosZ " + canvasLocalPosZ);
        //if (debug) Debug.Log("Canvas_Set canvasRayCam.orthographicSize " + canvasRayCam.orthographicSize);
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

        //if (debug) Debug.Log(canvasRayCam.aspect);
        if (debug) Debug.Log("Canvas_Set canvasEdgeOffset " + canvasWidthWithOffset);
    }

    float aspect;

    void Resize()
    {
        //if (debug) Debug.Log("Resize");
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

    //Rect pixelRect;

    void Aspect_Set()
    {
        //aspect = cam.aspect;
        aspect = cam.pixelWidth / (float)cam.pixelHeight;

        ////if (S3DEnabled && method == Method.SideBySide_HMD)
        //if (S3DEnabled && (method == Method.SideBySide_HMD || method == Method.SideBySide_Full))
        //    aspect *= .5f;

        if (S3DEnabled)
            if (method == Method.SideBySide_HMD || method == Method.SideBySide_Full)
                aspect *= .5f;
            else
            if (method == Method.OverUnder_Full)
                aspect *= 2;

        //cam.aspect = aspect;
        camera_left.aspect = camera_right.aspect = cam.aspect = aspect;

        //if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                    c.camera_left.aspect = c.camera_right.aspect = c.camera.aspect = aspect;

        //if (GUISizeKeep)
        //    canvasSize = new Vector2(cam.pixelWidth, cam.pixelWidth / aspect);
        //else
        //    canvasSize = new Vector2(canvasSize.y * aspect, canvasSize.y);

        if (GUISizeKeep)
            canvasSize = new Vector2(cam.pixelWidth, cam.pixelWidth / aspect);
        else
            canvasSize = new Vector2(canvasDefaultSize.y * aspect, canvasDefaultSize.y);

        //if (debug) Debug.Log("Aspect_Set Resize cam.rect " + cam.rect);
        if (debug) Debug.Log("Aspect_Set aspect " + aspect);
        //if (debug) Debug.Log("Aspect_Set cam.pixelWidth " + cam.pixelWidth + " cam.pixelHeight " + cam.pixelHeight);

        //if (method == Method.Two_Displays_MirrorX)
        //    pixelRect = new Rect((1 - cam.rect.x) * windowSize.x - cam.pixelWidth, cam.rect.y * windowSize.y, cam.pixelWidth, cam.pixelHeight);
        //else
        //    pixelRect = new Rect(cam.rect.x * windowSize.x, (1 - cam.rect.y) * windowSize.y - cam.pixelHeight, cam.pixelWidth, cam.pixelHeight);

        //ViewSet();
        FOV_Set();
        Render_Set();
        //Canvas_Set();
        //CanvasOffset_Set();
    }

    void S3DKeyHold()
    {
        S3DKeyTimer += Time.deltaTime;
        //if (debug) Debug.Log(S3DKeyTimer);

        if (S3DKeyTimer > 1 && !S3DKeyLoaded)
        {
            Load(slotName);
            //S3DKeyTimer = 0;
            S3DKeyLoaded = true;
            //if (debug) Debug.Log("S3DKeyLoaded");
        }

        GUI_Autoshow();
    }

    void S3DKeyUp(float modifier1, float modifier2, float modifier3)
    {
        if (S3DKeyTimer < 1)
            //if (modifier1 != 0 && modifier2 != 0 && method == Method.Two_Displays)
            if (modifier1 != 0 && modifier2 != 0 && (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY))
                //if (debug) Debug.Log("modifier1 != 0 & modifier2 != 0");
                TargetDisplays_Select();
            else
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
        if (debug) Debug.Log("GUI_Set");

        //if (canvas)
        //{
        //    if (debug) Debug.Log("GUI_Set canvas");
            //lastGUIOpened = GUIOpened;

            //if (hide2DCursor)
            //    Cursor.visible = false;

            //CursorLockMode cursorLockMode = Cursor.lockState;

            if (GUIVisible)
            {
                if (GUIOpened)
                {
#if LookWithMouse
                    if (lookWithMouseScript)
                        lookWithMouseScript.enabled = false;
#endif

//#if SimpleCameraController
//                    if (simpleCameraControllerScript)
//                        simpleCameraControllerScript.enabled = false;
//#endif

                    //if (inputSystem)
//#if STARTER_ASSETS_PACKAGES_CHECKED
//#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                    //{
                        cursorLockedDefault = starterAssetsInputs.cursorLocked;
                        cursorInputForLookDefault = starterAssetsInputs.cursorInputForLook;
                        starterAssetsInputs.cursorLocked = false;
                        starterAssetsInputs.cursorInputForLook = false;
                    //}
#endif
                    //else
                        cursorLockModeDefault = Cursor.lockState;
                        Cursor.lockState = CursorLockMode.None;

                    ////Cursor.visible = true;
                    //canvas.enabled = true;
                    //cursorRectTransform.GetComponent<Canvas>().enabled = true;
                    //canvas.gameObject.SetActive(true);
                    //GUIVisible = true;
                    cursorRectTransform.gameObject.SetActive(true);
                    canvas.GetComponent<CanvasGroup>().blocksRaycasts = true;
                }

                canvas.gameObject.SetActive(true);

                //if (!GUIAsOverlay)
                //if (PPI_inputField.transform.Find(PPI_inputField.name + " Input Caret"))
                //{
                //    InputFieldCaretMaterial_Set(PPI_inputField);
                //    InputFieldCaretMaterial_Set(userIPD_inputField);
                //    InputFieldCaretMaterial_Set(virtualIPD_inputField);
                //    InputFieldCaretMaterial_Set(FOV_inputField);
                //    InputFieldCaretMaterial_Set(panelDepth_inputField);
                //    InputFieldCaretMaterial_Set(screenDistance_inputField);
                //    InputFieldCaretMaterial_Set(slotName_inputField);
                //}
                //else
                //    Invoke("GUI_Set", Time.deltaTime); //try to get Caret in the next frame
                GUIMaterial_Set();
#if HDRP
                //if (method == Method.Two_Displays && (additionalS3DTopmostCameraIndex != -1 || GUIAsOverlay))
                if (GUIAsOverlay)
                {
                    if (!S3DEnabled)
                        cam.targetTexture = renderTexture;

                    camera_left.targetTexture = renderTexture_left;
                    camera_right.targetTexture = renderTexture_right;
                }
#endif
            }
            else
            {
                //canvas.enabled = false;
                //cursorRectTransform.GetComponent<Canvas>().enabled = false;
                canvas.gameObject.SetActive(false);
                //GUIVisible = false;
                cursorRectTransform.gameObject.SetActive(false);
                canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;
                tooltip.SetActive(false);

                //if (inputSystem)
                //{
                //    starterAssetsInputs.cursorLocked = cursorLockedDefault;
                //    starterAssetsInputs.cursorInputForLook = cursorInputForLookDefault;
                //}
                //else
                //    Cursor.lockState = cursorLockModeDefault;

                if (lastGUIOpened)
                    CursorRestore();

                //Cursor.lockState = CursorLockMode.Locked;
                ////Cursor.visible = false;

#if LookWithMouse
                if (lookWithMouseScript)
                    lookWithMouseScript.enabled = true;
#endif

//#if SimpleCameraController
//                if (simpleCameraControllerScript)
//                    simpleCameraControllerScript.enabled = true;
//#endif

////#if URP
#if HDRP
                //if (method == Method.Two_Displays && additionalS3DTopmostCameraIndex == -1)
                if (GUIAsOverlay && additionalS3DTopmostCameraIndex == -1)
                {
                    cam.targetTexture = null;

                    if (!S3DEnabled || method == Method.Two_Displays)
                    {
                        camera_left.targetTexture = null;
                        camera_right.targetTexture = null;
                    }
                }
#endif
            }

            lastGUIOpened = GUIOpened;
            TopMostCamera_Set();
            Clip_Set();
            HDRPSettings_Set();
            //Render_Set();

            //if (S3DEnabled)
                RenderTextureContextSet();
        //}
#if URP
        CameraStackSet();
#endif
    }

    void GUIMaterial_Set()
    {
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
            Invoke("GUIMaterial_Set", Time.deltaTime); //try to get Caret in the next frame
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
            //if (debug) Debug.Log(field + " Input Caret is not set");
            //Invoke("GUI_Set", Time.deltaTime); //try to get Caret in the next frame
            Invoke("GUIMaterial_Set", Time.deltaTime); //try to get Caret in the next frame
        }
    }

    void CursorRestore()
    {
        Debug.Log("CursorRestore");

//if (inputSystem)
//#if STARTER_ASSETS_PACKAGES_CHECKED
//#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#if STARTER_ASSETS_PACKAGES_CHECKED || UNITY_2022_1_OR_NEWER && INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        {
            starterAssetsInputs.cursorLocked = cursorLockedDefault;
            starterAssetsInputs.cursorInputForLook = cursorInputForLookDefault;
        }
#endif
        //else
            Cursor.lockState = cursorLockModeDefault;
    }

    void Clip_Set()
    {
        //if (debug) Debug.Log("Clip_Set canvasLocalPosZ " + canvasLocalPosZ);

        //if (canvasLocalPosZ < nearClip * 2)
        if (!GUIAsOverlay && GUIVisible && canvasLocalPosZ < sceneNearClip * 2)
        {
            nearClip = canvasLocalPosZ * .5f;
            //if (debug) Debug.Log("Clip_Set canvasLocalPosZ < nearClip");
        }
        else
            //if (!cineMachineEnabled || cineMachineEnabled && vCam != null)
            nearClip = sceneNearClip;
            //else
            //{
            //    Invoke("Clip_Set", Time.deltaTime);
            //    return;
            //}

        if (!GUIAsOverlay && GUIVisible && canvasLocalPosZ > sceneFarClip * .8f)
        {
            farClip = canvasLocalPosZ * 1.25f;
            //farClip = canvasLocalPosZ * 2f;
            //farClip = canvasLocalPosZ;
            //if (debug) Debug.Log("Clip_Set canvasLocalPosZ < nearClip");
        }
        else
            farClip = sceneFarClip;

        //if (debug) Debug.Log("Clip_Set sceneNearClip " + sceneNearClip);
        //if (debug) Debug.Log("Clip_Set sceneFarClip " + sceneFarClip);
        if (debug) Debug.Log("Clip_Set sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);

        //if (S3DEnabled)
        //{
        //    camera_left.nearClipPlane = camera_right.nearClipPlane = nearClip;
        //    camera_left.ResetProjectionMatrix();
        //    camera_right.ResetProjectionMatrix();
        //}
        //else
        //{
        //    if (cineMachineEnabled)
        //        VCamClipSet();
        //    else
        //        cam.nearClipPlane = nearClip;
        //}

        //camera_left.nearClipPlane = camera_right.nearClipPlane = nearClip;

        //if (additionalS3DCameras.Count != 0)
        if (additionalS3DTopmostCameraIndex != -1)
        {
            //additionalS3DCamerasStruct[additionalS3DCameras.Count - 1].camera_left.nearClipPlane = additionalS3DCamerasStruct[additionalS3DCameras.Count - 1].camera_right.nearClipPlane = nearClip;
            additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_left.nearClipPlane = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_right.nearClipPlane = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera.nearClipPlane = nearClip;
            cam.nearClipPlane = cameraNearClip; //restore camera original nearClipPlane if additionalS3DCameras.Count != 0
        }
        else
            camera_left.nearClipPlane = camera_right.nearClipPlane = cam.nearClipPlane = nearClip;

//#if CINEMACHINE
//        if (cineMachineEnabled)
//            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane;
//#endif
        //VCamClip_Sync();

        //camera_left.farClipPlane = camera_right.farClipPlane = farClip;
        camera_left.farClipPlane = camera_right.farClipPlane = cam.farClipPlane = farClip;
        //VCamClip_Sync();

        //camera_left.ResetProjectionMatrix();
        //camera_right.ResetProjectionMatrix();

        //if (!cam.usePhysicalProperties)
        //{
        //    camera_left.ResetProjectionMatrix();
        //    camera_right.ResetProjectionMatrix();
        //}

        //if (!S3DEnabled)
        //    if (cineMachineEnabled)
        //        VCamClipSet();
        //    else
        //        cam.nearClipPlane = nearClip;

        //        if (S3DEnabled)
        //        {
        //            if (nearClipHack)
        //            {
        //#if CINEMACHINE
        //                if (cineMachineEnabled)
        //                    VCamNearClipHack();
        //                else
        //#endif
        //                    cam.nearClipPlane = -1;
        //            }
        //            else
        //            {
        //#if CINEMACHINE
        //                if (cineMachineEnabled)
        //                    VCamClipRestore();
        //                else
        //#endif
        //                    cam.nearClipPlane = sceneNearClip;
        //            }
        //        }
        //        else
        //        {
        //            //nearClipHackApplyed = false;
        //            //VCamClipRestore();

        //#if CINEMACHINE
        //            if (cineMachineEnabled)
        //                VCamClipSet();
        //            else
        //#endif
        //            {
        //                cam.nearClipPlane = nearClip;
        //                cam.farClipPlane = farClip;
        //            }
        //        }

        //#if CINEMACHINE
        //        if (S3DEnabled)
        //            if (nearClipHack)
        //                if (cineMachineEnabled)
        //                    VCamNearClipHack();
        //                else
        //                    cam.nearClipPlane = -1;
        //            else
        //                if (cineMachineEnabled)
        //                    VCamClipRestore();
        //                else
        //                    //cam.nearClipPlane = sceneNearClip;
        //                    ClosestCamera_SceneNearClipSet();
        //        else
        //            if (cineMachineEnabled)
        //                VCamClipSet();
        //            else
        //            {
        //                //cam.nearClipPlane = nearClip;
        //                ClosestCamera_NearClipSet(); //todo check
        //                cam.farClipPlane = farClip;
        //            }

        //if (S3DEnabled)
        //    if (nearClipHack)
        //        if (cineMachineEnabled)
        //            if (debug) Debug.Log("S3DEnabled nearClipHack cineMachineEnabled");
        //        else
        //            if (debug) Debug.Log("S3DEnabled nearClipHack !cineMachineEnabled");
        //    else
        //        if (cineMachineEnabled)
        //            if (debug) Debug.Log("S3DEnabled !nearClipHack cineMachineEnabled");
        //        else
        //            if (debug) Debug.Log("S3DEnabled !nearClipHack !cineMachineEnabled");
        //else
        //    if (cineMachineEnabled)
        //        if (debug) Debug.Log("!S3DEnabled cineMachineEnabled");
        //    else
        //        if (debug) Debug.Log("!S3DEnabled !cineMachineEnabled");
        //#else
        //if (S3DEnabled)
        //    if (nearClipHack)
        //    {
        //        camNearClip = cam.nearClipPlane;
        //        cam.nearClipPlane = -1;
        //    }
        //    else
        //    {
        //        //cam.nearClipPlane = sceneNearClip;
        //        if (cam.nearClipPlane == -1)
        //            cam.nearClipPlane = camNearClip;
        //    }
        //else
        //{
        //    //cam.nearClipPlane = nearClip;
        //    ClosestCamera_NearClipSet();
        //    cam.farClipPlane = farClip;
        //}

        if (S3DEnabled && nearClipHack)
//#if CINEMACHINE
//            VCamNearClipHack();
//#else
            cam.nearClipPlane = -1;
//#endif
        //else
        //    //cam.nearClipPlane = camera_left.nearClipPlane; //restore camera original nearClipPlane if additionalS3DCameras.Count != 0
        //    cam.nearClipPlane = cameraNearClip; //restore camera original nearClipPlane if additionalS3DCameras.Count != 0
//#endif

        //lastCamNearClip = cam.nearClipPlane;
#if CINEMACHINE
        VCamClip_Sync();
#endif
        ViewSet();
    }

    void EnableS3D_Toggle(bool isOn)
    {
        S3DEnabled = isOn;
        //Render_Set();
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
        //Render_Set();
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
    //    //Render_Set();
    //}

    //void Vertical_Button()
    //{
    //    method = Method.Interlace_Vertical;

    //    //ViewSet();
    //    //Render_Set();
    //}

    //void Checkerboard_Button()
    //{
    //    method = Method.Interlace_Checkerboard;

    //    //ViewSet();
    //    //Render_Set();
    //}

    //void SideBySide_Button()
    //{
    //    method = Method.SideBySide;

    //    //ViewSet();
    //    //Render_Set();
    //}

    //void OverUnder_Button()
    //{
    //    method = Method.OverUnder;

    //    //ViewSet();
    //    //Render_Set();
    //}

    //void Anaglyph_Button()
    //{
    //    method = Method.Anaglyph_RedCyan;

    //    //ViewSet();
    //    //Render_Set();
    //}

    void PPI_InputField(string fieldString)
    {
        ////float value = Convert.ToSingle(fieldString);
        ////value = Mathf.Clamp(value, 1, 1000);
        ////if (debug) Debug.Log(value);
        ////PPI_inputField.text = value.ToString();
        ////PPI = Mathf.Clamp(Convert.ToSingle(fieldString), 1, 1000);
        ////PPI = Convert.ToSingle(fieldString);
        //PPI = float.Parse(fieldString);
        ////PPI_inputField.text = PPI.ToString();

        if (float.TryParse(fieldString, out float value))
            PPI = value;
        else
            PPI_inputField.text = PPI.ToString();

        //ViewSet();
    }

    void UserIPD_Slider(float value)
    {
        userIPD = value;
    }

    void UserIPD_InputField(string fieldString)
    {
        ////userIPD = Convert.ToSingle(fieldString);
        //userIPD = float.Parse(fieldString);

        if (float.TryParse(fieldString, out float value))
            userIPD = value;
        else
            userIPD_inputField.text = userIPD.ToString();
    }

    void VirtualIPD_Slider(float value)
    {
        virtualIPD = value;
    }

    void VirtualIPD_InputField(string fieldString)
    {
        ////virtualIPD = Convert.ToSingle(fieldString);
        //virtualIPD = float.Parse(fieldString);

        if (float.TryParse(fieldString, out float value))
            virtualIPD = value;
        else
            virtualIPD_inputField.text = virtualIPD.ToString();
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
        ////FOV = Convert.ToSingle(fieldString);
        //FOV = float.Parse(fieldString);

        if (float.TryParse(fieldString, out float value))
            FOV = value;
        else
            FOV_inputField.text = FOV.ToString();
    }

    void PanelDepth_Slider(float value)
    {
        //if (debug) Debug.Log("PanelDepth_Slider " + value);
        panelDepth = value;
    }

    void PanelDepth_InputField(string fieldString)
    {
        //if (debug) Debug.Log("PanelDepth_InputField " + fieldString);
        //panelDepth = Convert.ToSingle(fieldString);
        //panelDepth = float.Parse(fieldString);

        if (float.TryParse(fieldString, out float value))
            panelDepth = value;
        else
            panelDepth_inputField.text = panelDepth.ToString();
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
        //if (debug) Debug.Log(itemNumber);
        //slotName = slotName_dropdown.options[itemNumber].text;
        slotName = slotName_dropdown.captionText.text;
        //Load(slotName);
    }

    void OutputMethod_Dropdown(int itemNumber)
    {
       //if (debug) Debug.Log("OutputMethod_Dropdown");
        method = (Method)itemNumber;
    }

    void Save_Button()
    {
        Save(slotName);
        //SaveUserName(slotName);
    }

    void Load_Button()
    {
        Load(slotName);
    }

    void FieldPointerEnter(InputField field)
    {
       //if (debug) Debug.Log("Pointer Enter");
        field.ActivateInputField();
    }

    void FieldPointerExit(InputField field)
    {
       //if (debug) Debug.Log("Pointer Exit");
        field.DeactivateInputField();
    }

    //void PointerClick(Dropdown dropdown)
    //{
    //   //if (debug) Debug.Log("Pointer Click");
    //    trigger = dropdown.transform.Find("Dropdown List").gameObject.AddComponent<EventTrigger>();
    //    EventTrigger.Entry entry = new EventTrigger.Entry();
    //    entry.eventID = EventTriggerType.PointerExit;
    //    entry.callback.AddListener((eventData) => { ListPointerExit(dropdown); });
    //    trigger.triggers.Add(entry);
    //}

    //void ListPointerExit(Dropdown dropdown)
    //{
    //   //if (debug) Debug.Log("Dropdown Pointer Exit");
    //    dropdown.Hide();
    //}

    void VSyncSet()
    {
        //Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = vSync ? 1 : 0;
        //int targetFPS = -1 + (vSync ? Screen.currentResolution.refreshRate + 1 : 0);
        //int targetFPS = vSync ? Screen.currentResolution.refreshRate : -1;
        //Application.targetFrameRate = targetFPS;
//#if UNITY_2022_1_OR_NEWER
//        Application.targetFrameRate = vSync ? (int)Screen.currentResolution.refreshRateRatio.value : -1;
//#else
//        Application.targetFrameRate = vSync ? Screen.currentResolution.refreshRate : -1;
//#endif

        Application.targetFrameRate = vSync ? 
#if UNITY_2022_1_OR_NEWER
            (int)Screen.currentResolution.refreshRateRatio.value 
#else
            Screen.currentResolution.refreshRate
#endif
            : -1;

        //if (debug) Debug.Log("VSyncSet " + Application.targetFrameRate);
    }

    void PPISet()
    {
        //PPI = Mathf.Max(PPI, 1);
        PPI = Mathf.Clamp(PPI, 1, 1000);
        //pixelPitch = 25.4f / PPI;
        PPI_inputField.text = PPI.ToString();
        lastPPI = PPI;

        //ViewSet();
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
        //userIPD = Mathf.Clamp(userIPD, 0.01f, 100);

        if (GUIAsOverlay)
            userIPD = Mathf.Clamp(userIPD, 0, 100);
        else
            userIPD = Mathf.Clamp(userIPD, .01f, 100);

        userIPD_slider.value = userIPD;
        //userIPD_inputField.text = Convert.ToString(userIPD);
        userIPD_inputField.text = userIPD.ToString();
        lastUserIPD = userIPD;

        if (matchUserIPD)
            VirtualIPDSet();
        //else
            //CamSet();
    }

    void VirtualIPDSet()
    {
		if (matchUserIPD)
			virtualIPD = userIPD;
        else
            //virtualIPD = Mathf.Clamp(virtualIPD, 0.01f, virtualIPDMax);
            if (GUIAsOverlay)
                virtualIPD = Mathf.Clamp(virtualIPD, 0, virtualIPDMax);
            else
                virtualIPD = Mathf.Clamp(virtualIPD, .01f, virtualIPDMax);


        virtualIPD_slider.value = virtualIPD;
        //virtualIPD_inputField.text = Convert.ToString(virtualIPD);
        virtualIPD_inputField.text = virtualIPD.ToString();
        lastVirtualIPD = virtualIPD;

        //CamSet();
    }

    float scaleX;
    float scaleY;

    void FOV_Set()
    {
        //FOV = Mathf.Clamp(FOV, 1, 179);
        //aspect = cam.aspect;

        if (FOVControl && !camFOVChangedExternal)
        {
            FOV = Mathf.Clamp(FOV, FOVMinMax.x, FOVMinMax.y);
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * aspect;
            vFOV = 360 * Mathf.Atan(1 / scaleY) / Mathf.PI;

            //cam.fieldOfView = vFOV;
            //if (debug) Debug.Log("FOV_Set FOVControl && !camFOVChangedExternal FOV " + FOV + " vFOV " + vFOV);

#if CINEMACHINE
            if (cineMachineEnabled)
                VCamFOVSet();
            else
#endif
                cam.fieldOfView = vFOV;

            //if (debug) Debug.Log("FOV_Set FOVControl && !camFOVChangedExternal FOV " + FOV + " vFOV " + vFOV + " cineMachineEnabled " + cineMachineEnabled + " cam.fieldOfView " + cam.fieldOfView);
        }
        else
        {
            camFOVChangedExternal = false;
            vFOV = cam.fieldOfView;
            FOV = Mathf.Atan(aspect * Mathf.Tan(vFOV * Mathf.PI / 360)) * 360 / Mathf.PI;
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * aspect;
            //if (debug) Debug.Log("!(FOV_Set FOVControl && !camFOVChangedExternal) FOV " + FOV + " vFOV " + vFOV);
        }

        if (debug) Debug.Log("FOV_Set " + FOV + " vFOV " + vFOV);
        //Debug.Break();
        //if (debug) Debug.Log("FOV_Set cam.fieldOfView " + cam.fieldOfView + " vFOV " + vFOV);
        //camera_left.fieldOfView = camera_right.fieldOfView = vFOV;

        FOV_slider.value = FOV;
        //FOV_inputField.text = Convert.ToString(FOV);
        FOV_inputField.text = FOV.ToString();
        lastFOV = FOV;

        ViewSet();
    }

    //void CheckCamFOVSet()
    //{
    //    if (debug) Debug.Log("CheckCamFOVSet");

    //    if (cam.fieldOfView == vFOV)
    //        FOVSetInProcess = false;
    //    else
    //        Invoke("CheckCamFOVSet", Time.deltaTime);
    //}

    //void GetFOVFromCam()
    //{
    //    //if (debug) Debug.Log("GetFOVFromCam");
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
            camera_left.transform.localPosition = rightCamPos;
            camera_right.transform.localPosition = leftCamPos;

            //if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.camera_left.transform.localPosition = rightCamPos;
                        c.camera_right.transform.localPosition = leftCamPos;
                    }
        }
        else
        {
            camera_left.transform.localPosition = leftCamPos;
            camera_right.transform.localPosition = rightCamPos;

            //if (additionalS3DCamerasStruct != null)
                foreach (var c in additionalS3DCamerasStruct)
                    if (c.camera)
                    {
                        c.camera_left.transform.localPosition = leftCamPos;
                        c.camera_right.transform.localPosition = rightCamPos;
                    }
        }

        ViewSet();
    }

    //Camera clearCamera;

    //void CamRect_Set()
    //{
    //    camera_left.rect = camera_right.rect = new Rect(0, 0, Mathf.Max(1 / cam.rect.width * (1 - cam.rect.x), 1), Mathf.Max(1 / cam.rect.height * (1 - cam.rect.y), 1));
    //    //if (debug) Debug.Log("CamRect_Set");

    //    //if (cam.rect.x > 0 || cam.rect.xMax > 1 || cam.rect.y > 0 || cam.rect.yMax > 1)
    //    //{

    //    //    if (!clearCamera)
    //    //    {
    //    //        clearCamera = new GameObject("clearCamera").AddComponent<Camera>();
    //    //        clearCamera.CopyFrom(canvasRayCam);
    //    //    }
    //    //}

    //    //foreach (var c in additionalS3DCamerasStruct)
    //    //    if (c.camera)
    //    //        c.camera.rect = cam.rect;
    //}

    //float shift;
    float screenDistance;
    float imageWidth;
    float oneRowShift;
    Matrix4x4 camMatrix;

    void ViewSet()
    {
        //if (cam.tag == "MainCamera" && (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY))
        //    if (eyePriority == EyePriority.Left)
        //    {
        //        camera_left.tag = "MainCamera"; //prevent TerrainsVisibilityUpdater(in "Test Track" sample) throw errors while the main camera disabled and no other camera with "MainCamera" tag in the scene
        //        camera_right.tag = "Untagged";
        //    }
        //    else
        //    {
        //        camera_left.tag = "Untagged";
        //        camera_right.tag = "MainCamera"; //prevent TerrainsVisibilityUpdater(in "Test Track" sample) throw errors while the main camera disabled and no other camera with "MainCamera" tag in the scene
        //    }

        //imageWidth = cam.pixelWidth * pixelPitch; //real size of rendered image on screen
        imageWidth = cam.pixelWidth * 25.4f / PPI; //real size of rendered image on screen

        if (method == Method.SideBySide_HMD)
            imageWidth *= .5f;

        //shift = imageOffset = userIPD / imageWidth; //shift optic axis relative to the screen size (UserIPD/screenSize)
        float shift = imageOffset = userIPD / imageWidth; //shift optic axis relative to the screen size (UserIPD/screenSize)

        if (method == Method.SideBySide_HMD)
            shift = -1 + shift;

        //float oneRowShift = 0;
        //oneRowShift = 0;

        if (!optimize && method == Method.Interlace_Horizontal)
            oneRowShift = 2f / cam.pixelHeight;
        else
            oneRowShift = 0;

        //if (debug) Debug.Log(oneRowShift);

        screenDistance = scaleX * imageWidth * .5f; //calculated distance to screen from user eyes where real FOV will match to virtual for realistic view
        screenDistance_inputField.text = screenDistance.ToString();

        camera_left.fieldOfView = camera_right.fieldOfView = vFOV;

        foreach (var c in additionalS3DCamerasStruct)
            if (c.camera)
                c.camera_left.fieldOfView = c.camera_right.fieldOfView = c.camera.fieldOfView = vFOV;

        //if (setMatrixDirectly)
        if (!cam.usePhysicalProperties)
        {
            //if (cam.projectionMatrix != Matrix4x4.zero)
            //{
            //    if (debug) Debug.Log("cam.projectionMatrix != Matrix4x4.zero");
            //    camMatrix = cam.projectionMatrix;

            //    if (S3DEnabled && matrixKillHack)
            //        cam.projectionMatrix = Matrix4x4.zero;
            //}

            //if (matrixKillHack && S3DEnabled && cam.projectionMatrix != Matrix4x4.zero)
            //{
            //    if (debug) Debug.Log("cam.projectionMatrix != Matrix4x4.zero");
            //    camMatrix = cam.projectionMatrix;
            //    if (debug) Debug.Log(camMatrix);
            //    cam.projectionMatrix = Matrix4x4.zero;
            //}

            //if (matrixKillHack && S3DEnabled)
            //{
            //    //if (cam.projectionMatrix != Matrix4x4.zero)
            //    if (cam.projectionMatrix[3, 2] != 0)
            //    {
            //        //if (debug) Debug.Log("cam.projectionMatrix != Matrix4x4.zero");
            //        if (debug) Debug.Log("cam.projectionMatrix[3, 2] != 0");
            //        //camMatrix = cam.projectionMatrix;
            //        if (debug) Debug.Log("camMatrix\n" + camMatrix);
            //        cam.projectionMatrix = Matrix4x4.zero;
            //    }
            //}
            //else
            //    //if (cam.projectionMatrix == Matrix4x4.zero)
            //    if (cam.projectionMatrix[3, 2] == 0)
            //        cam.projectionMatrix = camMatrix;

            //if (S3DEnabled)
            //{
            //    if (matrixKillHack)
            //        cam.projectionMatrix = Matrix4x4.zero;
            //    else
            //        if (nearClipHack)
            //        {
            //            cam.projectionMatrix = camMatrix;
            //            Matrix_Set(cam, 0, 0);
            //        }
            //        else
            //            cam.projectionMatrix = camMatrix;
            //}
            //else
            //    cam.projectionMatrix = camMatrix;

            //cam.projectionMatrix = camMatrix;

            //if (cam.projectionMatrix != Matrix4x4.zero)
            //    camMatrix = cam.projectionMatrix;

            //if (S3DEnabled)
            //    if (matrixKillHack)
            //        cam.projectionMatrix = Matrix4x4.zero;
            //    else
            //        if (nearClipHack)
            //            Matrix_Set(cam, 0, 0);

            //if (S3DEnabled)
            //{
            //    if (nearClipHack)
            //        Matrix_Set(cam, 0, 0);

            //    if (matrixKillHack)
            //        cam.projectionMatrix = Matrix4x4.zero;
            //}

            //if (matrixKillHack)
            //    cam.projectionMatrix = Matrix4x4.zero;

            //set "shift" via matrix give fps gain from 304 to 308
            //if (swapLR)
            //{
            //    //camera_left.projectionMatrix = Matrix_Set(camera_left.projectionMatrix, -shift, oneRowShift);
            //    //camera_right.projectionMatrix = Matrix_Set(camera_right.projectionMatrix, shift, 0);
            //    Matrix_Set(camera_left, -shift, oneRowShift);
            //    Matrix_Set(camera_right, shift, 0);

            //    //if (additionalS3DCamerasStruct != null)
            //    foreach (var c in additionalS3DCamerasStruct)
            //            if (c.camera)
            //            {
            //                //c.camera_left.projectionMatrix = Matrix_Set(c.camera_left.projectionMatrix, -shift, oneRowShift);
            //                //c.camera_right.projectionMatrix = Matrix_Set(c.camera_right.projectionMatrix, shift, 0);
            //                Matrix_Set(c.camera_left, -shift, oneRowShift);
            //                Matrix_Set(c.camera_right, shift, 0);
            //            }
            //}
            //else
            //{
            //camera_left.projectionMatrix = Matrix_Set(camera_left.projectionMatrix, shift, oneRowShift);
            //camera_right.projectionMatrix = Matrix_Set(camera_right.projectionMatrix, -shift, 0);
            //camMatrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
            //camMatrix[1, 1] = scaleY; //1/tangent of half vertical FOV
            //Matrix_Set(cam, 0, 0);

            if (S3DEnabled && matrixKillHack)
                cam.projectionMatrix = Matrix4x4.zero;
            else
            {
                if (cam.projectionMatrix == Matrix4x4.zero) //restore cam.projectionMatrix when return from matrixKillHack
                    cam.projectionMatrix = camMatrix;

                Matrix_Set(cam, 0, 0);
                camMatrix = cam.projectionMatrix;
                if (debug) Debug.Log("ViewSet camMatrix\n" + camMatrix);
                //CamMatrix_Reset();
            }

            Matrix_Set(camera_left, shift, oneRowShift);
            //Matrix_Set(camera_right, -shift, 0);
            Matrix_Set(camera_right, shift, 0);

            //if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                {
                    //c.camera.ResetProjectionMatrix();
                    //Matrix4x4 m = c.camera.projectionMatrix;
                    //m[0, 0] = cam.projectionMatrix[0, 0];
                    //m[0, 2] = cam.projectionMatrix[0, 2];
                    //m[1, 1] = cam.projectionMatrix[1, 1];
                    //m[1, 2] = cam.projectionMatrix[1, 2];
                    //c.camera.projectionMatrix = m;
                    //c.camera_left.projectionMatrix = Matrix_Set(c.camera_left.projectionMatrix, shift, oneRowShift);
                    //c.camera_right.projectionMatrix = Matrix_Set(c.camera_right.projectionMatrix, -shift, 0);
                    Matrix_Set(c.camera, 0, 0);
                    Matrix_Set(c.camera_left, shift, oneRowShift);
                    //Matrix_Set(c.camera_right, -shift, 0);
                    Matrix_Set(c.camera_right, shift, 0);
                }
            //}
        }
        else
        {
            ////return matrix control to camera settings
            //camera_left.ResetProjectionMatrix();
            //camera_right.ResetProjectionMatrix();

            ////set "shift" via cam "lensShift" required set "physical" cam
            //camera_left.usePhysicalProperties = camera_right.usePhysicalProperties = true; //need set again after ResetProjectionMatrix
            camera_left.sensorSize = camera_right.sensorSize = cam.sensorSize = new Vector2(imageWidth, imageWidth / aspect);
            camera_left.gateFit = camera_right.gateFit = cam.gateFit = Camera.GateFitMode.None;
            //cam.focalLength = camera_left.focalLength = camera_right.focalLength = screenDistance;

            Vector2 lensShift = new Vector2(-shift * .5f, 0);

            if (swapLR)
                lensShift.x *= -1;

            if (!optimize && method == Method.Interlace_Horizontal)
                lensShift.y = -oneRowShift * .5f;

            //camera_left.lensShift = -lensShift;
            //camera_right.lensShift = new Vector2(lensShift.x, 0);
            camera_left.lensShift = cam.lensShift - lensShift;
            camera_right.lensShift = cam.lensShift + new Vector2(lensShift.x, 0);

            //if (additionalS3DCamerasStruct != null)
            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                {
                    //c.camera_left.ResetProjectionMatrix();
                    //c.camera_right.ResetProjectionMatrix();
                    //c.camera_left.usePhysicalProperties = c.camera_right.usePhysicalProperties = true; //need set again after ResetProjectionMatrix
                    c.camera_left.usePhysicalProperties = c.camera_right.usePhysicalProperties = c.camera.usePhysicalProperties = true; //need set again after ResetProjectionMatrix
                    c.camera_left.sensorSize = c.camera_right.sensorSize = c.camera.sensorSize = new Vector2(imageWidth, imageWidth / aspect);
                    c.camera_left.gateFit = c.camera_right.gateFit = c.camera.gateFit = Camera.GateFitMode.None;
                    //c.camera_left.lensShift = -lensShift;
                    //c.camera_right.lensShift = new Vector2(lensShift.x, 0);
                    c.camera.lensShift = cam.lensShift;
                    c.camera_left.lensShift = c.camera.lensShift - lensShift;
                    c.camera_right.lensShift = c.camera.lensShift + new Vector2(lensShift.x, 0);
                }
        }

        ////if (debug) Debug.Log("ViewSet vFOV " + vFOV);
        ////camera_left.fieldOfView = camera_right.fieldOfView = cam.fieldOfView = vFOV;
        //camera_left.fieldOfView = camera_right.fieldOfView = vFOV;

        ////if (additionalS3DCamerasStruct != null)
        //    foreach (var c in additionalS3DCamerasStruct)
        //        if (c.camera)
        //            c.camera_left.fieldOfView = c.camera_right.fieldOfView = c.camera.fieldOfView = vFOV;

        CameraDataStruct_Change();
        Canvas_Set();
        if (debug) Debug.Log("ViewSet cam.projectionMatrix\n" + cam.projectionMatrix);
    }

    //Matrix4x4 Matrix_Set(Matrix4x4 matrix, float shiftX, float shiftY)
    //{	
    //    matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
    //    //matrix[0, 2] = shiftX; //shift whole image projection in X axis of screen clip space
    //    matrix[0, 2] = cam.projectionMatrix[0, 2] + shiftX; //shift whole image projection in X axis of screen clip space
    //    matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
    //    //matrix[1, 2] = shiftY;
    //    matrix[1, 2] = cam.projectionMatrix[1, 2] + shiftY;

    //    return matrix;
    //}

    void Matrix_Set(Camera c, float shiftX, float shiftY)
    {
        //if (debug) Debug.Log("Matrix_Set");
        Matrix4x4 matrix;

        if (c.name.Contains("_left") || c.name.Contains("_right"))
        {
            //Matrix4x4 matrix = c.projectionMatrix;
            Camera parent = c.transform.parent.GetComponent<Camera>();
            //Matrix4x4 matrix = parent.projectionMatrix;
            //matrix = parent.projectionMatrix;

            if (parent == cam)
            {
                matrix = camMatrix;
                //matrix_Set();

                //if (matrixKillHack)
                if (cam.projectionMatrix == Matrix4x4.zero)
                {
                    //matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
                    //matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
                    //matrix[3, 2] = -1;
                    matrixFOV_Set();
                    matrixClip_Set();
                }
                else
                    if (cam.nearClipPlane < 0)
                        matrixClip_Set();

                //float nearClip = c.nearClipPlane;
                //float farClip = c.farClipPlane;
                //matrix[2, 2] = -(farClip + nearClip) / (farClip - nearClip);
                //matrix[2, 3] = -(2 * farClip * nearClip) / (farClip - nearClip);
                ////if (debug) Debug.Log("nearClip " + nearClip + " farClip " + farClip);
                //if (debug) Debug.Log(c.name + " Matrix_Set " + matrix);
            }
            else
            {
                matrix = parent.projectionMatrix;
                //matrix[2, 2] = -(parent.farClipPlane + parent.nearClipPlane) / (parent.farClipPlane - parent.nearClipPlane);
                //matrix[2, 3] = -(2 * parent.farClipPlane * parent.nearClipPlane) / (parent.farClipPlane - parent.nearClipPlane);
            }

            //matrix[0, 2] = parent.projectionMatrix[0, 2] + shiftX; //shift whole image projection in X axis of screen clip space
            //matrix[0, 2] += shiftX; //shift whole image projection in X axis of screen clip space
            matrix[0, 2] += shiftX * (c.name.Contains("_left") ? 1 : -1) * (swapLR ? -1 : 1); //shift whole image projection in X axis of screen clip space
            //matrix[1, 2] = parent.projectionMatrix[1, 2] + shiftY;
            matrix[1, 2] += shiftY;
            //matrix[3, 2] = -1;

          //  if (method == Method.Two_Displays_MirrorX && c.name.Contains("_right"))
          //  {
          //      matrix[0, 2] *= -1;
		        //matrix *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
          //  }
          //  else
          //      if (method == Method.Two_Displays_MirrorY && c.name.Contains("_right"))
		        //    matrix *= Matrix4x4.Scale(new Vector3(1, -1, 1));

            //if (debug) Debug.Log("Matrix_Set parent " + parent);
            //if (debug) Debug.Log("nearClip " + nearClip + " farClip " + farClip);
            //if (debug) Debug.Log(c.name + " Matrix_Set\n" + matrix);
            //if (debug) Debug.Log(matrix[0, 2] + " " + matrix[1, 2]);
            c.projectionMatrix = matrix;
        }
        else
        {
            //matrix = c.projectionMatrix;

            ////if (!(c == cam && matrixKillHack))
            //if (!(c == cam && cam.projectionMatrix == Matrix4x4.zero))
            //{
            //    matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
            //    matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
            //    matrix[2, 2] = -(c.farClipPlane + c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
            //    matrix[2, 3] = -(2 * c.farClipPlane * c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);

            //    if (c == cam)
            //        camMatrix = matrix;

            //}

            //if (c == cam)
            //{
            //    ////if (cam.projectionMatrix == Matrix4x4.zero)
            //    //if (cam.projectionMatrix[3, 2] == 0 || nearClipHack)
            //    //{
            //    //    matrix = camMatrix;
            //    //    matrix_Set();
            //    //}
            //    //else
            //    //{
            //    //    matrix = c.projectionMatrix;
            //    //    matrix_Set();
            //    //    c.projectionMatrix = matrix;
            //    //}

            //    //camMatrix = matrix;
            //}
            //else
            //{
                matrix = c.projectionMatrix;
                //matrix_Set();
                matrixFOV_Set();
                matrixClip_Set();
                //matrix[2, 2] = -(c.farClipPlane + c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
                //matrix[2, 3] = -(2 * c.farClipPlane * c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
                c.projectionMatrix = matrix;
            //}

            //void matrix_Set()
            //{
            //    //matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
            //    //matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
            //    matrix[2, 2] = -(c.farClipPlane + c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
            //    matrix[2, 3] = -(2 * c.farClipPlane * c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
            //}

            //if (c == cam)
            //    if (debug) Debug.Log("Matrix_Set c == cam " + matrix);
            //else
            //    if (debug) Debug.Log(c.name + " Matrix_Set c != cam " + matrix);
        }

        //c.projectionMatrix = matrix;

        //void matrix_Set()
        //{
        //    matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
        //    matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
        //    matrix[2, 2] = -(c.farClipPlane + c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
        //    matrix[2, 3] = -(2 * c.farClipPlane * c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
        //}

        void matrixFOV_Set()
        {
            matrix[0, 0] = scaleX; // 1/tangent of half horizontal FOV
            matrix[1, 1] = scaleY; //1/tangent of half vertical FOV
        }

        void matrixClip_Set()
        {
            matrix[2, 2] = -(c.farClipPlane + c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
            matrix[2, 3] = -(2 * c.farClipPlane * c.nearClipPlane) / (c.farClipPlane - c.nearClipPlane);
        }

        if (debug) Debug.Log("Matrix_Set " + c.name + "\n" + matrix);
    }

    //void CamMatrix_Reset()
    //{
    //    if (debug) Debug.Log("CamMatrix_Reset camMatrix\n" + camMatrix);
    //    if (debug) Debug.Log("CamMatrix_Reset cam.projectionMatrix\n" + cam.projectionMatrix);
    //    if (cam.projectionMatrix == Matrix4x4.zero) //restore cam.projectionMatrix when return from matrixKillHack
    //        cam.projectionMatrix = camMatrix;

    //    Matrix_Set(cam, 0, 0);
    //    camMatrix = cam.projectionMatrix;
    //    if (debug) Debug.Log("CamMatrix_Reset Matrix_Set camMatrix\n" + camMatrix);
    //}

    RenderTexture renderTexture;
    public RenderTexture renderTexture_left;   
    public RenderTexture renderTexture_right;
#if HDRP || URP
    RenderTexture canvasRenderTexture;
    RenderTexture canvasRenderTexture_left;
    RenderTexture canvasRenderTexture_right;
#endif
    //RenderTexture leftCamAdditionalRT;
    //RenderTexture rightCamAdditionalRT;
    int pass;
    ComputeBuffer verticesPosBuffer;
    ComputeBuffer verticesUVBuffer;
    //bool GUIAsOverlayState;
    string displays;
    //Display[] fakeDisplays;
    Display display_left;
    Display display_right;
    int displayIndex_left = 0;
    int displayIndex_right = 1;
    bool displaySelectWaitInput;
    int rtWidth;
    int rtHeight;
    Camera topmostCamera;
    Camera topmostCamera_left;
    Camera topmostCamera_right;

    void Render_Set()
    {
        if (debug) Debug.Log("Render_Set");
        Render_Release(); //remove RT before adding new to avoid duplication
        //cam.enabled = true;

        //if (canvasCamera)
        //{
        //    canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
        //    leftCameraStack.Remove(canvasCamera_left);
        //    rightCameraStack.Remove(canvasCamera_right);
        //}

  //      int rtWidth = cam.pixelWidth;
		//int rtHeight = cam.pixelHeight;
        //rtWidth = cam.pixelWidth;
        //rtHeight = cam.pixelHeight;
        //rtWidth = Screen.width;
        //rtHeight = Screen.height;
        rtWidth = (int)windowSize.x;
        rtHeight = (int)windowSize.y;

//#if URP || HDRP
        camera_left.rect = camera_right.rect = cam.rect;
        camera_left.targetDisplay = 0;
        camera_right.targetDisplay = 0;

        //for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
        //    if (additionalS3DCamerasStruct[i].camera)
        //        additionalS3DCamerasStruct[i].camera_left.rect = additionalS3DCamerasStruct[i].camera_right.rect = cam.rect;

        foreach (var c in additionalS3DCamerasStruct)
            if (c.camera)
            {
                c.camera_left.rect = c.camera_right.rect = cam.rect;
                c.camera_left.targetDisplay = 0;
                c.camera_right.targetDisplay = 0;
            }
//#endif

        if (canvasCamera)
        {
            canvasCamera_left.rect = canvasCamera_right.rect = cam.rect;
            canvasCamera_left.targetDisplay = 0;
            canvasCamera_right.targetDisplay = 0;
        }

        if (S3DEnabled)
        {
#if POST_PROCESSING_STACK_V2
            if (PPLayer)
                PPLayerLastStatus = PPLayer.enabled = false; //disabling Post Process Layer if exist due it heavily eats fps even when the camera doesn't render the scene
#endif

            //if (GetComponent<Cinemachine.CinemachineBrain>())
            //    Invoke("GetVCam", Time.deltaTime);

            //cam.cullingMask = 0;
            //cam.targetTexture = null;

            if (disableCullingMask)
#if CINEMACHINE
                if (cineMachineEnabled)
                    VCamCullingOff();
                    //nearClipHack = true;
                else
                //if (!cineMachineEnabled)
#endif
                    cam.cullingMask = 0;

#if HDRP
            camData.volumeLayerMask = 0;
            camData.probeLayerMask = 0;
            //CameraDataStruct_Set();
            //lastCameraDataStruct = cameraDataStruct;
            //CameraDataStruct_Change();
#endif

            //Invoke("VCamCullingOff", 10);
            //camera_left.enabled = true;
            //camera_right.enabled = true;
            camera_left.enabled = camera_right.enabled = true;

            //if (additionalS3DCamerasStruct != null)
//            foreach (var c in additionalS3DCamerasStruct)
//                if (c.camera)
//                {
//                    c.camera.enabled = false;
//                    c.camera_left.enabled = c.camera_right.enabled = true;

//#if POST_PROCESSING_STACK_V2
//                    if (c.PPLayer)
//                        additionalS3DCamerasStruct[additionalS3DCameras.IndexOf(c.camera)].PPLayerLastStatus = c.PPLayer.enabled = false; //disabling Post Process Layer if exist due it heavily eats fps even when the camera doesn't render the scene
//#endif
//                }

            for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                if (additionalS3DCamerasStruct[i].camera)
                {
                    additionalS3DCamerasStruct[i].camera.enabled = false;
                    additionalS3DCamerasStruct[i].camera_left.enabled = additionalS3DCamerasStruct[i].camera_right.enabled = true;

//#if POST_PROCESSING_STACK_V2
//                    if (additionalS3DCamerasStruct[i].PPLayer)
//                        additionalS3DCamerasStruct[i].PPLayerLastStatus = additionalS3DCamerasStruct[i].PPLayer.enabled = false; //disabling Post Process Layer if exist due it heavily eats fps even when the camera doesn't render the scene //not required for additional S3D cameras as PPLayer dont affect FPS while the camera is disabled
//#endif
                }

      //      int rtWidth = cam.pixelWidth;
		    //int rtHeight = cam.pixelHeight;
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

                    //if (optimize)
                    //    rtWidth /= 2;

                    //verticesUV[2] = new Vector2(2, 1);
                    //verticesUV[3] = new Vector2(2, 0);
                    //pass = 3;
                    MethodSideBySide();
                break;

                case Method.SideBySide_Full:

                    //if (optimize)
                    //    rtWidth /= 2;

                    //verticesUV[2] = new Vector2(2, 1);
                    //verticesUV[3] = new Vector2(2, 0);
                    //pass = 3;
                    MethodSideBySide();
                break;

                case Method.SideBySide_HMD:

                    //if (optimize)
                    //    rtWidth /= 2;

                    //verticesUV[2] = new Vector2(2, 1);
                    //verticesUV[3] = new Vector2(2, 0);
                    //pass = 3;
                    MethodSideBySide();
                break;

                void MethodSideBySide()
                {
                    if (optimize)
                        rtWidth /= 2;

                    verticesUV[2] = new Vector2(2, 1);
                    verticesUV[3] = new Vector2(2, 0);
                    pass = 3;
                }

                case Method.OverUnder:

                    //if (optimize)
                    //    rtHeight /= 2;

                    //verticesUV[1] = new Vector2(0, 2);
                    //verticesUV[2] = new Vector2(1, 2);
                    //pass = 4;
                    MethodOverUnder();
                break;

                case Method.OverUnder_Full:
                    MethodOverUnder();
                break;

                void MethodOverUnder()
                {
                    if (optimize)
                        rtHeight /= 2;

                    verticesUV[1] = new Vector2(0, 2);
                    verticesUV[2] = new Vector2(1, 2);
                    pass = 4;
                }

                case Method.Sequential:
//#if !HDRP
#if !URP
                    if (optimize)
                    {
                        camera_left.enabled = camera_right.enabled = false;

//#if !URP
                        foreach (var c in additionalS3DCamerasStruct)
                            if (c.camera)
                                c.camera_left.enabled = c.camera_right.enabled = false;
//#endif
                    }
#endif
                    //nullRT = new RenderTexture(8, 8, 0, RenderTextureFormat.R8); //minimal RenderTexture for fastest rendering
                    ////nullRT = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.R8); //minimal RenderTexture for fastest rendering
                    //nullRT.filterMode = FilterMode.Point;
                    ////nullRT = RT_Make();

                    pass = 5;
                break;

                case Method.Anaglyph_RedCyan:
                    S3DMaterial.SetColor("_LeftCol", Color.red);
                    S3DMaterial.SetColor("_RightCol", Color.cyan);
                    pass = 6;
                break;

                case Method.Anaglyph_RedBlue:
                    S3DMaterial.SetColor("_LeftCol", Color.red);
                    S3DMaterial.SetColor("_RightCol", Color.blue);
                    pass = 6;
                break;

                case Method.Anaglyph_GreenMagenta:
                    S3DMaterial.SetColor("_LeftCol", Color.green);
                    S3DMaterial.SetColor("_RightCol", Color.magenta);
                    pass = 6;
                    break;

                case Method.Anaglyph_AmberBlue:
                    S3DMaterial.SetColor("_LeftCol", new Color(1, 1, 0, 1));
                    S3DMaterial.SetColor("_RightCol", Color.blue);
                    pass = 6;
                break;

                //case Method.Two_Displays:
                //    cam.enabled = false;
                //break;

                case Method.Two_Displays_MirrorX:
                    S3DMaterial.SetInt("_FlipX", 1);
                    S3DMaterial.SetInt("_FlipY", 0);
                    pass = 7;
                break;

                case Method.Two_Displays_MirrorY:
                    S3DMaterial.SetInt("_FlipX", 0);
                    S3DMaterial.SetInt("_FlipY", 1);
                    pass = 7;
                break;
            }

            //if (debug) Debug.Log("Render_Set Width: " + rtWidth + " Height: " + rtHeight);
            //if (debug) Debug.Log("Render_Set columns: " + columns + " rows: " + rows);

            //if (!(method == Method.Two_Displays))
            //if (method == Method.Two_Displays)
            //if (method == Method.Two_Displays)
            //if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
            if (method.ToString().Contains("Two_Displays") || method == Method.D3D11)
            {
                //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
                //    cam.usePhysicalProperties = false;

                cam.enabled = false;
                //camera_right.tag = "MainCamera"; //prevent TerrainsVisibilityUpdater(in "Test Track" sample) throw errors while the main camera disabled and no other camera with "MainCamera" tag in the scene
                ////canvasCamera.targetTexture = null;
//#if HDRP
//                //if (GUIAsOverlay)
//                //    GUIAsOverlayState = GUIAsOverlay;

//                GUIAsOverlay = false;
//#endif

                //if (Display.displays.Length > 1)
                //    Display.displays[1].Activate();
                //TargetDisplays_Set();

                if (method.ToString().Contains("Two_Displays"))
                    if (Display.displays.Length < 2)
                    {
                        //GameObject staticTooltip = Instantiate(cursorRectTransform.Find("Tooltip").gameObject);
                        ////staticTooltip.transform.parent = panel;
                        //staticTooltip.transform.SetParent(panel, false);
                        //staticTooltip.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -panel.GetComponent<RectTransform>().sizeDelta.y);
                        //staticTooltip.SetActive(true);
                        //Text staticTooltipText = staticTooltip.transform.Find("Text (Legacy)").GetComponent<Text>();
                        ////staticTooltip.transform.Find("Text (Legacy)").GetComponent<Text>().text = "Second display is not connected";
                        //staticTooltipText.text = "Second display is not connected";
                        //staticTooltip.transform.Find("Image_Background").GetComponent<RectTransform>().sizeDelta = new Vector2(staticTooltipText.preferredWidth, staticTooltipText.preferredHeight);
                        StaticTooltip_Make("Second display is not connected");
                        //displayIndex_left = 0;
                        //displayIndex_right = 1;
                        TargetDisplays_Set();
                    }
                    else
                        if (Display.displays.Length == 2)
                        {
                            TargetDisplays_Set();
                            TargetDisplays_Activate();
                        }
                        else
                            if (display_left == null || display_right == null || !display_left.active || !display_right.active)
                            {
                                //TargetDisplays_Set(0, 1);
                                //display_left = display_right = null;
                                ////string displays = "";
                                ////string[] fakeDisplays = new string[2];
                                ////Display[] fakeDisplays = new Display[3];
                                //fakeDisplays = new Display[3];
                                //fakeDisplays[0] = fakeDisplays[1] = fakeDisplays[2] = Display.displays[0];

                                ////for (int i = 0; i < Display.displays.Length; i++)
                                //for (int i = 0; i < fakeDisplays.Length; i++)
                                //    //if (!Display.displays[i].active)
                                //    //if (!fakeDisplays[i].active)
                                //    //displays.Insert(0, i.ToString());
                                //    displays += " " + i.ToString();

                                //if (debug) Debug.Log("fakeDisplays[0].active " + fakeDisplays[0].active);
                                //StaticTooltip_Make("Press the number key to select the display for Left camera:" + displays);
                                //displaySelectWaitInput = true;
                                TargetDisplays_Select();
                            }

                //void StaticTooltip_Make(string text)
                //{
                //    GameObject staticTooltip = Instantiate(cursorRectTransform.Find("Tooltip").gameObject);
                //    staticTooltip.transform.SetParent(panel, false);
                //    staticTooltip.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -panel.GetComponent<RectTransform>().sizeDelta.y);
                //    staticTooltip.SetActive(true);
                //    Text staticTooltipText = staticTooltip.transform.Find("Text (Legacy)").GetComponent<Text>();
                //    staticTooltipText.text = text;
                //    staticTooltip.transform.Find("Image_Background").GetComponent<RectTransform>().sizeDelta = new Vector2(staticTooltipText.preferredWidth, staticTooltipText.preferredHeight);
                //}

                ////camera_left.targetDisplay = 0;
                ////camera_right.targetDisplay = 1;
                //camera_left.targetDisplay = displayIndex_left;
                //camera_right.targetDisplay = displayIndex_right;
                //camera_left.rect = camera_right.rect = cam.rect;
                ////OnOffToggle();

                ////if (additionalS3DCamerasStruct != null)
                //foreach (var c in additionalS3DCamerasStruct)
                //    if (c.camera)
                //    {
                //        //c.camera_left.targetDisplay = 0;
                //        //c.camera_right.targetDisplay = 1;
                //        c.camera_left.targetDisplay = displayIndex_left;
                //        c.camera_right.targetDisplay = displayIndex_right;
                //        c.camera_left.rect = c.camera_right.rect = c.camera.rect;
                //    }

                //                //TargetDisplays_Set();
                //                camera_left.rect = camera_right.rect = cam.rect;

                //                for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                //                    if (additionalS3DCamerasStruct[i].camera)
                //                        additionalS3DCamerasStruct[i].camera_left.rect = additionalS3DCamerasStruct[i].camera_right.rect = cam.rect;

                //                if (canvasCamera)
                //                {
                //                    //canvasCamera_left.targetTexture = null;
                //                    //canvasCamera_right.targetTexture = null;
                //                    canvasCamera_left.rect = canvasCamera_right.rect = cam.rect;
                ////#if URP
                ////                    canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

                ////                    if (!leftCameraStack.Contains(canvasCamera_left))
                ////                    {
                ////                        leftCameraStack.Add(canvasCamera_left);
                ////                        rightCameraStack.Add(canvasCamera_right);
                ////                    }
                ////#endif
                ////#else
                ////#endif
                //                    //canvasCamera_left.targetDisplay = 0;
                //                    //canvasCamera_right.targetDisplay = 1;
                //                    canvasCamera_left.targetDisplay = displayIndex_left;
                //                    canvasCamera_right.targetDisplay = displayIndex_right;
                //                }

                //if (debug) Debug.Log("Display.displays[0].systemWidth: " + Display.displays[0].systemWidth + " Display.displays[0].systemHeight: " + Display.displays[0].systemHeight);
                //if (debug) Debug.Log("Display.displays[0].renderingWidth: " + Display.displays[0].renderingWidth + " Display.displays[0].renderingHeight: " + Display.displays[0].renderingHeight);
                //if (debug) Debug.Log("Display.displays[0]: " + Display.displays[0]);

//#if !(URP || HDRP)
//                if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                {
//                    //Camera.onPreCull += PreCull;
//                    Camera.onPreRender += RenderTexture_Reset;
//                    Camera.onPostRender += PostRender;
//                }
//#endif
            }
            else
            {
                //Destroy(staticTooltip);
                //displaySelectWaitInput = false;
                StaticTooltip_Destroy();

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                if (inputSystem_KeyListener != null)
                    inputSystem_KeyListener.Dispose();
#endif
                cam.enabled = true;
                //camera_left.tag = "Untagged";
                //camera_right.tag = "Untagged";
                ////GUIAsOverlay = GUIAsOverlayState;
//#if HDRP
//                GUIAsOverlayState = GUIAsOverlay;
//#endif
                //camera_left.targetDisplay = camera_right.targetDisplay = 0;

                //   renderTexture_left = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   renderTexture_right = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   canvasRenderTexture_left = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   canvasRenderTexture_right = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   //leftCamAdditionalRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   //rightCamAdditionalRT = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //   //renderTexture_left.filterMode = FilterMode.Point;
                //   //renderTexture_right.filterMode = FilterMode.Point;
                //   renderTexture_left.filterMode = renderTexture_right.filterMode = canvasRenderTexture_left.filterMode = canvasRenderTexture_right.filterMode = FilterMode.Point;
                //   //renderTexture_left.filterMode = renderTexture_right.filterMode = canvasRenderTexture_left.filterMode = canvasRenderTexture_right.filterMode = leftCamAdditionalRT.filterMode = rightCamAdditionalRT.filterMode = FilterMode.Point;
                //   //renderTexture_left.wrapMode = TextureWrapMode.Repeat; //need for OpenGL SBS & OU to work with the only one 4 vertex screen quad and UV coordinates over 1
                //   //renderTexture_right.wrapMode = TextureWrapMode.Repeat;
                //   renderTexture_left.wrapMode = renderTexture_right.wrapMode = canvasRenderTexture_left.wrapMode = canvasRenderTexture_right.wrapMode = TextureWrapMode.Repeat;
                //   //renderTexture_left.wrapMode = renderTexture_right.wrapMode = canvasRenderTexture_left.wrapMode = canvasRenderTexture_right.wrapMode = leftCamAdditionalRT.wrapMode = rightCamAdditionalRT.wrapMode = TextureWrapMode.Repeat;
            }

            //if (method != Method.Two_Displays
            //if (method != Method.Two_Displays && method != Method.Two_Displays_MirrorX && method != Method.Two_Displays_MirrorY //for MirrorX & MirrorY by camera matrix but not working with physical camera and HDRP skybox
            if (method != Method.Two_Displays
//#if URP
//                //|| method == Method.Two_Displays && GUIAsOverlay && GUIVisible
//                //|| GUIAsOverlay && GUIVisible
//                || GUIAsOverlay
//#elif HDRP
#if HDRP
                //|| method == Method.Two_Displays && (GUIAsOverlay && GUIVisible || additionalS3DCamerasStruct.Length != 0)
                //|| GUIAsOverlay && GUIVisible || additionalS3DCamerasStruct != null //not working as not null even with additionalS3DCamerasStruct.Length == 0
                //|| GUIAsOverlay && GUIVisible || additionalS3DCamerasStruct.Length != 0
                //|| GUIAsOverlay && GUIVisible || additionalS3DTopmostCameraIndex != -1
                || GUIAsOverlay || additionalS3DTopmostCameraIndex != -1
#endif
                )
            {
                //renderTexture = RT_Make();
                renderTexture_left = RT_Make();
                renderTexture_right = RT_Make();
                renderTexture_left.Create();
                renderTexture_right.Create();
#if !UNITY_EDITOR
                if (method == Method.D3D11)
                    SetTextureFromUnity(renderTexture_left.GetNativeTexturePtr(), renderTexture_right.GetNativeTexturePtr(), renderTexture_left.width, renderTexture_left.height);
#endif
                //canvasRenderTexture_left = RT_Make();
                //canvasRenderTexture_right = RT_Make();

                //camera_left.targetDisplay = camera_right.targetDisplay = 0;
                //camera_left.rect = camera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);

            if (method != Method.Two_Displays
#if HDRP
                || additionalS3DTopmostCameraIndex != -1
#endif
            )
                {
                    //cam.targetTexture = renderTexture;
                    camera_left.targetTexture = renderTexture_left;
                    camera_right.targetTexture = renderTexture_right;
                }

                ////if (additionalS3DCamerasStruct != null)
                //    foreach (var c in additionalS3DCamerasStruct)
                //        if (c.camera)
                //        {
                //            //c.camera_left.targetTexture = renderTexture_left;
                //            //c.camera_right.targetTexture = renderTexture_right;

                //            //c.camera_left.targetTexture = leftCamAdditionalRT;
                //            //c.camera_right.targetTexture = rightCamAdditionalRT;

                //            c.renderTexture_left = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //            c.camera_left.targetTexture = c.renderTexture_left;
                //            c.camera_right.targetTexture = c.renderTexture_right;
                //        }

                //if (additionalS3DCamerasStruct != null)
                for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                    if (additionalS3DCamerasStruct[i].camera)
                    {
                        //additionalS3DCamerasStruct[i].camera_left.targetDisplay = additionalS3DCamerasStruct[i].camera_right.targetDisplay = 0;
                        //additionalS3DCamerasStruct[i].camera_left.rect = additionalS3DCamerasStruct[i].camera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);
#if HDRP
                        //additionalS3DCamerasStruct[i].renderTexture_left = new RenderTexture(renderTexture_left);
                        //additionalS3DCamerasStruct[i].renderTexture_right = new RenderTexture(renderTexture_right);
                        additionalS3DCamerasStruct[i].renderTexture_left = RT_Make();
                        additionalS3DCamerasStruct[i].renderTexture_right = RT_Make();
                        additionalS3DCamerasStruct[i].camera_left.targetTexture = additionalS3DCamerasStruct[i].renderTexture_left;
                        additionalS3DCamerasStruct[i].camera_right.targetTexture = additionalS3DCamerasStruct[i].renderTexture_right;
#else
//#elif !URP
#if URP
                        if (method != Method.Two_Displays_MirrorX && method != Method.Two_Displays_MirrorY) //fix overlay cameras unmatched output properties in Unity 2021 as not set targetTexture if using blit to screen
#endif
                        {
                            additionalS3DCamerasStruct[i].camera_left.targetTexture = renderTexture_left;
                            additionalS3DCamerasStruct[i].camera_right.targetTexture = renderTexture_right;
                        }
#endif
                    }

                if (canvasCamera)
                {
                    //canvasCamera_left.targetDisplay = canvasCamera_right.targetDisplay = 0;
                    //canvasCamera_left.rect = canvasCamera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);
                    //#if URP
                    //                    canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
                    //                    leftCameraStack.Remove(canvasCamera_left);
                    //                    rightCameraStack.Remove(canvasCamera_right);
                    ////#else
                    ////#endif
#if HDRP
//#if HDRP || URP
//#elif HDRP
                    canvasRenderTexture_left = RT_Make();
                    canvasRenderTexture_right = RT_Make();
                    canvasCamera_left.targetTexture = canvasRenderTexture_left;
                    //CanvasCameraLeftRenderTexture_Set();
                    canvasCamera_right.targetTexture = canvasRenderTexture_right;
                    //CanvasCameraRightRenderTexture_Set();

                    //if (method == Method.Sequential)
                    //    RenderPipelineManager.beginContextRendering += RenderTexture_Reset; //add render context
#else
#if URP
                    if (method != Method.Two_Displays_MirrorX && method != Method.Two_Displays_MirrorY) //fix GUI blinking in Unity 2021 as not set targetTexture if using blit to screen
#endif
                    {
                        canvasCamera_left.targetTexture = renderTexture_left;
                        canvasCamera_right.targetTexture = renderTexture_right;
                    }
//#if !URP
//                    if (method == Method.Two_Displays)
//                    {
//                        canvasCamera_left.gameObject.AddComponent<CameraBlit>();
//                        canvasCamera_right.gameObject.AddComponent<CameraBlit>();
//                    }
//#endif
#endif
                }

                //RenderTexture RT_Make()
                //{
                //    RenderTexture rt = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
                //    rt.filterMode = FilterMode.Point;
                //    rt.wrapMode = TextureWrapMode.Repeat;
                //    return rt;
                //}

//#if URP || HDRP
//                RenderPipelineManager.beginContextRendering += RenderTexture_Reset; //add render context
//#endif

                if (method != Method.Two_Displays_MirrorX && method != Method.Two_Displays_MirrorY)
                {
                    S3DMaterial.SetTexture("_LeftTex", renderTexture_left);
                    S3DMaterial.SetTexture("_RightTex", renderTexture_right);

                    //if (debug) Debug.Log("Render_Set renderTexture_left.height: " + renderTexture_left.height + " renderTexture_left.width: " + renderTexture_left.width);

                    verticesPosBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(Vector2)));
                    verticesPosBuffer.SetData(verticesPos);
                    S3DMaterial.SetBuffer("verticesPosBuffer", verticesPosBuffer);

                    verticesUVBuffer = new ComputeBuffer(4, Marshal.SizeOf(typeof(Vector2)));
                    verticesUVBuffer.SetData(verticesUV);
                    S3DMaterial.SetBuffer("verticesUVBuffer", verticesUVBuffer);

                    //if (method == Method.Sequential)
                    //    RenderPipelineManager.beginContextRendering += RenderTexture_Reset; //add render context
                }
//#if URP || HDRP
                else
                {
                    //if (method == Method.Two_Displays_MirrorX)
                    //{
                    //    RenderTextureFlipMaterial.SetInt("_FlipX", 1);
                    //    RenderTextureFlipMaterial.SetInt("_FlipY", 0);
                    //}
                    //else
                    //    if (method == Method.Two_Displays_MirrorY)
                    //    {
                    //        RenderTextureFlipMaterial.SetInt("_FlipX", 0);
                    //        RenderTextureFlipMaterial.SetInt("_FlipY", 1);
                    //    }
#if !(URP || HDRP)
                    //Camera.onPreCull += PreCull;
                    Camera.onPreRender += RenderTexture_Reset;
                    //Camera.onPostRender += PostRender;
#endif
                }

                TopMostCamera_Set();
#if URP || HDRP
                //if (method == Method.Two_Displays && GUIVisible || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
                //    RenderPipelineManager.beginCameraRendering += RenderTexture_Reset; //add render context
                RenderTextureContextSet();

                //RenderPipelineManager.endContextRendering += PostRenderContext; //add render context
                //RenderPipelineManager.endCameraRendering += PostRenderContext; //add render context
#endif

                //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
                //{
                //    if (canvasCamera_left && canvasCamera_left.isActiveAndEnabled)
                //    {
                //        //canvasCamera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //        //canvasCamera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //        topmostCamera_left = canvasCamera_left;
                //        topmostCamera_right = canvasCamera_right;
                //    }
                //    else
                //        if (additionalS3DCamerasStruct.Length != 0)
                //        {
                //            //additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //            //additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //            topmostCamera_left = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left;
                //            topmostCamera_right = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right;
                //        }
                //        else
                //        {
                //            //camera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //            //camera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //            topmostCamera_left = camera_left;
                //            topmostCamera_right = camera_right;
                //        }

                //    topmostCamera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //    topmostCamera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //}
            }

//#if !(URP || HDRP)
//            //clearFrameCount = 0;
//            Camera.onPreRender += PreRenderClearScreen;
//#endif

            //#if UNITY_2019_1_OR_NEWER
            //                if (!defaultRender)
            //                {
            //                    //RenderPipelineManager.beginCameraRendering += RenderTexture_Reset; //add render context
            //                    RenderPipelineManager.endCameraRendering += PostRenderContext; //add render context

            //                    //                if (nearClipHack)
            //                    //#if CINEMACHINE
            //                    //                    if (cineMachineEnabled)
            //                    //                    {
            //                    //                        VCamNearClipHack();
            //                    //                        //nearClipHackApplyed = true;
            //                    //                    }
            //                    //                    else
            //                    //#endif
            //                    //                        cam.nearClipPlane = -1;

            //                    //                //nearClipHackApplyed = true;
            //                    //                //Invoke("VCamCullingOff", 10);
            //                }
            //#endif
            //}

            ////if (!Application.isEditor)
            //if (matrixKillHack)
            //    cam.projectionMatrix = Matrix4x4.zero;

            //CameraDataStruct_Change();
        }
        else
        {
            cameraRestore();
            StaticTooltip_Destroy();

            //renderTexture = RT_Make();
            //cam.targetTexture = renderTexture;

#if HDRP
//#if HDRP || URP
            //if (additionalS3DCameras.Count != 0 || GUIAsOverlay)
            if (
//#if HDRP
                //additionalS3DCameras.Count != 0 || 
                additionalS3DTopmostCameraIndex != -1 ||
//#endif
                //canvasCamera && canvasCamera.isActiveAndEnabled
                GUIAsOverlay
                )
            {
                //if (debug) Debug.Log("Canvas1");
                renderTexture = RT_Make();

                if (additionalS3DTopmostCameraIndex != -1)
                    cam.targetTexture = renderTexture;

                //if (additionalS3DCameras.Count != 0 || canvasCamera && canvasCamera.isActiveAndEnabled)
                //    cam.targetTexture = renderTexture;

                for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                    if (additionalS3DCamerasStruct[i].camera)
                    {
//#if HDRP
                        additionalS3DCamerasStruct[i].renderTexture = RT_Make();
                        additionalS3DCamerasStruct[i].camera.targetTexture = additionalS3DCamerasStruct[i].renderTexture;
//#else
//                        additionalS3DCamerasStruct[i].camera.targetTexture = renderTexture; //in URP this only prevent warning sign in the cameraStack
//#endif
                    }

//#if URP
//                foreach (Camera c in cameraStack)
//                    c.targetTexture = renderTexture;
//#endif

//#if HDRP
//#if HDRP || URP

                //if (canvasCamera && canvasCamera.isActiveAndEnabled)
                if (GUIAsOverlay)
//#endif
                {
                    //if (debug) Debug.Log("Canvas2");
                    //canvasCamera.rect = Rect.MinMaxRect(0, 0, 1, 1);
//#if HDRP
//#if HDRP || URP
                    canvasRenderTexture = RT_Make();
                    //canvasCamera.targetTexture = canvasRenderTexture;
//#else
//                    canvasCamera.targetTexture = renderTexture;
//#endif
                    //CanvasCameraRenderTexture_Set();
                    canvasCamera.targetTexture = canvasRenderTexture;
                }

                ////RenderPipelineManager.beginContextRendering += RenderTexture_Reset; //add render context
                //RenderPipelineManager.beginCameraRendering += RenderTexture_Reset; //add render context
                TopMostCamera_Set();
                RenderTextureContextSet();
            }
//#if HDRP
//            for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
//                if (additionalS3DCamerasStruct[i].camera)
//                {
//                    //if (debug) Debug.Log("Render_Set additionalS3DCamerasStruct[i].camera ==================================================================" + additionalS3DCamerasStruct[i].camera);
//                    //additionalS3DCamerasStruct[i].camera.rect = Rect.MinMaxRect(0, 0, 1, 1);
////#if HDRP
//                    additionalS3DCamerasStruct[i].renderTexture = RT_Make();
//                    additionalS3DCamerasStruct[i].camera.targetTexture = additionalS3DCamerasStruct[i].renderTexture;
////#else
////                    additionalS3DCamerasStruct[i].camera.targetTexture = renderTexture;
////#endif
//                }
//#endif

//            if (canvasCamera && canvasCamera.isActiveAndEnabled)
//            {
//                if (debug) Debug.Log("Canvas2");
////#if URP
////                    canvasCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
////                    cameraStack.Remove(canvasCamera);
////#endif
//                //canvasCamera.rect = Rect.MinMaxRect(0, 0, 1, 1);
//#if HDRP
//                canvasRenderTexture = RT_Make();
//                canvasCamera.targetTexture = canvasRenderTexture;
//#else
//                canvasCamera.targetTexture = renderTexture;
//#endif
//            }
            //if (method != Method.Two_Displays
            //if (method != Method.Two_Displays && method != Method.Two_Displays_MirrorX && method != Method.Two_Displays_MirrorY //for MirrorX & MirrorY by camera matrix but not working with physical camera and HDRP skybox
//            if (method != Method.Two_Displays
////#if URP
////                //|| method == Method.Two_Displays && GUIAsOverlay && GUIVisible
////                || GUIAsOverlay && GUIVisible
////#elif HDRP
//                //|| method == Method.Two_Displays && (GUIAsOverlay && GUIVisible || additionalS3DCamerasStruct.Length != 0)
//                || GUIAsOverlay && GUIVisible || additionalS3DCamerasStruct.Length != 0
////#endif
//            )
//                //RenderPipelineManager.endContextRendering += PostRenderContext; //add render context
//                RenderPipelineManager.endCameraRendering += PostRenderContext; //add render context
#endif
        }

        //RenderTexture RT_Make()
        //{
        //    RenderTexture rt = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
        //    rt.filterMode = FilterMode.Point;
        //    rt.wrapMode = TextureWrapMode.Repeat;
        //    return rt;
        //}

//#if UNITY_2019_1_OR_NEWER
//        if (!defaultRender)
//        {
////#if HDRP
//            RenderPipelineManager.beginContextRendering += RenderTexture_Reset; //add render context
////#endif
//            RenderPipelineManager.endContextRendering += PostRenderContext; //add render context
//            //RenderPipelineManager.endCameraRendering += RenderBlit; //add render context
//        }
//#endif

        CameraDataStruct_Change();
    }

    void TopMostCamera_Set()
    {
#if !(URP || HDRP)
        //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //{
        //    //if (topmostCamera_left) //restore render texture from last topmost Cameras before it changes
        //    //{
        //    //    topmostCamera_left.targetTexture = renderTexture_left;
        //    //    topmostCamera_right.targetTexture = renderTexture_right;
        //    //}
        //}

        //if (topmostCamera_left)
        //{
        //    Destroy(topmostCamera_left.GetComponent<OnRenderImageDelegate>());
        //    Destroy(topmostCamera_right.GetComponent<OnRenderImageDelegate>());
        //}

        OnRenderImageEvent_Remove();
#endif

        //if (canvasCamera_left && canvasCamera_left.isActiveAndEnabled)
        if (GUIAsOverlay && GUIVisible)
        {
            //canvasCamera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
            //canvasCamera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
            topmostCamera = canvasCamera;
            topmostCamera_left = canvasCamera_left;
            topmostCamera_right = canvasCamera_right;
        }
        else
            //if (additionalS3DCamerasStruct.Length != 0)
            if (additionalS3DTopmostCameraIndex != -1)
            {
                //additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //topmostCamera_left = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left;
                //topmostCamera_right = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right;
                topmostCamera = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera;
                topmostCamera_left = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_left;
                topmostCamera_right = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_right;
            }
            else
            {
                //camera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                //camera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
                topmostCamera = cam;
                topmostCamera_left = camera_left;
                topmostCamera_right = camera_right;
            }

#if !(URP || HDRP)
        if (S3DEnabled && (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY))
        {
            topmostCamera_left.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
            topmostCamera_right.gameObject.AddComponent<OnRenderImageDelegate>().RenderImageEvent += OnRenderImageEvent;
        }
#endif

        //if (debug) Debug.Log("TopMostCamera_Set left: " + topmostCamera_left + " right: " + topmostCamera_right);
        if (debug) Debug.Log("TopMostCamera_Set: " + topmostCamera + " left: " + topmostCamera_left + " right: " + topmostCamera_right);
    }

    void OnRenderImageEvent_Remove()
    {
        if (topmostCamera_left)
        {
            Destroy(topmostCamera_left.GetComponent<OnRenderImageDelegate>());
            Destroy(topmostCamera_right.GetComponent<OnRenderImageDelegate>());
        }
    }

    void RenderTextureContextSet()
    {
        if (debug) Debug.Log("RenderTextureContextSet");
#if URP || HDRP
        //RenderPipelineManager.endCameraRendering -= RenderQuad;
        RenderPipelineManager.endFrameRendering -= RenderQuad;
        //RenderPipelineManager.endContextRendering -= RenderQuad;
        //RenderPipelineManager.beginCameraRendering -= RenderTexture_Reset; //remove render context if exist before add to avoid duplication
        RenderPipelineManager.beginFrameRendering -= RenderTexture_Reset; //remove render context if exist before add to avoid duplication
        //RenderPipelineManager.endCameraRendering -= RenderTexture_BlitToScreen;
        RenderPipelineManager.endFrameRendering -= RenderTexture_BlitToScreen;
        //RenderPipelineManager.endCameraRendering -= RenderTexture_BlitToRenderTexture;
        RenderPipelineManager.endFrameRendering -= RenderTexture_BlitToRenderTexture;

        if (
#if HDRP
        //method == Method.Two_Displays && (additionalS3DCamerasStruct.Length != 0 || GUIAsOverlay && GUIVisible) || 
        method == Method.Two_Displays && (additionalS3DTopmostCameraIndex != -1 || GUIAsOverlay && GUIVisible) || 
#endif
        S3DEnabled && (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY))
        {
            if (additionalS3DTopmostCameraIndex != -1 || GUIAsOverlay && GUIVisible)
                //RenderPipelineManager.endCameraRendering += RenderTexture_BlitToRenderTexture; //blit Render Texture to main Render Texture(required for HDRP overlay cameras) after render is finished
                RenderPipelineManager.endFrameRendering += RenderTexture_BlitToRenderTexture; //blit Render Texture to main Render Texture(required for HDRP overlay cameras) after render is finished

            //RenderPipelineManager.endCameraRendering += RenderTexture_BlitToScreen; //blit Render Texture to scren after render is finished
            RenderPipelineManager.endFrameRendering += RenderTexture_BlitToScreen; //blit Render Texture to scren after render is finished
            //RenderPipelineManager.beginCameraRendering += RenderTexture_Reset; //required reset Render Texture after set it to null for blit to screen
            RenderPipelineManager.beginFrameRendering += RenderTexture_Reset; //required reset Render Texture after set it to null for blit to screen
        }
        else
            if (method != Method.Two_Displays)
            {
                if (S3DEnabled)
                    //RenderPipelineManager.endCameraRendering += RenderQuad; //draw fullscreen quad at main camera with S3D combined output by S3D shader
                    RenderPipelineManager.endFrameRendering += RenderQuad; //draw fullscreen quad at main camera with S3D combined output by S3D shader
                    //RenderPipelineManager.endContextRendering += RenderQuad; //draw fullscreen quad at main camera with S3D combined output by S3D shader
#if HDRP
                //if (topmostCamera_left != camera_left)
                if (additionalS3DTopmostCameraIndex != -1 || GUIAsOverlay && GUIVisible)
                    //RenderPipelineManager.endCameraRendering += RenderTexture_BlitToRenderTexture; //blit Render Texture to main Render Texture(required for HDRP overlay cameras) after render is finished
                    RenderPipelineManager.endFrameRendering += RenderTexture_BlitToRenderTexture; //blit Render Texture to main Render Texture(required for HDRP overlay cameras) after render is finished

            if (!S3DEnabled)
                    if (additionalS3DTopmostCameraIndex != -1 || GUIAsOverlay && GUIVisible)
                    {
                        //RenderPipelineManager.endCameraRendering += RenderTexture_BlitToScreen; //blit Render Texture to scren after render is finished
                        RenderPipelineManager.endFrameRendering += RenderTexture_BlitToScreen; //blit Render Texture to scren after render is finished
                        //RenderPipelineManager.beginCameraRendering += RenderTexture_Reset; //required reset Render Texture after set it to null for blit to screen
                        RenderPipelineManager.beginFrameRendering += RenderTexture_Reset; //required reset Render Texture after set it to null for blit to screen
                    }
#endif
            }
#endif
    }

    RenderTexture RT_Make()
    {
//#if !URP && !HDRP && !UNITY_2022_1_OR_NEWER
//        if (QualitySettings.antiAliasing != 0)
//            lastRTFormat = RTFormat = RenderTextureFormat.ARGBFloat;
//#endif
        //RenderTexture rt = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);
        RenderTexture rt;

        if (method == Method.D3D11)
            rt = new RenderTexture(rtWidth, rtHeight, 24, RenderTextureFormat.BGRA32);
        else
            rt = new RenderTexture(rtWidth, rtHeight, 24, RTFormat);

        //RenderTextureDescriptor desc = new RenderTextureDescriptor(); //unflipped for use with bindMS = true custom antialiasing resolving samples in shader
        ////RenderTextureDescriptor desc = new RenderTextureDescriptor(rtWidth, rtHeight, RTFormat, 24);
        //desc.width = rtWidth;
        //desc.height = rtHeight;
        //desc.colorFormat = RTFormat;
        //desc.depthBufferBits = 24;
        ////desc.autoGenerateMips = true;
        ////desc.useMipMap = false;
        ////desc.sRGB = true;
        ////desc.enableRandomWrite = true;
        //desc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        //desc.volumeDepth = 1;
        ////desc.msaaSamples = 1;
        //desc.msaaSamples = QualitySettings.antiAliasing;
        //desc.bindMS = true;
        //RenderTexture rt = new RenderTexture(desc);
        //if (debug) Debug.Log(rt.descriptor.flags);

        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;

#if URP
        if (URPAsset.msaaSampleCount != 0)
            rt.antiAliasing = URPAsset.msaaSampleCount;
#elif HDRP
        if (HDRPSettings.msaaSampleCount != 0)
            rt.antiAliasing = (int)HDRPSettings.msaaSampleCount;
#else
        if (QualitySettings.antiAliasing != 0)
            rt.antiAliasing = QualitySettings.antiAliasing;
#endif

        return rt;
    }

    //void TargetDisplays_Input()
    void TargetDisplays_Input(string inputString)
    {
        //if (debug) Debug.Log("Input.inputString " + Input.inputString);
        if (debug) Debug.Log("inputString " + inputString);

        if (display_left == null)
        {
            //string inputString = Input.inputString.Replace(" ", "");
            //string inputString = Input.inputString.Trim();
            inputString = inputString.Trim();

            //if (Input.inputString != "" && displays.Contains(Input.inputString))
            if (inputString != "" && displays.Contains(inputString))
            {
                //if (debug) Debug.Log("displays.Contains(Input.inputString) " + Input.inputString);
                //if (debug) Debug.Log("displays.Contains(Input.inputString)" + Input.inputString + "end");
                if (debug) Debug.Log("displays.Contains(inputString) " + inputString);
                displayIndex_left = Convert.ToInt32(inputString);
                //display_left = Display.displays[Convert.ToInt32(inputString)];
                //display_left = fakeDisplays[Convert.ToInt32(inputString)];
                display_left = Display.displays[displayIndex_left];
                ////display_left = fakeDisplays[displayIndex_left];
                //display_left.Activate();
                //if (debug) Debug.Log("display_left " + display_left);
                //DisplayRight_Set();
                displays = displays.Replace(inputString, "");
                //if (debug) Debug.Log("displays " + displays);
                //Destroy(staticTooltip);
                StaticTooltip_Make("Press the number key to select the display for Right camera:" + displays);
            }
            else
                //if (debug) Debug.Log("!displays.Contains(Input.inputString) " + Input.inputString);
                if (debug) Debug.Log("!displays.Contains(inputString) " + inputString);
        }
        else
        {
            //string inputString = Input.inputString.Trim();
            inputString = inputString.Trim();

            if (inputString != "" && displays.Contains(inputString))
            {
                //if (debug) Debug.Log("displays.Contains(Input.inputString) " + Input.inputString);
                if (debug) Debug.Log("displays.Contains(inputString) " + inputString);
                displayIndex_right = Convert.ToInt32(inputString);
                //display_right = Display.displays[Convert.ToInt32(inputString)];
                //display_right = fakeDisplays[Convert.ToInt32(inputString)];
                display_right = Display.displays[displayIndex_right];
                ////display_right = fakeDisplays[displayIndex_right];
                //display_right.Activate();
                //if (debug) Debug.Log("display_right " + display_right);
                displays = displays.Replace(inputString, "");
                //Destroy(staticTooltip);
                //displaySelectWaitInput = false;
                StaticTooltip_Destroy();
#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                inputSystem_KeyListener.Dispose();
#endif
                TargetDisplays_Set();
                //TargetDisplays_Set(displayIndex_left, displayIndex_right);
                TargetDisplays_Activate();
            }
        }
    }

    void TargetDisplays_Select()
    {
        ////TargetDisplays_Set(0, 1);
        //displayIndex_left = 0;
        //displayIndex_right = 1;
        //TargetDisplays_Set();
        display_left = display_right = null;
        //string displays = "";
        displays = "";
        //string[] fakeDisplays = new string[2];
        //Display[] fakeDisplays = new Display[3];
        //fakeDisplays = new Display[3];
        //fakeDisplays[0] = fakeDisplays[1] = fakeDisplays[2] = Display.displays[0];

        for (int i = 0; i < Display.displays.Length; i++)
        //for (int i = 0; i < fakeDisplays.Length; i++)
            //if (!Display.displays[i].active)
            //if (!fakeDisplays[i].active)
            //displays.Insert(0, i.ToString());
            displays += " " + i.ToString();

        //if (debug) Debug.Log("fakeDisplays[0].active " + fakeDisplays[0].active);
        StaticTooltip_Make("Press the number key to select the display for Left camera:" + displays);
        displaySelectWaitInput = true;
#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        inputSystem_KeyListener = InputSystem.onAnyButtonPress.Call(AnyKeyPress);
#endif
    }

    void TargetDisplays_Set()
    //void TargetDisplays_Set(int displayIndex_left, int displayIndex_right)
    {
        if (debug) Debug.Log("TargetDisplays_Set displayIndex_left " + displayIndex_left + " displayIndex_right " + displayIndex_right);
        camera_left.targetDisplay = displayIndex_left;
        camera_right.targetDisplay = displayIndex_right;

        foreach (var c in additionalS3DCamerasStruct)
            if (c.camera)
            {
                c.camera_left.targetDisplay = displayIndex_left;
                c.camera_right.targetDisplay = displayIndex_right;
            }

        if (canvasCamera)
        {
            canvasCamera_left.targetDisplay = displayIndex_left;
            canvasCamera_right.targetDisplay = displayIndex_right;
        }

        //if (Display.displays.Length >= 2)
        //{
        //    display_left = Display.displays[displayIndex_left];
        //    display_right = Display.displays[displayIndex_right];

        //    if (!display_left.active)
        //        display_left.Activate();

        //    if (!display_right.active)
        //        display_right.Activate();
        //}
    }

    void TargetDisplays_Activate()
    {
        display_left = Display.displays[displayIndex_left];
        display_right = Display.displays[displayIndex_right];

        if (!display_left.active)
            display_left.Activate();

        if (!display_right.active)
            display_right.Activate();
    }

    GameObject staticTooltip;

    void StaticTooltip_Make(string text)
    {
        Destroy(staticTooltip);
        //StaticTooltip_Destroy();
        //GameObject staticTooltip = Instantiate(cursorRectTransform.Find("Tooltip").gameObject);
        staticTooltip = Instantiate(cursorRectTransform.Find("Tooltip").gameObject);
        staticTooltip.transform.SetParent(panel, false);
        staticTooltip.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -panel.GetComponent<RectTransform>().sizeDelta.y);
        staticTooltip.SetActive(true);
        Text staticTooltipText = staticTooltip.transform.Find("Text (Legacy)").GetComponent<Text>();
        staticTooltipText.text = text;
        staticTooltip.transform.Find("Image_Background").GetComponent<RectTransform>().sizeDelta = new Vector2(staticTooltipText.preferredWidth, staticTooltipText.preferredHeight);
    }

    void StaticTooltip_Destroy()
    {
        Destroy(staticTooltip);
        displaySelectWaitInput = false;
    }

    void cameraRestore()
    {
        //camera_left.enabled = false;
        //camera_right.enabled = false;
        camera_left.enabled = camera_right.enabled = false;
        cam.enabled = true;
        //camera_left.tag = "Untagged";
        //camera_right.tag = "Untagged";

        //if (additionalS3DCamerasStruct != null)
        foreach (var c in additionalS3DCamerasStruct)
            if (c.camera)
            {
                //c.camera_left.targetTexture = c.camera_right.targetTexture = null;
                c.camera_left.enabled = c.camera_right.enabled = false;
                c.camera.enabled = true;
                //c.camera.ResetProjectionMatrix();
            }

        cam.cullingMask = sceneCullingMask;
#if HDRP
            camData.volumeLayerMask = volumeLayerMask;
            camData.probeLayerMask = probeLayerMask;
            //CameraDataStruct_Set();
            //lastCameraDataStruct = cameraDataStruct;
            //CameraDataStruct_Change();
#endif

//        if (canvasCamera)
//        {
//#if URP
//            canvasCamera_left.GetUniversalAdditionalCameraData().renderType = canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
//            leftCameraStack.Remove(canvasCamera_left);
//            rightCameraStack.Remove(canvasCamera_right);
//#endif
//        }

        //CameraDataStruct_Change();
    }

//#if UNITY_2019_1_OR_NEWER
//#if HDRP
#if URP || HDRP
    CommandBuffer commandBuffer;
    //float frameTime;
    //Camera cameraToResetLeft;
    //Camera cameraToResetRight;
    //RenderTexture renderTextureToResetLeft;
    //RenderTexture renderTextureToResetRight;

    //void RenderTexture_Reset(ScriptableRenderContext context, List<Camera> cameraList)
    //void RenderTexture_Reset(ScriptableRenderContext context, Camera camera)
    void RenderTexture_Reset(ScriptableRenderContext context, Camera[] cameraList)
    {
        if (debug)
        foreach (Camera camera in cameraList)
            Debug.Log(camera + " RenderTexture_Reset " + Time.time);

        //        commandBuffer = new CommandBuffer();
        //        //commandBuffer.name = "S3DPreRenderReset";

        //        //if (camera == camera_left)
        //        //{
        //        //    //if (debug) Debug.Log("camera = camera_left");
        //        //    camera_left.targetTexture = renderTexture_left;
        //        //    //            commandBuffer = new CommandBuffer();
        //        //    //            commandBuffer.name = "left";
        //        //    //#if HDRP
        //        //    //            foreach (var c in additionalS3DCamerasStruct)
        //        //    //            {
        //        //    //                commandBuffer.Blit(c.renderTexture_left, renderTexture_left, S3DPanelMaterial);
        //        //    //                commandBuffer.Blit(c.renderTexture_right, renderTexture_right, S3DPanelMaterial);
        //        //    //            }

        //        //    //            if (canvasCamera && canvasCamera_left.isActiveAndEnabled)
        //        //    //            {
        //        //    //                //if (debug) Debug.Log("========================================================================== ");
        //        //    //                commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
        //        //    //                commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
        //        //    //            }
        //        //    //#endif
        //        //    //            commandBuffer.Blit(renderTexture_left, null as RenderTexture);
        //        //    //            context.ExecuteCommandBuffer(commandBuffer);
        //        //    //            commandBuffer.Release();
        //        //    //            context.Submit();
        //        //    //            //camera_left.targetTexture = renderTexture_left;
        //        //}
        //        //else
        //        //    if (camera == camera_right)
        //        //        camera_right.targetTexture = renderTexture_right;

        //        //foreach (Camera camera in cameraList)
        //            if (camera == camera_left)
        //            {
        //                //RenderTexture rta = RenderTexture.active;
        //                //RenderTexture.active = renderTexture_left;
        //                //GL.Clear(true, true, Color.clear);
        //                //RenderTexture.active = rta;

        //                //if (GUIAsOverlay && GUIVisible)
        //                if (GUIAsOverlay && GUIVisible || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //                //if (GUIAsOverlay && GUIVisible || method == Method.Sequential && !optimize && oddFrame)
        //                {
        //                    //if (debug) Debug.Log("RenderTexture_Reset camera_left.targetTexture = renderTexture_left " + Time.time);
        //                    camera_left.targetTexture = renderTexture_left;
        //                }
        //            }
        //            else
        //                if (camera == camera_right)
        //                {
        //                    //RenderTexture rta = RenderTexture.active;
        //                    //RenderTexture.active = renderTexture_right;
        //                    //GL.Clear(true, true, Color.clear);
        //                    //RenderTexture.active = rta;

        //                    //if (GUIAsOverlay && GUIVisible)
        //                if (GUIAsOverlay && GUIVisible || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //                    //if (GUIAsOverlay && GUIVisible || method == Method.Sequential && !optimize && oddFrame)
        //                        camera_right.targetTexture = renderTexture_right;

        ////                    if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        ////#if HDRP
        ////                        camera_right.GetComponent<HDAdditionalCameraData>().invertFaceCulling = true;
        ////#else
        ////                        GL.invertCulling = true;
        ////#endif
        //                }
        //                else
        //                    if (camera == canvasCamera && !S3DEnabled)
        //////#if HDRP
        ////#if HDRP || URP
        ////                        canvasCamera.targetTexture = canvasRenderTexture;
        ////#else
        ////                        canvasCamera.targetTexture = renderTexture;
        ////#endif
        //                        //CanvasCameraRenderTexture_Set();
        //                        canvasCamera.targetTexture = canvasRenderTexture;
        //                    else
        //                        if (camera == canvasCamera_left)
        //////#if HDRP
        ////#if HDRP || URP
        ////                            canvasCamera_left.targetTexture = canvasRenderTexture_left;
        ////#else
        ////                        {
        ////                            //canvasCamera_left.rect = Rect.MinMaxRect(0, 0, 1, 1);
        ////                            canvasCamera_left.targetTexture = renderTexture_left;
        ////                        }
        ////#endif
        //                            //CanvasCameraLeftRenderTexture_Set();
        //                            //canvasCamera_left.targetTexture = canvasRenderTexture_left;
        //                            canvasCamera_left.targetTexture = renderTexture_left;
        //                        else
        //                            if (camera == canvasCamera_right)
        //////#if HDRP
        ////#if HDRP || URP
        ////                                canvasCamera_right.targetTexture = canvasRenderTexture_right;
        ////#else
        ////                            {
        ////                                //canvasCamera_right.rect = Rect.MinMaxRect(0, 0, 1, 1);
        ////                                canvasCamera_right.targetTexture = renderTexture_right;
        ////                            }
        ////#endif
        //                                //CanvasCameraRightRenderTexture_Set();
        //                                //canvasCamera_right.targetTexture = canvasRenderTexture_right;
        //                                canvasCamera_right.targetTexture = renderTexture_right;
        //#if HDRP
        //                            else
        //                                //if (additionalS3DCameras.Count != 0)
        //                                if (additionalS3DTopmostCameraIndex != -1)
        //                                    if (camera == additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera)
        //                                        additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera.targetTexture = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].renderTexture;
        //                                    else
        //                                        if (camera == additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left)
        //                                            additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_left.targetTexture = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].renderTexture_left;
        //                                        else
        //                                            if (camera == additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right)
        //                                                additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].camera_right.targetTexture = additionalS3DCamerasStruct[additionalS3DCamerasStruct.Length - 1].renderTexture_right;
        //#endif
        //        context.ExecuteCommandBuffer(commandBuffer);
        //        commandBuffer.Release();
        //        context.Submit();

        foreach (Camera camera in cameraList)
#if URP
//        if (GetComponent<Camera>() == camera_left)
//            camera_left.targetTexture = renderTexture_left;
//        else
//            if (GetComponent<Camera>() == camera_right)
//                camera_right.targetTexture = renderTexture_right;

        if (camera == camera_left)
            camera.targetTexture = renderTexture_left;
        else
            if (camera == camera_right)
                camera.targetTexture = renderTexture_right;
#elif HDRP
        if (camera == cam)
            camera.targetTexture = renderTexture;
        else
            if (camera == camera_left)
                camera.targetTexture = renderTexture_left;
            else
                if (camera == camera_right)
                    camera.targetTexture = renderTexture_right;
                else
                    if (camera == canvasCamera)
                        camera.targetTexture = canvasRenderTexture;
                    else
                        if (camera == canvasCamera_left)
                            camera.targetTexture = canvasRenderTexture_left;
                        else
                            if (camera == canvasCamera_right)
                                camera.targetTexture = canvasRenderTexture_right;
                            else
                                //for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                                //    if (additionalS3DCamerasStruct[i].camera)
                                //        if (camera == additionalS3DCamerasStruct[i].camera_left)
                                //            additionalS3DCamerasStruct[i].camera_left.targetTexture = additionalS3DCamerasStruct[i].renderTexture_left;
                                //        else
                                //            if (camera == additionalS3DCamerasStruct[i].camera_right)
                                //                additionalS3DCamerasStruct[i].camera_right.targetTexture = additionalS3DCamerasStruct[i].renderTexture_right;
                                if (additionalS3DTopmostCameraIndex != -1)
                                        if (camera == additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera)
                                            additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera.targetTexture = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].renderTexture;
                                        else
                                            if (camera == additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_left)
                                                additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_left.targetTexture = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].renderTexture_left;
                                            else
                                                if (camera == additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_right)
                                                    additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].camera_right.targetTexture = additionalS3DCamerasStruct[additionalS3DTopmostCameraIndex].renderTexture_right;

        //if (camera == cameraToResetLeft)
        //    camera.targetTexture = renderTextureToResetLeft;
        //else
        //    if (camera == cameraToResetRight)
        //        camera.targetTexture = renderTextureToResetRight;
#endif
    }
//#endif

//    void CanvasCameraRenderTexture_Set()
//    {
//#if HDRP
//            canvasCamera.targetTexture = canvasRenderTexture;
//#elif URP
//        //if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
//        //{
//            //canvasCamera.clearFlags = CameraClearFlags.Depth;
//            canvasCamera.targetTexture = canvasRenderTexture;
//        //}
//        //else
//        //{
//        //    canvasCamera.clearFlags = CameraClearFlags.Nothing;
//        //    canvasCamera.targetTexture = renderTexture;
//        //}
//#else
//        canvasCamera.targetTexture = renderTexture;
//#endif
//    }

//    void CanvasCameraLeftRenderTexture_Set()
//    {
//#if HDRP
//            canvasCamera_left.targetTexture = canvasRenderTexture_left;
//#elif URP
//        //if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
//        //{
//            //canvasCamera_left.clearFlags = CameraClearFlags.Depth;
//            canvasCamera_left.targetTexture = canvasRenderTexture_left;
//        //}
//        //else
//        //{
//        //    canvasCamera_left.clearFlags = CameraClearFlags.Nothing;
//        //    canvasCamera_left.targetTexture = renderTexture_left;
//        //}
//#else
//        canvasCamera_left.targetTexture = renderTexture_left;
//#endif
//    }

//    void CanvasCameraRightRenderTexture_Set()
//    {
//#if HDRP
//            canvasCamera_right.targetTexture = canvasRenderTexture_right;
//#elif URP
//        //if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
//        //{
//            //canvasCamera_right.clearFlags = CameraClearFlags.Depth;
//            canvasCamera_right.targetTexture = canvasRenderTexture_right;
//        //}
//        //else
//        //{
//        //    canvasCamera_right.clearFlags = CameraClearFlags.Nothing;
//        //    canvasCamera_right.targetTexture = renderTexture_right;
//        //}
//#else
//        canvasCamera_right.targetTexture = renderTexture_right;
//#endif
//    }

    //void RenderQuad(ScriptableRenderContext context, Camera camera)
    void RenderQuad(ScriptableRenderContext context, Camera[] cameraList)
    //void RenderQuad(ScriptableRenderContext context, List<Camera> cameraList)
    {
        if (debug)
        foreach (Camera camera in cameraList)
            Debug.Log(camera + " RenderQuad " + Time.time);

        commandBuffer = new CommandBuffer();

        foreach (Camera camera in cameraList)
        if (camera == cam)
        {
            //commandBuffer = new CommandBuffer();
            //commandBuffer.name = "S3DRenderQuad";

            RenderTexture.active = null; //fixes rect inside rect
            //render clip space screen quad using S3DMaterial preset vertices buffer with:
#if URP //HDRP not support OpenGL
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                //CustomBlit(false, false);
                CustomBlit(null, null, S3DMaterial, pass, false, false);
            else
#endif
                commandBuffer.DrawProcedural(Matrix4x4.identity, S3DMaterial, pass, MeshTopology.Quads, 4); //this need "nearClipPlane = -1" for same quad position as using Blit with custom camera Rect coordinates
            
            //commandBuffer.Blit(null, cam.activeTexture, S3DMaterial, pass); //or this //not working with OpenGL core
            //commandBuffer.Blit(null, null as RenderTexture, S3DMaterial, pass); //or this //not working with OpenGL core

            //below works without commandBuffer
            //Graphics.Blit(null, null, S3DMaterial, pass);
            //S3DMaterial.SetPass(pass);
            //Graphics.DrawProceduralNow(MeshTopology.Quads, 4);

            //context.ExecuteCommandBuffer(commandBuffer);
            //commandBuffer.Release();
            //context.Submit();
        }

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Release();
        context.Submit();
    }

    //void RenderTexture_BlitToScreen(ScriptableRenderContext context, Camera camera)
    void RenderTexture_BlitToScreen(ScriptableRenderContext context, Camera[] cameraList)
    {
        if (debug)
        foreach (Camera camera in cameraList)
            Debug.Log(camera + " RenderTexture_BlitToScreen " + Time.time);

        commandBuffer = new CommandBuffer();

        foreach (Camera camera in cameraList)
#if URP
//        if (GetComponent<Camera>() == camera_left)
//        {
//            commandBuffer = new CommandBuffer();
//            camera_left.targetTexture = null;
//            commandBuffer.Blit(renderTexture_left, null as RenderTexture);
//            context.ExecuteCommandBuffer(commandBuffer);
//            commandBuffer.Release();
//            context.Submit();
//        }
//        else
//            if (GetComponent<Camera>() == camera_right)
//            {
//                commandBuffer = new CommandBuffer();
//                camera_right.targetTexture = null;
//                //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);
//                commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);
//                context.ExecuteCommandBuffer(commandBuffer);
//                commandBuffer.Release();
//                context.Submit();
//            }

        if (camera == camera_left)
        {
            camera.targetTexture = null;
            //commandBuffer.Blit(renderTexture_left, null as RenderTexture);
            ////commandBuffer.Blit(renderTexture_left, null as RenderTexture, S3DMaterial, pass);
            ////CustomBlit(renderTexture_left, null, S3DMaterial, pass);
            ////CustomBlit(false, false);
            ////CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);

            ////if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore)
            ////    CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
            ////else
            ////    commandBuffer.Blit(renderTexture_left, null as RenderTexture, S3DMaterial, pass);

            //if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore) //OpenGL not working(black screen and RenderTexture show once in rare case) with commandBuffer.Blit via material
            if (SystemInfo.graphicsDeviceType.ToString().Contains("OpenGL"))
            {
                ////CustomBlit(renderTexture_right, null, S3DMaterial, pass);

                ////Rect r = camera.rect;
                ////camera.rect = Rect.MinMaxRect(0, 0, 1, 1);
                //CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                ////camera.rect = r;

                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                    //CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                    CustomBlit(renderTexture_left, null, S3DMaterial, pass, method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                    //CustomBlit(renderTexture_left, null, null, pass, method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                    //CustomBlit(renderTexture_left, null, S3DMaterial, pass, false, false);
                else
                {
                    RenderTexture.active = null;
                    S3DMaterial.SetTexture("_MainTex", renderTexture_left);
                    commandBuffer.DrawProcedural(Matrix4x4.identity, S3DMaterial, pass, MeshTopology.Quads, 4); //this need "nearClipPlane = -1" for same quad position as using Blit with custom camera Rect coordinates
                }
            }
            else
                commandBuffer.Blit(renderTexture_left, null as RenderTexture, S3DMaterial, pass);
        }
        else
            if (camera == camera_right)
            {
                camera.targetTexture = null;
                //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);
                //commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);

                //Debug.Log("SystemInfo.graphicsDeviceType " + SystemInfo.graphicsDeviceType);
                //CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);

                ////if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore) //OpenGL not working(black screen and RenderTexture show once in rare case) with commandBuffer.Blit via material
                //if (SystemInfo.graphicsDeviceType.ToString().Contains("OpenGL"))
                //{
                //    //CustomBlit(renderTexture_right, null, S3DMaterial, pass);

                //    //Rect r = camera.rect;
                //    //camera.rect = Rect.MinMaxRect(0, 0, 1, 1);
                //    CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                //    //camera.rect = r;
                //}
                //else
                //    commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);

                commandBuffer.Blit(renderTexture_right, null as RenderTexture);
            }
#elif HDRP
        if (camera == topmostCamera)
        {
            camera.targetTexture = null;
            commandBuffer.Blit(renderTexture, null as RenderTexture);
        }
        else
            if (camera == topmostCamera_left)
            {
                //cameraToResetLeft = camera;
                //renderTextureToResetLeft = camera.targetTexture;
                camera.targetTexture = null;
                //commandBuffer.Blit(renderTexture_left, null as RenderTexture);

                if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
                    commandBuffer.Blit(renderTexture_left, null as RenderTexture, S3DMaterial, pass);
                else
                    commandBuffer.Blit(renderTexture_left, null as RenderTexture);
            }
            else
                if (camera == topmostCamera_right)
                {
                    //cameraToResetRight = camera;
                    //renderTextureToResetRight = camera.targetTexture;
                    camera.targetTexture = null;
                    //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);

                    //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
                    //    commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);
                    //else
                    //    commandBuffer.Blit(renderTexture_right, null as RenderTexture);

                    commandBuffer.Blit(renderTexture_right, null as RenderTexture);
                }

        //if (camera == camera_left && camera == topmostCamera_left)
        //{
        //    cameraToResetLeft = camera_left;
        //    renderTextureToResetLeft = renderTexture_left;
        //    camera_left.targetTexture = null;
        //    commandBuffer.Blit(renderTexture_left, null as RenderTexture);
        //}
        //else
        //if (camera == camera_right && camera == topmostCamera_right)
        //{
        //    cameraToResetRight = camera_right;
        //    renderTextureToResetRight = renderTexture_right;
        //    camera_right.targetTexture = null;

        //    if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //        commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);
        //    else
        //        commandBuffer.Blit(renderTexture_right, null as RenderTexture);
        //}
        //else
        //if (camera == canvasCamera_left)
        //{
        //    cameraToResetLeft = canvasCamera_left;
        //    renderTextureToResetLeft = canvasRenderTexture_left;
        //    canvasCamera_left.targetTexture = null;
        //    //RenderTexture.active = null;
        //    commandBuffer.Blit(renderTexture_left, null as RenderTexture);
        //}
        //else
        //    if (camera == canvasCamera_right)
        //    {
        //        cameraToResetRight = canvasCamera_right;
        //        renderTextureToResetRight = canvasRenderTexture_right;
        //        canvasCamera_right.targetTexture = null;
        //    //RenderTexture.active = null;
        //        //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);

        //        if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //            commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);
        //        else
        //            commandBuffer.Blit(renderTexture_right, null as RenderTexture);
        //    }
#endif

        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Release();
        context.Submit();
    }

    //void RenderTexture_BlitToRenderTexture(ScriptableRenderContext context, Camera camera)
    void RenderTexture_BlitToRenderTexture(ScriptableRenderContext context, Camera[] cameraList)
    {
        ////if (debug) Debug.Log(camera + " RenderTexture_BlitToRenderTexture " + Time.time);
        //if (debug)
        //foreach (Camera camera in cameraList)
        //    Debug.Log(camera + " RenderTexture_BlitToRenderTexture " + Time.time);

        commandBuffer = new CommandBuffer();
#if HDRP
        foreach (Camera camera in cameraList)
        if (camera == topmostCamera)
        {
            //foreach (var c in additionalS3DCamerasStruct)
            //    if (c.camera && topmostCamera != c.camera)
            //        commandBuffer.Blit(c.camera.targetTexture, renderTexture, S3DPanelMaterial);

            commandBuffer.Blit(topmostCamera.targetTexture, renderTexture, S3DPanelMaterial);
        }
        else
            if (camera == topmostCamera_left)
            {
                //camera.targetTexture = null;

                //foreach (var c in additionalS3DCamerasStruct)
                //    if (c.camera && topmostCamera_left != c.camera_left)
                //        commandBuffer.Blit(c.camera_left.targetTexture, renderTexture_left, S3DPanelMaterial);

                commandBuffer.Blit(topmostCamera_left.targetTexture, renderTexture_left, S3DPanelMaterial);
            }
            else
                if (camera == topmostCamera_right)
                {
                    //camera.targetTexture = null;

                    //foreach (var c in additionalS3DCamerasStruct)
                    //    if (c.camera && topmostCamera_right != c.camera_right)
                    //        commandBuffer.Blit(c.camera_right.targetTexture, renderTexture_right, S3DPanelMaterial);

                    commandBuffer.Blit(topmostCamera_right.targetTexture, renderTexture_right, S3DPanelMaterial);
                }
                else
                    foreach (var c in additionalS3DCamerasStruct)
                        if (camera == c.camera && topmostCamera != c.camera)
                            commandBuffer.Blit(c.camera.targetTexture, renderTexture, S3DPanelMaterial);
                        else
                            if (camera == c.camera_left && topmostCamera_left != c.camera_left)
                                commandBuffer.Blit(c.camera_left.targetTexture, renderTexture_left, S3DPanelMaterial);
                            else
                                if (camera == c.camera_right && topmostCamera_right != c.camera_right)
                                    commandBuffer.Blit(c.camera_right.targetTexture, renderTexture_right, S3DPanelMaterial);
#endif
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Release();
        context.Submit();
    }

//    //void PostRenderContext(ScriptableRenderContext context, List<Camera> cameraList)
//    void PostRenderContext(ScriptableRenderContext context, Camera camera)
//    {
//        //if (debug) Debug.Log(camera + " PostRenderContext " + Time.time);
//        //foreach (Camera camera in cameraList)
//            //if (debug) Debug.Log("cameraList.Count " + cameraList.Count + " " + Time.time);

//        commandBuffer = new CommandBuffer();
//        //commandBuffer.name = "S3DCamera";
//        //commandBuffer.name = "S3DPostRender";

//        //foreach (Camera camera in cameraList)
//            if (camera == cam)
//            {
//                if (S3DEnabled)
//                {
//                    //if (debug) Debug.Log("camera == cam " + Time.time);
//                    //commandBuffer = new CommandBuffer();
//                    //commandBuffer.name = "screenQuad";

//                    ////Graphics.Blit(leftCamAdditionalRT, renderTexture_left, S3DPanelMaterial);
//                    ////Graphics.Blit(rightCamAdditionalRT, renderTexture_right, S3DPanelMaterial);
//                    //commandBuffer.Blit(leftCamAdditionalRT, renderTexture_left, S3DPanelMaterial);
//                    //commandBuffer.Blit(rightCamAdditionalRT, renderTexture_right, S3DPanelMaterial);

//                    //#if HDRP
//                    //                foreach (var c in additionalS3DCamerasStruct)
//                    //                {
//                    //                    commandBuffer.Blit(c.renderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                    commandBuffer.Blit(c.renderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //                }

//                    //                //if (canvasCamera && canvasCamera.isActiveAndEnabled)
//                    //                //{
//                    //                //    commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                //    commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //                //}

//                    //                if (canvasCamera && canvasCamera_left.isActiveAndEnabled)
//                    //                {
//                    //                    //if (debug) Debug.Log("========================================================================== ");
//                    //                    commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                    commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //                }

//                    //                //commandBuffer.Blit(renderTexture_right, null as RenderTexture);
//                    //#endif

//                    //Graphics.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //Graphics.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);

//                    //            if (canvasCamera && canvasCamera.isActiveAndEnabled)
//                    //            {
//                    //#if URP
//                    //            if (frameTime != Time.time) //set targetTexture cause call this 5 times per frame so use condition to execute below commands only once per frame
//                    //            {
//                    //                frameTime = Time.time;

//                    //                if (oddFrame)
//                    //                {
//                    //                    if (method == Method.SideBySide_HMD)
//                    //                        canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
//                    //                    else
//                    //                        canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

//                    //                    if (method == Method.Interlace_Horizontal)
//                    //                        canvasCamMatrix[1, 3] = -oneRowShift;

//                    //                    canvasCamera.projectionMatrix = canvasCamMatrix;
//                    //                    canvasCamera.targetTexture = renderTexture_left;
//                    //                }
//                    //                else
//                    //                {
//                    //                    if (method == Method.SideBySide_HMD)
//                    //                        canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
//                    //                    else
//                    //                        canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

//                    //                    if (method == Method.Interlace_Horizontal)
//                    //                        canvasCamMatrix[1, 3] = 0;

//                    //                    canvasCamera.projectionMatrix = canvasCamMatrix;
//                    //                    canvasCamera.targetTexture = renderTexture_right;
//                    //                }

//                    //            UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);
//                    //            }
//                    //#elif HDRP
//                    //                commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //#endif
//                    //            }
//                    //            //else
//                    //            //{
//                    //            //    commandBuffer.Blit(null, renderTexture_left, S3DPanelMaterial);
//                    //            //    commandBuffer.Blit(null, renderTexture_right, S3DPanelMaterial);
//                    //            //    if (debug) Debug.Log("!S3DEnabled && canvasCamera ////////////////////////////////////////////////////////////////////////////");
//                    //            //}

//                    RenderTexture.active = null; //fixes rect in rect duplication
//                    //render clip space screen quad using S3DMaterial preset vertices buffer with:
//                    commandBuffer.DrawProcedural(Matrix4x4.identity, S3DMaterial, pass, MeshTopology.Quads, 4); //this need "nearClipPlane = -1" for same quad position as using Blit with custom camera Rect coordinates
//                    //commandBuffer.Blit(null, cam.activeTexture, S3DMaterial, pass); //or this

//                    //context.ExecuteCommandBuffer(commandBuffer);
//                    //commandBuffer.Release();
//                    //context.Submit();
//                }
//                //else
//                ////if (additionalS3DCameras.Count != 0 || canvasCamera && canvasCamera.isActiveAndEnabled)
//                //{
//                //    //if (debug) Debug.Log("PostRenderContext additionalS3DCameras.Count != 0");
//                //    //#if UNITY_EDITOR
//                //    if (additionalS3DCameras.Count != 0 || canvasCamera && canvasCamera.isActiveAndEnabled)
//                //    {
//                //        cam.targetTexture = null;
//                //        commandBuffer.Blit(renderTexture, null as RenderTexture);
//                //    }
//                //    //#else
//                //    //                    if (additionalS3DCamerasStruct.Length == 0 && !(canvasCamera && canvasCamera.isActiveAndEnabled))
//                //    //                        commandBuffer.Blit(renderTexture, null as RenderTexture);
//                //    //#endif

//                //    //foreach (var c in additionalS3DCamerasStruct)
//                //    //    commandBuffer.Blit(c.renderTexture, renderTexture, S3DPanelMaterial);

//                //    //if (canvasCamera && canvasCamera.isActiveAndEnabled)
//                //    //    commandBuffer.Blit(canvasRenderTexture, renderTexture, S3DPanelMaterial);

//                //    //commandBuffer.Blit(renderTexture, null as RenderTexture);
//                //}
//            }
////#if HDRP
//            else
//            {
////#if HDRP
////#if HDRP || URP
//#if URP
//                //if (method == Method.Two_Displays)
//                //if (!cam.enabled)
//                if (camera == camera_left)
//                {
//                //if (debug) Debug.Log("camera = camera_left");
//                //commandBuffer = new CommandBuffer();
//                //commandBuffer.name = "camera_left";
////#if HDRP
//                    //                    foreach (var c in additionalS3DCamerasStruct)
//                    //                    {
//                    //                        //if (debug) Debug.Log("foreach (var c in additionalS3DCamerasStruct)");
//                    //                        commandBuffer.Blit(c.renderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                        //commandBuffer.Blit(c.renderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //                    }

//                    //                    if (canvasCamera && canvasCamera_left.isActiveAndEnabled)
//                    //                    {
//                    //                        //if (debug) Debug.Log("========================================================================== ");
//                    //                        commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//                    //                        //commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                    //                    }
//                    //#endif

//                    //if (method == Method.Two_Displays)
//                    //if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                    if (
////#if !URP
////                    method == Method.Two_Displays || 
////#endif
//                    method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                    {
//                        //#if UNITY_EDITOR
//                        //camera_left.targetTexture = null; //kill FPS from 32 to 22 but required only in player to blit render texture to the screen
//                                                          //commandBuffer.Blit(renderTexture_left, null as RenderTexture);
//                                                          //#else
////                        if (
//////#if HDRP
////                            additionalS3DCamerasStruct.Length == 0 && 
//////#endif
////                            !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
////                        {
//                            camera_left.targetTexture = null; //kill FPS from 32 to 22 but required only in player to blit render texture to the screen
//                            commandBuffer.Blit(renderTexture_left, null as RenderTexture);
//                        //}
//                        //#endif
//                    }
//                    //else
//                    //    if (method == Method.Sequential && !optimize && !oddFrame)
//                    //    {
//                    //        //if (debug) Debug.Log("camera_left.targetTexture = nullRT " + Time.time);
//                    //        camera_left.targetTexture = nullRT;
//                    //    }

//                    //context.ExecuteCommandBuffer(commandBuffer);
//                    //commandBuffer.Release();
//                    //context.Submit();
//                    //camera_left.targetTexture = renderTexture_left;
////#endif
//                }
//                else
//                    if (camera == camera_right)
//                    {
//                //if (debug) Debug.Log("camera = camera_right");
//                //commandBuffer = new CommandBuffer();
//                //commandBuffer.name = "camera_right";
////#if HDRP
//                        //                        foreach (var c in additionalS3DCamerasStruct)
//                        //                            commandBuffer.Blit(c.renderTexture_right, renderTexture_right, S3DPanelMaterial);

//                        //                        if (canvasCamera && canvasCamera_right.isActiveAndEnabled)
//                        //                            commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//                        //#endif

//                        //if (method == Method.Two_Displays)
//                        //if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                        if (
////#if !URP
////                        method == Method.Two_Displays || 
////#endif
//                        method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                        {
//                            //#if UNITY_EDITOR
//                            //camera_right.targetTexture = null; //kill FPS from 32 to 22 but required only in player to blit render texture to the screen
//                                                               //commandBuffer.Blit(renderTexture_right, null as RenderTexture);
//                                                               //#else
////                            if (
//////#if HDRP
////                            additionalS3DCamerasStruct.Length == 0 && 
//////#endif
////                            !(canvasCamera_right && canvasCamera_right.isActiveAndEnabled))
////                            {
//                                camera_right.targetTexture = null; //kill FPS from 32 to 22 but required only in player to blit render texture to the screen
//                                //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);
//                                commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);
//                            //}
//                            //#endif
//                        }
//                        //else
//                        //    if (method == Method.Sequential && !optimize && !oddFrame)
//                        //        camera_right.targetTexture = nullRT;

//                        //context.ExecuteCommandBuffer(commandBuffer);
//                        //commandBuffer.Release();
//                        //context.Submit();
////#endif
////                        if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
////#if HDRP
////                            camera_right.GetComponent<HDAdditionalCameraData>().invertFaceCulling = false;
////#else
////                            GL.invertCulling = false;
////#endif
//                    }
//#endif

//#if HDRP
//                else
//                //if (canvasCamera && canvasCamera.isActiveAndEnabled)
//                //if (canvasCamera)
//                if (camera == canvasCamera)
//                {
//                    //if (debug) Debug.Log("camera == canvasCamera");

//                    //if (canvasCamera.isActiveAndEnabled)
//                    //{
//                    //if (debug) Debug.Log("blit2");
//                    //#if HDRP
////#if HDRP || URP
////#if URP
////                    if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
////#endif
//                        commandBuffer.Blit(canvasRenderTexture, renderTexture, S3DPanelMaterial);
////#endif
//                        canvasCamera.targetTexture = null;
//                        commandBuffer.Blit(renderTexture, null as RenderTexture);
//                    //}

////#if !UNITY_EDITOR
////                //if (additionalS3DCameras.Count != 0)
////                    commandBuffer.Blit(renderTexture, null as RenderTexture);
////#endif
//                }
//                else
//                    if (camera == canvasCamera_left)
//                    {
//                        //if (debug) Debug.Log("camera == canvasCamera_left " + camera + " " + Time.time);
////#if HDRP
////#if HDRP || URP
////#if URP
////                      if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
////#endif
//                        //commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
////#endif
////#if !UNITY_EDITOR
//                        //if (method == Method.Two_Displays)
//                        if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                        {
//                            canvasCamera_left.targetTexture = null;
//                            commandBuffer.Blit(renderTexture_left, null as RenderTexture);
//                            //RenderTexture rta = RenderTexture.active;
//                            //RenderTexture.active = renderTexture_left;
//                            //GL.Clear(true, true, Color.green);
//                            //RenderTexture.active = rta;
//                        }
////#endif
//                    }
//                    else
//                        if (camera == canvasCamera_right)
//                        {
//                            //if (debug) Debug.Log("camera == canvasCamera_right " + camera + " " + Time.time);
////#if HDRP
////#if HDRP || URP
////#if URP
////                          if (cam.rect != Rect.MinMaxRect(0, 0, 1, 1))
////#endif
//                            //commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
////#endif
////#if !UNITY_EDITOR
//                            //if (method == Method.Two_Displays)
//                            //if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
//                            //{
//                            //    //RenderTexture rt = new RenderTexture(renderTexture_right);
//                            //    //RenderTexture rta = RenderTexture.active;
//                            //    ////RenderTexture.active = null;
//                            //    ////S3DMaterial.SetTexture("_MainTex", rta);
//                            //    //commandBuffer.Blit(rta, rt);
//                            //    //canvasCamera_right.targetTexture = null; //URP with overlay cameras Blit dosn't work
//                            //    camera_right.targetTexture = null; //URP with overlay cameras Blit works
//                            //    //canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
//                            //    //commandBuffer.Blit(rt, null as RenderTexture, S3DMaterial, pass);
//                            //    ////commandBuffer.Blit(renderTexture_right, null as RenderTexture);
//                            //    ////canvasCamera_right.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;

//                            //    if (method == Method.Two_Displays)
//                            //        commandBuffer.Blit(renderTexture_right, null as RenderTexture);
//                            //    else
//                            //        //commandBuffer.Blit(renderTexture_right, null as RenderTexture, RenderTextureFlipMaterial);
//                            //        commandBuffer.Blit(renderTexture_right, null as RenderTexture, S3DMaterial, pass);

//                            //    //RenderTexture rta = RenderTexture.active;
//                            //    //RenderTexture.active = renderTexture_right;
//                            //    //GL.Clear(true, true, Color.blue);
//                            //    //RenderTexture.active = rta;
//                            //}
////#endif
//                        }
//                        else

//                //            foreach (var c in additionalS3DCamerasStruct)
//                //                if (c.camera == camera)
//                //                {
//                //                    commandBuffer.Blit(c.renderTexture, renderTexture, S3DPanelMaterial);
//                ////#if UNITY_EDITOR
//                ////                    cam.targetTexture = null;
//                ////#endif
//                //                    //commandBuffer.Blit(renderTexture, null as RenderTexture);
//                //                }

//                for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
//                    if (camera == additionalS3DCamerasStruct[i].camera)
//                    {
//                        commandBuffer.Blit(additionalS3DCamerasStruct[i].renderTexture, renderTexture, S3DPanelMaterial);
////#if !UNITY_EDITOR
//                        if (i == additionalS3DCamerasStruct.Length - 1 && !(canvasCamera && canvasCamera.isActiveAndEnabled))
//                        {
//                            //if (debug) Debug.Log("blit1");
//                            additionalS3DCamerasStruct[i].camera.targetTexture = null;
//                            commandBuffer.Blit(renderTexture, null as RenderTexture);
//                        }
////#endif
//                    }
//                    else
//                        if (camera == additionalS3DCamerasStruct[i].camera_left)
//                        {
//                            commandBuffer.Blit(additionalS3DCamerasStruct[i].renderTexture_left, renderTexture_left, S3DPanelMaterial);
//    //#if !UNITY_EDITOR
//                            //if (method == Method.Two_Displays && i == additionalS3DCamerasStruct.Length - 1 && !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
//                            if ((method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY) 
//                            && i == additionalS3DCamerasStruct.Length - 1 && !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
//                            {
//                                additionalS3DCamerasStruct[i].camera_left.targetTexture = null;
//                                commandBuffer.Blit(renderTexture_left, null as RenderTexture);
//                            }
////#endif
//                        }
//                        else
//                            if (camera == additionalS3DCamerasStruct[i].camera_right)
//                            {
//                                commandBuffer.Blit(additionalS3DCamerasStruct[i].renderTexture_right, renderTexture_right, S3DPanelMaterial);
//        //#if !UNITY_EDITOR
//                                //if (method == Method.Two_Displays && i == additionalS3DCamerasStruct.Length - 1 && !(canvasCamera_right && canvasCamera_right.isActiveAndEnabled))
//                                if ((method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY) 
//                                && i == additionalS3DCamerasStruct.Length - 1 && !(canvasCamera_right && canvasCamera_right.isActiveAndEnabled))
//                                {
//                                    additionalS3DCamerasStruct[i].camera_right.targetTexture = null;
//                                    commandBuffer.Blit(renderTexture_right, null as RenderTexture);
//                                }
//        //#endif
//                            }
//#endif
////#endif
//            }

//        //commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//        //canvasCamera_left.targetTexture = null;
//        //commandBuffer.Blit(renderTexture_left, null as RenderTexture);

//        //commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//        //canvasCamera_right.targetTexture = null;
//        //commandBuffer.Blit(renderTexture_right, null as RenderTexture);
////#endif

//        context.ExecuteCommandBuffer(commandBuffer);
//        commandBuffer.Release();
//        context.Submit();

////        commandBuffer = new CommandBuffer();
////        commandBuffer.name = "S3DCamera2";

////        foreach (Camera camera in cameraList)
////            if (camera == canvasCamera_left)
////                {
////                    if (debug) Debug.Log("camera == canvasCamera_left " + camera + " " + Time.time);
////                    commandBuffer.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
////#if !UNITY_EDITOR
////                    if (method == Method.Two_Displays)
////                        commandBuffer.Blit(renderTexture_left, null as RenderTexture);
////#endif
////                }
////                else
////                    if (camera == canvasCamera_right)
////                    {
////                        if (debug) Debug.Log("camera == canvasCamera_right " + camera + " " + Time.time);
////                        commandBuffer.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
////#if !UNITY_EDITOR
////                    if (method == Method.Two_Displays)
////                        commandBuffer.Blit(renderTexture_right, null as RenderTexture);
////#endif
////                    }

////        context.ExecuteCommandBuffer(commandBuffer);
////        commandBuffer.Release();
////        context.Submit();

//        //Graphics.Blit(leftCamAdditionalRT, renderTexture_left, S3DPanelMaterial);
//        //Graphics.Blit(rightCamAdditionalRT, renderTexture_right, S3DPanelMaterial);

//        //        if (S3DEnabled && camera == canvasCamera)
//        //        {
//        //#if URP
//        //            //if (debug) Debug.Log(Time.time);
//        //            //canvasCamera.enabled = true;
//        //            //canvasCamera.enabled = false;

//        //            //if (oddFrame)
//        //            //{
//        //            //    canvasCamera.targetTexture = renderTexture_left;
//        //            //    //canvasCamera.Render();
//        //            //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);
//        //            //}
//        //            //else
//        //            //{
//        //            //    canvasCamera.targetTexture = renderTexture_right;
//        //            //    //canvasCamera.Render();
//        //            //}

//        //            if (frameTime != Time.time) //set targetTexture cause call this 5 times per frame so use condition to execute below commands only once per frame
//        //            {
//        //                frameTime = Time.time;
//        //                //if (debug) Debug.Log("frameTime != Time.time " + frameTime);
//        //                //Matrix4x4 canvasCamMatrix = canvasCamera.projectionMatrix;
//        //                //canvasCamMatrix = canvasCamera.projectionMatrix;

//        //                if (oddFrame)
//        //                {
//        //                    if (method == Method.SideBySide_HMD)
//        //                        canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
//        //                    else
//        //                        canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

//        //                    if (method == Method.Interlace_Horizontal)
//        //                        canvasCamMatrix[1, 3] = -oneRowShift;

//        //                    canvasCamera.projectionMatrix = canvasCamMatrix;
//        //                    canvasCamera.targetTexture = renderTexture_left;
//        //                    //canvasCamera.targetTexture = canvasRenderTexture_left;
//        //                    //canvasCamera.Render();
//        //                    //Graphics.CopyTexture(canvasRenderTexture_left, renderTexture_left);
//        //                }
//        //                else
//        //                {
//        //                    if (method == Method.SideBySide_HMD)
//        //                        canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
//        //                    else
//        //                        canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

//        //                    if (method == Method.Interlace_Horizontal)
//        //                        canvasCamMatrix[1, 3] = 0;

//        //                    canvasCamera.projectionMatrix = canvasCamMatrix;
//        //                    canvasCamera.targetTexture = renderTexture_right;
//        //                    //canvasCamera.targetTexture = canvasRenderTexture_right;
//        //                    //canvasCamera.Render();
//        //                    //Graphics.CopyTexture(canvasRenderTexture_right, renderTexture_right);
//        //                }

//        //                //Graphics.CopyTexture(canvasRenderTexture_left, renderTexture_left);
//        //                //Graphics.CopyTexture(canvasRenderTexture_right, renderTexture_right);
//        //                //Graphics.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//        //                //Graphics.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//        //                //canvasCamera.enabled = false;


//        //                //canvasCamera.projectionMatrix = canvasCamMatrix;
//        //                //#if URP
//        //                UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);
//        //                //#endif
//        //            }

//        //            //UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);

//        //            //if (oddFrame)
//        //            //    UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);

//        //            //canvasCamera.Render();
//        //            //Debug.Break();
//        //            //canvasCamera.enabled = false;
//        //            //if (debug) Debug.Log(camera);
//        //            //if (debug) Debug.Log(oddFrame + " PostRenderContext camera == canvasCamera " + Time.time);
//        //            //UniversalRenderPipeline.RenderSingleCamera(context, canvasCamera);
//        ////#elif HDRP
//        ////            //Graphics.Blit(leftCamAdditionalRT, renderTexture_left, S3DPanelMaterial);
//        ////            //Graphics.Blit(rightCamAdditionalRT, renderTexture_right, S3DPanelMaterial);

//        ////            Graphics.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
//        ////            Graphics.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
//        //#endif
//        //        }
//    }

//    //void RenderBlit(ScriptableRenderContext context, Camera camera) //render context for SRP
//    void RenderBlit(ScriptableRenderContext context, List<Camera> cameraList) //render context for SRP
//    {
//        foreach (Camera camera in cameraList)
//            if (camera == cam)
//            {
//                commandBuffer = new CommandBuffer();
//                commandBuffer.name = "S3DCamera2";

//#if UNITY_EDITOR
//                cam.targetTexture = null;
//#endif
//                commandBuffer.Blit(renderTexture, null as RenderTexture);

//                context.ExecuteCommandBuffer(commandBuffer);
//                commandBuffer.Release();
//                context.Submit();
//            }
//    }
#else

    //ignored in SRP(URP or HDRP) but in default render via cam buffer even empty function give fps gain from 294 to 308
    void OnRenderImage(RenderTexture source, RenderTexture destination) //works only in the default render pipeline
    //void OnPostRender() //works only in the default render pipeline //not working if antialiasing set in quality settings
    //void PostRender(Camera c) //works only in the default render pipeline
    {
        //if (debug) Debug.Log("OnRenderImage");
        //if (debug) Debug.Log("OnRenderImage Camera.current: " + Camera.current);

        //if (defaultRender) //commented till SRP don't go here
        if (S3DEnabled)
        {
            //if (method == Method.Two_Displays || method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
            //{
            //    Camera.SetupCurrent(camera_left);
            //    camera_left.targetTexture = null;
            //    Graphics.Blit(renderTexture_left, null as RenderTexture);

            //    Camera.SetupCurrent(camera_right);
            //    camera_right.targetTexture = null;
            //    Graphics.Blit(renderTexture_right, null, RenderTextureFlipMaterial);
            //}
            //else
            {
                //if (canvasCamera && canvasCamera.isActiveAndEnabled)
                //{
                //    //CanvasCamS3DRender_Set();

                //    Graphics.Blit(canvasRenderTexture_left, renderTexture_left, S3DPanelMaterial);
                //    Graphics.Blit(canvasRenderTexture_right, renderTexture_right, S3DPanelMaterial);
                //}
                ////else
                //Graphics.Blit(null, destination, S3DMaterial, pass);

                Rect camRect = cam.rect;
                cam.rect = Rect.MinMaxRect(0, 0, 1, 1); //set temporary rect required before Graphics.Blit to blit render texture correctly with no fullscreen rect

                //canvasCamera_left.targetTexture = null;

                //if (c == cam)
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                    CustomBlit(null, null, S3DMaterial, pass, false, false);
                else
                {
                    //canvasCamera_left.targetTexture = null;
                    //canvasCamera_right.targetTexture = null;
                    //RenderTexture rt = RenderTexture.active;
                    //cam.targetTexture = null;
                    RenderTexture.active = null;
                    //Graphics.Blit(null, null, S3DMaterial, pass); //not working with OpenGL core
                    S3DMaterial.SetPass(pass);
                    Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
                    //RenderTexture.active = rt;
                }


                //if (method != Method.Two_Displays)
                //Graphics.Blit(source, null, S3DMaterial, pass); //or this
                //else
                //    if (canvasCamera_left && canvasCamera_left.isActiveAndEnabled)
                //    {
                //        canvasCamera_left.targetTexture = renderTexture_left;
                //        canvasCamera_right.targetTexture = renderTexture_right;
                //    }

                cam.rect = camRect;
            }
        }
        else
            Graphics.Blit(source, destination);
    }

    //public RenderTexture renderTextureTarget;
    //public RenderTexture renderTextureActive;

    //void PostRender(Camera c) //Graphics.Blit & Graphics.DrawProceduralNow not working with antialiasing in quality settings and working OK with OnRenderImage
    //{
    //    //if (debug) Debug.Log(c + " PostRender " + Time.time);

    //    //if (c == canvasCamera_right)
    //    //    canvasCamera_right.targetTexture = null;

    //    if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
    //        if (!(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
    //        {
    //            //if (debug) Debug.Log(c + " !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled) " + Time.time);

    //            if (c == camera_left)
    //            {
    //                camera_left.targetTexture = null;
    //                //Graphics.Blit(renderTexture_left, null as RenderTexture);
    //                Graphics.Blit(RenderTexture.active, null as RenderTexture);
    //            }
    //            else
    //                if (c == camera_right)
    //                {
    //                    //canvasCamera_left.targetTexture = renderTexture_left;
    //                    //cam.targetTexture = renderTexture;
    //                    //GL.invertCulling = false;
    //                    camera_right.targetTexture = null;
    //                    //RenderTexture.active = null;
    //                    //RenderTextureFlipMaterial.SetTexture("_MainTex", renderTexture_right);

    //                        //if (method == Method.Two_Displays_MirrorX)
    //                        //{
    //                        //    RenderTextureFlipMaterial.SetInt("_FlipX", 1);
    //                        //    RenderTextureFlipMaterial.SetInt("_FlipY", 0);
    //                        //}
    //                        //else
    //                        //    if (method == Method.Two_Displays_MirrorY)
    //                        //    {
    //                        //        RenderTextureFlipMaterial.SetInt("_FlipX", 0);
    //                        //        RenderTextureFlipMaterial.SetInt("_FlipY", 1);
    //                        //    }

    //                    //RenderTextureFlipMaterial.SetPass(0);
    //                    //Graphics.Blit(renderTexture_right, null, RenderTextureFlipMaterial);
    //                    Graphics.Blit(RenderTexture.active, null, RenderTextureFlipMaterial);
    //                    //Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
    //                }
    //        }
    //        else
    //            if (c == canvasCamera_left)
    //            {
    //                //camera_left.targetTexture = null;
    //                canvasCamera_left.targetTexture = null;
    //                //Graphics.Blit(renderTexture_left, null as RenderTexture);
    //                Graphics.Blit(RenderTexture.active, null as RenderTexture);
    //            }
    //            else
    //                if (c == canvasCamera_right)
    //                {
    //                    //renderTextureTarget = canvasCamera_right.targetTexture;
    //                    //renderTextureActive = RenderTexture.active;
    //                    //if (debug) Debug.Log(c + " !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled) " + Time.time);
    //                    //canvasCamera_left.targetTexture = renderTexture_left;
    //                    //cam.targetTexture = renderTexture;
    //                    //GL.invertCulling = false;
    //                    //camera_right.targetTexture = null;
    //                    //RenderTexture rt = RenderTexture.active;
    //                    canvasCamera_right.targetTexture = null;
    //                    //RenderTexture.active = null;
    //                    //RenderTextureFlipMaterial.SetTexture("_MainTex", renderTexture_right);

    //                    //if (method == Method.Two_Displays_MirrorX)
    //                    //{
    //                    //    RenderTextureFlipMaterial.SetInt("_FlipX", 1);
    //                    //    RenderTextureFlipMaterial.SetInt("_FlipY", 0);
    //                    //}
    //                    //else
    //                    //    if (method == Method.Two_Displays_MirrorY)
    //                    //    {
    //                    //        RenderTextureFlipMaterial.SetInt("_FlipX", 0);
    //                    //        RenderTextureFlipMaterial.SetInt("_FlipY", 1);
    //                    //    }

    //                    //RenderTextureFlipMaterial.SetPass(0);
    //                    //Graphics.Blit(renderTexture_right, null, RenderTextureFlipMaterial);
    //                    Graphics.Blit(RenderTexture.active, null, RenderTextureFlipMaterial);
    //                    //RenderTexture.active = null;
    //                    //RenderTextureFlipMaterial.SetTexture("_MainTex", renderTexture_right);
    //                    //RenderTextureFlipMaterial.SetTexture("_MainTex", renderTextureActive);
    //                    //Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
    //                }
    //}

    void OnRenderImageEvent(RenderTexture src, RenderTexture dest, Camera c)
    //void OnRenderImageEvent(Camera c)
    {
        if (debug) Debug.Log(c + " OnRenderImageEvent " + Time.time);

        //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //    if (!(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
        //    {
        //        //if (debug) Debug.Log(c + " !(canvasCamera_left && canvasCamera_left.isActiveAndEnabled) " + Time.time);

        //        if (c == camera_left)
        //            BlitToScreen(c, null);
        //        else
        //            if (c == camera_right)
        //                BlitToScreen(c, RenderTextureFlipMaterial);
        //    }
        //    else
        //        if (c == canvasCamera_left)
        //            BlitToScreen(c, null);
        //        else
        //            if (c == canvasCamera_right)
        //                BlitToScreen(c, RenderTextureFlipMaterial);

        //if (c.name.Contains("_left"))
        //    BlitToScreen(c, null);
        //else
        //    BlitToScreen(c, RenderTextureFlipMaterial);

        c.targetTexture = null;
#if UNITY_2022_1_OR_NEWER
        Rect r = c.rect;
        c.rect = Rect.MinMaxRect(0, 0, 1, 1);
#endif

        if (c.name.Contains("_left"))
        {
            if (SystemInfo.graphicsDeviceType.ToString().Contains("OpenGL"))
            {
                //Rect r = camera.rect;
                //camera.rect = Rect.MinMaxRect(0, 0, 1, 1);
                //S3DMaterial.SetTexture("_MainTex", renderTexture_left);
                //S3DMaterial.SetPass(pass);
                //CustomBlit(method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY, rt, S3DMaterial);
                //camera.rect = r;
#if UNITY_2022_1_OR_NEWER
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                    CustomBlit(renderTexture_left, null, S3DMaterial, pass, method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                else
                {
                    RenderTexture.active = null;
                    S3DMaterial.SetTexture("_MainTex", renderTexture_left);
                    S3DMaterial.SetPass(pass);
                    Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
                }
#else
                RenderTexture rt = new RenderTexture(RenderTexture.active);
                Graphics.CopyTexture(RenderTexture.active, rt);

                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
                    CustomBlit(rt, null, S3DMaterial, pass, method == Method.Two_Displays_MirrorX, method == Method.Two_Displays_MirrorY);
                else
                {
                    RenderTexture.active = null;
                    S3DMaterial.SetTexture("_MainTex", rt);
                    S3DMaterial.SetPass(pass);
                    Graphics.DrawProceduralNow(MeshTopology.Quads, 4); //169 *2 FPS //299 *2 FPS Mono
                }

                rt.Release();
#endif
            }
            else
                //Graphics.Blit(RenderTexture.active, null as RenderTexture);
                //Graphics.Blit(RenderTexture.active, null, S3DMaterial, pass);
                //Graphics.Blit(src, dest);
                //Graphics.Blit(dest, null as RenderTexture);
                //Graphics.Blit(renderTexture_left, null as RenderTexture);
                //Graphics.Blit(renderTexture_left, null, S3DMaterial, pass);
                //Graphics.Blit(rt, null, S3DMaterial, pass);
                Graphics.Blit(RenderTexture.active, null, S3DMaterial, pass);
        }
        else
            //Graphics.Blit(RenderTexture.active, null, S3DMaterial, pass);
            Graphics.Blit(RenderTexture.active, null as RenderTexture);
            //Graphics.Blit(renderTexture_right, null, S3DMaterial, pass);
            //Graphics.Blit(renderTexture_right, null as RenderTexture);
            //CustomBlit(false, false, renderTexture_right, S3DMaterial);

#if UNITY_2022_1_OR_NEWER
        c.rect = r;
#endif
    }

    //void BlitToScreen(Camera c, Material m)
    //{
    //    c.targetTexture = null;

    //    if (m)
    //        Graphics.Blit(RenderTexture.active, null, RenderTextureFlipMaterial);
    //    else
    //        Graphics.Blit(RenderTexture.active, null as RenderTexture);
    //}

    //void CanvasCamS3DRender_Set()
    //{
    //    if (oddFrame)
    //    {
    //        if (method == Method.SideBySide_HMD)
    //            canvasCamMatrix[0, 3] = (1 - imageOffset * panelDepth) * (swapLR ? -1 : 1);
    //        else
    //            canvasCamMatrix[0, 3] = -imageOffset * (swapLR ? -1 : 1) * panelDepth;

    //        if (method == Method.Interlace_Horizontal)
    //            canvasCamMatrix[1, 3] = -oneRowShift;

    //        canvasCamera.projectionMatrix = canvasCamMatrix;
    //        canvasCamera.targetTexture = canvasRenderTexture_left;
    //    }
    //    else
    //    {
    //        if (method == Method.SideBySide_HMD)
    //            canvasCamMatrix[0, 3] = (-1 + imageOffset * panelDepth) * (swapLR ? -1 : 1);
    //        else
    //            canvasCamMatrix[0, 3] = imageOffset * (swapLR ? -1 : 1) * panelDepth;

    //        if (method == Method.Interlace_Horizontal)
    //            canvasCamMatrix[1, 3] = 0;

    //        canvasCamera.projectionMatrix = canvasCamMatrix;
    //        canvasCamera.targetTexture = canvasRenderTexture_right;
    //    }
    //}

    //void OnPreCull()
    void RenderTexture_Reset(Camera c)
    {
        //if (debug) Debug.Log(c + " RenderTexture_Reset " + Time.time);

        //if (c == canvasCamera_right)
        //    canvasCamera_right.targetTexture = renderTexture_right;

        //canvasCamera_left.targetTexture = renderTexture_left;
        //if (c == cam)

        //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //    if (!(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
        //    {
        //        if (c == camera_left)
        //        {
        //            //camera_left.GetComponent<BlitToScreen>().enabled = true;
        //            camera_left.targetTexture = renderTexture_left;

        //        }
        //        else
        //            if (c == camera_right)
        //            {
        //                //camera_right.GetComponent<BlitToScreen>().enabled = true;
        //                //camera_right.GetComponent<BlitToScreen>().SetMaterial(RenderTextureFlipMaterial);
        //                //camera_right.GetComponent<BlitToScreen>().material = RenderTextureFlipMaterial;
        //                //canvasCamera_left.targetTexture = renderTexture_left;
        //                //cam.targetTexture = renderTexture;
        //                //GL.invertCulling = true;
        //                camera_right.targetTexture = renderTexture_right;
        //            }

        //        //camera_left.GetComponent<BlitToScreen>().enabled = true;
        //        //camera_right.GetComponent<BlitToScreen>().enabled = true;
        //    }
        //    else
        //    {
        //        if (c == canvasCamera_left)
        //        {
        //            //camera_left.GetComponent<BlitToScreen>().enabled = false;
        //            canvasCamera_left.targetTexture = renderTexture_left;
        //        }
        //        else
        //            if (c == canvasCamera_right)
        //            {
        //                //camera_right.GetComponent<BlitToScreen>().enabled = false;
        //                //canvasCamera_right.GetComponent<BlitToScreen>().SetMaterial(RenderTextureFlipMaterial);
        //                //canvasCamera_right.GetComponent<BlitToScreen>().material = RenderTextureFlipMaterial;
        //                //canvasCamera_left.targetTexture = renderTexture_left;
        //                //cam.targetTexture = renderTexture;
        //                //GL.invertCulling = true;
        //                canvasCamera_right.targetTexture = renderTexture_right;
        //            }

        //        //camera_left.GetComponent<BlitToScreen>().enabled = false;
        //        //camera_right.GetComponent<BlitToScreen>().enabled = false;
        //    }

        if (c.name.Contains("_left")) //restore all textures at once here works better(same performance but no black blinks) than restore individual last topmost Cameras before it changes in TopMostCamera_Set()
        {
            //if (debug) Debug.Log(c + " RenderTexture_Reset_left " + Time.time);
            c.targetTexture = renderTexture_left;
        }
        else
        {
            //if (debug) Debug.Log(c + " RenderTexture_Reset_right " + Time.time);
            c.targetTexture = renderTexture_right;
        }

        //if (c == topmostCamera_left)
        //{
        //    if (debug) Debug.Log(c + " RenderTexture_Reset_left " + Time.time);
        //    c.targetTexture = renderTexture_left;
        //}
        //else
        //    if (c == topmostCamera_right)
        //    {
        //        if (debug) Debug.Log(c + " RenderTexture_Reset_right " + Time.time);
        //        c.targetTexture = renderTexture_right;
        //    }
    }
#endif

    //void CustomBlit(RenderTexture source, RenderTexture destination, Material material, int pass) 
    //void CustomBlit(bool flipX, bool flipY) 
void CustomBlit(RenderTexture source, RenderTexture destination, Material material, int pass, bool flipX, bool flipY) 
    {
        // Set new rendertexture as active and feed the source texture into the material
        RenderTexture.active = destination;
        //RenderTexture.active = null;
        int vertexMin = 0;

        if (material)
        {
            material.SetTexture("_MainTex", source);
            material.SetPass(pass);    // start the first rendering pass
            vertexMin = -1;
        }

        // Low-Level Graphics Library calls
        GL.PushMatrix();    // Calculate MVP Matrix and push it to the GL stack
        //GL.Clear(true, true, Color.clear);
        GL.LoadOrtho();    // Set up Ortho-Perspective Transform
        //GL.LoadIdentity();
        //GL.Viewport(new Rect(cam.rect.x * windowSize.x, cam.rect.y * windowSize.y, cam.rect.width * windowSize.x, cam.rect.height * windowSize.y));
        //GL.Viewport(pixelRect);

        //GL.Viewport(new Rect(flipX ? (1 - cam.rect.x) * windowSize.x - cam.pixelWidth : cam.rect.x * windowSize.x, 
        //    flipY ? (1 - cam.rect.y) * windowSize.y - cam.pixelHeight : cam.rect.y * windowSize.y, 
        //    cam.rect.width * windowSize.x, 
        //    cam.rect.height * windowSize.y));

        GL.Begin(GL.QUADS);

        //GL.TexCoord2(flipX ? 1 : 0, flipY ? 1 : 0); // prepare input struct (Texcoord0 (UV's)) for this vertex
        //GL.Vertex3(0.0f, 0.0f, 0.0f); // Finalize and submit this vertex for rendering (bottom left)

        //GL.TexCoord2(flipX ? 1 : 0, flipY ? 0 : 1); // prepare input struct (Texcoord0 (UV's)) for this vertex
        //GL.Vertex3(0.0f, 1.0f, 0.0f); // Finalize and submit this vertex for rendering (top left)

        //GL.TexCoord2(flipX ? 0 : 1, flipY ? 0 : 1); // prepare input struct (Texcoord0 (UV's)) for this vertex
        //GL.Vertex3(1.0f, 1.0f, 0.0f); // Finalize and submit this vertex for rendering  (top right)

        //GL.TexCoord2(flipX ? 0 : 1, flipY ? 1 : 0); // prepare input struct (Texcoord0 (UV's)) for this vertex
        //GL.Vertex3(1.0f, 0.0f, 0.0f); // Finalize and submit this vertex for rendering  (bottom right)

        //for OpenGLES20
        GL.TexCoord2(flipX ? verticesUV[3].x : 0, flipY ? verticesUV[1].y : 0); // prepare input struct (Texcoord0 (UV's)) for this vertex
        GL.Vertex3(vertexMin, vertexMin, 0.0f); // Finalize and submit this vertex for rendering (bottom left)

        GL.TexCoord2(flipX ? verticesUV[2].x : 0, flipY ? 0 : verticesUV[1].y); // prepare input struct (Texcoord0 (UV's)) for this vertex
        GL.Vertex3(vertexMin, 1.0f, 0.0f); // Finalize and submit this vertex for rendering (top left)

        GL.TexCoord2(flipX ? 0 : verticesUV[2].x, flipY ? 0 : verticesUV[2].y); // prepare input struct (Texcoord0 (UV's)) for this vertex
        GL.Vertex3(1.0f, 1.0f, 0.0f); // Finalize and submit this vertex for rendering  (top right)

        GL.TexCoord2(flipX ? 0 : verticesUV[3].x, flipY ? verticesUV[2].y : 0); // prepare input struct (Texcoord0 (UV's)) for this vertex
        GL.Vertex3(1.0f, vertexMin, 0.0f); // Finalize and submit this vertex for rendering  (bottom right)

        GL.End();
        GL.PopMatrix(); // Pop the matrices off the stack
    }

    void Vertices() //set clip space vertices and texture coordinates for render fullscreen quad via shader buffer
    {
        //if (defaultRender)
//#if !(URP || HDRP)
        //{
            verticesPos[0] = new Vector2(-1, -1);
            verticesPos[1] = new Vector2(-1, 1);
            verticesPos[2] = new Vector2(1, 1);
            verticesPos[3] = new Vector2(1, -1);
           //if (debug) Debug.Log("defaultRender");
        //}
        //else
        //{
//#else
//            //translate camera rect coordinates to clip coordinates
//            //need when using custom camera rect with Blit or DrawProcedural & "nearClipPlane = -1" hack
//            Vector4 clipRect = new Vector4(Mathf.Max(cam.rect.min.x, 0), Mathf.Max(cam.rect.min.y, 0), Mathf.Min(cam.rect.max.x, 1), Mathf.Min(cam.rect.max.y, 1)) * 2 - Vector4.one;

//            verticesPos[0] = new Vector2(clipRect.x, clipRect.y);
//            verticesPos[1] = new Vector2(clipRect.x, clipRect.w);
//            verticesPos[2] = new Vector2(clipRect.z, clipRect.w);
//            verticesPos[3] = new Vector2(clipRect.z, clipRect.y);

//        //if (debug) Debug.Log("Vertices() cam.rect.max.y " + cam.rect.max.y);
//        //if (debug) Debug.Log("Vertices() clipRect " + clipRect);
//        //}
//#endif

        verticesUV[0] = new Vector2(0, 0);
        verticesUV[1] = new Vector2(0, 1);
        verticesUV[2] = new Vector2(1, 1);
        verticesUV[3] = new Vector2(1, 0);
    }

    void OnDisable()
    {
        if (!name.Contains("(Clone)"))
        {
            if (debug) Debug.Log("OnDisable");
            CursorRestore();
            Render_Release();
            cameraRestore();
            //Destroy(camera_left.gameObject);
            //Destroy(camera_right.gameObject);
#if URP
            CameraStackRestore();

            if (cameraStack != null)
                cameraStack.Remove(canvasCamera);

            //leftCameraStack.RemoveAll(t => t);
            //rightCameraStack.RemoveAll(t => t);
            //leftCameraStack.Remove(canvasCamera_left);
            //rightCameraStack.Remove(canvasCamera_right);

            //foreach (var c in leftCameraStack)
            //    cameraStack.Add(c);
#endif
            ////if (additionalS3DCamerasStruct != null)
            //    foreach (var c in additionalS3DCamerasStruct)
            //        if (c.camera)
            //        {
            //            Destroy(c.camera_left.gameObject);
            //            Destroy(c.camera_right.gameObject);
            //        }

            //if (canvasCamera)
            //    Destroy(canvasCamera.gameObject);

            //#if CINEMACHINE
            //            VCamClipRestore();

            //            //if (cineMachineEnabled && defaultVCam != null && Cinemachine.CinemachineBrain.SoloCamera.ToString() != "null")
            //            //    Cinemachine.CinemachineBrain.SoloCamera = defaultVCam;

            //            cineMachineEnabled = false;
            //            vCam = null;
            //#else
            //                    cam.nearClipPlane = sceneNearClip;
            //                    cam.farClipPlane = sceneFarClip;
            //#endif

            //{
            //if (debug) Debug.Log("OnDisable !cineMachineEnabled");
            //cam.nearClipPlane = sceneNearClip;
            //if (debug) Debug.Log("OnDisable sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);
            //ClosestCamera_SceneNearClipSet();

            ////if (additionalS3DCameras.Count != 0)
            //if (additionalS3DTopmostCameraIndex != -1)
            //    additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane = sceneNearClip;

            //if (lastAdditionalS3DTopmostCamera != null)
            if (lastAdditionalS3DTopmostCamera)
                    lastAdditionalS3DTopmostCamera.nearClipPlane = sceneNearClip;

                //cam.nearClipPlane = camera_left.nearClipPlane; //restore camera original nearClipPlane if additionalS3DCameras.Count != 0
                cam.nearClipPlane = cameraNearClip; //restore camera original nearClipPlane if additionalS3DCameras.Count != 0
                if (debug) Debug.Log("OnDisable sceneNearClip " + sceneNearClip + " sceneFarClip " + sceneFarClip);
                cam.farClipPlane = sceneFarClip;
#if CINEMACHINE
                VCamClip_Sync();
#endif

            //if (Cinemachine.CinemachineBrain.SoloCamera == null || Cinemachine.CinemachineBrain.SoloCamera.ToString() == "null") //virtual camera lost
            //if (vCam != null)
            //if (Cinemachine.CinemachineBrain.SoloCamera != null || Cinemachine.CinemachineBrain.SoloCamera.ToString() != "null")
            //if (GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera != null)
            //Cinemachine.CinemachineBrain.SoloCamera = vCam;
            //Cinemachine.CinemachineBrain.SoloCamera = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera;

            //}

#if CINEMACHINE
            //if (vCam != null)
            //    if (debug) Debug.Log("OnDisable ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane " + ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane + " cam.nearClipPlane " + cam.nearClipPlane);

            if (cineMachineEnabled)
            {
                //VCamClipRestore();
                cineMachineEnabled = false;
                vCam = null;
                //Cinemachine.CinemachineBrain.SoloCamera = vCam = null;

                //if (vCamChanged)
                //    vCamChanged = false;
                //else
                //if (!vCamChanged)
                //    Cinemachine.CinemachineBrain.SoloCamera = null;
            }
            //else
#endif

            cam.ResetAspect();
            scaleX = 1 / Mathf.Tan(FOV * Mathf.PI / 360);
            scaleY = scaleX * cam.aspect;
            cam.fieldOfView = 360 * Mathf.Atan(1 / scaleY) / Mathf.PI;

            HDRPSettings_Restore();
            cameraDataStructIsReady = false;

#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            GUIAction.Disable();
            S3DAction.Disable();
            FOVAction.Disable();
            modifier1Action.Disable();
            modifier2Action.Disable();
            modifier3Action.Disable();
            //inputSystem_KeyListener.Dispose();
#endif

            Destroy(camera_left.gameObject);
            Destroy(camera_right.gameObject);

            foreach (var c in additionalS3DCamerasStruct)
                if (c.camera)
                {
                    Destroy(c.camera_left.gameObject);
                    Destroy(c.camera_right.gameObject);
                }

            //if (canvasCamera)
            //    Destroy(canvasCamera.gameObject);

            Destroy(S3DMaterial);
            Destroy(canvas.gameObject);
            Resources.UnloadUnusedAssets(); //free memory

//#if INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
//            GUIAction.Disable();
//            S3DAction.Disable();
//            FOVAction.Disable();
//            modifier1Action.Disable();
//            modifier2Action.Disable();
//            modifier3Action.Disable();
//#endif

////#if HDRP
//            //HDRPSettings.colorBufferFormat = defaultColorBufferFormat;
//            //HDRPSettings = defaultHDRPSettings;
//            //typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GraphicsSettings.currentRenderPipeline, HDRPSettings);
//            HDRPSettings_Restore();
//            //#endif
//            cameraDataStructIsReady = false;
            //cam.ResetProjectionMatrix();
            //if (debug) Debug.Log("OnDisable camMatrix\n" + camMatrix);

            if (!cam.usePhysicalProperties)
            {
                cam.projectionMatrix = camMatrix;
                Matrix_Set(cam, 0, 0);
                //CamMatrix_Reset();
            }

            if (debug) Debug.Log("OnDisable cam.projectionMatrix\n" + cam.projectionMatrix);
            CancelInvoke(); //kill all Invoke processes
        }
    }

    void OnApplicationQuit()
    {
        if (debug) Debug.Log("OnApplicationQuit");

#if CINEMACHINE
        if (cineMachineEnabled)
            Cinemachine.CinemachineBrain.SoloCamera = null;
#endif
    }

    //    void ClosestCamera_SceneNearClipSet()
    //    {
    //        if (debug) Debug.Log("additionalS3DTopmostCameraIndex " + additionalS3DTopmostCameraIndex);

    //        if (additionalS3DCameras.Count != 0)
    //            //additionalS3DCameras[additionalS3DCameras.Count - 1].nearClipPlane = sceneNearClip;
    //            additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane = sceneNearClip;
    //        else
    //        {
    //            cam.nearClipPlane = sceneNearClip;

    ////#if CINEMACHINE
    ////            if (cineMachineEnabled)
    ////                ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = sceneNearClip;
    ////#endif
    //            VCamClip_Sync();
    //        }
    //    }

#if CINEMACHINE
    void VCamClip_Sync()
    {
//#if CINEMACHINE
        //if (debug) Debug.Log("VCamClip_Sync");

        //if (cineMachineEnabled)
        //    if (vCam != null)
        if (cineMachineEnabled && vCam != null)
        {
            if (debug) Debug.Log("VCamClip_Sync cineMachineEnabled");
            //((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane;
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.FarClipPlane = cam.farClipPlane;
            ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = cam.nearClipPlane;
        }
        //else
        //    Invoke("VCamClip_Sync", Time.deltaTime);
//#endif
    }
#endif

    //void ClosestCamera_NearClipSet()
    //{
    //    if (additionalS3DCameras.Count != 0)
    //        additionalS3DCameras[additionalS3DTopmostCameraIndex].nearClipPlane = nearClip;
    //    else
    //        cam.nearClipPlane = nearClip;
    //}

    void Render_Release()
    {
        if (debug) Debug.Log("Render_Release " + name);
        //#if UNITY_2019_1_OR_NEWER
        //if (!defaultRender)
        //{
//#if HDRP
#if URP || HDRP
            //RenderPipelineManager.endCameraRendering -= RenderQuad;
            RenderPipelineManager.endFrameRendering -= RenderQuad;
            //RenderPipelineManager.endContextRendering -= RenderQuad;
            //RenderPipelineManager.beginContextRendering -= RenderTexture_Reset; //remove render context
            //RenderPipelineManager.beginCameraRendering -= RenderTexture_Reset; //remove render context
            RenderPipelineManager.beginFrameRendering -= RenderTexture_Reset; //remove render context
//#endif
            //RenderPipelineManager.endContextRendering -= PostRenderContext; //remove render context
            //RenderPipelineManager.endCameraRendering -= PostRenderContext; //remove render context
            //RenderPipelineManager.endCameraRendering -= RenderBlit; //remove render context
            //RenderPipelineManager.endContextRendering -= RenderBlit; //remove render context
            //RenderPipelineManager.endCameraRendering -= RenderTexture_BlitToScreen;
            RenderPipelineManager.endFrameRendering -= RenderTexture_BlitToScreen;
            //RenderPipelineManager.endCameraRendering -= RenderTexture_BlitToRenderTexture;
            RenderPipelineManager.endFrameRendering -= RenderTexture_BlitToRenderTexture;
        //}
#else
        //clearScreen = true;
        //Camera.onPreCull -= PreCull;
        Camera.onPreRender -= RenderTexture_Reset;
        //Camera.onPreRender -= PreRenderClearScreen;
        //Camera.onPostRender -= PostRender;
        //camera_left.gameObject.GetComponent<OnRenderImageDelegate>().RenderImageEvent -= OnRenderImageEvent;
        //camera_right.gameObject.GetComponent<OnRenderImageDelegate>().RenderImageEvent -= OnRenderImageEvent;
        //canvasCamera_left.gameObject.GetComponent<OnRenderImageDelegate>().RenderImageEvent -= OnRenderImageEvent;
        //canvasCamera_right.gameObject.GetComponent<OnRenderImageDelegate>().RenderImageEvent -= OnRenderImageEvent;
        //Destroy(camera_left.gameObject.GetComponent<OnRenderImageDelegate>());
        //Destroy(camera_right.gameObject.GetComponent<OnRenderImageDelegate>());
        OnRenderImageEvent_Remove();
#endif
        //cam.targetTexture = null;
        //camera_left.targetTexture = null;
        //camera_right.targetTexture = null;
        //camera_left.targetTexture = camera_right.targetTexture = null;
        camera_left.targetTexture = camera_right.targetTexture = cam.targetTexture = null;
//        //camera_left.enabled = false;
//		//camera_right.enabled = false;
//        camera_left.enabled = camera_right.enabled = false;

//        //if (additionalS3DCamerasStruct != null)
//            foreach (var c in additionalS3DCamerasStruct)
//                if (c.camera)
//                {
//                    c.camera_left.targetTexture = c.camera_right.targetTexture = null;
//                    c.camera_left.enabled = c.camera_right.enabled = false;
//                    c.camera.enabled = true;
//                    //c.camera.ResetProjectionMatrix();
//                }

#if POST_PROCESSING_STACK_V2
        if (PPLayer)
            PPLayerLastStatus = PPLayer.enabled = PPLayerDefaultStatus;
#endif
        //CameraDataStruct_Change();

//        //if (additionalS3DCamerasStruct != null)
//        foreach (var c in additionalS3DCamerasStruct)
//            if (c.camera)
//            {
//                c.camera.rect = cam.rect;
//                //c.camera_left.targetTexture = c.camera_right.targetTexture = null;
//                c.camera_left.targetTexture = c.camera_right.targetTexture = c.camera.targetTexture = null;

//#if POST_PROCESSING_STACK_V2
//                if (c.PPLayer)
//                    additionalS3DCamerasStruct[additionalS3DCameras.IndexOf(c.camera)].PPLayerLastStatus = c.PPLayer.enabled = c.PPLayerDefaultStatus;
//#endif
//                //Destroy(c.camera_left.gameObject.GetComponent<OnRenderImageDelegate>());
//                //Destroy(c.camera_right.gameObject.GetComponent<OnRenderImageDelegate>());
//            }

        for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
            if (additionalS3DCamerasStruct[i].camera)
            {
                additionalS3DCamerasStruct[i].camera.rect = cam.rect;
                additionalS3DCamerasStruct[i].camera_left.targetTexture = additionalS3DCamerasStruct[i].camera_right.targetTexture = additionalS3DCamerasStruct[i].camera.targetTexture = null;

//#if POST_PROCESSING_STACK_V2
//                if (additionalS3DCamerasStruct[i].PPLayer)
//                    additionalS3DCamerasStruct[i].PPLayerLastStatus = additionalS3DCamerasStruct[i].PPLayer.enabled = additionalS3DCamerasStruct[i].PPLayerDefaultStatus;
//#endif
            }

        //#if URP
        //        foreach (Camera c in cameraStack)
        //            c.targetTexture = null;
        //#endif

        if (canvasCamera)
        {
            //canvasCamera_left.targetTexture = canvasCamera_right.targetTexture = null;
            canvasCamera_left.targetTexture = canvasCamera_right.targetTexture = canvasCamera.targetTexture = null;
            //Destroy(canvasCamera_left.gameObject.GetComponent<OnRenderImageDelegate>());
            //Destroy(canvasCamera_right.gameObject.GetComponent<OnRenderImageDelegate>());
        }

//        cam.cullingMask = sceneCullingMask;
//#if HDRP
//        camData.volumeLayerMask = volumeLayerMask;
//        camData.probeLayerMask = probeLayerMask;
//        //CameraDataStruct_Set();
//        //lastCameraDataStruct = cameraDataStruct;
//        //CameraDataStruct_Change();
//#endif
        //cam.nearClipPlane = nearClip;
        //if (debug) Debug.Log("Render_Release nearClip " + nearClip);

        //if (cineMachineEnabled)
        //    VCamClipRestore();
        //else
        //    cam.nearClipPlane = sceneNearClip;

        //#if CINEMACHINE
        //        VCamClipRestore();
        //        //nearClipHackApplyed = false;
        //#else
        //        cam.nearClipPlane = sceneNearClip;
        //        cam.farClipPlane = sceneFarClip;
        //#endif

        //nearClipHackApplyed = false;

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

        //Clip_Set();
        //cam.ResetProjectionMatrix();

        if (verticesPosBuffer != null)
        {
            verticesPosBuffer.Release();
            verticesUVBuffer.Release();
            renderTexture_left.Release();
	        renderTexture_right.Release();
        }

        //Destroy(canvasCamera_left.GetComponent<CameraBlit>());
        //Destroy(canvasCamera_right.GetComponent<CameraBlit>());

        //lastRTFormat = RTFormat = defaultRTFormat;
    }

    void CameraDataStruct_Change()
    {
        if (debug) Debug.Log("CameraDataStruct_Change");
        CameraDataStruct_Set();
        //lastCameraDataStruct = cameraDataStruct;
        SetLastCameraDataStruct();
    }

    //void VCamNearClipRestore()
    //{
    //    if (vCam != null)
    //        ((Cinemachine.CinemachineVirtualCamera)vCam).m_Lens.NearClipPlane = nearClip;
    //    else
    //        Invoke("VCamNearClipRestore", Time.deltaTime);
    //}

    //void PreCull(Camera c)
    //{
    //    //if (debug) Debug.Log(c + " PreCull " + Time.time);

    //    if (c == camera_right)
    //    {
    //        //canvasCamera_left.targetTexture = renderTexture_left;
    //        //cam.targetTexture = renderTexture;

    //  //c.ResetWorldToCameraMatrix();
    //  //c.ResetProjectionMatrix();
    //  //Vector3 scale = new Vector3(flipHorizontal ? -1 : 1, 1, 1);
    //  //Vector3 scale = new Vector3(-1, 1, 1);
    //  //c.projectionMatrix = c.projectionMatrix * Matrix4x4.Scale(scale);
    //    }
    //}

    void PreRenderClearScreen(
#if URP || HDRP
        ScriptableRenderContext context,
#endif
        Camera c)
    {
        //if (debug) Debug.Log(c + " PreRenderClearScreen " + Time.time);

        //if (method == Method.Two_Displays_MirrorX || method == Method.Two_Displays_MirrorY)
        //{
        //    //if (!(canvasCamera_left && canvasCamera_left.isActiveAndEnabled))
        //    //{
        //    //    if (c == camera_left)
        //    //        ClearScreen(c);
        //    //    else
        //    //        if (c == camera_right)
        //    //            ClearScreen(c);
        //    //}
        //    //else
        //    //    if (c == canvasCamera_left)
        //    //        ClearScreen(c);
        //    //    else
        //    //        if (c == canvasCamera_right)
        //    //            ClearScreen(c);

        //    if (c == topmostCamera_left || c == topmostCamera_right)
        //        ClearScreen(c);
        //}
        //else
        //    if (method == Method.Two_Displays)
        //        //if (c == camera_left)
        //        //    ClearScreen(c);
        //        //else
        //        //    if (c == camera_right)
        //        //        ClearScreen(c);
                if (c == camera_left || c == camera_right || c == cam)
                    ClearScreen(c);
    }

    //bool clearScreen;
    int clearFrameCount;

    void ClearScreen(Camera c)
    {
        clearFrameCount++;
        if (debug) Debug.Log(c + " clearScreen clearFrameCount " + clearFrameCount);

        //if (clearFrameCount <= 4) //2 is enought in Player but min 3 is required in Editor
        //{
            //RenderTexture rt = c.targetTexture;
            //c.targetTexture = null;
            GL.Clear(true, true, Color.clear);

            //Rect rect = c.rect;
            //c.rect = Rect.MinMaxRect(0, 0, 1, 1); //set temporary rect required before Graphics.Blit to blit render texture correctly with no fullscreen rect
            //Graphics.Blit(RenderTexture.active, null as RenderTexture);
            //c.rect = cam.rect;
            //c.rect = rect;
            //c.targetTexture = rt;
        //}
        //else
        //    Camera.onPreRender -= PreRenderClearScreen;
//#if UNITY_2019 || UNITY_2020 || UNITY_2021
#if !UNITY_2022_1_OR_NEWER
        if (clearFrameCount == 5) //4 is not enought in UNITY_2019 when switch from SideBySide to Mirror not clear all swapchain frames
#else
        if (clearFrameCount == 4) //2 is enought in Player but min 3 is required in Editor
#endif
        //if (clearFrameCount == 1) //1 is enought in URP
        {
            //clearScreen = false;
            clearFrameCount = 0;
#if URP || HDRP
            RenderPipelineManager.beginCameraRendering -= PreRenderClearScreen;
#else
            Camera.onPreRender -= PreRenderClearScreen;
#endif
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
        //if (debug) Debug.Log(Application.persistentDataPath);
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
        SaveUserName(slotName);
    }

    void Load(string name)
    {
       if (debug) Debug.Log("Load");

        string filePath = Application.persistentDataPath + "/S3D_Settings_" + name;

        if (File.Exists(filePath))
        {
            FileStream file = File.OpenRead(filePath);
            BinaryFormatter binF = new BinaryFormatter();
            SaveLoad data = (SaveLoad)binF.Deserialize(file);
            file.Close();

            //if (debug) Debug.Log(data.PPI);
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
            //panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(data.panelPos.x, data.panelPos.y);
            lastPanelPosition = panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(data.panelPos.x, data.panelPos.y);

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

            //if (debug) Debug.Log(data.userName);
            slotName = data.userName;
            Load(slotName);
        }
        else
            lastPanelPosition = panel.GetComponent<RectTransform>().anchoredPosition;

        //Load(slotName);
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
                //if (debug) Debug.Log(itemName);
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
        //   //if (debug) Debug.Log(slotName_dropdown.options[i].text);

        //    if (slotName_dropdown.options[i].text == name)
        //    {
        //        slotName_dropdown.value = i;
        //    }
        //}

        foreach (Dropdown.OptionData optionData in slotName_dropdown.options)
        {
           //if (debug) Debug.Log(optionData.text);

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

    void CameraDataStruct_Set()
    {
//        cameraDataStruct = new CameraDataStruct(
//#if URP
//            //camData.renderType,
//            //camData.scriptableRenderer,
//            //camData.renderPostProcessing,
//            //camData.antialiasing,
//            //camData.antialiasingQuality,
//            //camData.stopNaN,
//            //camData.dithering,
//            //camData.renderShadows,
//            // //camData.cameraStack,

//             camData.requiresDepthTexture,
//             camData.stopNaN,
//             camData.antialiasingQuality,
//             camData.antialiasing,
//             camData.renderPostProcessing,
//             camData.volumeStack,
//             camData.volumeTrigger,
//             camData.volumeLayerMask,
//             camData.requiresColorTexture,
//             camData.allowXRRendering,
//             camData.renderType,
//             camData.requiresColorOption,
//             camData.requiresDepthOption,
//             camData.renderShadows,
//             camData.dithering,
//#elif HDRP
//            camData.clearColorMode,
//            camData.backgroundColorHDR,
//            camData.clearDepth,
//            camData.customRenderingSettings,
//            camData.volumeLayerMask,
//            camData.volumeAnchorOverride,
//            camData.antialiasing,
//            camData.dithering,
//            camData.xrRendering,
//            camData.SMAAQuality,
//            camData.stopNaNs,
//            camData.taaSharpenStrength,
//            camData.TAAQuality,
//            camData.taaHistorySharpening,
//            camData.taaAntiFlicker,
//            camData.taaMotionVectorRejection,
//            camData.taaAntiHistoryRinging,
//            camData.taaBaseBlendFactor,
//            camData.taaJitterScale,
//            camData.flipYMode,
//            camData.fullscreenPassthrough,
//            camData.allowDynamicResolution,
//            camData.invertFaceCulling,
//            camData.probeLayerMask,
//            camData.hasPersistentHistory,
//            camData.exposureTarget,
//            camData.physicalParameters,
//            camData.renderingPathCustomFrameSettings,
//            camData.renderingPathCustomFrameSettingsOverrideMask,
//            camData.defaultFrameSettings,
//            //camData.probeCustomFixedExposure,
//            camData.allowDeepLearningSuperSampling,
//            camData.deepLearningSuperSamplingUseCustomQualitySettings,
//            camData.deepLearningSuperSamplingQuality,
//            camData.deepLearningSuperSamplingUseCustomAttributes,
//            camData.deepLearningSuperSamplingUseOptimalSettings,
//            camData.deepLearningSuperSamplingSharpening,
//            camData.materialMipBias,
//#endif
//            cam.depth,
//            cam.useOcclusionCulling
//            );

        cameraDataStruct = CameraDataStruct_Make(cam);

        //if (additionalS3DCamerasStruct != null)
            for (int i = 0; i < additionalS3DCamerasStruct.Length; i++)
                if (additionalS3DCamerasStruct[i].camera)
                    additionalS3DCamerasStruct[i].cameraDataStruct = CameraDataStruct_Make(additionalS3DCamerasStruct[i].camera);
    }

    CameraDataStruct CameraDataStruct_Make(Camera c)
    {
#if URP
        UniversalAdditionalCameraData cd = c.GetUniversalAdditionalCameraData();
#elif HDRP
        HDAdditionalCameraData cd = c.GetComponent<HDAdditionalCameraData>();
#endif

        CameraDataStruct cds = new CameraDataStruct(
#if URP
             cd.requiresDepthTexture,
             cd.stopNaN,
             cd.antialiasingQuality,
             cd.antialiasing,
             cd.renderPostProcessing,
#if UNITY_2021_1_OR_NEWER
             cd.volumeStack,
#endif
             cd.volumeTrigger,
             cd.volumeLayerMask,
             cd.requiresColorTexture,
#if UNITY_2020_2_OR_NEWER
             cd.allowXRRendering,
#endif
             cd.renderType,
             cd.requiresColorOption,
             cd.requiresDepthOption,
             cd.renderShadows,
             cd.dithering,
#elif HDRP
            cd.clearColorMode,
            cd.backgroundColorHDR,
            cd.clearDepth,
            cd.customRenderingSettings,
            cd.volumeLayerMask,
            cd.volumeAnchorOverride,
            cd.antialiasing,
            cd.dithering,
#if UNITY_2020_2_OR_NEWER
            cd.xrRendering,
#endif
            cd.SMAAQuality,
            cd.stopNaNs,
            cd.taaSharpenStrength,
#if UNITY_2020_2_OR_NEWER
            cd.TAAQuality,
            cd.taaHistorySharpening,
            cd.taaAntiFlicker,
            cd.taaMotionVectorRejection,
            cd.taaAntiHistoryRinging,
#endif
#if UNITY_2021_2_OR_NEWER
            cd.taaBaseBlendFactor,
#endif
#if UNITY_2021_3_OR_NEWER
            cd.taaJitterScale,
#endif
            cd.flipYMode,
            cd.fullscreenPassthrough,
            //cd.allowDynamicResolution,
            cd.invertFaceCulling,
            cd.probeLayerMask,
            cd.hasPersistentHistory,
#if UNITY_2020_2_OR_NEWER
            cd.exposureTarget,
#endif
#if !UNITY_2022_1_OR_NEWER
            cd.physicalParameters,
#endif
            cd.renderingPathCustomFrameSettings,
            cd.renderingPathCustomFrameSettingsOverrideMask,
            cd.defaultFrameSettings,
        //cd.probeCustomFixedExposure,
#if UNITY_2021_2_OR_NEWER
            cd.allowDeepLearningSuperSampling,
            cd.deepLearningSuperSamplingUseCustomQualitySettings,
            cd.deepLearningSuperSamplingQuality,
            cd.deepLearningSuperSamplingUseCustomAttributes,
            cd.deepLearningSuperSamplingUseOptimalSettings,
            cd.deepLearningSuperSamplingSharpening,
            cd.materialMipBias,
#endif
#endif
        //c.depth,
        //c.useOcclusionCulling

        ////
        //// Summary:
        ////     The number of cameras in the current Scene.
        //c.allCamerasCount, // { get; }
        ////
        //// Summary:
        ////     Returns all enabled cameras in the Scene.
        //c.allCameras, // { get; }
        ////
        //// Summary:
        ////     The first enabled Camera component that is tagged "MainCamera" (Read Only).
        //c.main, // { get; }
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property mainCamera has been deprecated. Use Camera.main instead (UnityUpgradable) -> main", true)]
        //c.mainCamera, // { get; }
        ////
        //// Summary:
        ////     The camera we are currently rendering with, for low-level render control only
        ////     (Read Only).
        //c.current, // { get; }
        //
        // Summary:
        //     How and if camera generates a depth texture.
        //c.depthTextureMode, // { get; set; }
        //
        // Summary:
        //     Should the camera clear the stencil buffer after the deferred light pass?
        c.clearStencilAfterLightingPass, // { get; set; }
        //
        // Summary:
        //     Sets a custom matrix for the camera to use for all culling queries.
        //c.cullingMatrix, // { get; set; }
        //
        // Summary:
        //     Whether or not the Camera will use occlusion culling during rendering.
        c.useOcclusionCulling, // { get; set; }
        //
        // Summary:
        //     Enable usePhysicalProperties to use physical camera properties to compute the
        //     field of view and the frustum.
        c.usePhysicalProperties, // { get; set; }
        //
#if !CINEMACHINE
        // Summary:
        //     The size of the camera sensor, expressed in millimeters.
        c.sensorSize, // { get; set; }
        //
#endif
        // Summary:
        //     Per-layer culling distances.
        //c.layerCullDistances, // { get; set; }
        //
        // Summary:
        //     Sets the culling mask used to determine which objects from which Scenes to draw.
        //     See EditorSceneManager.SetSceneCullingMask.
        //[NativeConditional("UNITY_EDITOR")]
        c.overrideSceneCullingMask, // { get; set; }
        //
        // Summary:
        //     Identifies what kind of camera this is, using the CameraType enum.
        c.cameraType, // { get; set; }
        //
        // Summary:
        //     How to perform per-layer culling for a Camera.
        c.layerCullSpherical, // { get; set; }
        //
        // Summary:
        //     Mask to select which layers can trigger events on the camera.
        c.eventMask, // { get; set; }
        //
        // Summary:
        //     How the camera clears the background.
        c.clearFlags, // { get; set; }
        //
        // Summary:
        //     The color with which the screen will be cleared.
        c.backgroundColor, // { get; set; }
        //
        // Summary:
        //     The aspect ratio (width divided by height).
        c.aspect, // { get; set; }
        //
        // Summary:
        //     Get the world-space speed of the camera (Read Only).
        //c.velocity, // { get; }
        //
        // Summary:
        //     The lens offset of the camera. The lens shift is relative to the sensor size.
        //     For example, a lens shift of 0.5 offsets the sensor by half its horizontal size.
        c.lensShift, // { get; set; }
        //
        // Summary:
        //     Camera's depth in the camera rendering order.
        c.depth, // { get; set; }
        //
        // Summary:
        //     An axis that describes the direction along which the distances of objects are
        //     measured for the purpose of sorting.
        c.transparencySortAxis, // { get; set; }
        //
        // Summary:
        //     Transparent object sorting mode.
        c.transparencySortMode, // { get; set; }
        //
        // Summary:
        //     Opaque object sorting mode.
        c.opaqueSortMode, // { get; set; }
        //
        // Summary:
        //     Is the camera orthographic (true) or perspective (false)?
        c.orthographic, // { get; set; }
        //
        // Summary:
        //     Camera's half-size when in orthographic mode.
        c.orthographicSize, // { get; set; }
        //
        // Summary:
        //     Should camera rendering be forced into a RenderTexture.
        //[NativeProperty("ForceIntoRT")]
        c.forceIntoRenderTexture, // { get; set; }
        //
        // Summary:
        //     Dynamic Resolution Scaling.
        c.allowDynamicResolution, // { get; set; }
        //
        // Summary:
        //     MSAA rendering.
        c.allowMSAA, // { get; set; }
        //
        // Summary:
        //     High dynamic range rendering.
        c.allowHDR, // { get; set; }
        //
        // Summary:
        //     The rendering path that is currently being used (Read Only).
        c.actualRenderingPath, // { get; }
        //
        // Summary:
        //     The rendering path that should be used, if possible.
        c.renderingPath, // { get; set; }
        //
        // Summary:
        //     This is used to render parts of the Scene selectively.
        c.cullingMask, // { get; set; }
        //
        // Summary:
        //     The camera focal length, expressed in millimeters. To use this property, enable
        //     UsePhysicalProperties.
        c.focalLength, // { get; set; }
        //
        // Summary:
        //     How tall is the camera in pixels (not accounting for dynamic resolution scaling)
        //     (Read Only).
        c.pixelHeight, // { get; }
        //
        // Summary:
        //     Where on the screen is the camera rendered in normalized coordinates.
        //[NativeProperty("NormalizedViewportRect")]
        c.rect, // { get; set; }
        //
        // Summary:
        //     Render only once and use resulting image for both eyes.
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //[Obsolete("Property stereoMirrorMode is no longer supported. Please use single pass stereo rendering instead.", true)]
        //c.stereoMirrorMode, // { get; set; }
        //
        // Summary:
        //     High dynamic range rendering.
        //[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property hdr has been deprecated. Use Camera.allowHDR instead (UnityUpgradable) -> UnityEngine.Camera.allowHDR", false)]
        //c.hdr, // { get; set; }
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property fov has been deprecated. Use Camera.fieldOfView instead (UnityUpgradable) -> UnityEngine.Camera.fieldOfView", false)]
        //c.fov, // { get; set; }
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property far has been deprecated. Use Camera.farClipPlane instead (UnityUpgradable) -> UnityEngine.Camera.farClipPlane", false)]
        //c.far, // { get; set; }
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property near has been deprecated. Use Camera.nearClipPlane instead (UnityUpgradable) -> UnityEngine.Camera.nearClipPlane", false)]
        //c.near, // { get; set; }
        ////[EditorBrowsable(EditorBrowsableState.Never)]
        ////[Obsolete("Property isOrthoGraphic has been deprecated. Use orthographic (UnityUpgradable) -> orthographic", true)]
        //c.isOrthoGraphic, // { get; set; }
        //
        // Summary:
        //     Number of command buffers set up on this camera (Read Only).
        c.commandBufferCount, // { get; }
#if UNITY_2021_2_OR_NEWER
        //[NativeConditional("UNITY_EDITOR")]
        c.sceneViewFilterMode, // { get; }
        //
#endif
        // Summary:
        //     Returns the eye that is currently rendering. If called when stereo is not enabled
        //     it will return Camera.MonoOrStereoscopicEye.Mono. If called during a camera rendering
        //     callback such as OnRenderImage it will return the currently rendering eye. If
        //     called outside of a rendering callback and stereo is enabled, it will return
        //     the default eye which is Camera.MonoOrStereoscopicEye.Left.
        c.stereoActiveEye, // { get; }
        //
        // Summary:
        //     Defines which eye of a VR display the Camera renders into.
        c.stereoTargetEye, // { get; set; }
        //
        // Summary:
        //     Determines whether the stereo view matrices are suitable to allow for a single
        //     pass cull.
        c.areVRStereoViewMatricesWithinSingleCullTolerance, // { get; }
        //
        // Summary:
        //     Distance to a point where virtual eyes converge.
        c.stereoConvergence, // { get; set; }
        //
        // Summary:
        //     The distance between the virtual eyes. Use this to query or set the current eye
        //     separation. Note that most VR devices provide this value, in which case setting
        //     the value will have no effect.
        c.stereoSeparation, // { get; set; }
        //
        // Summary:
        //     Stereoscopic rendering.
        c.stereoEnabled, // { get; }
        //
        // Summary:
        //     If not null, the camera will only render the contents of the specified Scene.
        c.scene, // { get; set; }
        //
        // Summary:
        //     Get the view projection matrix used on the last frame.
        c.previousViewProjectionMatrix, // { get; }
        //
        // Summary:
        //     Should the jittered matrix be used for transparency rendering?
        //[NativeProperty("UseJitteredProjectionMatrixForTransparent")]
        c.useJitteredProjectionMatrixForTransparentRendering, // { get; set; }
        //
#if !CINEMACHINE
        // Summary:
        //     Get or set the raw projection matrix with no camera offset (no jittering).
        c.nonJitteredProjectionMatrix, // { get; set; }
        //
        // Summary:
        //     Set a custom projection matrix.
        c.projectionMatrix, // { get; set; }
        //
#endif
        // Summary:
        //     Matrix that transforms from world to camera space.
        //c.worldToCameraMatrix, // { get; set; }
        //
        // Summary:
        //     Matrix that transforms from camera space to world space (Read Only).
        //c.cameraToWorldMatrix, // { get; }
        //
        // Summary:
        //     Set the target display for this Camera.
        c.targetDisplay, // { get; set; }
        //
        // Summary:
        //     Gets the temporary RenderTexture target for this Camera.
        //c.activeTexture, // { get; }
        //
        // Summary:
        //     Destination render texture.
        //c.targetTexture, // { get; set; }
        //
        // Summary:
        //     How tall is the camera in pixels (accounting for dynamic resolution scaling)
        //     (Read Only).
        c.scaledPixelHeight, // { get; }
        //
        // Summary:
        //     How wide is the camera in pixels (accounting for dynamic resolution scaling)
        //     (Read Only).
        c.scaledPixelWidth, // { get; }
        //
        // Summary:
        //     The distance of the far clipping plane from the Camera, in world units.
        //[NativeProperty("Far")]
        c.farClipPlane, // { get; set; }
        //
        // Summary:
        //     How wide is the camera in pixels (not accounting for dynamic resolution scaling)
        //     (Read Only).
        c.pixelWidth, // { get; }
        //
        // Summary:
        //     Where on the screen is the camera rendered in pixel coordinates.
        //[NativeProperty("ScreenViewportRect")]
        c.pixelRect, // { get; set; }
        //
#if !CINEMACHINE
        // Summary:
        //     There are two gates for a camera, the sensor gate and the resolution gate. The
        //     physical camera sensor gate is defined by the sensorSize property, the resolution
        //     gate is defined by the render target area.
        c.gateFit, // { get; set; }
        //
#endif
        // Summary:
        //     The distance of the near clipping plane from the the Camera, in world units.
        //[NativeProperty("Near")]
        c.nearClipPlane // { get; set; }
        //
        // Summary:
        //     The vertical field of view of the Camera, in degrees.
        //[NativeProperty("VerticalFieldOfView")]
        //c.fieldOfView // { get; set; }
            );

        return cds;
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
#if UNITY_2021_1_OR_NEWER
        public VolumeStack volumeStack;
#endif
        public Transform volumeTrigger;
        public LayerMask volumeLayerMask;
        public bool requiresColorTexture;
#if UNITY_2020_2_OR_NEWER
        public bool allowXRRendering;
#endif
        public CameraRenderType renderType;
        public CameraOverrideOption requiresColorOption;
        public CameraOverrideOption requiresDepthOption;
        public bool renderShadows;
        public bool dithering;
#elif HDRP
        public HDAdditionalCameraData.ClearColorMode clearColorMode;
        public Color backgroundColorHDR;
        public bool clearDepth;
        public bool customRenderingSettings;
        public LayerMask volumeLayerMask;
        public Transform volumeAnchorOverride;
        public HDAdditionalCameraData.AntialiasingMode antialiasing;
        public bool dithering;
#if UNITY_2020_2_OR_NEWER
        public bool xrRendering;
#endif
        public HDAdditionalCameraData.SMAAQualityLevel SMAAQuality;
        public bool stopNaNs;
        public float taaSharpenStrength;
#if UNITY_2020_2_OR_NEWER
        public HDAdditionalCameraData.TAAQualityLevel TAAQuality;
        public float taaHistorySharpening;
        public float taaAntiFlicker;
        public float taaMotionVectorRejection;
        public bool taaAntiHistoryRinging;
#endif
#if UNITY_2021_2_OR_NEWER
        public float taaBaseBlendFactor;
#endif
#if UNITY_2021_3_OR_NEWER
        public float taaJitterScale;
#endif
        public HDAdditionalCameraData.FlipYMode flipYMode;
        public bool fullscreenPassthrough;
        //public bool allowDynamicResolution;
        public bool invertFaceCulling;
        public LayerMask probeLayerMask;
        public bool hasPersistentHistory;
#if UNITY_2020_2_OR_NEWER
        public GameObject exposureTarget;
#endif
#if !UNITY_2022_1_OR_NEWER
        public HDPhysicalCamera physicalParameters;
#endif
        public FrameSettings renderingPathCustomFrameSettings;
        public FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask;
        public FrameSettingsRenderType defaultFrameSettings;
        //public object probeCustomFixedExposure;
#if UNITY_2021_2_OR_NEWER
        bool allowDeepLearningSuperSampling;
        public bool deepLearningSuperSamplingUseCustomQualitySettings;
        public uint deepLearningSuperSamplingQuality;
        public bool deepLearningSuperSamplingUseCustomAttributes;
        public bool deepLearningSuperSamplingUseOptimalSettings;
        public float deepLearningSuperSamplingSharpening;
        public float materialMipBias;
#endif
#endif
        //public float depth;
        //public bool useOcclusionCulling;

        //public DepthTextureMode depthTextureMode;
        public bool clearStencilAfterLightingPass;
        //public Matrix4x4 cullingMatrix;
        public bool useOcclusionCulling;
        public bool usePhysicalProperties;
#if !CINEMACHINE
        public Vector2 sensorSize;
#endif
        //public float[] layerCullDistances;
        public ulong overrideSceneCullingMask;
        public CameraType cameraType;
        public bool layerCullSpherical;
        public int eventMask;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public float aspect;
        //public Vector3 velocity;
        public Vector2 lensShift;
        public float depth;
        public Vector3 transparencySortAxis;
        public TransparencySortMode transparencySortMode;
        public OpaqueSortMode opaqueSortMode;
        public bool orthographic;
        public float orthographicSize;
        public bool forceIntoRenderTexture;
        public bool allowDynamicResolution;
        public bool allowMSAA;
        public bool allowHDR;
        public RenderingPath actualRenderingPath;
        public RenderingPath renderingPath;
        public int cullingMask;
        public float focalLength;
        public int pixelHeight;
        public Rect rect;
        //public bool stereoMirrorMode;
        public int commandBufferCount;
#if UNITY_2021_2_OR_NEWER
        public Camera.SceneViewFilterMode sceneViewFilterMode;
#endif
        public Camera.MonoOrStereoscopicEye stereoActiveEye;
        public StereoTargetEyeMask stereoTargetEye;
        public bool areVRStereoViewMatricesWithinSingleCullTolerance;
        public float stereoConvergence;
        public float stereoSeparation;
        public bool stereoEnabled;
        public UnityEngine.SceneManagement.Scene scene;
        public Matrix4x4 previousViewProjectionMatrix;
        public bool useJitteredProjectionMatrixForTransparentRendering;
#if !CINEMACHINE
        public Matrix4x4 nonJitteredProjectionMatrix;
        public Matrix4x4 projectionMatrix;
#endif
        //public Matrix4x4 worldToCameraMatrix;
        //public Matrix4x4 cameraToWorldMatrix;
        public int targetDisplay;
        //public RenderTexture activeTexture;
        //public RenderTexture targetTexture;
        public int scaledPixelHeight;
        public int scaledPixelWidth;
        public float farClipPlane;
        public int pixelWidth;
        public Rect pixelRect;
#if !CINEMACHINE
        public Camera.GateFitMode gateFit;
#endif
        public float nearClipPlane;
        //public float fieldOfView;

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
#if UNITY_2021_1_OR_NEWER
            VolumeStack volumeStack,
#endif
            Transform volumeTrigger,
            LayerMask volumeLayerMask,
            bool requiresColorTexture,
#if UNITY_2020_2_OR_NEWER
            bool allowXRRendering,
#endif
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
#if UNITY_2020_2_OR_NEWER
            bool xrRendering,
#endif
            HDAdditionalCameraData.SMAAQualityLevel SMAAQuality,
            bool stopNaNs,
            float taaSharpenStrength,
#if UNITY_2020_2_OR_NEWER
            HDAdditionalCameraData.TAAQualityLevel TAAQuality,
            float taaHistorySharpening,
            float taaAntiFlicker,
            float taaMotionVectorRejection,
            bool taaAntiHistoryRinging,
#endif
#if UNITY_2021_2_OR_NEWER
            float taaBaseBlendFactor,
#endif
#if UNITY_2021_3_OR_NEWER
            float taaJitterScale,
#endif
            HDAdditionalCameraData.FlipYMode flipYMode,
            bool fullscreenPassthrough,
            //bool allowDynamicResolution,
            bool invertFaceCulling,
            LayerMask probeLayerMask,
            bool hasPersistentHistory,
#if UNITY_2020_2_OR_NEWER
            GameObject exposureTarget,
#endif
#if !UNITY_2022_1_OR_NEWER
            HDPhysicalCamera physicalParameters,
#endif
            FrameSettings renderingPathCustomFrameSettings,
            FrameSettingsOverrideMask renderingPathCustomFrameSettingsOverrideMask,
            FrameSettingsRenderType defaultFrameSettings,
            //object probeCustomFixedExposure,
#if UNITY_2021_2_OR_NEWER
            bool allowDeepLearningSuperSampling,
            bool deepLearningSuperSamplingUseCustomQualitySettings,
            uint deepLearningSuperSamplingQuality,
            bool deepLearningSuperSamplingUseCustomAttributes,
            bool deepLearningSuperSamplingUseOptimalSettings,
            float deepLearningSuperSamplingSharpening,
            float materialMipBias,
#endif
#endif
            //float depth,
            //bool useOcclusionCulling

            //DepthTextureMode depthTextureMode,
            bool clearStencilAfterLightingPass,
            //Matrix4x4 cullingMatrix,
            bool useOcclusionCulling,
            bool usePhysicalProperties,
#if !CINEMACHINE
            Vector2 sensorSize,
#endif
            //float[] layerCullDistances,
            ulong overrideSceneCullingMask,
            CameraType cameraType,
            bool layerCullSpherical,
            int eventMask,
            CameraClearFlags clearFlags,
            Color backgroundColor,
            float aspect,
            //Vector3 velocity,
            Vector2 lensShift,
            float depth,
            Vector3 transparencySortAxis,
            TransparencySortMode transparencySortMode,
            OpaqueSortMode opaqueSortMode,
            bool orthographic,
            float orthographicSize,
            bool forceIntoRenderTexture,
            bool allowDynamicResolution,
            bool allowMSAA,
            bool allowHDR,
            RenderingPath actualRenderingPath,
            RenderingPath renderingPath,
            int cullingMask,
            float focalLength,
            int pixelHeight,
            Rect rect,
            //bool stereoMirrorMode,
            int commandBufferCount,
#if UNITY_2021_2_OR_NEWER
            Camera.SceneViewFilterMode sceneViewFilterMode,
#endif
            Camera.MonoOrStereoscopicEye stereoActiveEye,
            StereoTargetEyeMask stereoTargetEye,
            bool areVRStereoViewMatricesWithinSingleCullTolerance,
            float stereoConvergence,
            float stereoSeparation,
            bool stereoEnabled,
            UnityEngine.SceneManagement.Scene scene,
            Matrix4x4 previousViewProjectionMatrix,
            bool useJitteredProjectionMatrixForTransparentRendering,
#if !CINEMACHINE
            Matrix4x4 nonJitteredProjectionMatrix,
            Matrix4x4 projectionMatrix,
#endif
            //Matrix4x4 worldToCameraMatrix,
            //Matrix4x4 cameraToWorldMatrix,
            int targetDisplay,
            //RenderTexture activeTexture,
            //RenderTexture targetTexture,
            int scaledPixelHeight,
            int scaledPixelWidth,
            float farClipPlane,
            int pixelWidth,
            Rect pixelRect,
#if !CINEMACHINE
            Camera.GateFitMode gateFit,
#endif
            float nearClipPlane
            //float fieldOfView
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
#if UNITY_2021_1_OR_NEWER
             this.volumeStack = volumeStack;
#endif
             this.volumeTrigger = volumeTrigger;
             this.volumeLayerMask = volumeLayerMask;
             this.requiresColorTexture = requiresColorTexture;
#if UNITY_2020_2_OR_NEWER
             this.allowXRRendering = allowXRRendering;
#endif
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
#if UNITY_2020_2_OR_NEWER
            this.xrRendering = xrRendering;
#endif
            this.SMAAQuality = SMAAQuality;
            this.stopNaNs = stopNaNs;
            this.taaSharpenStrength = taaSharpenStrength;
#if UNITY_2020_2_OR_NEWER
            this.TAAQuality = TAAQuality;
            this.taaHistorySharpening = taaHistorySharpening;
            this.taaAntiFlicker = taaAntiFlicker;
            this.taaMotionVectorRejection = taaMotionVectorRejection;
            this.taaAntiHistoryRinging = taaAntiHistoryRinging;
#endif
#if UNITY_2021_2_OR_NEWER
            this.taaBaseBlendFactor = taaBaseBlendFactor;
#endif
#if UNITY_2021_3_OR_NEWER
            this.taaJitterScale = taaJitterScale;
#endif
            this.flipYMode = flipYMode;
            this.fullscreenPassthrough = fullscreenPassthrough;
            //this.allowDynamicResolution = allowDynamicResolution;
            this.invertFaceCulling = invertFaceCulling;
            this.probeLayerMask = probeLayerMask;
            this.hasPersistentHistory = hasPersistentHistory;
#if UNITY_2020_2_OR_NEWER
            this.exposureTarget = exposureTarget;
#endif
#if !UNITY_2022_1_OR_NEWER
            this.physicalParameters = physicalParameters;
#endif
            this.renderingPathCustomFrameSettings = renderingPathCustomFrameSettings;
            this.renderingPathCustomFrameSettingsOverrideMask = renderingPathCustomFrameSettingsOverrideMask;
            this.defaultFrameSettings = defaultFrameSettings;
            //this.probeCustomFixedExposure = probeCustomFixedExposure;
#if UNITY_2021_2_OR_NEWER
            this.allowDeepLearningSuperSampling = allowDeepLearningSuperSampling;
            this.deepLearningSuperSamplingUseCustomQualitySettings = deepLearningSuperSamplingUseCustomQualitySettings;
            this.deepLearningSuperSamplingQuality = deepLearningSuperSamplingQuality;
            this.deepLearningSuperSamplingUseCustomAttributes = deepLearningSuperSamplingUseCustomAttributes;
            this.deepLearningSuperSamplingUseOptimalSettings = deepLearningSuperSamplingUseOptimalSettings;
            this.deepLearningSuperSamplingSharpening = deepLearningSuperSamplingSharpening;
            this.materialMipBias = materialMipBias;
#endif
#endif
            //this.depth = depth;
            //this.useOcclusionCulling = useOcclusionCulling;

            //this.depthTextureMode = depthTextureMode;
            this.clearStencilAfterLightingPass = clearStencilAfterLightingPass;
            //this.cullingMatrix = cullingMatrix;
            this.useOcclusionCulling = useOcclusionCulling;
            this.usePhysicalProperties = usePhysicalProperties;
#if !CINEMACHINE
            this.sensorSize = sensorSize;
#endif
            //this.layerCullDistances = layerCullDistances;
            this.overrideSceneCullingMask = overrideSceneCullingMask;
            this.cameraType = cameraType;
            this.layerCullSpherical = layerCullSpherical;
            this.eventMask = eventMask;
            this.clearFlags = clearFlags;
            this.backgroundColor = backgroundColor;
            this.aspect = aspect;
            //this.velocity = velocity;
            this.lensShift = lensShift;
            this.depth = depth;
            this.transparencySortAxis = transparencySortAxis;
            this.transparencySortMode = transparencySortMode;
            this.opaqueSortMode = opaqueSortMode;
            this.orthographic = orthographic;
            this.orthographicSize = orthographicSize;
            this.forceIntoRenderTexture = forceIntoRenderTexture;
            this.allowDynamicResolution = allowDynamicResolution;
            this.allowMSAA = allowMSAA;
            this.allowHDR = allowHDR;
            this.actualRenderingPath = actualRenderingPath;
            this.renderingPath = renderingPath;
            this.cullingMask = cullingMask;
            this.focalLength = focalLength;
            this.pixelHeight = pixelHeight;
            this.rect = rect;
            //this.stereoMirrorMode = stereoMirrorMode;
            this.commandBufferCount = commandBufferCount;
#if UNITY_2021_2_OR_NEWER
            this.sceneViewFilterMode = sceneViewFilterMode;
#endif
            this.stereoActiveEye = stereoActiveEye;
            this.stereoTargetEye = stereoTargetEye;
            this.areVRStereoViewMatricesWithinSingleCullTolerance = areVRStereoViewMatricesWithinSingleCullTolerance;
            this.stereoConvergence = stereoConvergence;
            this.stereoSeparation = stereoSeparation;
            this.stereoEnabled = stereoEnabled;
            this.scene = scene;
            this.previousViewProjectionMatrix = previousViewProjectionMatrix;
            this.useJitteredProjectionMatrixForTransparentRendering = useJitteredProjectionMatrixForTransparentRendering;
#if !CINEMACHINE
            this.nonJitteredProjectionMatrix = nonJitteredProjectionMatrix;
            this.projectionMatrix = projectionMatrix;
#endif
            //this.worldToCameraMatrix = worldToCameraMatrix;
            //this.cameraToWorldMatrix = cameraToWorldMatrix;
            this.targetDisplay = targetDisplay;
            //this.activeTexture = activeTexture;
            //this.targetTexture = targetTexture;
            this.scaledPixelHeight = scaledPixelHeight;
            this.scaledPixelWidth = scaledPixelWidth;
            this.farClipPlane = farClipPlane;
            this.pixelWidth = pixelWidth;
            this.pixelRect = pixelRect;
#if !CINEMACHINE
            this.gateFit = gateFit;
#endif
            this.nearClipPlane = nearClipPlane;
            //this.fieldOfView = fieldOfView;
        }
    }

    //[Serializable]
    public struct AdditionalS3DCamera
    {
        //public Camera[] cameras;
        //public List<Camera> cameras;
        public Camera camera;
        public Camera camera_left;
        public Camera camera_right;
#if POST_PROCESSING_STACK_V2
        public PostProcessLayer PPLayer;
        //public bool PPLayerDefaultStatus;
        public bool PPLayerLastStatus;
#endif
#if HDRP
        public RenderTexture renderTexture;
        public RenderTexture renderTexture_left;
        public RenderTexture renderTexture_right;
#endif
        public CameraDataStruct cameraDataStruct;
        public CameraDataStruct lastCameraDataStruct;

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

        //public SaveLoad(
        //    bool inSwapLR,
        //    bool inOptimize,
        //    bool inVSync,
        //    Method inMethod,
        //    float inPPI,
        //    float inUserIPD,
        //    float inVirtualIPD,
        //    bool inMatchUserIPD,
        //    float inFOV,
        //    float inPanelDepth,
        //    //Vector2 inPanelPos
        //    SavableVector2 inPanelPos
        //    )
        //{
        //    swapLR = inSwapLR;
        //    optimize = inOptimize;
        //    vSync = inVSync;
        //    method = inMethod;
        //    PPI = inPPI;
        //    userIPD = inUserIPD;
        //    virtualIPD = inVirtualIPD;
        //    matchUserIPD = inMatchUserIPD;
        //    FOV = inFOV;
        //    panelDepth = inPanelDepth;
        //    panelPos = inPanelPos;
        //}

        public SaveLoad(
            bool swapLR,
            bool optimize,
            bool vSync,
            Method method,
            float PPI,
            float userIPD,
            float virtualIPD,
            bool matchUserIPD,
            float FOV,
            float panelDepth,
            //Vector2 panelPos
            SavableVector2 panelPos
            )
        {
            this.swapLR = swapLR;
            this.optimize = optimize;
            this.vSync = vSync;
            this.method = method;
            this.PPI = PPI;
            this.userIPD = userIPD;
            this.virtualIPD = virtualIPD;
            this.matchUserIPD = matchUserIPD;
            this.FOV = FOV;
            this.panelDepth = panelDepth;
            this.panelPos = panelPos;
        }
    }

    [Serializable]
    class SaveLoadUserName
    {
        public string userName;

        public SaveLoadUserName(string userName)
        {
            this.userName = userName;
        }
    }

//#if UNITY_EDITOR
//    //#if UNITY_EDITOR && UNITY_2021_2_OR_NEWER
//    [UnityEditor.InitializeOnLoad]
//    public class Startup
//    {
//        static Startup()
//        {
//            //if (debug) Debug.Log("Up and running");

//            //#if UNITY_2021_2_OR_NEWER
//            //foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
//            //    if (package.name == "com.unity.cinemachine")
//            //UnityEditor.PackageManager.Requests.ListRequest packageListRequest = UnityEditor.PackageManager.Client.List();

//            //while (!packageListRequest.IsCompleted){ } //wait for packageListRequest.IsCompleted

//            //foreach (var package in packageListRequest.Result)
//            //    Debug.Log(package.assetPath);

//            //UnityEditor.Compilation.CompilationPipeline.assemblyCompilationStarted -= CompilationPipeline_assemblyCompilationStarted;
//            //UnityEditor.Compilation.CompilationPipeline.assemblyCompilationStarted += CompilationPipeline_assemblyCompilationStarted;
//            //UnityEditor.Compilation.CompilationPipeline.compilationStarted -= CompilationPipeline_compilationStarted;
//            UnityEditor.Compilation.CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted;

//            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine") != null)
//            {
//                ////if (debug) Debug.Log(UnityEditor.EditorUserBuildSettings.selectedStandaloneTarget);
//                //var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
//                //UnityEditor.PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
//                ////if (debug) Debug.Log(defines.Length);

//                //bool cinemachineDefine = false;

//                //foreach (string define in defines)
//                //    if (define.Contains("CINEMACHINE"))
//                //        cinemachineDefine = true;

//                //if (!cinemachineDefine)
//                //{
//                //    string[] newDefines = new string[defines.Length + 1];
//                //    defines.CopyTo(newDefines, 0);
//                //    newDefines.SetValue("CINEMACHINE", defines.Length);
//                //    UnityEditor.PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);

//                //    //foreach (string define in newDefines)
//                //        //if (debug) Debug.Log(define);
//                //}

//                AddDefine("CINEMACHINE");
//            }
//            //#endif

//            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") != null)
//                AddDefine("INPUT_SYSTEM");
//            //else
//            //    RemoveDefine("INPUT_SYSTEM");

//            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.postprocessing") != null)
//                AddDefine("POST_PROCESSING_STACK_V2");
//            //else
//            //    RemoveDefine("POST_PROCESSING_STACK_V2");

//            foreach (var assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies())
//            {
//                if (assembly.name.StartsWith("Assembly-CSharp"))
//                {

//                    foreach (var fileName in assembly.sourceFiles)
//                    {
//                        if (fileName.Contains("LookWithMouse"))
//                            AddDefine("LookWithMouse");

//                        if (fileName.Contains("SimpleCameraController"))
//                            AddDefine("SimpleCameraController");
//                    }
//                }
//            }

//            //if (GraphicsSettings.defaultRenderPipeline.name.Contains("Universal"))
//            //    AddDefine("URP");

//            //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
//            //    AddDefine("URP");

//            //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset"))
//            //    AddDefine("HDRP");

//            //if (GraphicsSettings.defaultRenderPipeline)
//            if (GraphicsSettings.currentRenderPipeline)
//            {
//                //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
//                if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
//                    AddDefine("URP");
//                else
//                    //if (GraphicsSettings.defaultRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset"))
//                    if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HDRenderPipelineAsset"))
//                    AddDefine("HDRP");
//            }

//            //if (debug) Debug.Log(GraphicsSettings.defaultRenderPipeline.GetType().ToString());
//            //if (debug) Debug.Log(GraphicsSettings.defaultRenderPipeline);
//        }
//    }

//    static void CompilationPipeline_assemblyCompilationStarted(string s)
//    {
//        //throw new System.NotImplementedException();
//        Debug.Log("CompilationPipeline_assemblyCompilationStarted: " + s);

//        //if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") == null)
//        //    RemoveDefine("INPUT_SYSTEM");

//        //if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.postprocessing") == null)
//        //    RemoveDefine("POST_PROCESSING_STACK_V2");

//        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") != null)
//            AddDefine("INPUT_SYSTEM");
//        else
//            RemoveDefine("INPUT_SYSTEM");

//        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.postprocessing") != null)
//            AddDefine("POST_PROCESSING_STACK_V2");
//        else
//            RemoveDefine("POST_PROCESSING_STACK_V2");
//    }

//    private static void CompilationPipeline_compilationStarted(object o)
//    {
//        //throw new System.NotImplementedException();
//        Debug.Log("CompilationPipeline_compilationStarted: " + o);

//        //if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") == null)
//        //    RemoveDefine("INPUT_SYSTEM");

//        //if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.postprocessing") == null)
//        //    RemoveDefine("POST_PROCESSING_STACK_V2");

//        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") != null)
//            AddDefine("INPUT_SYSTEM");
//        else
//            RemoveDefine("INPUT_SYSTEM");

//        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.postprocessing") != null)
//            AddDefine("POST_PROCESSING_STACK_V2");
//        else
//            RemoveDefine("POST_PROCESSING_STACK_V2");
//    }

//    static void AddDefine(string defineName)
//    {
//#if UNITY_2021_2_OR_NEWER
//        //if (debug) Debug.Log("AddDefine");
//        var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
//        UnityEditor.PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);

//        bool alreadyDefined = false;

//        foreach (string define in defines)
//            //if (define.Contains(defineName))
//            if (define == defineName)
//            {
//                //if (debug) Debug.Log(defineName + " already Defined");
//                alreadyDefined = true;
//            }

//        if (!alreadyDefined)
//        {
//            string[] newDefines = new string[defines.Length + 1];
//            defines.CopyTo(newDefines, 0);
//            newDefines.SetValue(defineName, defines.Length);

//            //foreach(var def in newDefines)
//            //    if (debug) Debug.Log(def);

//            UnityEditor.PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
//        }
//#else
//        var buildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
//        string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

//        if (!defines.Contains(defineName))
//        {
//            if (defines != "")
//                defines += ";";

//            defines += defineName;
//            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
//        }
//#endif
//    }

//    static void RemoveDefine(string defineName) //not working in same script with defines due after package removed errors in this script not allow code execution so remove defines script must be separate from defines
//    {
//#if UNITY_2021_2_OR_NEWER
//        //if (debug) Debug.Log("AddDefine");
//        var buildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
//        UnityEditor.PlayerSettings.GetScriptingDefineSymbols(buildTarget, out string[] defines);
//        //Debug.Log(defines.ToString());
//        //bool alreadyDefined = false;

//        for (int i = 0; i < defines.Length; i++)
//            //if (define.Contains(defineName))
//            if (defines[i] == defineName)
//            {
//                //if (debug) Debug.Log(defineName + " already Defined");
//                //alreadyDefined = true;
//                defines.SetValue("", i);
//            }

//        //if (!alreadyDefined)
//        //{
//        //    string[] newDefines = new string[defines.Length + 1];
//        //    defines.CopyTo(newDefines, 0);
//        //    newDefines.SetValue(defineName, defines.Length);

//        //    //foreach(var def in newDefines)
//        //    //    if (debug) Debug.Log(def);

//        //    UnityEditor.PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
//        //}
//#else
//        var buildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
//        string defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

//        if (!defines.Contains(defineName))
//        {
//            if (defines != "")
//                defines += ";";

//            defines += defineName;
//            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
//        }
//#endif
//    }
//#endif
}