# Architecture Runtime Valendia

## Objectif

Le runtime garde un composant Unity principal, `ValendiaLandscapeGenerator`, pour conserver la compatibilite de scene et les references serialisees existantes.

La logique de generation est decoupee en fichiers `partial` par responsabilite. Ce choix evite une migration risquee vers plusieurs composants tout en rendant le code navigable et plus facile a modifier.

## Structure

- `ValendiaLandscapeGenerator.cs` : configuration serialisee, cycle Unity, orchestration de generation, API publique de sampling.
- `ValendiaLandscapeGenerator.Terrain.cs` : chunks de terrain, foothold terrain, sous-meshs de biomes.
- `ValendiaLandscapeGenerator.Composition.cs` : chemin, montagnes lointaines, bosquets auteurs, rubans floraux, nuages.
- `ValendiaLandscapeGenerator.Scattering.cs` : distribution proceduralisee des arbres, rochers, herbes, fleurs, scrub et vegetation de bordure.
- `ValendiaLandscapeGenerator.Sampling.cs` : hauteur, pente, chemin, biomes, couleurs de sol et points aleatoires.
- `ValendiaLandscapeGenerator.Vegetation.cs` : arbres, patches de prairie, batches d'herbe et vegetation basse.
- `ValendiaLandscapeGenerator.Geometry.cs` : rochers, canopees, nuages, helpers de rendu et LOD.
- `ValendiaLandscapeGenerator.Baking.cs` : combinaison des renderers statiques et nettoyage de hierarchie generee.
- `ValendiaLandscapeGenerator.Meshes.cs` : factories de meshes proceduraux et primitives de triangles.
- `ValendiaLandscapeGenerator.Materials.cs` : materiaux, textures de sol, atmosphere, skybox et lumiere.

## Regles De Modification

- Garder `Generate()` comme source de verite pour l'ordre de generation.
- Ne pas changer l'ordre des appels ou des seeds pendant un refactor purement architectural.
- Ajouter une nouvelle famille de generation dans un fichier partial dedie si elle a une responsabilite claire.
- Garder les champs serialises dans `ValendiaLandscapeGenerator.cs` sauf si une migration Unity explicite est planifiee.
- Compiler en batch apres tout deplacement de code, car Unity valide les partials et les `.meta` au refresh.
