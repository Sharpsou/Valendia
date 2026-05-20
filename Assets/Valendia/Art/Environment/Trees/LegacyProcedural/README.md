# Legacy Procedural Trees

Les arbres actuels de Valendia ne sont pas des fichiers 3D importes. Ils sont generes par code dans :

- `Assets/Valendia/Scripts/Runtime/ValendiaLandscapeGenerator.Vegetation.cs`
- `Assets/Valendia/Scripts/Runtime/ValendiaLandscapeGenerator.Geometry.cs`
- `Assets/Valendia/Scripts/Runtime/ValendiaLandscapeGenerator.Meshes.cs`

Styles existants :

- broadleaf facette avec tronc polygonal et canopee en lobes ;
- conifer stylise avec cone/couronnes verticales ;
- variations de materiaux par biome : spring, warm, dark, autumn.

Le code est conserve comme reference et fallback via `generateLegacyProceduralTrees`.
Pour le chantier d'arbres authores Blender, ce flag est desactive afin que la scene Valendia puisse etre nettoyee des arbres legacy sans supprimer le systeme.
