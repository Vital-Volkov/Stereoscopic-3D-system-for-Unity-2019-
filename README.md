# Stereoscopic 3D system for Direct3D 11.1 native S3D of Unity 2018, 2019 and 2020 with default render + Post Processing Stack v2
![Stereo3D](https://drive.google.com/uc?id=10Vul4ZncqyWoVubNM5CcqAyIQVcoUMOS)

I spent thousands of hours in virtual reality using the horizontal interleaved Stereo3D method with my Zalman polarized glasses and LG D2342P monitor, sometimes more than 12 hours per day without any problems with eyes. Stereo 3D even more healthy for the eyes as they not holding constant geometry focus on the screen plane like with mono image - in S3D eyes geometry focus dynamically changes in the full depth range dependent where you looking at from before a screen objects to infinity far behind a screen on distant objects, same as watching through a window to outdoor distant mountains or Starsky in a reality where eyes relaxing being on parallel axes. :sunglasses:

I love Stereoscopic 3D and don't want to see a mono image of the 3D world anymore, but using drivers like iZ3D, Tridef, Nvidia 3D Vision, etc was always a pain.
All of them have incorrect approaches in settings like Separation/Convergence which valid only for fixed FOV(Field Of View), when FOV is changed then settings ruined.
Also there always problems with the not correct depth of shadows, post-process effects, etc, bad profiles for games. I also made fixes and profiles for games in past.

I good understand how Stereoscopic 3D working and made the correct system for Unity.  
Now I using precision Stereo3D in my projects and enjoying it like never.  

Move the `Stereo3D` & `Editor` folder to the Unity `Assets` folder or import `Stereo3D.unitypackage`.  
Just add C# script to any camera and go.  

Tested on Unity 2018, 2019 and 2020 with default render + `Post Processing Stack v2`, URP, and HDRP.  
DirectX11.1 S3D works correct with default render pipeline and Gamma color space, in Unity 2018(Linear color space, URP and HDRP not working), 2019 and 2020(exclusive fullscreen not work, Linear white-out, URP and HDRP have problems).  

Direct3D 11.1 S3D only works in built Player if `Stereo Display` SDK is added in `Project Settings-Player-XR Settings-Virtual Reality Supported`(Unity2018,2019 or via custom Editor panel menu `VR SDK\Build with Stereo3D`) and `Stereo 3D` enabled in Player launch window(in Unity 2019+ it always enabled) and Direct3D 11.1 Stereoscopic driver is enabled in Windows8.1+.  
More info in the script file.  

I also made a build with Unity default demo scene with `Post Processing Stack V2`:

7z - https://github.com/Vital-Volkov/Stereoscopic-3D-system-for-Unity-2019-/blob/Direct3D11.1/Direct3D11.1_Stereo3D_Gamma_Unity_Demo.7z  

or

zip - https://drive.google.com/file/d/1o8i4hF2F9Ci_ARVzW9W8AM4rPphA0h9-/view?usp=sharing

Post-process effects: Color Grading, Bloom, Motion Blur, Vignette, Ambient Occlusion, Depth Of Field, Temporal Anti-aliasing.  
Run via `3DWE.exe` for `Fullscreen Windowed` mode with VSync or `3DWE.exe - Exclusive Fullscreen_noVSync` for `Exclusive Fullscreen` mode without VSync.

Key Controls:
   `W,S,A,D` moving, `Q,E` down/up  
   `Tab` - hide/show Stereo3D settings panel  
   `*` swap left-right cameras  
   `+,-` Field Of View, `Ctrl + +,-` custom `Virtual IPD` when unchecked `Match User IPD`  
   All above keys  + `Left Shift` for faster changes  
   `Mouse move + right click` Look around  
   `Esc` exit  

When launch, Monitor's Pixels Per Inch(PPI) should be autodetected and precision screen width will be calculated internally and settings should be in real millimeters, so you don't need to set `PPI` or `Pixel Pitch` manually if the `PPI` of your screen autodetected correctly. Sure, also Save/Load user settings should be implemented with a Unity project.
Set `User IPD` to your own interpupillary distance(IPD) for a realistic view with infinity S3D depth.  
Uncheck `Match user IPD` and set `Virtual IPD`(Cameras IPD in the virtual world) larger than your own IPD for toy world effect and vise versa.
`Screen Distance` will show how far from the screen your eyes should be(camera's point) where Real and Virtual FOV will match and you get a 100% realistic view. (Very important for Vehicle Simulators).
