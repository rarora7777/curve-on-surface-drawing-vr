# Mid-Air Drawing of Curves on Surfaces in Virtual Reality
Source code and study data for the TOG 2021 paper: Mid-Air Drawing of Curves on Surfaces in Virtual Reality.

For a barebones demo of our _Mimicry_ technique, try out the pre-built demo: https://github.com/rarora7777/curve-on-surface-drawing-vr/releases/tag/v0.1.

To try out the user study for yourself, download the study executable: https://github.com/rarora7777/curve-on-surface-drawing-vr/releases/tag/StudyExecutable.

To download the raw data from our user study and code to read and analyze the data: https://github.com/rarora7777/curve-on-surface-vr-data.

For the MATLAB pre-processing code required for Phong projection, and the C++ code for building the Phong projection DLL: https://github.com/rarora7777/smooth-closest-point.

To use the C#/Unity source code for using the projection techniques described in the paper, read on.


## Getting started
To start, clone the repository including the submodules
```
git clone --recurse-submodules https://github.com/raora7777/curve-on-surface-drawing-vr
```
Then add this project to Unity and open it. The included example scene `./Assets/Scences/main.unity` shows an example usage.

## Dependencies
- **Unity**. Tested with Unity 2020.3 LTS, but it should work with Unity 2019.4 onwards, with some fiddling with SteamVR Unity Asset and Unity XR due to their ever changing APIs. https://unity3d.com/get-unity/download
- **SteamVR**. https://store.steampowered.com/app/250820/SteamVR/
- **Steam** required for SteamVR. https://store.steampowered.com/about/
- All other depdendencies are included.

## Setting up your own scene (for anchored closest point: mimicry without Phong projection)
- Add a `StrokeMimicryManager` component to any GameObject.
- Add The `CameraRig` prefab included with SteamVR.
- Add the model you want to use as a target surface for drawing curves and add a `StrokeMimicryTarget` component to it. This object does not need to have the `MeshFilter` component attached to itself, but one of its descendents must.
- Set the model name. You must include the target mesh as an OBJ file in the StreamingAssets folder `./Assets/StreamingAssets/<modelname>.obj`. This folder can be modified: see the Inspector UI for `StrokeMimicryManager`.

## Additional steps for setting up your own scene (for anchored smooth closest point: mimicry with Phong projection)
- Download the pre-processing code below and follow the instructions to run it on your model.
- Add the generated files `modelname_tri.txt`, `modelname_tet.txt`, `modelname_out.obj` and (if generated) `modelname_in.obj` to the StreamingAssets folder or the custom path you set up above.
- If `modelname_in.obj` is generated, select the `Load Inside Offset Surface` checkbox in the Inspector UI for `StrokeMimicryTarget`.

## Things to keep in mind
- Only one `StrokeMimicryTarget` component should be active at a time. If multiple target objects (multiple objects with `StrokeMimicryTarget` components) are present and active before running the game, one will be arbitrarily selected. If multiple target objects become active only during gameplay, then the component activated last is selected.
- The brains of the code are in `./Assets/Scripts/Core/Projection.cs`. Other files in `Core` are required helper classes. Files outside of `Core` are only required for the demo, and should probably be replaced in any real-world application.

## Controls
Two controls are defined:
- **Action**. Hold the action button to draw or erase curves.
- **Toggle**. Press the toggle button to switch between drawing and erasing interactions.

**Default Control Mappings**
| Control       | Oculus Touch  | Valve Knuckles | Vive Controller |
| ------------- | ------------- | -------------- | --------------- |
| Action        | Main trigger  | Trigger        | Trigger         |
| Toggle        | A/X           | A              | Menu            |

## Citing

If this code is useful in a research project, please cite the following paper:
```
@article{arora2021midair,
	title={Mid-Air Drawing of Curves on {3D} Surfaces in Virtual Reality}, 
	author={Rahul Arora and Karan Singh},
	journal={ACM Trans. Graph.},
	volume={40},
	number={3},
	numpages={17},
	year={2021},
	month = jul,
	url = {http://doi.org/10.1145/3459090},
	doi = {10.1145/1122445.1122456},
	publisher = {Association for Computing Machinery},
	address = {New York, NY, USA}
}
```
