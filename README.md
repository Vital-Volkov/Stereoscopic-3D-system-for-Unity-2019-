# Stereoscopic 3D system for Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP
![Stereo3D](https://forum.unity.com/attachments/8xmsaa-taa-png.768466/)

I spent thousands of hours in virtual reality using the horizontal interleaved Stereo3D method with my Zalman polarized glasses and LG D2342P monitor, sometimes more than 12 hours per day without any problems with eyes. Stereo 3D even more healthy for the eyes as they not holding constant geometry focus on the screen plane like with mono image - in S3D eyes geometry focus dynamically changes in the full depth range dependent where you looking at from before a screen objects to infinity far behind a screen on distant objects, same as watching through a window to outdoor distant mountains or Starsky in a reality where eyes relaxing being on parallel axes. :sunglasses:

I love Stereoscopic 3D and don't want to see a mono image of the 3D world anymore, but using drivers like iZ3D, Tridef, Nvidia 3D Vision, etc was always a pain.
All of them have incorrect approaches in settings like Separation/Convergence which valid only for fixed FOV(Field Of View), when FOV is changed then settings ruined.
Also there always problems with the not correct depth of shadows, post-process effects, etc, bad profiles for games. I also made fixes and profiles for games in past.

I good understand how Stereoscopic 3D working and made the correct system for Unity.  
Now I using precision Stereo3D in my projects and enjoying it like never.  

Move the `Stereo3D` folder to the Unity `Assets` folder or import `Stereo3D.unitypackage`.  
Just add C# script to any camera and go.  
Tested on Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP.  
In Unity 2018 SRP not rendering screenQuad so use it only with the default render.  
More info in the script file.  


I also made a build with Unity default demo scene with `Post Processing Stack V2`:

7z - https://github.com/Vital-Volkov/Stereoscopic-3D-system-for-Unity-2019-/blob/main/Stereo3D_Unity_Demo.7z

or

zip - https://drive.google.com/file/d/1cORanOGO8Elsz7Cn8yj2XwyNVYY5t9lg/view?usp=sharing

Post-process effects: Color Grading, Bloom, Motion Blur, Vignette, Ambient Occlusion, Depth Of Field, Temporal Anti-aliasing.  
Run via `3DWE.exe` for `Fullscreen Windowed` mode with VSync or `3DWE.exe - Exclusive Fullscreen_noVSync` for `Exclusive Fullscreen` mode without VSync.

Key Controls:
   `W,S,A,D` moving, `Q,E` down/up  
   `Tab` - hide/show Stereo3D settings panel  
   `*` On/Off S3D, `Ctrl + *` swap left-right cameras  
   `+,-` Field Of View, `Ctrl + +,-` custom `Virtual IPD` when unchecked `Match User IPD`  
   All above keys  + `Left Shift` for faster changes  
   `Mouse move + right click` Look around  
   `Esc` exit  

When launch, Monitor's Pixels Per Inch(PPI) should be autodetected and precision screen width will be calculated internally and settings should be in real millimeters, so you don't need to set `PPI` or `Pixel Pitch` manually if the `PPI` of your screen autodetected correctly. Sure, also Save/Load user settings should be implemented with a Unity project.
Set `User IPD` to your own interpupillary distance(IPD) for a realistic view with infinity S3D depth.
Or set `User IPD` lower than your own IPD for close distance max depth(aquarium back wall effect).
Uncheck `Match user IPD` and set `Virtual IPD`(Cameras IPD in the virtual world) larger than your own IPD for toy world effect and vise versa.
`Screen Distance` will show how far from the screen your eyes should be(camera's point) where Real and Virtual FOV will match and you get a 100% realistic view. (Very important for Vehicle Simulators).
