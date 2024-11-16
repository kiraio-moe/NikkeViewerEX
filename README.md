<div aling="center">
  <img src="./nikke-viewer-ex-logo.png" />
  <br />
  <br />
</div>

Nikke Viewer EX is a tool designed for previewing characters from the game GODDESS OF VICTORY: NIKKE. This viewer allows users to engage with the character Live 2D (Spine 2D) animations along with audio, providing an immersive and dynamic experience.

![Nikke Viewer EX preview](./preview.png "Nikke Viewer EX preview")

## Features

- Interactable Live 2D:
  - Portrait ✅
  - Cover 🚧
  - Aim 🚧
- Preview 3D Mesh with Animations 🚧
- Live Wallpaper Support ✅

## How to Use

> [!NOTE]  
> You can download the Nikke's assets from [nikke-db](https://github.com/Nikke-db/Nikke-db.github.io/tree/main/l2d).

### Nikke List

1. Adding a Nikke to Nikke List:
    - Click the **Add** button.
    - Browse and select the local assets from your device using the browse button ⬜ or enter the URL directly.
2. Setting up Assets:
    - Skel: Select an asset or URL ending with `.skel`.
    - Atlas: Select an asset or URL ending with `.atlas`.
    - Textures: Choose assets or URLs ending with `.png`.
    - Voices (Optional): Select a directory containing audio files on your local device. For URL, ensure the server return JSON data or directly link to a `.json` file listing the audio file paths. Supported formats include `.mp3`, `.ogg`, `.wav`.
    - Nikke name: Give your Nikke a name for easy identification.
3. Applying Settings:
    - Click the **Apply** button to save your settings.
4. Interacting with Nikke:
    - Move Nikke by **clicking** and **holding** with the `Left Mouse` button, then drag and drop.
    - Interact with Nikke by **clicking** with the `Left Mouse` button.
5. Others:
    - Toggle the main control panel visibility by hovering your mouse on top screen until a bar with **Hide UI** toggle appear.
    - Lock Nikke position & scale by toggling the **Lock Button** on each Nikke item in Nikke List tab.
    - Reset Nikke position & scale by clicking on **Person icon** in scale slider.
    - Remove a Nikke by clicking ❌ button.

### Settings

- **BG**: The background image.
- **BGM**: The background music.
  - **Volume**: Control the BGM volume with the slider and toggle pause/play of the BGM with ▶️ button.
- **FPS**: Change the application FPS. Automatically applied without clicking **Apply** button.
- **Apply**: Save your settings and load the image/audio you specified in the input fields.

## Contributing

We welcome contributions to enhance this project! Feel free to suggest improvements, report bugs, or submit pull requests. Don't hesitate ;).

### Development

- Unity 6 (6000.0.23f1)
- Platform: Windows

## Credits

- [EsotericSoftware](https://github.com/EsotericSoftware)/[spine-unity](http://esotericsoftware.com/spine-unity) - The backbone of this project.
- [skuqre](https://github.com/skuqre)/[nikke-font-generator](https://github.com/skuqre/nikke-font-generator) - Used as project logo.
- [Ayfel](https://github.com/Ayfel)/[MRTK-Keyboard](https://github.com/Ayfel/MRTK-Keyboard) - Stripped NonNative Keyboard from the MRTK project: <https://github.com/microsoft/MixedRealityToolkit-Unity>
- [yasirkula](https://github.com/yasirkula)/
  - [DynamicPanels](https://github.com/yasirkula/UnityDynamicPanels) - Provide the dynamic tabbed menu.
  - [UnityIngameDebugConsole](https://github.com/yasirkula/UnityIngameDebugConsole) - Runtime console.
  - [UnitySimpleFileBrowser](https://github.com/yasirkula/UnitySimpleFileBrowser) - Runtime file/folder browser.
- [gilzoide](https://github.com/gilzoide)/[unity-serializable-collections](https://github.com/gilzoide/unity-serializable-collections) - Provide serialized types that Unity not support.

## Licenses

This project is licensed under [MIT License](./LICENSE "See LICENSE file").  
Spine Runtimes is licensed under [Spine Runtimes License Agreement](https://esotericsoftware.com/spine-runtimes-license).  
For others, see their repositories in the [Credits](#credits) section.
