# Isolate Selected in Editor

Isolationist is a small Unity Editor utility that allows you to toggle selected assets into an Isolate mode similar to `Tools > Isolate Select` in Maya where all other objects are hidden while you are editing the selected objects. This makes the objects you've isolated easiser to work with and keeps you from accidentally selecting/editing objects you aren't working on.

## To Use
- Install from the [Unity Asset Store](https://www.assetstore.unity3d.com/#!/content/57758)
- Or download and install the [Isolationist.unitypackage](https://github.com/bjennings76/isolationist-unity/raw/master/Isolationist.unitypackage)

## To Enter Isolate Mode
- Select the object or objects you want to isolate
- Use the keyboard shortcut <kbd>I</kbd>
- Or use `Tools Menu > Isolate Selected`

## To Exit Isolate Mode
- Select a hidden object in the heirarchy
- Or use the keyboard shortcut <kbd>I</kbd>
- Or use `Tools Menu > Isolate Selected`

## To Modify the Keyboard Shortcut
- Open `Edit > Preferences... > Isolationist` 
- Select the desired base key and modifiers

## Tips and Tricks

### Ctrl+Click or Shift+Click to Add
While in Isolate mode, use <kbd>Ctrl</kbd>+<kbd>Left Click</kbd> or <kbd>Shift</kbd>+<kbd>Left Click</kbd> in the hierarchy to add hidden objects to the isolation group.

### Leave Isolate Before Building
Isolate mode doesn't know when a build is about to start, so objects may get into the build in their 'hidden' state. They will be re-enabled as soon as the game starts (on `Awake()`), but this may not be fast enough for scripts that run even faster.
