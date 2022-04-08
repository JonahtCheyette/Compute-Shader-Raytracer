# Compute Shader Raytracer
This is a raytracer I made mostly to learn about compute shaders, so it's not feature rich at all. It can display a ground plane and spheres, all with varying materials. The raytracer will work in the edit mode, but it doesn't converge unless you're in playmode. Made using parts 1 and 2 of [This Tutorial](http://blog.three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/) then heavily modified to be more user-friendly

## How To Use The Raytracer
In edit mode, you'll get a preview of what the raytracer is going to be see. Because of how unity works with image effects, the image will always be very grainy in edit mode.  
If you want the image to be nicer, go into play mode, find a nice angle and spot (mouse to look around, WASD to move, Spacebar to fly up, LShift to fly down) and stay still. The raytracer uses progressive sampling, which is nice and fast (which is great for me because I'm making this on a laptop), but also requires the camera and objects in the scene to not be moving.  
If you aren't moving, and the image still isn't clearing up, try pressing ESC, as that will immediately stop all camera movement.  
As for how to actually set up the scene, in the settings folder, you shouldl find 2 sub folders, one for ground settings and one for sphere generation settings. if you want to add a new preset, simply right click, go to Create, and select either Ground Settings or Sphere Generation Settings. Play around with the variables and sliders in there. When you want to apply your settings to the raytracer, click Main Camera in the scene editor, go to the "Ray Tracing Master" Script attatched to it, and drag your settings into the ground settings and sphere generation settings box respectively.  

## Explanation of Variables
![Camera Settings](https://cdn.discordapp.com/attachments/647518062328938497/890453140464357396/Camera_Editor.png)  
**Max Horizontal Speed/Horizontal Acceleration/Vertical Speed:** All pretty self explanatory, just up these if the camera in play mode ever seems slow  
**Max Num Reflections:** The maximum # of reflections the raytracer will go through before stopping. Move this slider down if your computer is struggling  
**Skybox Lighting:** A value that controls how bright the skybox is. All the light in the scene comes from the skybox, so bringing this up makes everything brighter.  
**Ground Settings/Sphere Generation Settings:** Once again, pretty self explanatory. This is where you drag and drop Ground/Sphere Generation Settings assets to apply them to the camera.  
**Ray Tracing Shader:** Holds the compute shader that does the actual raytracing. ***DO NOT TOUCH!!***  
**Skybox Texture:** Drag and drop the skybox texture you wanna use here. The project comes with 3, but feel free to use your own.  

![Sphere Generation Settings](https://cdn.discordapp.com/attachments/647518062328938497/890453136894984212/Sphere_Generation_Settings.png)  
**Sphere Seed:** The seed for the random # generator  
**Sphere Radius:** The minimum and maximum radii the spheres can have   
**Spheres Max:** The maximum # of spheres that will be created  
**Sphere Placement Radius:** The spheres are placed in a circle on the ground. This number controls the size of that circle  
**Use Emissive Spheres:** Whether or not some spheres should be glowing.  
**Use size range:** Whether or not to only make spheres in a certain size range glow. *This does not mean that all spheres within that size range glow. The percentage of spheres within that range that glow is determined by Emissive Chance.*  
**Emissive Chance:** If Use Emissive Spheres is checked, the chance of a given sphere glowing  
**Emissive Size Range:** If Use Size Range is checked, the size range a given sphere has to be in to glow  
**Metallic Percentage:** The percentage of spheres that will be metallic (Highly reflective)  
**Non Metal Reflectiveness:** Controlls how reflective the non-metal spheres will be  
**Metallic Smoothness Range:** Controlls the range of smoothness values the metallic spheres will have, which determines how light will reflect off them  
**Non Metallic Smoothness Range:** Same as above, but for the non metallic spheres  
  
![Ground Settings](https://cdn.discordapp.com/attachments/647518062328938497/890453148538396702/Ground_Settings.png)  
**Albedo:** The color of the ground, as RGB  
**Specular:** The tint of the ground's reflections, as RGB  
**Smoothness:** How smooth the ground is, which affects reflections  
**Emission:** The light the ground emits, as RGB  
  
## Some Examples
![Image 1](https://cdn.discordapp.com/attachments/647518062328938497/890453143710728192/Compute-Shader-Raytracer_-_SampleScene_-_PC_Mac__Linux_Standalone_-_Unity_2019.4.3f1_Personal___DX11.png)  
![Image 2](https://cdn.discordapp.com/attachments/647518062328938497/890453146437029918/Compute-Shader-Raytracer_-_SampleScene_-_PC_Mac__Linux_Standalone_-_Unity_2019.4.3f1_Personal___DX11.png)  
![Image 3](https://cdn.discordapp.com/attachments/647518062328938497/890453148932665404/Compute-Shader-Raytracer_-_SampleScene_-_PC_Mac__Linux_Standalone_-_Unity_2019.4.3f1_Personal___DX11.png)  
