# Stereoscopic 3D system for Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP
![Stereo3D](https://forum.unity.com/attachments/8xmsaa-taa-png.768466/)

I spent thousands of hours in virtual reality using the horizontal interleaved Stereo3D method with my Zalman polarized glasses and LG D2342P monitor, sometimes more than 12 hours per day without any problems with my eyes. This even more healthy for the eyes as they not holding converge fixed focus on the screen unlike mono image - focus all-time dynamically changes from near to very far on distant objects, same like watching through a window in a reality where eyes relaxing being on parallel axes. :sunglasses:

I love Stereoscopic 3D and don't want to see a mono image of the 3D world on screen anymore, but using drivers like iZ3D, Tridef, Nvidia 3D Vision, etc was always a pain.
All of them have incorrect approaches in settings like Separation/Convergence which valid only for fixed FOV(Field Of View), when FOV is changed then settings no longer valid.
Also there always problems with the not correct depth of shadows, post-process effects, etc, bad profiles for games. I also made fixes and profiles for games in past.

I good understand how Stereoscopic 3D working and made the correct system for Unity.  
Now I using precision Stereo3D in my projects and enjoying it like never.  

Move the `Stereo3D` folder to the Unity `Assets` folder or import `Stereo3D.unitypackage`.  
Just add C# script to any camera and go.  
Tested on Unity 2019 and 2020 with default render + Post Processing Stack v2, URP, and HDRP.  
More info in the script file.  

I also made the build `Stereo3D_Unity_Demo.7z` with Unity default demo scene with `Post Processing Stack V2`.  
Post-process effects: Color Grading, Bloom, Motion Blur, Vignette, Ambient Occlusion, Depth Of Field, Temporal Anti-aliasing.  
Run via `3DWE.exe` for `Fullscreen Windowed` mode with VSync or `3DWE.exe - Exclusive Fullscreen_noVSync` for `Exclusive Fullscreen` mode without VSync.  

Key Controls:
   `W,S,A,D` moving, `Q,E` down/up  
   `Tab` - hide/show Stereo3D settings panel  
   `*` On/Off S3D, `Ctrl + *` swap left-right cameras  
   `+,-` Field Of View, `Ctrl + `+,-`` custom `Virtual IPD` when unchecked `Match User IPD`  
   All above keys  + `Left Shift` for faster changes  
   `Mouse` Look around  
   `Esc` exit  

When launch, Monitor's Dots Per Inch(DPI) will be autodetected and precision Screen width will be calculated,
as result - settings will be in real millimeters so you don't need to set `DPI` or `Pixel Pitch` manually, set it only if `DPI` incorrect.
Set `User IPD` to your own interpupillary distance(IPD) for a realistic view with infinity S3D depth.
Or set `User IPD` lower than your own IPD for close distance max depth(aquarium back wall effect).
Uncheck `Match user IPD` and set `Virtual IPD`(Cameras IPD in the virtual world) larger than your own IPD for toy world effect and vise versa.
With realistic IPD's `Screen Distance` will show how far from the screen your eyes should be(camera's point) where Real and Virtual FOV will match and you get a 100% realistic view. (Very important for Vehicle Simulators). 
Also you can download S3D demo as zip archive from here - https://drive.google.com/file/d/1cORanOGO8Elsz7Cn8yj2XwyNVYY5t9lg/view?usp=sharing
