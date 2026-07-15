# Skald Unity Plugin

A Unity plugin that connects your game project to [Skald](https://skald.dual-daggers.com).

See [Skald Unity Documentation](https://docs.skald.dual-daggers.com/unity) for in-depth explanations.

Skald is a web-based tool for authoring dialogue and narrative. Write and iterate on conversations in Skald, sync them into Unity, and run them at runtime with a built-in dialogue engine.

## Features

- **Sync from Skald** — Log in through the Unity editor, browse your Skald projects, and import them as JSON into your Unity project.
- **Dialogue engine** — Play back conversations with support for:
  - Dialogue lines with speaker characters
  - Player choices (with optional preconditions)
  - Variables (`string`, `integer`, `float`, `boolean`)
  - Variable assignments and conditional branching
  - Rich text (bold, italic, underline, strikethrough, color) and inline variable interpolation
- **Pluggable presentation** — Implement `IDialoguePresenter` to drive your own UI (UGUI, UI Toolkit, world-space text, etc.).
- **Demo included** — A sample scene with a UI Toolkit conversation UI based on *Little Red Riding Hood*.

## Requirements

- **Unity 6** (tested with `6000.4.8f1`)
- [Newtonsoft Json for Unity](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest) (`com.unity.nuget.newtonsoft-json`)

## Installation

### Option A — Use this repository as a starting point

Clone the repo and open it in Unity:

```bash
git clone https://github.com/Dreunin/skald-unity-plugin.git
```

Open the project folder in Unity Hub.

### Option B — Add to an existing Unity project

1. Copy the `Assets/Skald` folder into your Unity project.
2. Install the Newtonsoft Json package via **Window → Package Manager** (or add `"com.unity.nuget.newtonsoft-json": "3.2.2"` to `Packages/manifest.json`).

The demo-specific code lives under `Assets/Skald/Demo/` and is optional for integration into your own game.

## Syncing a project from Skald

1. In Unity, open **Tools → Skald → Skald UI** in the header.
2. In the opened UI, click **Connect to Skald**. Your browser opens to authenticate with [skald.dual-daggers.com](https://skald.dual-daggers.com).
3. Complete login in the browser. Unity polls until the session is verified.
4. Click **Get projects** to fetch your Skald project list.
5. Select a project and click **Import Selected Project**.

Imported data is written to:

```
Assets/Resources/Skald/<project-id>.json
```

Re-import any time you update narrative content in Skald.

## Running conversations at runtime

### Quick start with the demo

1. Open `Assets/Skald/Demo/Scenes/DemoScene UI Toolkit.unity`.
2. Press Play. The included `Presentation` component loads the example project and starts a conversation.

## Project structure

```
Assets/Skald/
├── Code/
│   ├── DialogueEngine.cs    # Conversation runner and variable state
│   ├── Interpreter.cs       # Expression and rich-text evaluation
│   ├── ExpressionResult.cs  # Typed expression results
│   ├── LanguageClasses.cs   # Skald language / AST types
│   ├── SyncClasses.cs       # Import schema (nodes, characters, etc.)
│   └── Editor/
│       ├── SkaldUI.cs       # Tools → Skald → Skald UI window
│       ├── SyncWithSkald.cs # API client for skald.dual-daggers.com
│       └── SyncWithSkaldState.cs
├── Demo/                    # Example scene, materials, and UI Toolkit presenter
│   └── UI/                  # Editor window styling
├── Prefabs/                 # Skald prefab
    

Assets/Resources/Skald/      # Imported project JSON (created on sync)
```

## Links

- **Skald (authoring):** [skald.dual-daggers.com](https://skald.dual-daggers.com)
- **Skald Documentation::** [docs.skald.dual-daggers.com](https://docs.skald.dual-daggers.com)
