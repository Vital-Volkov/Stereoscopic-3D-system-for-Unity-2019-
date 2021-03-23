# Stereoscopic 3D system for Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP + Direct3D 11.1 native S3D
![Stereo3D](https://drive.google.com/uc?id=19r3Bb8qQI4b1cdDhZ444Z1ttUry3luPs)

I spent thousands of hours in virtual reality using the horizontal interleaved Stereo3D method with my Zalman polarized glasses and LG D2342P monitor, sometimes more than 12 hours per day without any problems with eyes. Stereo 3D even more healthy for the eyes as they not holding constant geometry focus on the screen plane like with mono image - in S3D eyes geometry focus dynamically changes in the full depth range dependent where you looking at from before a screen objects to infinity far behind a screen on distant objects, same as watching through a window to outdoor distant mountains or Starsky in a reality where eyes relaxing being on parallel axes. :sunglasses:

I love Stereoscopic 3D and don't want to see a mono image of the 3D world anymore, but using drivers like iZ3D, Tridef, Nvidia 3D Vision, etc was always a pain.
All of them have incorrect approaches in settings like Separation/Convergence which valid only for fixed FOV(Field Of View), when FOV is changed then settings ruined.
Also there always problems with the not correct depth of shadows, post-process effects, etc, bad profiles for games. I also made fixes and profiles for games in past.

I good understand how Stereoscopic 3D working and made the correct system for Unity.  
Now I using precision Stereo3D in my projects and enjoying it like never.  

Move the `Stereo3D` & `Editor` folder to the Unity `Assets` folder or import `Stereo3D.unitypackage`.  
Just add C# script to any camera and go.  

Tested on Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP.  
In Unity 2018 SRP not rendering screenQuad so use it only with the default render.  

Direct3D 11.1 S3D only works in built Player if `Stereo Display` SDK is added in `Project Settings-Player-XR Settings-Virtual Reality Supported`(Unity2018,2019 or via custom Editor panel menu `VR SDK\Build with Stereo3D`) and `Stereo 3D` enabled in Player launch window(in Unity 2019+ it always enabled) and Direct3D 11.1 Stereoscopic driver is enabled in Windows8.1+.  
Set the same Key for Direct3D 11.1 S3D driver on/off in Windows and `driverS3DKey` in this script to toggle them together by one Key(useful for switch to mono with more FPS and correct eye of `parentCam`(Direct3D 11.1's mono is slow and using left eye always).  
Direct3D 11.1 S3D works in Gamma color space in Unity 2018(Linear color space not work) and 2019(exclusive fullscreen not work, Linear white-out, SRP broke S3D when switching fullscreen/window mode if `DirectX11.1 S3D` disabled, so switch window mode while it enabled).  
More info in the script file.  

I also made a build with Unity default demo scene with `Post Processing Stack V2`:

7z Gamma - https://github.com/Vital-Volkov/Stereoscopic-3D-system-for-Unity-2019-/blob/main%26Direct3D11.1/Stereo3D%26Direct3D11.1_Gamma_Unity_Demo.7z  
7z Linear - https://github.com/Vital-Volkov/Stereoscopic-3D-system-for-Unity-2019-/blob/main%26Direct3D11.1/Stereo3D%26Direct3D11.1_Linear_Unity_Demo.7z

or

zip - https://drive.google.com/file/d/1GcSBspZIISI0UMH745li9e0Xp2iZbxyQ/view?usp=sharing

Post-process effects: Color Grading, Bloom, Motion Blur, Vignette, Ambient Occlusion, Depth Of Field, Temporal Anti-aliasing.  
Run via `3DWE.exe` for `Fullscreen Windowed` mode with VSync or `3DWE.exe - Exclusive Fullscreen_noVSync` for `Exclusive Fullscreen` mode without VSync.(`Exclusive Fullscreen` not working with Direct3D 11.1 S3D in Unity 2019+)

Key Controls:
   `W,S,A,D` moving, `Q,E` down/up  
   `Tab` - hide/show Stereo3D settings panel  
   `*` On/Off S3D, `Ctrl + *` swap left-right cameras 
   `Scroll Lock` On/Off `DirectX11.1 S3D`
   `+,-` Field Of View, `Ctrl + +,-` custom `Virtual IPD` when unchecked `Match User IPD`  
   All above keys  + `Left Shift` for faster changes  
   `Mouse move + right click` Look around  
   `Esc` exit  

When launch, Monitor's Pixels Per Inch(PPI) should be autodetected and precision screen width will be calculated internally and settings should be in real millimeters, so you don't need to set `PPI` or `Pixel Pitch` manually if the `PPI` of your screen autodetected correctly. Sure, also Save/Load user settings should be implemented with a Unity project.
Set `User IPD` to your own interpupillary distance(IPD) for a realistic view with infinity S3D depth.
Or set `User IPD` lower than your own IPD for close distance max depth(aquarium back wall effect).
Uncheck `Match user IPD` and set `Virtual IPD`(Cameras IPD in the virtual world) larger than your own IPD for toy world effect and vise versa.
`Screen Distance` will show how far from the screen your eyes should be(camera's point) where Real and Virtual FOV will match and you get a 100% realistic view. (Very important for Vehicle Simulators).
