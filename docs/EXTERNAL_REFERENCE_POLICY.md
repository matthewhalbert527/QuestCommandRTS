# External Reference Policy

QuestCommandRTS must remain an original Unity project. Classic RTS material can be used only as local reference for broad concepts such as command routing, grid maps, production queues, visibility, and simulation sequencing.

## Boundary

- Reference source must not live under `Assets`.
- Reference source must not be imported by Unity.
- Reference source must not be compiled, linked, packaged, or committed with this project.
- Reference source must not be copied, translated, or ported into Unity scripts.
- Reference comments, identifiers, constants, data tables, algorithms, names, logos, art, audio, missions, story, and faction identity must not be embedded in QuestCommandRTS.
- Future implementation work should identify the general RTS concept first, then build original Unity systems using project-appropriate names and behavior.

## Local Reference Location

The copied reference folder was moved out of the Unity project:

`E:\UnityProjects\ExternalReference\CnC_Red_Alert-main`

The Unity project ignores local reference folders with `.gitignore` rules so this material is not committed or packaged.

## Validation

Use this check before commits that touch project layout or imports:

```powershell
Get-ChildItem -Path .\Assets -Recurse -File -Include *.cpp,*.c,*.h,*.hpp,*.asm,*.mak,*.pas,*.rc
```

Expected result: no files.
