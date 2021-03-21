 using UnityEditor;
 
 [InitializeOnLoad]
 public class VR_SDK_Enable_EditorMenu
 {
     [MenuItem("VR SDK/Build with Stereo3D")]
     static void WithStereo3D()
     {
         PlayerSettings.virtualRealitySupported = true;
         UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone, new string[] { "stereo" });
     }
 
     [MenuItem("VR SDK/Build without Stereo3D")]
     static void WithoutStereo3D()
     {
         PlayerSettings.virtualRealitySupported = true;
         UnityEditorInternal.VR.VREditor.SetVREnabledDevicesOnTargetGroup(BuildTargetGroup.Standalone, new string[] { });
     }

     [MenuItem("VR SDK/Disable Stereo3D SDK")]
     static void DisableVRSDK()
     {
         PlayerSettings.virtualRealitySupported = false;
     }
 }