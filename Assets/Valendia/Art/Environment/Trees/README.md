# Tree Art Pipeline

## Dossiers

- `SourceAssets/Valendia/Art/Environment/Trees/Blender/` : fichiers `.blend` editables, hors `Assets` pour eviter que Unity tente de les importer.
- `Exports/FBX/` : exports Unity importables.
- `Previews/` : rendus de validation avant integration.
- `LegacyProcedural/` : notes sur les arbres generes par code conserves en reference.

Le script `Tools/Blender/generate_oak_variations.py` regenere la source Blender,
les 5 exports FBX finaux et la preview de validation. Le fichier
`Tools/Blender/generate_tree_proposals.py` contient les helpers de construction.

## Regles

- Valider les silhouettes dans une preview avant integration au monde.
- Garder les sources Blender versionnees avec les exports.
- Exporter avec transforms appliques et une echelle coherente Unity.
- Integrer ensuite sous forme de prefabs ou d'une bibliotheque d'arbres, pas en mesh procedural direct.
