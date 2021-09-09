# Mid-Air Drawing of Curves on Surfaces in Virtual Reality
Source code and study data for the TOG 2021 paper: Mid-Air Drawing of Curves on Surfaces in Virtual Reality.


## How to use
Clone the repository including the submodules
```
git clone --recurse-submodules https://github.com/raora7777/curve-on-surface-drawing-vr
```
Then add this project to Unity and open it. The included example scene `./Assets/Scences/main.unity` shows an example usage.

### Dependencies
- **Unity**. Tested with Unity 2020.3 LTS, but it should work with Unity 2019.4 onwards, with some fiddling with SteamVR Unity Asset and Unity XR due to their ever changing APIs. https://unity3d.com/get-unity/download
- **SteamVR**. https://store.steampowered.com/app/250820/SteamVR/
- **Steam** required for SteamVR. https://store.steampowered.com/about/
- All other depdendencies are included.

### Setting up your own scene (for anchored closest point: mimicry without Phong projection)
- Add a `StrokeMimicryManager` component to any GameObject.
- Add The `CameraRig` prefab included with SteamVR.
- Add the model you want to use as a target surface for drawing curves and add a `StrokeMimicryTarget` component to it. This object does not need to have the `MeshFilter` component attached to itself, but one of its descendents must.
- Set the model name. You must include the target mesh as an OBJ file in the StreamingAssets folder `./Assets/StreamingAssets/<modelname>.obj`. This folder can be modified: see the Inspector UI for `StrokeMimicryManager`.

### Additional steps for setting up your own scene (for anchored smooth closest point: mimicry with Phong projection)
- Download the pre-processing code below and follow the instructions to run it on your model.
- Add the generated files `modelname_tri.txt`, `modelname_tet.txt`, `modelname_out.obj` and (if generated) `modelname_in.obj` to the StreamingAssets folder or the custom path you set up above.
- If `modelname_in.obj` is generated, select the `Load Inside Offset Surface` checkbox in the Inspector UI for `StrokeMimicryTarget`.

### Things to keep in mind
- Only one `StrokeMimicryTarget` component should be active at a time. If multiple target objects (multiple objects with `StrokeMimicryTarget` components) are present and active before running the game, one will be arbitrarily selected. If multiple target objects become active only during gameplay, then the component activated last is selected.
- The brains of the code are in `./Assets/Scripts/Core/Projection.cs`. Other files in `Core` are required helper classes. Files outside of `Core` are only required for the demo, and should probably be replaced in any real-world application.

## Pre-processing code
Pre-processing code (MATLAB) and C++ source for the Phong projection DLL are available at https://github.com/rarora7777/smooth-closest-point. 

## Study data
Data will be made available soon (by Sep 2021).
