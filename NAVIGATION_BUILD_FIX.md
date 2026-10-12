# Player navigation repair

RaidPresentationNavigation.asset is native binary NavMeshData (class/fileID 23800000).
The broken 24,670-byte file contained thousands of UTF-8 replacement sequences
(EF BF BD): a previous text-normalization pass treated the binary .asset as text.
The Editor could retain its already loaded navigation; the Player build had to
read the damaged file from disk. CopySerialized was not the demonstrated cause.

Regenerated from scene colliders with NavMeshSurface.BuildNavMesh and saved the
fresh NavMeshData with AssetDatabase.CreateAsset. GUID/reference remained valid.
Text formatting must be restricted to files with a YAML header, never all .asset files.

125/125 EditMode tests passed. Windows x86_64 build succeeded, including
Outbreak Extraction.exe and Outbreak Extraction_Data in Builds/PresentationValidation.
The user also confirmed the executable runs. Existing material/URP/MainMenu changes
are retained locally, separately from this repair.
